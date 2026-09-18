using System.Collections.Generic;

namespace TapAway.Core
{
	/// <summary>
	/// Level-quality helpers for face-adjacency topology.
	/// Not a Core gameplay law — validators for authored levels.
	/// </summary>
	public static class LevelTopology
	{
		private static readonly GridPosition[] FaceOffsets =
		{
			new GridPosition(1, 0, 0),
			new GridPosition(-1, 0, 0),
			new GridPosition(0, 1, 0),
			new GridPosition(0, -1, 0),
			new GridPosition(0, 0, 1),
			new GridPosition(0, 0, -1)
		};

		/// <summary>
		/// Snapshot of face-connectivity after a solution prefix.
		/// </summary>
		public readonly struct SolutionStateReport
		{
			public readonly int StepIndex;
			public readonly int ActiveCount;
			public readonly int ComponentCount;
			public readonly int SingletonComponentCount;
			public readonly int DiagonalAttachmentSingletonCount;

			public SolutionStateReport(
				int stepIndex,
				int activeCount,
				int componentCount,
				int singletonComponentCount,
				int diagonalAttachmentSingletonCount = 0)
			{
				StepIndex = stepIndex;
				ActiveCount = activeCount;
				ComponentCount = componentCount;
				SingletonComponentCount = singletonComponentCount;
				DiagonalAttachmentSingletonCount = diagonalAttachmentSingletonCount;
			}
		}

		/// <summary>
		/// True when two cells share a full face (Manhattan distance 1 on one axis).
		/// </summary>
		public static bool AreFaceAdjacent(GridPosition a, GridPosition b)
		{
			var dx = a.X == b.X ? 0 : 1;
			var dy = a.Y == b.Y ? 0 : 1;
			var dz = a.Z == b.Z ? 0 : 1;
			if (dx + dy + dz != 1)
			{
				return false;
			}

			if (dx == 1)
			{
				return System.Math.Abs(a.X - b.X) == 1;
			}

			if (dy == 1)
			{
				return System.Math.Abs(a.Y - b.Y) == 1;
			}

			return System.Math.Abs(a.Z - b.Z) == 1;
		}

		/// <summary>
		/// Counts connected components using 6-neighbor face adjacency.
		/// </summary>
		public static int CountFaceConnectedComponents(IReadOnlyList<PuzzleBlock> blocks)
		{
			if (blocks == null || blocks.Count == 0)
			{
				return 0;
			}

			var byPos = new Dictionary<GridPosition, int>();
			for (var i = 0; i < blocks.Count; i++)
			{
				byPos[blocks[i].Position] = i;
			}

			var visited = new bool[blocks.Count];
			var components = 0;
			var queue = new Queue<int>();

			for (var start = 0; start < blocks.Count; start++)
			{
				if (visited[start])
				{
					continue;
				}

				components++;
				visited[start] = true;
				queue.Enqueue(start);
				while (queue.Count > 0)
				{
					var index = queue.Dequeue();
					var pos = blocks[index].Position;
					for (var o = 0; o < FaceOffsets.Length; o++)
					{
						var neighbor = pos.Offset(FaceOffsets[o].X, FaceOffsets[o].Y, FaceOffsets[o].Z);
						if (!byPos.TryGetValue(neighbor, out var ni) || visited[ni])
						{
							continue;
						}

						visited[ni] = true;
						queue.Enqueue(ni);
					}
				}
			}

			return components;
		}

		public static bool IsSingleFaceConnectedComponent(IReadOnlyList<PuzzleBlock> blocks)
		{
			return CountFaceConnectedComponents(blocks) == 1;
		}

		/// <summary>
		/// Counts components that contain exactly one block.
		/// </summary>
		public static int CountSingletonComponents(IReadOnlyList<PuzzleBlock> blocks)
		{
			if (blocks == null || blocks.Count == 0)
			{
				return 0;
			}

			var byPos = new Dictionary<GridPosition, int>();
			for (var i = 0; i < blocks.Count; i++)
			{
				byPos[blocks[i].Position] = i;
			}

			var visited = new bool[blocks.Count];
			var singletons = 0;
			var queue = new Queue<int>();

			for (var start = 0; start < blocks.Count; start++)
			{
				if (visited[start])
				{
					continue;
				}

				visited[start] = true;
				queue.Enqueue(start);
				var size = 0;
				while (queue.Count > 0)
				{
					var index = queue.Dequeue();
					size++;
					var pos = blocks[index].Position;
					for (var o = 0; o < FaceOffsets.Length; o++)
					{
						var neighbor = pos.Offset(FaceOffsets[o].X, FaceOffsets[o].Y, FaceOffsets[o].Z);
						if (!byPos.TryGetValue(neighbor, out var ni) || visited[ni])
						{
							continue;
						}

						visited[ni] = true;
						queue.Enqueue(ni);
					}
				}

				if (size == 1)
				{
					singletons++;
				}
			}

			return singletons;
		}

