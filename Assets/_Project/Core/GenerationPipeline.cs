using System.Collections.Generic;

namespace TapAway.Core
{
	/// <summary>
	/// Candidate pipeline: construct → solve → topology → difficulty → accept/reject.
	/// Hard attempt budget — never unbounded retry loops.
	/// </summary>
	public static class GenerationPipeline
	{
		public static GenerationResult Generate(int seed, GeneratorConfig config = null)
		{
			config = config ?? GeneratorConfig.Small();
			var attempts = 0;
			LevelRejectionReason lastReason = LevelRejectionReason.ConstructionFailed;
			var lastDetail = "no attempts";

			while (attempts < config.MaxPipelineAttempts)
			{
				attempts++;
				// Derive per-attempt seed so retries stay deterministic from the root seed.
				var attemptSeed = unchecked(seed + attempts * 9973);
				var result = EvaluateCandidate(attemptSeed, config, attempts);
				if (result.Accepted)
				{
					result.Seed = seed;
					if (result.Level != null)
					{
						result.Level.Seed = seed;
						result.Level.LevelId = "gen_" + seed;
					}

					return result;
				}

				lastReason = result.RejectionReason;
				lastDetail = result.Detail;
			}

			return new GenerationResult
			{
				Accepted = false,
				Seed = seed,
				AttemptsUsed = attempts,
				RejectionReason = LevelRejectionReason.AttemptBudgetExceeded,
				Detail = "Budget " + config.MaxPipelineAttempts +
				         " exhausted; last=" + lastReason + " (" + lastDetail + ")"
			};
		}

		private static GenerationResult EvaluateCandidate(
			int attemptSeed,
			GeneratorConfig config,
			int attemptsUsed)
		{
			if (!LevelGenerator.TryConstruct(
				    attemptSeed,
				    config,
				    out var blocks,
				    out var constructed,
				    out var constructFail))
			{
				return Reject(
					LevelRejectionReason.ConstructionFailed,
					constructFail,
					attemptsUsed,
					attemptSeed);
			}

			if (HasDuplicateGeometry(blocks))
			{
				return Reject(
					LevelRejectionReason.InvalidGeometry,
					"Duplicate id or position",
					attemptsUsed,
					attemptSeed);
			}

			var solver = PuzzleSolver.Solve(blocks, config.Solver);
			if (solver.Status == SolverStatus.BudgetExceeded)
			{
				return Reject(
					LevelRejectionReason.SolverBudgetExceeded,
					solver.FailureReason,
					attemptsUsed,
					attemptSeed);
			}

			if (!solver.IsSolvable)
			{
				return Reject(
					LevelRejectionReason.Unsolvable,
					solver.FailureReason ?? solver.Status.ToString(),
					attemptsUsed,
					attemptSeed);
			}

			if (!PuzzleSolver.TryReplaySolution(blocks, solver.Solution))
			{
				return Reject(
					LevelRejectionReason.Unsolvable,
					"Solver solution failed replay",
					attemptsUsed,
					attemptSeed);
			}

			var quality = LevelQualityAnalyzer.Analyze(blocks, solver.Solution, config.Quality);
			if (!quality.Passed)
			{
				return Reject(
					quality.RejectionReason,
					quality.Detail,
					attemptsUsed,
					attemptSeed);
			}

			var difficulty = DifficultyAnalyzer.Analyze(blocks, solver, quality);
			if (config.MinDifficulty.HasValue && difficulty.Band < config.MinDifficulty.Value)
			{
				return Reject(
					LevelRejectionReason.DifficultyOutOfRange,
					"band " + difficulty.Band + " < min " + config.MinDifficulty,
					attemptsUsed,
					attemptSeed);
			}

			if (config.MaxDifficulty.HasValue && difficulty.Band > config.MaxDifficulty.Value)
			{
				return Reject(
					LevelRejectionReason.DifficultyOutOfRange,
					"band " + difficulty.Band + " > max " + config.MaxDifficulty,
					attemptsUsed,
					attemptSeed);
			}

			var level = new GeneratedLevel
			{
				GeneratorVersion = GeneratorConfig.GeneratorVersion,
				Seed = attemptSeed,
				LevelId = "gen_" + attemptSeed,
				Blocks = blocks,
				CanonicalSolution = solver.Solution,
				Difficulty = difficulty,
				Quality = quality,
				Solver = solver
			};

			// Prefer independent solver order; constructed order is only a construction hint.
			_ = constructed;

			return new GenerationResult
			{
				Accepted = true,
				Level = level,
				RejectionReason = LevelRejectionReason.None,
				Detail = difficulty.Explanation,
				AttemptsUsed = attemptsUsed,
				Seed = attemptSeed
			};
		}

		private static bool HasDuplicateGeometry(List<PuzzleBlock> blocks)
		{
			var ids = new HashSet<int>();
			var pos = new HashSet<GridPosition>();
			for (var i = 0; i < blocks.Count; i++)
			{
				if (!ids.Add(blocks[i].Id.Value) || !pos.Add(blocks[i].Position))
				{
					return true;
				}
			}

			return false;
		}

		private static GenerationResult Reject(
			LevelRejectionReason reason,
			string detail,
			int attempts,
			int seed)
		{
			return new GenerationResult
			{
				Accepted = false,
				RejectionReason = reason,
				Detail = detail,
				AttemptsUsed = attempts,
				Seed = seed
			};
		}
	}
}
