using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace TapAway.Core
{
	/// <summary>
	/// Pure-C# stress harness for mass generation without Unity presentation.
	/// </summary>
	public static class GenerationStressHarness
	{
		public sealed class StressReport
		{
			public int Attempted { get; set; }
			public int Accepted { get; set; }
			public int Rejected { get; set; }
			public Dictionary<string, int> RejectionCounts { get; set; } =
				new Dictionary<string, int>();
			public Dictionary<string, int> DifficultyCounts { get; set; } =
				new Dictionary<string, int>();
			public long ElapsedMilliseconds { get; set; }
			public long TotalSolverStates { get; set; }
			public int MaxSolverStates { get; set; }
			public int AcceptedOver64 { get; set; }
			public List<int> ExampleSeedsByBand { get; set; } = new List<int>();
			public bool ReproducibilityOk { get; set; }
			public string Summary { get; set; }
		}

		/// <summary>
		/// Runs a deterministic multi-bucket generation sample.
		/// </summary>
		public static StressReport RunDefaultSuite(int baseSeed = 1000)
		{
			var report = new StressReport();
			var sw = Stopwatch.StartNew();
			var bandExamples = new Dictionary<DifficultyBand, int>();
			var bandExampleConfigs = new Dictionary<DifficultyBand, GeneratorConfig>();

			RunBucket(report, baseSeed + 40000, 100, GeneratorConfig.Tiny(), bandExamples, bandExampleConfigs);
			RunBucket(report, baseSeed + 0, 350, GeneratorConfig.Small(), bandExamples, bandExampleConfigs);
			RunBucket(report, baseSeed + 10000, 300, GeneratorConfig.Medium(), bandExamples, bandExampleConfigs);
			RunBucket(report, baseSeed + 20000, 200, GeneratorConfig.Large(), bandExamples, bandExampleConfigs);
			RunBucket(report, baseSeed + 30000, 50, GeneratorConfig.StressOver64(), bandExamples, bandExampleConfigs);

			// Reproducibility: re-generate the first accepted example in each band.
			report.ReproducibilityOk = true;
			if (bandExamples.Count > 0)
			{
				foreach (var pair in bandExamples)
				{
					report.ExampleSeedsByBand.Add(pair.Value);
					var config = bandExampleConfigs[pair.Key];
					var a = GenerationPipeline.Generate(pair.Value, config);
					var b = GenerationPipeline.Generate(pair.Value, config);
					if (!a.Accepted || !b.Accepted || a.Level.Blocks.Count != b.Level.Blocks.Count)
					{
						report.ReproducibilityOk = false;
						break;
					}

					for (var i = 0; i < a.Level.Blocks.Count; i++)
					{
						if (a.Level.Blocks[i].Position.Equals(b.Level.Blocks[i].Position) &&
						    a.Level.Blocks[i].EscapeDirection == b.Level.Blocks[i].EscapeDirection &&
						    a.Level.Blocks[i].Id.Value == b.Level.Blocks[i].Id.Value)
						{
							continue;
						}

						report.ReproducibilityOk = false;
						break;
					}

					if (!report.ReproducibilityOk)
					{
						break;
					}
				}
			}

			sw.Stop();
			report.ElapsedMilliseconds = sw.ElapsedMilliseconds;
			report.Summary = BuildSummary(report, bandExamples);
			return report;
		}

		private static void RunBucket(
			StressReport report,
			int seedStart,
			int count,
			GeneratorConfig config,
			Dictionary<DifficultyBand, int> bandExamples,
			Dictionary<DifficultyBand, GeneratorConfig> bandExampleConfigs)
		{
			for (var i = 0; i < count; i++)
			{
				var seed = seedStart + i;
				var result = GenerationPipeline.Generate(seed, config);
				report.Attempted++;
				if (result.Accepted)
				{
					report.Accepted++;
					var states = result.Level.Solver.ExploredStates;
					report.TotalSolverStates += states;
					if (states > report.MaxSolverStates)
					{
						report.MaxSolverStates = states;
					}

					if (result.Level.Blocks.Count > 64)
					{
						report.AcceptedOver64++;
					}

					var band = result.Level.Difficulty.Band.ToString();
					if (!report.DifficultyCounts.ContainsKey(band))
					{
						report.DifficultyCounts[band] = 0;
					}

					report.DifficultyCounts[band]++;
					if (!bandExamples.ContainsKey(result.Level.Difficulty.Band))
					{
						bandExamples[result.Level.Difficulty.Band] = seed;
						bandExampleConfigs[result.Level.Difficulty.Band] = config;
					}
				}
				else
				{
					report.Rejected++;
					var key = result.RejectionReason.ToString();
					if (!report.RejectionCounts.ContainsKey(key))
					{
						report.RejectionCounts[key] = 0;
					}

					report.RejectionCounts[key]++;
				}
			}
		}

		private static string BuildSummary(
			StressReport report,
			Dictionary<DifficultyBand, int> bandExamples)
		{
			var sb = new StringBuilder();
			sb.AppendLine("attempted=" + report.Attempted);
			sb.AppendLine("accepted=" + report.Accepted);
			sb.AppendLine("rejected=" + report.Rejected);
			sb.AppendLine("ms=" + report.ElapsedMilliseconds);
			sb.AppendLine("maxSolverStates=" + report.MaxSolverStates);
			sb.AppendLine("acceptedOver64=" + report.AcceptedOver64);
			sb.AppendLine("reproducible=" + report.ReproducibilityOk);
			foreach (var pair in report.RejectionCounts)
			{
				sb.AppendLine("reject." + pair.Key + "=" + pair.Value);
			}

			foreach (var pair in report.DifficultyCounts)
			{
				sb.AppendLine("diff." + pair.Key + "=" + pair.Value);
			}

			foreach (var pair in bandExamples)
			{
				sb.AppendLine("example." + pair.Key + ".seed=" + pair.Value);
			}

			return sb.ToString();
		}
	}
}
