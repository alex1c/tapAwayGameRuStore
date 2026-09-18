using System;
using System.Collections.Generic;

namespace TapAway.Core
{
	/// <summary>
	/// Describes one playable level for runtime (handcrafted or generated).
	/// Presentation consumes <see cref="PuzzleLevel"/>; metadata is for HUD/QA.
	/// </summary>
	public sealed class GameLevelDescriptor
	{
		public string Id { get; set; }
		public string DisplayName { get; set; }
		public PuzzleLevel Puzzle { get; set; }
		public int? Seed { get; set; }
		public string GeneratorVersion { get; set; }
		public int BlockCount { get; set; }
		public double? DifficultyScore { get; set; }
		public DifficultyBand? DifficultyBand { get; set; }
		public bool IsTutorialFixture { get; set; }
	}

	/// <summary>Ordered source of gameplay levels.</summary>
	public interface IGameLevelSource
	{
		int Count { get; }
		GameLevelDescriptor GetLevel(int index);
	}

	/// <summary>Phase 1 prototype as a single-level source / tutorial fixture.</summary>
	public sealed class HandcraftedLevelSource : IGameLevelSource
	{
		public int Count => 1;

		public GameLevelDescriptor GetLevel(int index)
		{
			if (index != 0)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			var level = Phase1PrototypeLevel.Create();
			return new GameLevelDescriptor
			{
				Id = level.Id,
				DisplayName = level.DisplayName,
				Puzzle = level,
				BlockCount = level.Blocks.Count,
				IsTutorialFixture = true
			};
		}
	}

	/// <summary>One curated Phase 4 device-QA entry.</summary>
	public readonly struct Phase4QaEntry
	{
		public readonly int Index;
		public readonly int Seed;
		public readonly int TargetBlockCount;
		public readonly string Label;

		public Phase4QaEntry(int index, int seed, int targetBlockCount, string label)
		{
			Index = index;
			Seed = seed;
			TargetBlockCount = targetBlockCount;
			Label = label;
		}
	}

	/// <summary>
	/// Deterministic Phase 4 device-QA level set (generator 1.1.0).
	/// Seeds are accepted pipeline outputs — verified by EditMode tests.
	/// </summary>
	public static class Phase4QaLevelSet
	{
		public const string GeneratorVersion = GeneratorConfig.GeneratorVersion;

		/// <summary>
		/// Curated accepted seeds. Counts match generator TargetBlockCount.
		/// Discovered via pipeline search; locked for device QA reproducibility.
		/// </summary>
		public static readonly Phase4QaEntry[] Entries =
		{
			new Phase4QaEntry(1, 41000, 8, "QA-1"),
			new Phase4QaEntry(2, 42000, 10, "QA-2"),
			new Phase4QaEntry(3, 1000, 12, "QA-3"),
			new Phase4QaEntry(4, 43000, 15, "QA-4"),
			new Phase4QaEntry(5, 44000, 18, "QA-5"),
			new Phase4QaEntry(6, 45000, 22, "QA-6"),
			new Phase4QaEntry(7, 11000, 28, "QA-7")
		};

		public static int Count => Entries.Length;

		public static GeneratorConfig ConfigForCount(int blockCount)
		{
			var half = Math.Max(2, (blockCount + 3) / 4);
			var yHalf = Math.Max(1, half / 2 + 1);
			return new GeneratorConfig
			{
				TargetBlockCount = blockCount,
				MinX = -half,
				MaxX = half,
				MinY = -yHalf,
				MaxY = yHalf + 1,
				MinZ = -half,
				MaxZ = half,
				MaxConstructionAttempts = Math.Max(60, blockCount * 4),
				MaxPipelineAttempts = Math.Max(25, blockCount),
				Quality = new LevelQualityConfig
				{
					EndgameActiveAllowance = blockCount <= 12 ? 3 : 4,
					MaxActiveForPrematureSingleton = blockCount <= 12 ? 3 : 4,
					MaxActiveForDiagonalIsland = blockCount <= 12 ? 3 : 4,
					MaxComponentsBeforeEndgame = 2,
					AlternatePathSampleCount = blockCount >= 22 ? 2 : 3
				}
			};
		}

		public static GenerationResult GenerateEntry(Phase4QaEntry entry)
		{
			return GenerationPipeline.Generate(entry.Seed, ConfigForCount(entry.TargetBlockCount));
		}

