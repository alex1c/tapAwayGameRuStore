using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TapAway.Core;
using UnityEditor;
using UnityEngine;

namespace TapAway.Editor
{
	/// <summary>
	/// Phase 5 campaign seed discovery: finds 30 accepted generator seeds
	/// along a comfortable mobile block-count curve (no 28+).
	/// </summary>
	public static class Phase5Tools
	{
		private const string OutputPath = "Logs/phase5-campaign-seeds.txt";

		/// <summary>Block counts for campaign levels 1..30 (comfortable mobile curve).</summary>
		private static readonly int[] CampaignBlockCounts =
		{
			6, 6, 6, // L1-3
			7, 7, // L4-5
			8, 8, // L6-7
			9, 9, 9, // L8-10
			10, 10, // L11-12
			12, 12, 12, // L13-15
			14, 14, 14, // L16-18
			15, 15, 15, // L19-21
			16, 16, 16, // L22-24
			18, 18, 18, // L25-27
			20, 20, 20 // L28-30
		};

		/// <summary>
		/// Preferred Phase 4 QA seeds reused when the target count matches.
		/// </summary>
		private static readonly Dictionary<int, int[]> PreferredSeedsByCount =
			new Dictionary<int, int[]>
			{
				{ 8, new[] { 41000 } },
				{ 10, new[] { 42000 } },
				{ 12, new[] { 1000 } },
				{ 15, new[] { 43000 } },
				{ 18, new[] { 44000 } }
			};

		/// <summary>Search start ranges per block count (after preferred seeds).</summary>
		private static readonly Dictionary<int, int> SearchStartByCount =
			new Dictionary<int, int>
			{
				{ 6, 50000 },
				{ 7, 51000 },
				{ 8, 52000 },
				{ 9, 53000 },
				{ 10, 54000 },
				{ 12, 55000 },
				{ 14, 56000 },
				{ 15, 57000 },
				{ 16, 58000 },
				{ 18, 59000 },
				{ 20, 60000 }
			};

		[MenuItem("TapAway/Phase5/Discover Campaign Seeds")]
		public static void DiscoverCampaignSeeds()
		{
			var exitCode = 0;
			try
			{
				var report = DiscoverInternal();
				Directory.CreateDirectory("Logs");
				File.WriteAllText(OutputPath, report);
				Debug.Log("[Phase5] Wrote " + OutputPath + "\n" + report);
			}
			catch (Exception ex)
			{
				exitCode = 1;
				Debug.LogError("[Phase5] Discovery failed: " + ex);
			}

			if (Application.isBatchMode)
			{
				EditorApplication.Exit(exitCode);
			}
		}

		/// <summary>Batchmode entry: -executeMethod TapAway.Editor.Phase5Tools.DiscoverCampaignSeedsBatch</summary>
		public static void DiscoverCampaignSeedsBatch()
		{
			DiscoverCampaignSeeds();
		}

