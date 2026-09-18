using System.IO;
using TapAway.Core;
using TapAway.Runtime;
using UnityEditor;
using UnityEngine;

namespace TapAway.Editor
{
	/// <summary>
	/// Phase 3 development menus: stress harness + generated level preview.
	/// </summary>
	public static class Phase3Tools
	{
		private const string StressReportPath = "Logs/phase3-stress-report.txt";

		[MenuItem("TapAway/Phase3/Run Generation Stress Suite")]
		public static void RunStressSuite()
		{
			var report = GenerationStressHarness.RunDefaultSuite(1000);
			Directory.CreateDirectory("Logs");
			File.WriteAllText(StressReportPath, report.Summary);
			Debug.Log("[Phase3Stress]\n" + report.Summary);
			if (Application.isBatchMode)
			{
				EditorApplication.Exit(report.Accepted > 0 && report.ReproducibilityOk ? 0 : 1);
			}
		}

		/// <summary>Batchmode entry point.</summary>
		public static void RunStressSuiteBatch()
		{
			RunStressSuite();
		}

		[MenuItem("TapAway/Phase3/Preview Generated Seed")]
		public static void PreviewGeneratedSeed()
		{
			const int seed = 4242;
			if (!EditorApplication.isPlaying)
			{
				EditorUtility.DisplayDialog(
					"Tap Away",
					"Enter Play Mode, then run TapAway/Phase3/Preview Generated Seed again.\n" +
					"Default seed: " + seed,
					"OK");
				return;
			}

			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			if (bootstrap == null)
			{
				Debug.LogError("[Phase3] No LevelBootstrap in the loaded scene.");
				return;
			}

			if (bootstrap.TryLoadGeneratedSeed(seed, GeneratorConfig.Small()))
			{
				Debug.Log("[Phase3] Loaded generated seed " + seed);
			}
		}

		[MenuItem("TapAway/Phase3/Load Prototype Level")]
		public static void LoadPrototype()
		{
			if (!EditorApplication.isPlaying)
			{
				return;
			}

			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			bootstrap?.LoadPrototype();
		}
	}
}
