namespace TapAway.Core
{
	/// <summary>
	/// Outcome of attempting to remove a block via its escape ray.
	/// Core is the authority; presentation only visualizes this result.
	/// </summary>
	public enum MoveStatus
	{
		/// <summary>Escape path is clear; block was removed from active set.</summary>
		Allowed = 0,

		/// <summary>Another active block occupies the escape ray.</summary>
		Blocked = 1,

		/// <summary>Requested BlockId is not part of this puzzle.</summary>
		UnknownBlock = 2,

		/// <summary>Block exists but was already removed.</summary>
		AlreadyRemoved = 3
	}

	/// <summary>
	/// Structured move result returned by <see cref="PuzzleState.TryRemove"/>.
	/// </summary>
	public readonly struct MoveResult
	{
		public readonly MoveStatus Status;
		public readonly BlockId BlockId;
		public readonly BlockId BlockingBlockId;

		public bool IsAllowed => Status == MoveStatus.Allowed;
		public bool IsBlocked => Status == MoveStatus.Blocked;

		public MoveResult(MoveStatus status, BlockId blockId, BlockId blockingBlockId = default)
		{
			Status = status;
			BlockId = blockId;
			BlockingBlockId = blockingBlockId;
		}

		public static MoveResult Allowed(BlockId id)
		{
			return new MoveResult(MoveStatus.Allowed, id);
		}

		public static MoveResult Blocked(BlockId id, BlockId by)
		{
			return new MoveResult(MoveStatus.Blocked, id, by);
		}

		public static MoveResult Unknown(BlockId id)
		{
			return new MoveResult(MoveStatus.UnknownBlock, id);
		}

		public static MoveResult AlreadyRemoved(BlockId id)
		{
			return new MoveResult(MoveStatus.AlreadyRemoved, id);
		}

		public override string ToString()
		{
			if (Status == MoveStatus.Blocked)
			{
				return Status + " " + BlockId + " by " + BlockingBlockId;
			}

			return Status + " " + BlockId;
		}
	}
}
