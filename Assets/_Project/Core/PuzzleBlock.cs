namespace TapAway.Core
{
	/// <summary>
	/// Immutable definition of a block placement inside a level.
	/// Runtime active/removed state lives in <see cref="PuzzleState"/>.
	/// </summary>
	public readonly struct PuzzleBlock
	{
		public readonly BlockId Id;
		public readonly GridPosition Position;
		public readonly EscapeDirection EscapeDirection;

		public PuzzleBlock(BlockId id, GridPosition position, EscapeDirection escapeDirection)
		{
			Id = id;
			Position = position;
			EscapeDirection = escapeDirection;
		}

		public PuzzleBlock(int id, int x, int y, int z, EscapeDirection escapeDirection)
			: this(new BlockId(id), new GridPosition(x, y, z), escapeDirection)
		{
		}
	}
}
