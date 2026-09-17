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
		/// Maximum ray steps when scanning for blockers.
		/// Bounds are small for Phase 1 levels; generous headroom for safety.
		/// </summary>
		public const int MaxRaySteps = 64;

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

			EscapeDirectionUtil.GetStep(definition.EscapeDirection, out var dx, out var dy, out var dz);
			var cell = definition.Position.Offset(dx, dy, dz);

			for (var step = 0; step < MaxRaySteps; step++)
			{
				if (occupancy.TryGetValue(cell, out var occupant))
				{
					// Occupant on the escape ray always blocks; never the mover itself
					// because the ray starts on the adjacent cell.
					blockingId = occupant;
					return MoveStatus.Blocked;
				}

				cell = cell.Offset(dx, dy, dz);
			}

			return MoveStatus.Allowed;
		}
	}
}
