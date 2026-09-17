using System.Collections.Generic;

namespace TapAway.Core
{
	/// <summary>
	/// Immutable authored level data (blocks + metadata).
	/// </summary>
	public sealed class PuzzleLevel
	{
		public readonly string Id;
		public readonly string DisplayName;
		public readonly IReadOnlyList<PuzzleBlock> Blocks;
		public readonly IReadOnlyList<int> DocumentedSolutionIds;

		public PuzzleLevel(
			string id,
			string displayName,
			IReadOnlyList<PuzzleBlock> blocks,
			IReadOnlyList<int> documentedSolutionIds)
		{
			Id = id;
			DisplayName = displayName;
			Blocks = blocks;
			DocumentedSolutionIds = documentedSolutionIds;
		}

		public PuzzleState CreateState()
		{
			return new PuzzleState(Blocks);
		}
	}
}