		/// <summary>
		/// Walks a documented solution and reports connectivity after each removal.
		/// </summary>
		public static List<SolutionStateReport> AnalyzeSolutionStates(
			IReadOnlyList<PuzzleBlock> initialBlocks,
			IReadOnlyList<int> solutionIds)
		{
			var reports = new List<SolutionStateReport>();
			var state = new PuzzleState(initialBlocks);
			reports.Add(MakeReport(-1, state));

			for (var i = 0; i < solutionIds.Count; i++)
			{
				var result = state.TryRemove(new BlockId(solutionIds[i]));
				if (result.Status != MoveStatus.Allowed)
				{
					break;
				}

				reports.Add(MakeReport(i, state));
			}

			return reports;
		}

		/// <summary>
		/// True when two cells touch on an edge or corner only (not a full face),
		/// within the 3x3x3 Moore neighborhood.
		/// </summary>
		public static bool AreEdgeOrCornerAdjacent(GridPosition a, GridPosition b)
		{
			var dx = System.Math.Abs(a.X - b.X);
			var dy = System.Math.Abs(a.Y - b.Y);
			var dz = System.Math.Abs(a.Z - b.Z);
			if (dx > 1 || dy > 1 || dz > 1)
			{
				return false;
			}

			var axes = (dx > 0 ? 1 : 0) + (dy > 0 ? 1 : 0) + (dz > 0 ? 1 : 0);
			return axes >= 2;
		}

		/// <summary>
		/// Counts singleton blocks that are edge/corner-adjacent to another active
		/// block — the "touches only by an edge" visual failure mode.
		/// </summary>
		public static int CountDiagonalAttachmentSingletons(IReadOnlyList<PuzzleBlock> blocks)
		{
			if (blocks == null || blocks.Count < 2)
			{
				return 0;
			}

			var componentOf = new int[blocks.Count];
			var sizes = BuildComponentMap(blocks, componentOf);
			var count = 0;
			for (var i = 0; i < blocks.Count; i++)
			{
				if (sizes[componentOf[i]] != 1)
				{
					continue;
				}

				for (var j = 0; j < blocks.Count; j++)
				{
					if (i == j)
					{
						continue;
					}

					if (AreEdgeOrCornerAdjacent(blocks[i].Position, blocks[j].Position))
					{
						count++;
						break;
					}
				}
			}

			return count;
		}

		/// <summary>
		/// True when a diagonal-looking singleton island appears while more than
		/// <paramref name="allowedWhenActiveAtMost"/> blocks remain.
		/// </summary>
		public static bool HasDiagonalAttachmentIsland(
			IReadOnlyList<PuzzleBlock> initialBlocks,
			IReadOnlyList<int> solutionIds,
			int allowedWhenActiveAtMost = 1)
		{
			var reports = AnalyzeSolutionStates(initialBlocks, solutionIds);
			for (var i = 0; i < reports.Count; i++)
			{
				var report = reports[i];
				if (report.ActiveCount <= allowedWhenActiveAtMost)
				{
					continue;
				}

				if (report.DiagonalAttachmentSingletonCount > 0)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// True when a singleton island appears while more than
		/// <paramref name="singletonAllowedWhenActiveAtMost"/> blocks remain.
		/// Endgame single blocks are allowed.
		/// </summary>
		public static bool HasPrematureSingletonIsland(
			IReadOnlyList<PuzzleBlock> initialBlocks,
			IReadOnlyList<int> solutionIds,
			int singletonAllowedWhenActiveAtMost = 1)
		{
			var reports = AnalyzeSolutionStates(initialBlocks, solutionIds);
			for (var i = 0; i < reports.Count; i++)
			{
				var report = reports[i];
				if (report.ActiveCount <= singletonAllowedWhenActiveAtMost)
				{
					continue;
				}

				if (report.SingletonComponentCount > 0)
				{
					return true;
				}
			}

			return false;
		}

		private static SolutionStateReport MakeReport(int stepIndex, PuzzleState state)
		{
			var active = state.GetActiveBlocks();
			return new SolutionStateReport(
				stepIndex,
				active.Count,
				CountFaceConnectedComponents(active),
				CountSingletonComponents(active),
				CountDiagonalAttachmentSingletons(active));
		}

		private static int[] BuildComponentMap(IReadOnlyList<PuzzleBlock> blocks, int[] componentOf)
		{
			var byPos = new Dictionary<GridPosition, int>();
			for (var i = 0; i < blocks.Count; i++)
			{
				byPos[blocks[i].Position] = i;
			}

			var visited = new bool[blocks.Count];
			var sizes = new int[blocks.Count];
			var queue = new Queue<int>();
			var componentId = 0;

			for (var start = 0; start < blocks.Count; start++)
			{
				if (visited[start])
				{
					continue;
				}

				visited[start] = true;
				queue.Enqueue(start);
				var members = new List<int>();
				while (queue.Count > 0)
				{
					var index = queue.Dequeue();
					members.Add(index);
					componentOf[index] = componentId;
					var pos = blocks[index].Position;
					for (var o = 0; o < FaceOffsets.Length; o++)
					{
						var neighbor = pos.Offset(FaceOffsets[o].X, FaceOffsets[o].Y, FaceOffsets[o].Z);
						if (!byPos.TryGetValue(neighbor, out var ni) || visited[ni])
						{
							continue;
						}

						visited[ni] = true;
						queue.Enqueue(ni);
					}
				}

				sizes[componentId] = members.Count;
				componentId++;
			}

			return sizes;
		}
	}
}
