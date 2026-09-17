using System.Collections.Generic;

namespace TapAway.Core
{
	/// <summary>
	/// Pure rule evaluation for Tap Away escape rays.
	/// Authority for legality; never depends on Unity physics/colliders.
	/// </summary>
	public static class PuzzleRules
	{
		/// <summary>
		/// Evaluates whether <paramref name="blockId"/> can escape given active occupancy.
		/// </summary>
		/// <param name="blockId">Candidate block.</param>
		/// <param name="definition">Block definition (position + direction).</param>
		/// <param name="occupancy">Map of grid cell → active BlockId.</param>
		/// <param name="blockingId">Set when blocked by another active block.</param>
		public static MoveStatus EvaluateEscape(
			BlockId blockId,
			PuzzleBlock definition,
			IReadOnlyDictionary<GridPosition, BlockId> occupancy,
			out BlockId blockingId)
		{
			blockingId = default;
			var bestDistance = long.MaxValue;

			foreach (var pair in occupancy)
			{
				var position = pair.Key;
				long distance;
				switch (definition.EscapeDirection)
				{
					case EscapeDirection.PosX:
						if (position.Y != definition.Position.Y || position.Z != definition.Position.Z || position.X <= definition.Position.X)
							continue;
						distance = (long)position.X - definition.Position.X;
						break;
					case EscapeDirection.NegX:
						if (position.Y != definition.Position.Y || position.Z != definition.Position.Z || position.X >= definition.Position.X)
							continue;
						distance = (long)definition.Position.X - position.X;
						break;
					case EscapeDirection.PosY:
						if (position.X != definition.Position.X || position.Z != definition.Position.Z || position.Y <= definition.Position.Y)
							continue;
						distance = (long)position.Y - definition.Position.Y;
						break;
					case EscapeDirection.NegY:
						if (position.X != definition.Position.X || position.Z != definition.Position.Z || position.Y >= definition.Position.Y)
							continue;
						distance = (long)definition.Position.Y - position.Y;
						break;
					case EscapeDirection.PosZ:
						if (position.X != definition.Position.X || position.Y != definition.Position.Y || position.Z <= definition.Position.Z)
							continue;
						distance = (long)position.Z - definition.Position.Z;
						break;
					case EscapeDirection.NegZ:
						if (position.X != definition.Position.X || position.Y != definition.Position.Y || position.Z >= definition.Position.Z)
							continue;
						distance = (long)definition.Position.Z - position.Z;
						break;
					default:
						continue;
				}

				if (distance < bestDistance)
				{
					bestDistance = distance;
					blockingId = pair.Value;
				}
			}

			return bestDistance == long.MaxValue ? MoveStatus.Allowed : MoveStatus.Blocked;
		}
	}
}
