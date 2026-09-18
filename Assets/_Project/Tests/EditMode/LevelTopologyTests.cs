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

		[Test]
		public void Phase1Prototype_NoPrematureSingletonAlongDocumentedSolution()
		{
			var level = Phase1PrototypeLevel.Create();
			Assert.That(
				LevelTopology.HasPrematureSingletonIsland(
					level.Blocks,
					Phase1PrototypeLevel.DocumentedSolution,
					singletonAllowedWhenActiveAtMost: 1),
				Is.False,
				"Mid-solution singleton islands look like dangling cubes");
		}

		[Test]
		public void Phase1Prototype_NoDiagonalAttachmentIslandAlongDocumentedSolution()
		{
			var level = Phase1PrototypeLevel.Create();
			Assert.That(
				LevelTopology.HasDiagonalAttachmentIsland(
					level.Blocks,
					Phase1PrototypeLevel.DocumentedSolution,
					allowedWhenActiveAtMost: 1),
				Is.False,
				"Edge/corner-only visual attachment must not appear mid-solution");
		}

		[Test]
		public void Phase1Prototype_LateGameFiveBlocks_RemainOneFaceComponent()
		{
			// Known QA state after 1 → 6 → 7 → 8 → 9 → 11 → 2
			var state = Phase1PrototypeLevel.Create().CreateState();
			var prefix = new[] { 1, 6, 7, 8, 9, 11, 2 };
			foreach (var id in prefix)
			{
				Assert.That(state.TryRemove(new BlockId(id)).Status, Is.EqualTo(MoveStatus.Allowed));
			}

			var active = state.GetActiveBlocks();
			Assert.That(active.Count, Is.EqualTo(5));
			Assert.That(LevelTopology.CountFaceConnectedComponents(active), Is.EqualTo(1));
			Assert.That(LevelTopology.CountSingletonComponents(active), Is.EqualTo(0));
			Assert.That(LevelTopology.CountDiagonalAttachmentSingletons(active), Is.EqualTo(0));

			var ids = new System.Collections.Generic.HashSet<int>();
			foreach (var block in active)
			{
				ids.Add(block.Id.Value);
			}

			Assert.That(ids, Does.Contain(5));
			Assert.That(ids, Does.Contain(3));
			Assert.That(ids, Does.Contain(4));
			Assert.That(ids, Does.Contain(10));
			Assert.That(ids, Does.Contain(12));

			// Block 5 must remain face-adjacent to the cluster (not an edge island).
			PuzzleBlock block5 = default;
			foreach (var block in active)
			{
				if (block.Id.Value == 5)
				{
					block5 = block;
					break;
				}
			}

			Assert.That(block5.Position, Is.EqualTo(new GridPosition(2, 1, 1)));
			var faceJoined = false;
			foreach (var block in active)
			{
				if (block.Id.Value == 5)
				{
					continue;
				}

				if (LevelTopology.AreFaceAdjacent(block5.Position, block.Position))
				{
					faceJoined = true;
					break;
				}
			}

			Assert.That(faceJoined, Is.True, "Block 5 must stay face-connected in the late-game five");
		}

		[Test]
		public void PrematureSingleton_DetectedForOrphanMidSolution()
		{
			// Mirrors the old Block 5 @ (0,1,0) failure mode: after removing its
			// only face neighbors, a singleton remains while many blocks are active.
			var blocks = new[]
			{
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.NegX),
				new PuzzleBlock(5, 0, 1, 0, EscapeDirection.NegY),
				new PuzzleBlock(6, 1, 1, 0, EscapeDirection.PosY),
				new PuzzleBlock(2, 1, 0, 0, EscapeDirection.NegX)
			};
			var solution = new[] { 1, 6 };
			Assert.That(
				LevelTopology.HasPrematureSingletonIsland(blocks, solution, 1),
				Is.True);
			Assert.That(
				LevelTopology.HasDiagonalAttachmentIsland(blocks, solution, 1),
				Is.True,
				"Orphan at (0,1,0) is edge-adjacent to (1,0,0)");
		}

		[Test]
		public void EdgeOrCornerAdjacent_DetectsDiagonalTouch()
		{
			Assert.That(
				LevelTopology.AreEdgeOrCornerAdjacent(new GridPosition(0, 1, 0), new GridPosition(1, 0, 0)),
				Is.True);
			Assert.That(
				LevelTopology.AreEdgeOrCornerAdjacent(new GridPosition(0, 0, 0), new GridPosition(1, 0, 0)),
				Is.False);
		}
	}
}
