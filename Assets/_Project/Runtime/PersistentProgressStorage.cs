using System.IO;
using TapAway.Core;
using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Unity persistentDataPath-backed progress storage with atomic replace.
	/// </summary>
	public sealed class PersistentProgressStorage : IProgressStorage
	{
		private readonly string _directory;

		public PersistentProgressStorage(string directory = null)
		{
			_directory = string.IsNullOrEmpty(directory)
				? Path.Combine(Application.persistentDataPath, "TapAway")
				: directory;
			Directory.CreateDirectory(_directory);
		}

		public string DirectoryPath => _directory;

		public bool Exists(string fileName)
		{
			return File.Exists(PathFor(fileName));
		}

		public string ReadAllText(string fileName)
		{
			return File.ReadAllText(PathFor(fileName));
		}

		public void WriteAllText(string fileName, string contents)
		{
			var path = PathFor(fileName);
			Directory.CreateDirectory(_directory);
			File.WriteAllText(path, contents ?? string.Empty);
		}

		public void Delete(string fileName)
		{
			var path = PathFor(fileName);
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}

		public void Replace(string sourceFileName, string destinationFileName)
		{
			var source = PathFor(sourceFileName);
			var destination = PathFor(destinationFileName);
			if (!File.Exists(source))
			{
				throw new FileNotFoundException("temp progress missing", source);
			}

			if (File.Exists(destination))
			{
				File.Delete(destination);
			}

			File.Move(source, destination);
		}

		private string PathFor(string fileName)
		{
			return Path.Combine(_directory, fileName);
		}
	}
}
