namespace TapAway.Core
{
	/// <summary>
	/// Intentionally designed Phase 1 prototype level (~12 blocks).
	/// Demonstrates free moves, blocked moves, dependencies, and all six directions.
	/// Grid positions form a single 6-neighbor FACE-connected component
	/// (validated by <see cref="LevelTopology"/>).
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
			7,  // (0,0,1) +Z free
			8,  // (1,0,-1) -Z free
			9,  // (3,0,0) +X free
			11, // (0,-1,0) -Y free
			2,  // (1,0,0) -X now free (1 removed)
			12, // (2,0,-1) -X now free (8 removed)
			3,  // (2,0,0) -X now free (2 removed)
			4,  // (2,0,1) -Z now free (3 removed)
			5,  // (0,1,0) -Y now free (1 removed)
			10  // (2,1,0) +X free (was always free but ordered late for clarity)
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

				// Y dependency on block 1
				new PuzzleBlock(5, 0, 1, 0, EscapeDirection.NegY),

				// Free escapes covering remaining directions
				new PuzzleBlock(6, 1, 1, 0, EscapeDirection.PosY),
				new PuzzleBlock(7, 0, 0, 1, EscapeDirection.PosZ),
				new PuzzleBlock(8, 1, 0, -1, EscapeDirection.NegZ),
				new PuzzleBlock(9, 3, 0, 0, EscapeDirection.PosX),
				new PuzzleBlock(10, 2, 1, 0, EscapeDirection.PosX),
				new PuzzleBlock(11, 0, -1, 0, EscapeDirection.NegY),

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
