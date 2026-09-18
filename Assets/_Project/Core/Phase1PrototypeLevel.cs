namespace TapAway.Core
{
	/// <summary>
	/// Intentionally designed Phase 1 prototype level (~12 blocks).
	/// Demonstrates free moves, blocked moves, dependencies, and all six directions.
	/// Grid positions form a single 6-neighbor FACE-connected component
	/// (validated by <see cref="LevelTopology"/>).
	/// Mid-solution layout notes:
	/// - Block 5 at (2,1,1) stays face-joined to the (2,*,*) cluster (avoids the
	///   old (0,1,0) orphan that looked edge-only after early removals).
	/// - Blocks 7 and 11 attach to the lasting X-chain (via block 2) so removing
	///   block 1 does not leave diagonal-looking dangling cubes.
	/// </summary>
	public static class Phase1PrototypeLevel
	{
		public const string LevelId = "phase1_prototype";

		/// <summary>
		/// Documented valid solution by BlockId values.
		/// Verified by EditMode tests — do not reorder without updating tests.
		/// </summary>
		public static readonly int[] DocumentedSolution =
		{
			1,  // (0,0,0) -X free
			6,  // (1,1,0) +Y free
			7,  // (1,0,1) +Z free
			8,  // (1,0,-1) -Z free
			9,  // (3,0,0) +X free
			11, // (1,-1,0) -Y free
			2,  // (1,0,0) -X now free (1 removed)
			12, // (2,0,-1) -X now free (8 removed)
			3,  // (2,0,0) -X now free (2 removed)
			4,  // (2,0,1) -Z now free (3 removed)
			5,  // (2,1,1) +Y free (stays face-joined to 10 until late)
			10  // (2,1,0) +X free
		};

		public static PuzzleLevel Create()
		{
			var blocks = new[]
			{
				// X-chain dependency: 3 blocked by 2 blocked by 1
				new PuzzleBlock(1, 0, 0, 0, EscapeDirection.NegX),
				new PuzzleBlock(2, 1, 0, 0, EscapeDirection.NegX),
				new PuzzleBlock(3, 2, 0, 0, EscapeDirection.NegX),

				// Z dependency on the chain tip
				new PuzzleBlock(4, 2, 0, 1, EscapeDirection.NegZ),

				// Stays attached to the (2,*,*) cluster through mid/late game.
				new PuzzleBlock(5, 2, 1, 1, EscapeDirection.PosY),

				// Free escapes covering remaining directions
				new PuzzleBlock(6, 1, 1, 0, EscapeDirection.PosY),
				// Face-joined to block 2 (and 4) so removing 1 does not orphan it.
				new PuzzleBlock(7, 1, 0, 1, EscapeDirection.PosZ),
				new PuzzleBlock(8, 1, 0, -1, EscapeDirection.NegZ),
				new PuzzleBlock(9, 3, 0, 0, EscapeDirection.PosX),
				new PuzzleBlock(10, 2, 1, 0, EscapeDirection.PosX),
				// Face-joined to block 2 until 11 itself escapes.
				new PuzzleBlock(11, 1, -1, 0, EscapeDirection.NegY),

				// Blocked by 8 until 8 escapes
				new PuzzleBlock(12, 2, 0, -1, EscapeDirection.NegX)
			};

			return new PuzzleLevel(
				LevelId,
				"Phase 1 Prototype",
				blocks,
				DocumentedSolution);
		}
	}
}
