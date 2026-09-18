using System.Collections.Generic;
using NUnit.Framework;
using TapAway.Core;

namespace TapAway.Core.Tests
{
	/// <summary>Pure-C# solver coverage for Phase 3.</summary>
	public sealed class PuzzleSolverTests
	{
		[Test]
		public void Empty_IsSolvedEmpty()
		{
			var result = PuzzleSolver.Solve(System.Array.Empty<PuzzleBlock>());
			Assert.That(result.Status, Is.EqualTo(SolverStatus.Empty));
			Assert.That(result.IsSolvable, Is.True);
			Assert.That(result.SolutionLength, Is.EqualTo(0));
		}

		[Test]
		public void SingleBlock_SolvesInOneMove()
		{
			var blocks = new[] { new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX) };
			var result = PuzzleSolver.Solve(blocks);
			Assert.That(result.Status, Is.EqualTo(SolverStatus.Solved));
			Assert.That(result.Solution, Is.EqualTo(new[] { 1 }));
			Assert.That(PuzzleSolver.TryReplaySolution(blocks, result.Solution), Is.True);
		}

		[Test]
		public void ForcedSequence_RemovesBlockerFirst()
		{
			var blocks = new[]
			{
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX),
				new PuzzleBlock(2, 1, 0, 0, EscapeDirection.PosX)
			};
			var result = PuzzleSolver.Solve(blocks);
			Assert.That(result.IsSolvable, Is.True);
			Assert.That(result.Solution[0], Is.EqualTo(2));
			Assert.That(result.ForcedMoveCount, Is.EqualTo(2));
			Assert.That(PuzzleSolver.TryReplaySolution(blocks, result.Solution), Is.True);
		}

		[Test]
		public void MultipleFirstMoves_PicksLowestBlockId()
		{
			var blocks = new[]
			{
				new PuzzleBlock(3, 2, 0, 0, EscapeDirection.PosY),
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.NegX),
				new PuzzleBlock(2, 1, 0, 0, EscapeDirection.PosZ)
			};
			var a = PuzzleSolver.Solve(blocks);
			var b = PuzzleSolver.Solve(blocks);
			Assert.That(a.Solution, Is.EqualTo(b.Solution));
			Assert.That(a.Solution[0], Is.EqualTo(1));
			Assert.That(a.LegalMovesAtStart.Count, Is.GreaterThanOrEqualTo(2));
		}

		[Test]
		public void CyclicMutualBlock_IsUnsolvable()
		{
			// Two blocks on the same axis blocking each other.
			var blocks = new[]
			{
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX),
				new PuzzleBlock(2, 1, 0, 0, EscapeDirection.NegX)
			};
			var result = PuzzleSolver.Solve(blocks);
			Assert.That(result.Status, Is.EqualTo(SolverStatus.Unsolvable));
			Assert.That(result.IsSolvable, Is.False);
		}

		[Test]
		public void Phase1Prototype_IsSolvableAndReplays()
		{
			var level = Phase1PrototypeLevel.Create();
			var result = PuzzleSolver.Solve(level);
			Assert.That(result.IsSolvable, Is.True);
			Assert.That(result.SolutionLength, Is.EqualTo(12));
			Assert.That(PuzzleSolver.TryReplaySolution(level.Blocks, result.Solution), Is.True);
		}

		[Test]
		public void AllSixDirections_AreSolvableIndependently()
		{
			foreach (EscapeDirection dir in System.Enum.GetValues(typeof(EscapeDirection)))
			{
				var blocks = new[] { new PuzzleBlock(1, 0, 0, 0, dir) };
				var result = PuzzleSolver.Solve(blocks);
				Assert.That(result.IsSolvable, Is.True, dir.ToString());
			}
		}

		[Test]
		public void RemovedState_AlreadyClearedBoard_Empty()
		{
			Assert.That(PuzzleSolver.TryReplaySolution(
				new[] { new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX) },
				new[] { 1 }), Is.True);
		}

		[Test]
		public void Deterministic_RepeatedSolve_SameSolution()
		{
			var level = Phase1PrototypeLevel.Create();
			var a = PuzzleSolver.Solve(level);
			var b = PuzzleSolver.Solve(level);
			Assert.That(a.Solution, Is.EqualTo(b.Solution));
			Assert.That(a.ExploredStates, Is.EqualTo(b.ExploredStates));
		}

		[Test]
		public void Over64Blocks_SupportedAndSolvable()
		{
			var blocks = new List<PuzzleBlock>();
			for (var i = 0; i < 70; i++)
			{
				// Column of free +Y blocks — each escapes upward independently.
				blocks.Add(new PuzzleBlock(i + 1, i, 0, 0, EscapeDirection.PosY));
			}

			var result = PuzzleSolver.Solve(blocks);
			Assert.That(result.IsSolvable, Is.True);
			Assert.That(result.SolutionLength, Is.EqualTo(70));
			Assert.That(PuzzleSolver.TryReplaySolution(blocks, result.Solution), Is.True);
		}

		[Test]
		public void DuplicateIds_InvalidInput()
		{
			var blocks = new[]
			{
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX),
				new PuzzleBlock(1, 1, 0, 0, EscapeDirection.PosY)
			};
			var result = PuzzleSolver.Solve(blocks);
			Assert.That(result.Status, Is.EqualTo(SolverStatus.InvalidInput));
		}

		[Test]
		public void DeadEnd_UnsolvableCluster()
		{
			var blocks = new[]
			{
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX),
				new PuzzleBlock(2, 1, 0, 0, EscapeDirection.NegX),
				new PuzzleBlock(3, 5, 0, 0, EscapeDirection.PosY)
			};
			var result = PuzzleSolver.Solve(blocks);
			Assert.That(result.Status, Is.EqualTo(SolverStatus.Unsolvable));
		}

		[Test]
		public void MultipleSolutions_ExistForIndependentBlocks()
		{
			var blocks = new[]
			{
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.NegX),
				new PuzzleBlock(2, 2, 0, 0, EscapeDirection.PosX)
			};
			var primary = PuzzleSolver.Solve(blocks);
			Assert.That(primary.IsSolvable, Is.True);
			var alts = PuzzleSolver.SampleAlternateSolutions(blocks, 3);
			Assert.That(alts.Count, Is.GreaterThanOrEqualTo(1));
			Assert.That(alts[0][0], Is.Not.EqualTo(primary.Solution[0]));
			Assert.That(PuzzleSolver.TryReplaySolution(blocks, alts[0]), Is.True);
		}

		[Test]
		public void AlternateSampling_SpreadsAcrossWideFirstChoiceSet()
		{
			var blocks = new List<PuzzleBlock>();
			for (var i = 0; i < 9; i++)
			{
				blocks.Add(new PuzzleBlock(i + 1, 0, i, 0, EscapeDirection.PosX));
			}

			var alts = PuzzleSolver.SampleAlternateSolutions(blocks, 3);

			Assert.That(alts.Count, Is.EqualTo(3));
			Assert.That(alts[0][0], Is.EqualTo(2));
			Assert.That(alts[1][0], Is.EqualTo(6));
			Assert.That(alts[2][0], Is.EqualTo(9));
		}

		[Test]
		public void Monotonic_LegalRemovalPreservesSolvability_OnFixtures()
		{
			Assert.That(
				PuzzleSolver.LegalRemovalPreservesSolvability(Phase1PrototypeLevel.Create().Blocks),
				Is.True);
			Assert.That(
				PuzzleSolver.LegalRemovalPreservesSolvability(new[]
				{
					new PuzzleBlock(1, 0, 0, 0, EscapeDirection.NegX),
					new PuzzleBlock(2, 2, 0, 0, EscapeDirection.PosX),
					new PuzzleBlock(3, 1, 1, 0, EscapeDirection.PosY)
				}),
				Is.True);
		}

		[Test]
		public void ActiveMask_SupportsMoreThan64()
		{
			var mask = ActiveMask.AllActive(70);
			Assert.That(mask.IsSet(0), Is.True);
			Assert.That(mask.IsSet(69), Is.True);
			mask.Clear(65);
			Assert.That(mask.IsSet(65), Is.False);
			Assert.That(mask.CountSet(70), Is.EqualTo(69));
		}
	}
}
