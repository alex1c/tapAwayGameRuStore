using NUnit.Framework;
using TapAway.Core;

namespace TapAway.Core.Tests
{
	/// <summary>
	/// Level-quality topology checks (face adjacency), not Core escape-law tests.
	/// </summary>
	public sealed class LevelTopologyTests
	{
		[Test]
		public void FaceAdjacent_RequiresSharedFace()
		{
			Assert.That(
				LevelTopology.AreFaceAdjacent(new GridPosition(0, 0, 0), new GridPosition(1, 0, 0)),
				Is.True);
			Assert.That(
				LevelTopology.AreFaceAdjacent(new GridPosition(0, 0, 0), new GridPosition(1, 1, 0)),
				Is.False,
				"Edge-only adjacency must not count as face-connected");
			Assert.That(
				LevelTopology.AreFaceAdjacent(new GridPosition(0, 0, 0), new GridPosition(1, 1, 1)),
				Is.False,
				"Corner-only adjacency must not count as face-connected");
		}

		[Test]
		public void TwoDisconnectedBlocks_CountAsTwoComponents()
		{
			var blocks = new[]
			{
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX),
				new PuzzleBlock(2, 2, 0, 0, EscapeDirection.PosX)
			};
			Assert.That(LevelTopology.CountFaceConnectedComponents(blocks), Is.EqualTo(2));
		}

		[Test]
		public void EdgeOnlyPair_CountsAsTwoComponents()
		{
			var blocks = new[]
			{
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.PosX),
				new PuzzleBlock(2, 1, 1, 0, EscapeDirection.PosY)
			};
			Assert.That(LevelTopology.CountFaceConnectedComponents(blocks), Is.EqualTo(2));
		}

		[Test]
		public void Phase1Prototype_IsSingleFaceConnectedComponent()
		{
			var level = Phase1PrototypeLevel.Create();
			Assert.That(LevelTopology.IsSingleFaceConnectedComponent(level.Blocks), Is.True);
			Assert.That(LevelTopology.CountFaceConnectedComponents(level.Blocks), Is.EqualTo(1));
		}

		[Test]
		public void Phase1Prototype_DocumentedSolutionStillCompletes()
		{
			var state = Phase1PrototypeLevel.Create().CreateState();
			foreach (var id in Phase1PrototypeLevel.DocumentedSolution)
			{
				var result = state.TryRemove(new BlockId(id));
				Assert.That(result.Status, Is.EqualTo(MoveStatus.Allowed), "block " + id);
			}

			Assert.That(state.IsComplete, Is.True);
		}
	}
}