		public static GameLevelDescriptor ToDescriptor(Phase4QaEntry entry, GeneratedLevel generated)
		{
			var puzzle = new PuzzleLevel(
				entry.Label.ToLowerInvariant() + "_" + entry.Seed,
				entry.Label + " (" + generated.Blocks.Count + ")",
				generated.Blocks,
				generated.CanonicalSolution);

			return new GameLevelDescriptor
			{
				Id = puzzle.Id,
				DisplayName = puzzle.DisplayName,
				Puzzle = puzzle,
				Seed = entry.Seed,
				GeneratorVersion = GeneratorVersion,
				BlockCount = generated.Blocks.Count,
				DifficultyScore = generated.Difficulty != null ? generated.Difficulty.Score : (double?)null,
				DifficultyBand = generated.Difficulty != null ? generated.Difficulty.Band : (DifficultyBand?)null,
				IsTutorialFixture = false
			};
		}

		/// <summary>
		/// Finds the first accepted seed at or after <paramref name="startSeed"/>.
		/// Development helper for catalog curation — not used at runtime.
		/// </summary>
		public static int FindAcceptedSeed(int blockCount, int startSeed, int maxTries = 200)
		{
			var config = ConfigForCount(blockCount);
			for (var i = 0; i < maxTries; i++)
			{
				var seed = startSeed + i;
				var result = GenerationPipeline.Generate(seed, config);
				if (result.Accepted && result.Level.Blocks.Count == blockCount)
				{
					return seed;
				}
			}

			return -1;
		}
	}

	/// <summary>IGameLevelSource backed by <see cref="Phase4QaLevelSet"/>.</summary>
	public sealed class Phase4QaLevelSource : IGameLevelSource
	{
		private readonly GameLevelDescriptor[] _cache;

		public Phase4QaLevelSource()
		{
			_cache = new GameLevelDescriptor[Phase4QaLevelSet.Count];
			for (var i = 0; i < Phase4QaLevelSet.Entries.Length; i++)
			{
				var entry = Phase4QaLevelSet.Entries[i];
				var generated = Phase4QaLevelSet.GenerateEntry(entry);
				if (!generated.Accepted)
				{
					throw new InvalidOperationException(
						"QA entry " + entry.Label + " seed " + entry.Seed + " rejected: " +
						generated.RejectionReason + " " + generated.Detail);
				}

				_cache[i] = Phase4QaLevelSet.ToDescriptor(entry, generated.Level);
			}
		}

		public int Count => _cache.Length;

		public GameLevelDescriptor GetLevel(int index)
		{
			if (index < 0 || index >= _cache.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			// Rebuild from seed so Restart always matches pipeline output.
			var entry = Phase4QaLevelSet.Entries[index];
			var generated = Phase4QaLevelSet.GenerateEntry(entry);
			if (!generated.Accepted)
			{
				throw new InvalidOperationException("QA seed became invalid: " + entry.Seed);
			}

			return Phase4QaLevelSet.ToDescriptor(entry, generated.Level);
		}

		/// <summary>Cached descriptor without regenerating (read-only inspection).</summary>
		public GameLevelDescriptor PeekCached(int index)
		{
			return _cache[index];
		}
	}

	/// <summary>Local-only per-level play metrics for future difficulty calibration.</summary>
	public sealed class LevelPlayMetrics
	{
		public int Seed { get; set; }
		public int BlockCount { get; set; }
		public double DifficultyScore { get; set; }
		public DifficultyBand DifficultyBand { get; set; }
		public float ElapsedSeconds { get; set; }
		public int SuccessfulRemovals { get; set; }
		public int BlockedTaps { get; set; }
		public int BlockTaps { get; set; }
		public int OrbitGestures { get; set; }
		public int PinchGestures { get; set; }

		public void Reset(GameLevelDescriptor level)
		{
			Seed = level.Seed ?? 0;
			BlockCount = level.BlockCount;
			DifficultyScore = level.DifficultyScore ?? 0;
			DifficultyBand = level.DifficultyBand ?? DifficultyBand.Tutorial;
			ElapsedSeconds = 0f;
			SuccessfulRemovals = 0;
			BlockedTaps = 0;
			BlockTaps = 0;
			OrbitGestures = 0;
			PinchGestures = 0;
		}

		public string ToDebugLine()
		{
			return "seed=" + Seed +
			       " blocks=" + BlockCount +
			       " diff=" + DifficultyScore.ToString("0.0") + "/" + DifficultyBand +
			       " t=" + ElapsedSeconds.ToString("0.0") + "s" +
			       " ok=" + SuccessfulRemovals +
			       " blocked=" + BlockedTaps +
			       " taps=" + BlockTaps +
			       " orbit=" + OrbitGestures +
			       " pinch=" + PinchGestures;
		}
	}
}
