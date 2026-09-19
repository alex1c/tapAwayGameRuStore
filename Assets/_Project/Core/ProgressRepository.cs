using System;

namespace TapAway.Core
{
	/// <summary>
	/// Abstract file IO for campaign progress — Runtime supplies persistentDataPath,
	/// EditMode tests supply an in-memory map.
	/// </summary>
	public interface IProgressStorage
	{
		bool Exists(string fileName);
		string ReadAllText(string fileName);
		void WriteAllText(string fileName, string contents);
		void Delete(string fileName);
		/// <summary>Atomically replace destination with source when the platform allows.</summary>
		void Replace(string sourceFileName, string destinationFileName);
	}

	/// <summary>Result of a load attempt including recovery path taken.</summary>
	public sealed class ProgressLoadResult
	{
		public CampaignProgress Progress { get; set; }
		public bool UsedBackup { get; set; }
		public bool UsedFreshDefault { get; set; }
		public string Diagnostic { get; set; }
	}

	/// <summary>Result of a save attempt.</summary>
	public sealed class ProgressSaveResult
	{
		public bool Success { get; set; }
		public string Diagnostic { get; set; }
	}

	/// <summary>
	/// Atomic campaign progress repository with primary + backup recovery
	/// and schema migration dispatch foundation.
	/// </summary>
	public sealed class ProgressRepository
	{
		public const string PrimaryFileName = "campaign_progress.json";
		public const string BackupFileName = "campaign_progress.bak.json";
		public const string TempFileName = "campaign_progress.tmp.json";

		private readonly IProgressStorage _storage;

		public ProgressRepository(IProgressStorage storage)
		{
			_storage = storage ?? throw new ArgumentNullException(nameof(storage));
		}

		public ProgressLoadResult Load()
		{
			var result = new ProgressLoadResult();

			if (TryLoadFile(PrimaryFileName, out var primary, out var primaryError))
			{
				result.Progress = ProgressMigration.MigrateToCurrent(primary);
				result.Diagnostic = "primary";
				return result;
			}

			if (TryLoadFile(BackupFileName, out var backup, out var backupError))
			{
				result.Progress = ProgressMigration.MigrateToCurrent(backup);
				result.UsedBackup = true;
				result.Diagnostic = "backup after primary fail: " + primaryError;
				// Best-effort restore primary from backup.
				try
				{
					Save(result.Progress);
				}
				catch
				{
					// ignore restore failure — progress object is still valid in memory
				}

				return result;
			}

			result.Progress = CampaignProgress.CreateFresh();
			result.UsedFreshDefault = true;
			result.Diagnostic = "fresh default; primary=" + primaryError + "; backup=" + backupError;
			return result;
		}

		public ProgressSaveResult Save(CampaignProgress progress)
		{
			var result = new ProgressSaveResult();
			if (progress == null)
			{
				result.Success = false;
				result.Diagnostic = "null progress";
				return result;
			}

			try
			{
				progress.Sanitize();
				var json = CampaignProgressJson.Serialize(progress);

				// Round-trip validate before replacing primary.
				if (!CampaignProgressJson.TryDeserialize(json, out _, out var validateError))
				{
					result.Success = false;
					result.Diagnostic = "serialize validate failed: " + validateError;
					return result;
				}

				_storage.WriteAllText(TempFileName, json);

				// Promote previous primary to backup when present.
				if (_storage.Exists(PrimaryFileName))
				{
					try
					{
						var existing = _storage.ReadAllText(PrimaryFileName);
						_storage.WriteAllText(BackupFileName, existing);
					}
					catch
					{
						// Backup promotion failure is non-fatal if temp is valid.
					}
				}

				_storage.Replace(TempFileName, PrimaryFileName);

				// Verify primary readable.
				if (!TryLoadFile(PrimaryFileName, out _, out var verifyError))
				{
					result.Success = false;
					result.Diagnostic = "post-write verify failed: " + verifyError;
					return result;
				}

				result.Success = true;
				result.Diagnostic = "ok";
				return result;
			}
			catch (Exception ex)
			{
				result.Success = false;
				result.Diagnostic = ex.Message;
				return result;
			}
		}

		private bool TryLoadFile(string name, out CampaignProgress progress, out string error)
		{
			progress = null;
			error = null;
			try
			{
				if (!_storage.Exists(name))
				{
					error = name + " missing";
					return false;
				}

				var json = _storage.ReadAllText(name);
				if (!CampaignProgressJson.TryDeserialize(json, out progress, out error))
				{
					return false;
				}

				return true;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				progress = null;
				return false;
			}
		}
	}

	/// <summary>
	/// Schema migration dispatcher. v1 is current — hooks ready for v2+.
	/// </summary>
	public static class ProgressMigration
	{
		public static CampaignProgress MigrateToCurrent(CampaignProgress progress)
		{
			if (progress == null)
			{
				return CampaignProgress.CreateFresh();
			}

			var version = progress.SchemaVersion;
			if (version <= 0)
			{
				version = 1;
				progress.SchemaVersion = 1;
			}

			// Future: while (version < Current) { progress = MigrateV{n}toV{n+1}(progress); version++; }
			if (version > CampaignProgress.CurrentSchemaVersion)
			{
				// Newer than this build — keep what we understand, clamp version for safety.
				progress.SchemaVersion = CampaignProgress.CurrentSchemaVersion;
			}
			else
			{
				progress.SchemaVersion = CampaignProgress.CurrentSchemaVersion;
			}

			progress.Sanitize();
			return progress;
		}
	}

	/// <summary>In-memory storage for EditMode persistence tests.</summary>
	public sealed class MemoryProgressStorage : IProgressStorage
	{
		private readonly System.Collections.Generic.Dictionary<string, string> _files =
			new System.Collections.Generic.Dictionary<string, string>();

		public bool Exists(string fileName)
		{
			return _files.ContainsKey(fileName);
		}

		public string ReadAllText(string fileName)
		{
			return _files[fileName];
		}

		public void WriteAllText(string fileName, string contents)
		{
			_files[fileName] = contents ?? string.Empty;
		}

		public void Delete(string fileName)
		{
			_files.Remove(fileName);
		}

		public void Replace(string sourceFileName, string destinationFileName)
		{
			if (!_files.TryGetValue(sourceFileName, out var contents))
			{
				throw new InvalidOperationException("missing source " + sourceFileName);
			}

			_files[destinationFileName] = contents;
			_files.Remove(sourceFileName);
		}

		/// <summary>Test helper: write corrupt bytes to a named file.</summary>
		public void Corrupt(string fileName, string junk)
		{
			_files[fileName] = junk;
		}
	}
}
