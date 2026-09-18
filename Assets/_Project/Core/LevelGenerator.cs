using System.Collections.Generic;

namespace TapAway.Core
{
	/// <summary>
	/// Solution-by-construction reverse generator.
	/// Prepends free face-adjacent blocks so a known forward removal order exists,
	/// then relies on independent <see cref="PuzzleSolver"/> validation.
	/// </summary>
	public static class LevelGenerator
	{
		private static readonly EscapeDirection[] AllDirections =
		{
			EscapeDirection.PosX,
			EscapeDirection.NegX,
			EscapeDirection.PosY,
			EscapeDirection.NegY,
			EscapeDirection.PosZ,
			EscapeDirection.NegZ
		};

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
		/// Builds one candidate from seed. Does not run full quality/difficulty pipeline.
		/// </summary>
		public static bool TryConstruct(
			int seed,
			GeneratorConfig config,
			out List<PuzzleBlock> blocks,
			out List<int> constructedSolution,
			out string failure)
		{
			config = config ?? GeneratorConfig.Small();
			blocks = new List<PuzzleBlock>();
			constructedSolution = new List<int>();
			failure = null;

			var rng = new DeterministicRng(seed);
			var occupied = new HashSet<GridPosition>();
			var target = config.TargetBlockCount;
			if (target <= 0)
			{
				failure = "TargetBlockCount must be positive";
				return false;
			}

			// Seed the structure with one free block at origin-ish.
			var origin = new GridPosition(0, 0, 0);
			if (!InBounds(origin, config))
			{
				origin = new GridPosition(config.MinX, config.MinY, config.MinZ);
			}

			var firstDir = AllDirections[rng.NextIndex(AllDirections.Length)];
			var nextId = 1;
			blocks.Add(new PuzzleBlock(nextId, origin.X, origin.Y, origin.Z, firstDir));
			occupied.Add(origin);
			constructedSolution.Add(nextId);
			nextId++;

			var attempts = 0;
			while (blocks.Count < target && attempts < config.MaxConstructionAttempts)
			{
				attempts++;
				if (!TryPrependBlock(rng, config, blocks, occupied, constructedSolution, ref nextId))
				{
					continue;
				}
			}

			if (blocks.Count < target)
			{
				failure = "Construction stopped at " + blocks.Count + "/" + target +
				          " after " + attempts + " attempts";
				return false;
			}

			// Renumber so ascending BlockId matches the constructed removal order.
			// Independent lowest-Id greedy solving then peels the same onion layers,
			// keeping intermediate topology aligned with construction intent.
			RenumberBySolutionOrder(blocks, constructedSolution);
			return true;
		}

		/// <summary>
		/// Rewrites ids so constructedSolution becomes 1..N in order.
		/// </summary>
		private static void RenumberBySolutionOrder(
			List<PuzzleBlock> blocks,
			List<int> constructedSolution)
		{
			var byOldId = new Dictionary<int, PuzzleBlock>();
			for (var i = 0; i < blocks.Count; i++)
			{
				byOldId[blocks[i].Id.Value] = blocks[i];
			}

			var renumbered = new List<PuzzleBlock>(blocks.Count);
			var newSolution = new List<int>(constructedSolution.Count);
			for (var i = 0; i < constructedSolution.Count; i++)
			{
				var oldId = constructedSolution[i];
				var old = byOldId[oldId];
				var newId = i + 1;
				renumbered.Add(new PuzzleBlock(
					newId,
					old.Position.X,
					old.Position.Y,
					old.Position.Z,
					old.EscapeDirection));
				newSolution.Add(newId);
			}

			blocks.Clear();
			blocks.AddRange(renumbered);
			constructedSolution.Clear();
			constructedSolution.AddRange(newSolution);
		}

		/// <summary>
		/// Prepends a new free block (becomes new first removal) face-adjacent to the mass.
		/// </summary>
		private static bool TryPrependBlock(
			DeterministicRng rng,
			GeneratorConfig config,
			List<PuzzleBlock> blocks,
			HashSet<GridPosition> occupied,
			List<int> constructedSolution,
			ref int nextId)
		{
			// Candidate cells: face neighbors of existing blocks inside bounds.
			var candidates = new List<GridPosition>();
			var seen = new HashSet<GridPosition>();
			for (var i = 0; i < blocks.Count; i++)
			{
				var p = blocks[i].Position;
				for (var o = 0; o < FaceOffsets.Length; o++)
				{
					var n = p.Offset(FaceOffsets[o].X, FaceOffsets[o].Y, FaceOffsets[o].Z);
					if (!InBounds(n, config) || occupied.Contains(n) || !seen.Add(n))
					{
						continue;
					}

					candidates.Add(n);
				}
			}

			if (candidates.Count == 0)
			{
				return false;
			}

			// Try a deterministic random subset of candidate cells.
			var tries = System.Math.Min(12, candidates.Count);
			for (var t = 0; t < tries; t++)
			{
				var cell = candidates[rng.NextIndex(candidates.Count)];
				var dirOrder = (EscapeDirection[])AllDirections.Clone();
				for (var d = dirOrder.Length - 1; d > 0; d--)
				{
					var j = rng.NextIndex(d + 1);
					var tmp = dirOrder[d];
					dirOrder[d] = dirOrder[j];
					dirOrder[j] = tmp;
				}

				for (var d = 0; d < dirOrder.Length; d++)
				{
					var dir = dirOrder[d];
					if (!IsRayClear(cell, dir, occupied))
					{
						continue;
					}

					var id = nextId;
					var block = new PuzzleBlock(id, cell.X, cell.Y, cell.Z, dir);
					blocks.Add(block);
					occupied.Add(cell);
					constructedSolution.Insert(0, id);
					nextId++;
					return true;
				}
			}

			return false;
		}

		private static bool IsRayClear(
			GridPosition from,
			EscapeDirection direction,
			HashSet<GridPosition> occupied)
		{
			// Mirror PuzzleRules: any occupied cell on the escape axis blocks.
			foreach (var cell in occupied)
			{
				switch (direction)
				{
					case EscapeDirection.PosX:
						if (cell.Y == from.Y && cell.Z == from.Z && cell.X > from.X) return false;
						break;
					case EscapeDirection.NegX:
						if (cell.Y == from.Y && cell.Z == from.Z && cell.X < from.X) return false;
						break;
					case EscapeDirection.PosY:
						if (cell.X == from.X && cell.Z == from.Z && cell.Y > from.Y) return false;
						break;
					case EscapeDirection.NegY:
						if (cell.X == from.X && cell.Z == from.Z && cell.Y < from.Y) return false;
						break;
					case EscapeDirection.PosZ:
						if (cell.X == from.X && cell.Y == from.Y && cell.Z > from.Z) return false;
						break;
					case EscapeDirection.NegZ:
						if (cell.X == from.X && cell.Y == from.Y && cell.Z < from.Z) return false;
						break;
				}
			}

			return true;
		}

		private static bool InBounds(GridPosition p, GeneratorConfig config)
		{
			return p.X >= config.MinX && p.X <= config.MaxX
			       && p.Y >= config.MinY && p.Y <= config.MaxY
			       && p.Z >= config.MinZ && p.Z <= config.MaxZ;
		}
	}
}
