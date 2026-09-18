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
	}
}
