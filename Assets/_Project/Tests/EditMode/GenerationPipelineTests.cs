using System.Collections.Generic;
using NUnit.Framework;
using TapAway.Core;

namespace TapAway.Core.Tests
{
	/// <summary>Generator, quality, difficulty, and pipeline coverage.</summary>
	public sealed class GenerationPipelineTests
	{
		[Test]
		public void SameSeed_ProducesIdenticalLevel()
		{
			var a = GenerationPipeline.Generate(4242, GeneratorConfig.Small());
			var b = GenerationPipeline.Generate(4242, GeneratorConfig.Small());
			Assert.That(a.Accepted, Is.True);
			Assert.That(b.Accepted, Is.True);
			Assert.That(a.Level.Blocks.Count, Is.EqualTo(b.Level.Blocks.Count));
			for (var i = 0; i < a.Level.Blocks.Count; i++)
			{
				Assert.That(a.Level.Blocks[i].Id, Is.EqualTo(b.Level.Blocks[i].Id));
				Assert.That(a.Level.Blocks[i].Position, Is.EqualTo(b.Level.Blocks[i].Position));
				Assert.That(a.Level.Blocks[i].EscapeDirection, Is.EqualTo(b.Level.Blocks[i].EscapeDirection));
			}
		}

		[Test]
		public void AcceptedLevel_IsFaceConnectedAndSolvable()
		{
			GenerationResult result = null;
			for (var seed = 1; seed < 80; seed++)
			{
				var candidate = GenerationPipeline.Generate(seed, GeneratorConfig.Small());
				if (!candidate.Accepted)
				{
					continue;
				}

				result = candidate;
				break;
			}

			Assert.That(result, Is.Not.Null, "Expected an accepted small seed");
			Assert.That(LevelTopology.IsSingleFaceConnectedComponent(result.Level.Blocks), Is.True);
			Assert.That(PuzzleSolver.TryReplaySolution(
				result.Level.Blocks,
				result.Level.CanonicalSolution), Is.True);
		}

		[Test]
		public void Pipeline_RespectsAttemptBudget()
		{
			var config = GeneratorConfig.Small();
			config.MaxPipelineAttempts = 3;
			config.TargetBlockCount = 12;
			// Impossible band window forces every candidate to reject.
			config.MinDifficulty = DifficultyBand.Hard;
			config.MaxDifficulty = DifficultyBand.Tutorial;
			var result = GenerationPipeline.Generate(99, config);
			Assert.That(result.Accepted, Is.False);
			Assert.That(result.RejectionReason, Is.EqualTo(LevelRejectionReason.AttemptBudgetExceeded));
			Assert.That(result.AttemptsUsed, Is.EqualTo(3));
		}

		[Test]
		public void Quality_RejectsPrematureSingletonFixture()
		{
			var blocks = new[]
			{
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.NegX),
				new PuzzleBlock(5, 0, 1, 0, EscapeDirection.NegY),
				new PuzzleBlock(6, 1, 1, 0, EscapeDirection.PosY),
				new PuzzleBlock(2, 1, 0, 0, EscapeDirection.NegX),
				new PuzzleBlock(3, 2, 0, 0, EscapeDirection.PosX)
			};
			var solution = new[] { 1, 6, 5, 2, 3 };
			var report = LevelQualityAnalyzer.Analyze(
				blocks,
				solution,
				new LevelQualityConfig
				{
					EndgameActiveAllowance = 1,
					MaxActiveForPrematureSingleton = 1,
					MaxActiveForDiagonalIsland = 1,
					AlternatePathSampleCount = 0
				});
			Assert.That(report.Passed, Is.False);
			Assert.That(report.RejectionReason, Is.EqualTo(LevelRejectionReason.PoorIntermediateTopology));
		}

		[Test]
		public void Difficulty_ForcedSmall_RanksBelowChoiceHeavy()
		{
			var forced = new[]
			{
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX),
				new PuzzleBlock(2, 1, 0, 0, EscapeDirection.PosX),
				new PuzzleBlock(3, 2, 0, 0, EscapeDirection.PosX)
			};
			var choice = new List<PuzzleBlock>();
			for (var i = 0; i < 12; i++)
			{
				choice.Add(new PuzzleBlock(i + 1, i % 4, i / 4, 0, EscapeDirection.PosY));
			}

			var forcedDiff = DifficultyAnalyzer.Analyze(forced, PuzzleSolver.Solve(forced));
			var choiceDiff = DifficultyAnalyzer.Analyze(choice, PuzzleSolver.Solve(choice));
			Assert.That(choiceDiff.Score, Is.GreaterThan(forcedDiff.Score));
			Assert.That(forcedDiff.Band, Is.LessThanOrEqualTo(DifficultyBand.Easy));
		}

		[Test]
		public void Difficulty_Features_AreStable()
		{
			var level = Phase1PrototypeLevel.Create();
			var solver = PuzzleSolver.Solve(level);
			var a = DifficultyAnalyzer.ExtractFeatures(level.Blocks, solver);
			var b = DifficultyAnalyzer.ExtractFeatures(level.Blocks, solver);
			Assert.That(a.BlockCount, Is.EqualTo(b.BlockCount));
			Assert.That(a.ChoicePointCount, Is.EqualTo(b.ChoicePointCount));
			Assert.That(a.DirectionDiversity, Is.EqualTo(6));
		}

		[Test]
		public void Generator_ConstructsOver64_WhenConfigured()
		{
			var config = GeneratorConfig.StressOver64();
			config.MaxPipelineAttempts = 8;
			config.Quality.AlternatePathSampleCount = 1;

			var accepted = false;
			for (var seed = 50000; seed < 50080; seed++)
			{
				var result = GenerationPipeline.Generate(seed, config);
				if (!result.Accepted)
				{
					continue;
				}

				Assert.That(result.Level.Blocks.Count, Is.EqualTo(72));
				Assert.That(PuzzleSolver.Solve(result.Level.Blocks).IsSolvable, Is.True);
				accepted = true;
				break;
			}

			// Fallback: construction + solvability proves >64 mask/solver path.
			if (!accepted)
			{
				Assert.That(
					LevelGenerator.TryConstruct(50111, config, out var blocks, out var sol, out _),
					Is.True);
				Assert.That(blocks.Count, Is.EqualTo(72));
				Assert.That(PuzzleSolver.Solve(blocks).IsSolvable, Is.True);
				Assert.That(PuzzleSolver.TryReplaySolution(blocks, sol), Is.True);
				accepted = true;
			}

			Assert.That(accepted, Is.True);
		}

		[Test]
		public void Rng_IsDeterministic()
		{
			var a = new DeterministicRng(123);
			var b = new DeterministicRng(123);
			for (var i = 0; i < 20; i++)
			{
				Assert.That(a.NextUInt32(), Is.EqualTo(b.NextUInt32()));
			}
		}

		[Test]
		public void MiniStress_ProducesAcceptances()
		{
			var accepted = 0;
			for (var seed = 1000; seed < 1030; seed++)
			{
				if (GenerationPipeline.Generate(seed, GeneratorConfig.Small()).Accepted)
				{
					accepted++;
				}
			}

			Assert.That(accepted, Is.GreaterThan(0));
		}
	}
}
