using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TapAway.Core
{
	/// <summary>Per-level best metrics stored in campaign progress.</summary>
	public sealed class LevelProgressRecord
	{
		public bool Completed { get; set; }
		public float BestTimeSeconds { get; set; } = float.MaxValue;
		public int BestBlockedTaps { get; set; } = int.MaxValue;
		public int BestOrbitGestures { get; set; } = int.MaxValue;

		public void ApplyRun(float timeSeconds, int blockedTaps, int orbitGestures)
		{
			Completed = true;
			if (timeSeconds < BestTimeSeconds)
			{
				BestTimeSeconds = timeSeconds;
			}

			if (blockedTaps < BestBlockedTaps)
			{
				BestBlockedTaps = blockedTaps;
			}

			if (orbitGestures < BestOrbitGestures)
			{
				BestOrbitGestures = orbitGestures;
			}
		}
	}

	/// <summary>
	/// Local campaign progress (schema v1). Persisted as versioned JSON.
	/// Mid-level puzzle state is NOT resumed — only campaign unlocks/metrics.
	/// </summary>
	public sealed class CampaignProgress
	{
		public const int CurrentSchemaVersion = 1;

		public int SchemaVersion { get; set; } = CurrentSchemaVersion;
		public bool TutorialCompleted { get; set; }
		public int HighestUnlockedLevel { get; set; } = 1;
		public int LastPlayedLevel { get; set; } = 1;
		public Dictionary<int, LevelProgressRecord> Levels { get; set; } =
			new Dictionary<int, LevelProgressRecord>();

		public static CampaignProgress CreateFresh()
		{
			return new CampaignProgress
			{
				SchemaVersion = CurrentSchemaVersion,
				TutorialCompleted = false,
				HighestUnlockedLevel = 1,
				LastPlayedLevel = 1,
				Levels = new Dictionary<int, LevelProgressRecord>()
			};
		}

		public bool IsUnlocked(int levelIndex1Based)
		{
			return levelIndex1Based >= 1 &&
			       levelIndex1Based <= CampaignDefinition.LevelCount &&
			       levelIndex1Based <= HighestUnlockedLevel;
		}

		public bool IsCompleted(int levelIndex1Based)
		{
			return Levels != null &&
			       Levels.TryGetValue(levelIndex1Based, out var record) &&
			       record.Completed;
		}

		public LevelProgressRecord GetOrCreate(int levelIndex1Based)
		{
			if (Levels == null)
			{
				Levels = new Dictionary<int, LevelProgressRecord>();
			}

			if (!Levels.TryGetValue(levelIndex1Based, out var record))
			{
				record = new LevelProgressRecord();
				Levels[levelIndex1Based] = record;
			}

			return record;
		}

		/// <summary>
		/// Marks tutorial done. Does not change campaign unlocks.
		/// </summary>
		public void MarkTutorialCompleted()
		{
			TutorialCompleted = true;
		}

		/// <summary>
		/// Records a successful campaign clear and unlocks the next level.
		/// </summary>
		public void MarkLevelCompleted(
			int levelIndex1Based,
			float timeSeconds,
			int blockedTaps,
			int orbitGestures)
		{
			if (levelIndex1Based < 1 || levelIndex1Based > CampaignDefinition.LevelCount)
			{
				return;
			}

			var record = GetOrCreate(levelIndex1Based);
			record.ApplyRun(timeSeconds, blockedTaps, orbitGestures);
			LastPlayedLevel = levelIndex1Based;

			if (levelIndex1Based >= HighestUnlockedLevel &&
			    levelIndex1Based < CampaignDefinition.LevelCount)
			{
				HighestUnlockedLevel = levelIndex1Based + 1;
			}
			else if (levelIndex1Based > HighestUnlockedLevel)
			{
				// Should not happen with unlock rules; clamp safely.
				HighestUnlockedLevel = Math.Min(
					CampaignDefinition.LevelCount,
					Math.Max(HighestUnlockedLevel, levelIndex1Based));
			}
		}

		public void SetLastPlayed(int levelIndex1Based)
		{
			if (levelIndex1Based >= 1 && levelIndex1Based <= CampaignDefinition.LevelCount)
			{
				LastPlayedLevel = levelIndex1Based;
			}
		}

		/// <summary>
		/// Continue target: last played if unlocked, else highest unlocked.
		/// Fresh puzzles restart — no mid-level resume in Phase 5.
		/// </summary>
		public int ResolveContinueLevel()
		{
			if (IsUnlocked(LastPlayedLevel))
			{
				return LastPlayedLevel;
			}

			return Math.Max(1, Math.Min(HighestUnlockedLevel, CampaignDefinition.LevelCount));
		}

		/// <summary>Sanitizes ranges after load / migration.</summary>
		public void Sanitize()
		{
			if (SchemaVersion <= 0)
			{
				SchemaVersion = CurrentSchemaVersion;
			}

			if (HighestUnlockedLevel < 1)
			{
				HighestUnlockedLevel = 1;
			}

			if (HighestUnlockedLevel > CampaignDefinition.LevelCount)
			{
				HighestUnlockedLevel = CampaignDefinition.LevelCount;
			}

			if (LastPlayedLevel < 1)
			{
				LastPlayedLevel = 1;
			}

			if (LastPlayedLevel > CampaignDefinition.LevelCount)
			{
				LastPlayedLevel = CampaignDefinition.LevelCount;
			}

			if (Levels == null)
			{
				Levels = new Dictionary<int, LevelProgressRecord>();
			}

			var keys = new List<int>(Levels.Keys);
			foreach (var key in keys)
			{
				if (key < 1 || key > CampaignDefinition.LevelCount)
				{
					Levels.Remove(key);
					continue;
				}

				var record = Levels[key];
				if (record == null)
				{
					Levels.Remove(key);
					continue;
				}

				if (record.BestTimeSeconds < 0f)
				{
					record.BestTimeSeconds = float.MaxValue;
				}

				if (record.BestBlockedTaps < 0)
				{
					record.BestBlockedTaps = int.MaxValue;
				}

				if (record.BestOrbitGestures < 0)
				{
					record.BestOrbitGestures = int.MaxValue;
				}
			}
		}
	}

	/// <summary>Manual versioned JSON for campaign progress (no external deps).</summary>
	public static class CampaignProgressJson
	{
		public static string Serialize(CampaignProgress progress)
		{
			if (progress == null)
			{
				progress = CampaignProgress.CreateFresh();
			}

			progress.Sanitize();
			var sb = new StringBuilder(512);
			sb.Append("{\"schemaVersion\":");
			sb.Append(progress.SchemaVersion);
			sb.Append(",\"tutorialCompleted\":");
			sb.Append(progress.TutorialCompleted ? "true" : "false");
			sb.Append(",\"highestUnlockedLevel\":");
			sb.Append(progress.HighestUnlockedLevel);
			sb.Append(",\"lastPlayedLevel\":");
			sb.Append(progress.LastPlayedLevel);
			sb.Append(",\"levels\":{");
			var first = true;
			if (progress.Levels != null)
			{
				foreach (var pair in progress.Levels)
				{
					if (!first)
					{
						sb.Append(',');
					}

					first = false;
					var r = pair.Value;
					sb.Append('"').Append(pair.Key).Append("\":{");
					sb.Append("\"completed\":").Append(r.Completed ? "true" : "false");
					sb.Append(",\"bestTimeSeconds\":");
					AppendFloat(sb, r.BestTimeSeconds);
					sb.Append(",\"bestBlockedTaps\":").Append(SanitizeInt(r.BestBlockedTaps));
					sb.Append(",\"bestOrbitGestures\":").Append(SanitizeInt(r.BestOrbitGestures));
					sb.Append('}');
				}
			}

			sb.Append("}}");
			return sb.ToString();
		}

		public static bool TryDeserialize(string json, out CampaignProgress progress, out string error)
		{
			progress = null;
			error = null;
			if (string.IsNullOrEmpty(json))
			{
				error = "empty";
				return false;
			}

			try
			{
				var root = ParseObject(json);
				if (root == null)
				{
					error = "not an object";
					return false;
				}

				var result = CampaignProgress.CreateFresh();
				if (root.TryGetValue("schemaVersion", out var schemaTok))
				{
					result.SchemaVersion = ParseInt(schemaTok, CampaignProgress.CurrentSchemaVersion);
				}

				if (root.TryGetValue("tutorialCompleted", out var tutTok))
				{
					result.TutorialCompleted = ParseBool(tutTok);
				}

				if (root.TryGetValue("highestUnlockedLevel", out var hiTok))
				{
					result.HighestUnlockedLevel = ParseInt(hiTok, 1);
				}

				if (root.TryGetValue("lastPlayedLevel", out var lastTok))
				{
					result.LastPlayedLevel = ParseInt(lastTok, 1);
				}

				if (root.TryGetValue("levels", out var levelsTok) && levelsTok is Dictionary<string, object> levelsObj)
				{
					foreach (var pair in levelsObj)
					{
						if (!int.TryParse(pair.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var levelId))
						{
							continue;
						}

						if (!(pair.Value is Dictionary<string, object> recObj))
						{
							continue;
						}

						var record = new LevelProgressRecord();
						if (recObj.TryGetValue("completed", out var c))
						{
							record.Completed = ParseBool(c);
						}

						if (recObj.TryGetValue("bestTimeSeconds", out var t))
						{
							record.BestTimeSeconds = ParseFloat(t, float.MaxValue);
						}

						if (recObj.TryGetValue("bestBlockedTaps", out var b))
						{
							record.BestBlockedTaps = ParseInt(b, int.MaxValue);
						}

						if (recObj.TryGetValue("bestOrbitGestures", out var o))
						{
							record.BestOrbitGestures = ParseInt(o, int.MaxValue);
						}

						result.Levels[levelId] = record;
					}
				}

				result.Sanitize();
				progress = result;
				return true;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				progress = null;
				return false;
			}
		}

		private static void AppendFloat(StringBuilder sb, float value)
		{
			if (float.IsInfinity(value) || float.IsNaN(value) || value >= 1e20f)
			{
				sb.Append("null");
				return;
			}

			sb.Append(value.ToString("0.###", CultureInfo.InvariantCulture));
		}

		private static int SanitizeInt(int value)
		{
			return value == int.MaxValue ? -1 : value;
		}

		private static int ParseInt(object token, int fallback)
		{
			if (token == null)
			{
				return fallback;
			}

			if (token is long l)
			{
				if (l < 0)
				{
					return fallback == int.MaxValue ? int.MaxValue : fallback;
				}

				return (int)Math.Min(int.MaxValue, l);
			}

			if (token is double d)
			{
				return (int)d;
			}

			if (int.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
			{
				return v < 0 && fallback == int.MaxValue ? int.MaxValue : v;
			}

			return fallback;
		}

		private static float ParseFloat(object token, float fallback)
		{
			if (token == null)
			{
				return fallback;
			}

			if (token is double d)
			{
				return (float)d;
			}

			if (float.TryParse(token.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
			{
				return v;
			}

			return fallback;
		}

		private static bool ParseBool(object token)
		{
			if (token is bool b)
			{
				return b;
			}

			return string.Equals(token != null ? token.ToString() : null, "true", StringComparison.OrdinalIgnoreCase);
		}

		// Minimal JSON object/array parser sufficient for our schema.
		private static Dictionary<string, object> ParseObject(string json)
		{
			var i = 0;
			SkipWs(json, ref i);
			if (i >= json.Length || json[i] != '{')
			{
				return null;
			}

			return ParseObjectBody(json, ref i);
		}

		private static Dictionary<string, object> ParseObjectBody(string json, ref int i)
		{
			var dict = new Dictionary<string, object>();
			i++; // {
			SkipWs(json, ref i);
			if (i < json.Length && json[i] == '}')
			{
				i++;
				return dict;
			}

			while (i < json.Length)
			{
				SkipWs(json, ref i);
				var key = ParseString(json, ref i);
				SkipWs(json, ref i);
				if (i >= json.Length || json[i] != ':')
				{
					throw new FormatException("expected ':'");
				}

				i++;
				SkipWs(json, ref i);
				var value = ParseValue(json, ref i);
				dict[key] = value;
				SkipWs(json, ref i);
				if (i < json.Length && json[i] == ',')
				{
					i++;
					continue;
				}

				if (i < json.Length && json[i] == '}')
				{
					i++;
					return dict;
				}

				throw new FormatException("expected ',' or '}'");
			}

			throw new FormatException("unterminated object");
		}

		private static object ParseValue(string json, ref int i)
		{
			SkipWs(json, ref i);
			if (i >= json.Length)
			{
				throw new FormatException("unexpected end");
			}

			var c = json[i];
			if (c == '{')
			{
				return ParseObjectBody(json, ref i);
			}

			if (c == '"')
			{
				return ParseString(json, ref i);
			}

			if (c == 't' && MatchLiteral(json, ref i, "true"))
			{
				return true;
			}

			if (c == 'f' && MatchLiteral(json, ref i, "false"))
			{
				return false;
			}

			if (c == 'n' && MatchLiteral(json, ref i, "null"))
			{
				return null;
			}

			return ParseNumber(json, ref i);
		}

		private static string ParseString(string json, ref int i)
		{
			if (json[i] != '"')
			{
				throw new FormatException("expected string");
			}

			i++;
			var sb = new StringBuilder();
			while (i < json.Length)
			{
				var c = json[i++];
				if (c == '"')
				{
					return sb.ToString();
				}

				if (c == '\\' && i < json.Length)
				{
					sb.Append(json[i++]);
					continue;
				}

				sb.Append(c);
			}

			throw new FormatException("unterminated string");
		}

		private static object ParseNumber(string json, ref int i)
		{
			var start = i;
			if (json[i] == '-')
			{
				i++;
			}

			while (i < json.Length && char.IsDigit(json[i]))
			{
				i++;
			}

			var isFloat = false;
			if (i < json.Length && json[i] == '.')
			{
				isFloat = true;
				i++;
				while (i < json.Length && char.IsDigit(json[i]))
				{
					i++;
				}
			}

			var slice = json.Substring(start, i - start);
			if (isFloat)
			{
				return double.Parse(slice, CultureInfo.InvariantCulture);
			}

			return long.Parse(slice, CultureInfo.InvariantCulture);
		}

		private static bool MatchLiteral(string json, ref int i, string literal)
		{
			if (i + literal.Length > json.Length)
			{
				return false;
			}

			for (var k = 0; k < literal.Length; k++)
			{
				if (json[i + k] != literal[k])
				{
					return false;
				}
			}

			i += literal.Length;
			return true;
		}

		private static void SkipWs(string json, ref int i)
		{
			while (i < json.Length && char.IsWhiteSpace(json[i]))
			{
				i++;
			}
		}
	}
}