		private static string DiscoverInternal()
		{
			var needByCount = new Dictionary<int, int>();
			for (var i = 0; i < CampaignBlockCounts.Length; i++)
			{
				var c = CampaignBlockCounts[i];
				needByCount[c] = needByCount.TryGetValue(c, out var n) ? n + 1 : 1;
			}

			// Collect accepted seeds per count (preferred first, then search).
			var seedsByCount = new Dictionary<int, List<int>>();
			foreach (var pair in needByCount)
			{
				var count = pair.Key;
				var need = pair.Value;
				var found = new List<int>(need);
				var used = new HashSet<int>();

				if (PreferredSeedsByCount.TryGetValue(count, out var preferred))
				{
					for (var i = 0; i < preferred.Length && found.Count < need; i++)
					{
						var seed = preferred[i];
						if (used.Contains(seed))
						{
							continue;
						}

						if (TryAccept(seed, count, out _))
						{
							found.Add(seed);
							used.Add(seed);
							Debug.Log("[Phase5] preferred count=" + count + " seed=" + seed);
						}
						else
						{
							Debug.LogWarning(
								"[Phase5] preferred seed " + seed +
								" rejected for count=" + count);
						}
					}
				}

				var start = SearchStartByCount.TryGetValue(count, out var s) ? s : 50000 + count * 100;
				// Larger puzzles need a wider search window.
				var maxTries = count >= 16 ? 4000 : 2000;
				var cursor = start;
				var tries = 0;
				while (found.Count < need && tries < maxTries)
				{
					var seed = cursor;
					cursor++;
					tries++;
					if (used.Contains(seed))
					{
						continue;
					}

					if (TryAccept(seed, count, out _))
					{
						found.Add(seed);
						used.Add(seed);
						Debug.Log(
							"[Phase5] found count=" + count +
							" seed=" + seed +
							" (" + found.Count + "/" + need + ")");
					}

					if (tries % 200 == 0)
					{
						Debug.Log(
							"[Phase5] searching count=" + count +
							" tries=" + tries +
							" found=" + found.Count + "/" + need);
					}
				}

				if (found.Count < need)
				{
					throw new InvalidOperationException(
						"Only found " + found.Count + "/" + need +
						" seeds for blockCount=" + count +
						" after " + tries + " tries from " + start);
				}

				seedsByCount[count] = found;
			}

			// Assign seeds to levels 1..30 in curve order (consume per-count lists).
			var nextIndexByCount = new Dictionary<int, int>();
			var sb = new StringBuilder();
			sb.AppendLine("generator=" + GeneratorConfig.GeneratorVersion);
			sb.AppendLine("levels=" + CampaignBlockCounts.Length);
			sb.AppendLine(
				"# Index\tSeed\tBlocks\tSpanX\tSpanY\tSpanZ\tDensity\tInitLegal\tDiffScore\tDiffBand\tRadiusProxy\tSolvable");
			sb.AppendLine("// C#-ready rows:");

			for (var level = 0; level < CampaignBlockCounts.Length; level++)
			{
				var blocks = CampaignBlockCounts[level];
				if (!nextIndexByCount.TryGetValue(blocks, out var idx))
				{
					idx = 0;
				}

				var seed = seedsByCount[blocks][idx];
				nextIndexByCount[blocks] = idx + 1;

				if (!TryAccept(seed, blocks, out var result) || result.Level == null)
				{
					throw new InvalidOperationException(
						"Seed " + seed + " failed verification for level " + (level + 1));
				}

				var levelData = result.Level;
				var f = levelData.Difficulty.Features;
				var q = levelData.Quality;
				// Half-span extents → sphere radius proxy (cell units, matches framing math).
				var radiusProxy = CameraFramingMath.RadiusFromExtents(
					q.SpanX * 0.5f,
					q.SpanY * 0.5f,
					q.SpanZ * 0.5f);

				var inv = CultureInfo.InvariantCulture;
				sb.AppendLine(string.Format(
					inv,
					"// L{0}: seed={1}, blocks={2}, span={3}x{4}x{5}, dens={6:0.000}, legal={7}, diff={8:0.00}/{9}, radius={10:0.00}",
					level + 1,
					seed,
					blocks,
					q.SpanX,
					q.SpanY,
					q.SpanZ,
					q.Density,
					f.InitialLegalMoves,
					levelData.Difficulty.Score,
					levelData.Difficulty.Band,
					radiusProxy));

				sb.AppendLine(string.Format(
					inv,
					"{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6:0.000}\t{7}\t{8:0.00}\t{9}\t{10:0.00}\ttrue",
					level + 1,
					seed,
					blocks,
					q.SpanX,
					q.SpanY,
					q.SpanZ,
					q.Density,
					f.InitialLegalMoves,
					levelData.Difficulty.Score,
					levelData.Difficulty.Band,
					radiusProxy));

				sb.AppendLine(string.Format(
					inv,
					"\tnew CampaignLevelEntry({0}, {1}, {2}), // dens={3:0.00} legal={4} {5:0.0}/{6} r={7:0.00}",
					level + 1,
					seed,
					blocks,
					q.Density,
					f.InitialLegalMoves,
					levelData.Difficulty.Score,
					levelData.Difficulty.Band,
					radiusProxy));
			}

			return sb.ToString();
		}

		private static bool TryAccept(int seed, int blockCount, out GenerationResult result)
		{
			var config = Phase4QaLevelSet.ConfigForCount(blockCount);
			result = GenerationPipeline.Generate(seed, config);
			return result.Accepted &&
			       result.Level != null &&
			       result.Level.Blocks.Count == blockCount &&
			       result.Level.Solver != null &&
			       result.Level.Solver.IsSolvable;
		}
	}
}
