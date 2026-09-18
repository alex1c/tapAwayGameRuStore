using System.IO;
using System.Text;
using TapAway.Core;
using TapAway.Runtime;
using UnityEditor;
using UnityEngine;

namespace TapAway.Editor
{
	/// <summary>Phase 4 menus: QA seed discovery, preview, direction view cycling.</summary>
	public static class Phase4Tools
	{
		private static int _previewQaIndex;
		private static int _viewIndex;

		[MenuItem("TapAway/Phase4/Discover QA Seeds")]
		public static void DiscoverQaSeeds()
		{
			var counts = new[] { 8, 10, 12, 15, 18, 22, 28 };
			var starts = new[] { 41000, 42000, 1000, 43000, 44000, 45000, 11000 };
			var sb = new StringBuilder();
			sb.AppendLine("generator=" + GeneratorConfig.GeneratorVersion);
			for (var i = 0; i < counts.Length; i++)
			{
				var count = counts[i];
				var seed = Phase4QaLevelSet.FindAcceptedSeed(count, starts[i], 400);
				sb.AppendLine("QA-" + (i + 1) + " count=" + count + " seed=" + seed);
				Debug.Log("[Phase4] count=" + count + " seed=" + seed);
			}

			Directory.CreateDirectory("Logs");
			File.WriteAllText("Logs/phase4-qa-seeds.txt", sb.ToString());
			Debug.Log("[Phase4] Wrote Logs/phase4-qa-seeds.txt\n" + sb);
			if (Application.isBatchMode)
			{
				EditorApplication.Exit(0);
			}
		}

		public static void DiscoverQaSeedsBatch()
		{
			DiscoverQaSeeds();
		}

		[MenuItem("TapAway/Phase4/Preview QA Level")]
		public static void PreviewQaLevel()
		{
			if (!EditorApplication.isPlaying)
			{
				EditorUtility.DisplayDialog("Tap Away", "Enter Play Mode first.", "OK");
				return;
			}

			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			if (bootstrap == null)
			{
				Debug.LogError("[Phase4] No LevelBootstrap");
				return;
			}

			bootstrap.LoadQaIndex(_previewQaIndex);
			Debug.Log("[Phase4] Preview QA index " + _previewQaIndex);
		}

		[MenuItem("TapAway/Phase4/Next QA Level")]
		public static void NextQaLevel()
		{
			_previewQaIndex = (_previewQaIndex + 1) % Phase4QaLevelSet.Count;
			PreviewQaLevel();
		}

		[MenuItem("TapAway/Phase4/Direction Readability Views")]
		public static void CycleDirectionViews()
		{
			if (!EditorApplication.isPlaying)
			{
				EditorUtility.DisplayDialog("Tap Away", "Enter Play Mode first.", "OK");
				return;
			}

			var orbit = Object.FindFirstObjectByType<PuzzleOrbitCamera>();
			if (orbit == null)
			{
				return;
			}

			var dirs = DirectionReadabilityValidator.RepresentativeViewDirections;
			_viewIndex = (_viewIndex + 1) % dirs.Length;
			var look = dirs[_viewIndex];
			// Convert look-toward-puzzle into yaw/pitch around current pivot.
			var yaw = Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg;
			var pitch = -Mathf.Asin(Mathf.Clamp(look.y, -1f, 1f)) * Mathf.Rad2Deg;
			orbit.CaptureInitialAngles();
			// Apply via drag approximation: set by framing reset then drag.
			orbit.ResetToFramedView();
			orbit.ApplyDrag(new Vector2(yaw * 2f, -pitch * 2f));
			Debug.Log("[Phase4] Direction view " + _viewIndex + " look=" + look);
		}
	}
}
