using System.Collections.Generic;

namespace TapAway.Core
{
	/// <summary>Versioned generator settings (no scattered magic numbers).</summary>
	public sealed class GeneratorConfig
	{
		public const string GeneratorVersion = "1.0.0";

		public int TargetBlockCount { get; set; } = 16;
		public int MinX { get; set; } = -4;
		public int MaxX { get; set; } = 4;
		public int MinY { get; set; } = -2;
		public int MaxY { get; set; } = 3;
		public int MinZ { get; set; } = -4;
		public int MaxZ { get; set; } = 4;
		public int MaxConstructionAttempts { get; set; } = 80;
		public int MaxPipelineAttempts { get; set; } = 40;
		public DifficultyBand? MinDifficulty { get; set; }
		public DifficultyBand? MaxDifficulty { get; set; }
		public LevelQualityConfig Quality { get; set; } = new LevelQualityConfig();
		public SolverConfig Solver { get; set; } = new SolverConfig();

		public static GeneratorConfig Tiny()
		{
			return new GeneratorConfig
			{
				TargetBlockCount = 8,
				MinX = -2, MaxX = 2, MinY = -1, MaxY = 2, MinZ = -2, MaxZ = 2,
				MaxConstructionAttempts = 40,
				MaxPipelineAttempts = 20
			};
		}

		public static GeneratorConfig Small()
		{
			return new GeneratorConfig
			{
				TargetBlockCount = 12,
				MinX = -3, MaxX = 3, MinY = -1, MaxY = 2, MinZ = -3, MaxZ = 3
			};
		}

		public static GeneratorConfig Medium()
		{
			return new GeneratorConfig
			{
				TargetBlockCount = 28,
				MinX = -5, MaxX = 5, MinY = -2, MaxY = 3, MinZ = -5, MaxZ = 5
			};
		}

		public static GeneratorConfig Large()
		{
			return new GeneratorConfig
			{
				TargetBlockCount = 48,
				MinX = -6, MaxX = 6, MinY = -3, MaxY = 4, MinZ = -6, MaxZ = 6,
				MaxConstructionAttempts = 120,
				MaxPipelineAttempts = 50,
				Quality = new LevelQualityConfig
				{
					EndgameActiveAllowance = 4,
					MaxActiveForPrematureSingleton = 4,
					MaxActiveForDiagonalIsland = 4,
					MaxComponentsBeforeEndgame = 2,
					AlternatePathSampleCount = 2
				}
			};
		}

		public static GeneratorConfig StressOver64()
		{
			return new GeneratorConfig
			{
				TargetBlockCount = 72,
				MinX = -8, MaxX = 8, MinY = -3, MaxY = 4, MinZ = -8, MaxZ = 8,
				MaxConstructionAttempts = 160,
				MaxPipelineAttempts = 60,
				Quality = new LevelQualityConfig
				{
					EndgameActiveAllowance = 5,
					MaxActiveForPrematureSingleton = 5,
					MaxActiveForDiagonalIsland = 5,
					MaxComponentsBeforeEndgame = 3,
					AlternatePathSampleCount = 1
				}
			};
		}
	}

	/// <summary>Accepted generated level with reproducible metadata.</summary>
	public sealed class GeneratedLevel
	{
		public const string FormatVersion = "1";

		public string FormatVersionValue => FormatVersion;
		public string GeneratorVersion { get; set; }
		public int Seed { get; set; }
		public string LevelId { get; set; }
		public IReadOnlyList<PuzzleBlock> Blocks { get; set; }
		public IReadOnlyList<int> CanonicalSolution { get; set; }
		public DifficultyResult Difficulty { get; set; }
		public LevelQualityReport Quality { get; set; }
		public SolverResult Solver { get; set; }

		public PuzzleLevel ToPuzzleLevel()
		{
			return new PuzzleLevel(
				LevelId,
				"Generated " + Seed,
				Blocks,
				CanonicalSolution);
		}
	}

	/// <summary>Pipeline outcome for one Generate() call (may include retries).</summary>
	public sealed class GenerationResult
	{
		public bool Accepted { get; set; }
		public GeneratedLevel Level { get; set; }
		public LevelRejectionReason RejectionReason { get; set; }
		public string Detail { get; set; }
		public int AttemptsUsed { get; set; }
		public int Seed { get; set; }
	}
}
