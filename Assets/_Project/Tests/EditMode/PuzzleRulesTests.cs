using System;
using System.Collections.Generic;
using NUnit.Framework;
using TapAway.Core;

namespace TapAway.Core.Tests
{
	/// <summary>
	/// Pure-C# EditMode coverage for escape rules and PuzzleState.
	/// </summary>
	public sealed class PuzzleRulesTests
	{
		private static PuzzleState State(params PuzzleBlock[] blocks)
		{
			return new PuzzleState(blocks);
		}

		[Test]
		public void SingleBlock_CanEscape()
		{
			var state = State(new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX));
			var result = state.TryRemove(new BlockId(1));
			Assert.That(result.Status, Is.EqualTo(MoveStatus.Allowed));
			Assert.That(state.IsComplete, Is.True);
		}

		[Test]
		public void BlockDirectlyInPath_BlocksEscape()
		{
			var state = State(
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX),
				new PuzzleBlock(2, 1, 0, 0, EscapeDirection.PosY));

			var result = state.TryRemove(new BlockId(1));
			Assert.That(result.Status, Is.EqualTo(MoveStatus.Blocked));
			Assert.That(result.BlockingBlockId, Is.EqualTo(new BlockId(2)));
			Assert.That(state.IsActive(new BlockId(1)), Is.True);
		}

		[Test]
		public void BlockFartherOnSameRay_BlocksEscape()
		{
			var state = State(
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX),
				new PuzzleBlock(2, 3, 0, 0, EscapeDirection.PosY));

			var result = state.TryRemove(new BlockId(1));
			Assert.That(result.Status, Is.EqualTo(MoveStatus.Blocked));
			Assert.That(result.BlockingBlockId, Is.EqualTo(new BlockId(2)));
		}

		[Test]
		public void BlockBeyondLegacyRayCap_BlocksEscape()
		{
			var state = State(
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX),
				new PuzzleBlock(2, 65, 0, 0, EscapeDirection.PosY));

			var result = state.TryRemove(new BlockId(1));

			Assert.That(result.Status, Is.EqualTo(MoveStatus.Blocked));
			Assert.That(result.BlockingBlockId, Is.EqualTo(new BlockId(2)));
		}

		[Test]
		public void PositiveRay_DoesNotWrapAtIntMaxValue()
		{
			var state = State(
				new PuzzleBlock(1, int.MaxValue, 0, 0, EscapeDirection.PosX),
				new PuzzleBlock(2, int.MinValue, 0, 0, EscapeDirection.PosY));

			var result = state.TryRemove(new BlockId(1));

			Assert.That(result.Status, Is.EqualTo(MoveStatus.Allowed));
		}

		[Test]
		public void BlockBesideRay_DoesNotBlock()
		{
			var state = State(
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX),
				new PuzzleBlock(2, 1, 1, 0, EscapeDirection.PosY));

			var result = state.TryRemove(new BlockId(1));
			Assert.That(result.Status, Is.EqualTo(MoveStatus.Allowed));
		}

		[Test]
		public void BlockBehindSelected_DoesNotBlock()
		{
			var state = State(
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX),
				new PuzzleBlock(2, -1, 0, 0, EscapeDirection.PosY));

			var result = state.TryRemove(new BlockId(1));
			Assert.That(result.Status, Is.EqualTo(MoveStatus.Allowed));
		}

		[Test]
		public void Removal_ChangesSubsequentLegalMoves()
		{
			var state = State(
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.NegX),
				new PuzzleBlock(2, 1, 0, 0, EscapeDirection.NegX));

			Assert.That(state.TryRemove(new BlockId(2)).Status, Is.EqualTo(MoveStatus.Blocked));
			Assert.That(state.TryRemove(new BlockId(1)).Status, Is.EqualTo(MoveStatus.Allowed));
			Assert.That(state.TryRemove(new BlockId(2)).Status, Is.EqualTo(MoveStatus.Allowed));
			Assert.That(state.IsComplete, Is.True);
		}

		[TestCase(EscapeDirection.PosX, 1, 0, 0)]
		[TestCase(EscapeDirection.NegX, -1, 0, 0)]
		[TestCase(EscapeDirection.PosY, 0, 1, 0)]
		[TestCase(EscapeDirection.NegY, 0, -1, 0)]
		[TestCase(EscapeDirection.PosZ, 0, 0, 1)]
		[TestCase(EscapeDirection.NegZ, 0, 0, -1)]
		public void AllSixDirections_EscapeWhenClear(EscapeDirection direction, int ox, int oy, int oz)
		{
			var state = State(new PuzzleBlock(1, 0, 0, 0, direction));
			Assert.That(state.TryRemove(new BlockId(1)).Status, Is.EqualTo(MoveStatus.Allowed));
		}

		[TestCase(EscapeDirection.PosX, 1, 0, 0)]
		[TestCase(EscapeDirection.NegX, -1, 0, 0)]
		[TestCase(EscapeDirection.PosY, 0, 1, 0)]
		[TestCase(EscapeDirection.NegY, 0, -1, 0)]
		[TestCase(EscapeDirection.PosZ, 0, 0, 1)]
		[TestCase(EscapeDirection.NegZ, 0, 0, -1)]
		public void AllSixDirections_BlockedWhenOccupied(EscapeDirection direction, int ox, int oy, int oz)
		{
			var state = State(
				new PuzzleBlock(1, 0, 0, 0, direction),
				new PuzzleBlock(2, ox, oy, oz, EscapeDirection.PosY));

			var result = state.TryRemove(new BlockId(1));
			Assert.That(result.Status, Is.EqualTo(MoveStatus.Blocked));
			Assert.That(result.BlockingBlockId, Is.EqualTo(new BlockId(2)));
		}

		[Test]
		public void RemovedBlocks_NoLongerBlock()
		{
			var state = State(
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX),
				new PuzzleBlock(2, 1, 0, 0, EscapeDirection.PosY));

			Assert.That(state.TryRemove(new BlockId(1)).Status, Is.EqualTo(MoveStatus.Blocked));
			Assert.That(state.TryRemove(new BlockId(2)).Status, Is.EqualTo(MoveStatus.Allowed));
			Assert.That(state.TryRemove(new BlockId(1)).Status, Is.EqualTo(MoveStatus.Allowed));
		}

		[Test]
		public void UnknownBlockId_HandledSafely()
		{
			var state = State(new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX));
			var result = state.TryRemove(new BlockId(99));
			Assert.That(result.Status, Is.EqualTo(MoveStatus.UnknownBlock));
			Assert.That(state.ActiveCount, Is.EqualTo(1));
		}

		[Test]
		public void AlreadyRemoved_HandledSafely()
		{
			var state = State(new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX));
			Assert.That(state.TryRemove(new BlockId(1)).Status, Is.EqualTo(MoveStatus.Allowed));
			Assert.That(state.TryRemove(new BlockId(1)).Status, Is.EqualTo(MoveStatus.AlreadyRemoved));
		}

		[Test]
		public void PuzzleCompletes_WhenAllBlocksRemoved()
		{
			var state = State(
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.NegX),
				new PuzzleBlock(2, 0, 1, 0, EscapeDirection.PosY));

			Assert.That(state.IsComplete, Is.False);
			state.TryRemove(new BlockId(1));
			state.TryRemove(new BlockId(2));
			Assert.That(state.IsComplete, Is.True);
			Assert.That(state.ActiveCount, Is.EqualTo(0));
		}
	}

	/// <summary>
	/// Phase 1 prototype level: documented solution + blocked-move checks.
	/// </summary>
	public sealed class Phase1PrototypeLevelTests
	{
		[Test]
		public void Level_HasExpectedBlockCount()
		{
			var level = Phase1PrototypeLevel.Create();
			Assert.That(level.Blocks.Count, Is.EqualTo(12));
			Assert.That(Phase1PrototypeLevel.DocumentedSolution.Length, Is.EqualTo(12));
		}

		[Test]
		public void Level_UsesAllSixDirections()
		{
			var level = Phase1PrototypeLevel.Create();
			var seen = new HashSet<EscapeDirection>();
			foreach (var block in level.Blocks)
			{
				seen.Add(block.EscapeDirection);
			}

			Assert.That(seen.Count, Is.EqualTo(6));
		}

		[Test]
		public void DocumentedSolution_CompletesPuzzle()
		{
			var state = Phase1PrototypeLevel.Create().CreateState();
			foreach (var id in Phase1PrototypeLevel.DocumentedSolution)
			{
				var result = state.TryRemove(new BlockId(id));
				Assert.That(
					result.Status,
					Is.EqualTo(MoveStatus.Allowed),
					"Expected Allowed for block " + id + " but got " + result);
			}

			Assert.That(state.IsComplete, Is.True);
		}

		[Test]
		public void IntentionalBlockedMove_BeforeDependenciesCleared()
		{
			var state = Phase1PrototypeLevel.Create().CreateState();

			// Block 3 escapes -X but is blocked by block 2 at (1,0,0).
			var blocked = state.TryRemove(new BlockId(3));
			Assert.That(blocked.Status, Is.EqualTo(MoveStatus.Blocked));
			Assert.That(blocked.BlockingBlockId, Is.EqualTo(new BlockId(2)));

			// Block 4 escapes -Z into (2,0,0) occupied by 3.
			var blocked4 = state.TryRemove(new BlockId(4));
			Assert.That(blocked4.Status, Is.EqualTo(MoveStatus.Blocked));
			Assert.That(blocked4.BlockingBlockId, Is.EqualTo(new BlockId(3)));
		}

		[Test]
		public void DocumentedSolution_IsUniquePermutationCoverage()
		{
			var ids = new HashSet<int>(Phase1PrototypeLevel.DocumentedSolution);
			Assert.That(ids.Count, Is.EqualTo(12));
			foreach (var block in Phase1PrototypeLevel.Create().Blocks)
			{
				Assert.That(ids.Contains(block.Id.Value), Is.True);
			}
		}
	}
}
