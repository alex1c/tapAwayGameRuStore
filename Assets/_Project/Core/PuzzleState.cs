using System;
using System.Collections.Generic;

namespace TapAway.Core
{
	/// <summary>
	/// Mutable authoritative puzzle runtime state.
	/// Presentation mirrors this; it never owns legality.
	/// </summary>
	public sealed class PuzzleState
	{
		private readonly Dictionary<BlockId, PuzzleBlock> _definitions;
		private readonly HashSet<BlockId> _active;
		private readonly Dictionary<GridPosition, BlockId> _occupancy;

		public PuzzleState(IEnumerable<PuzzleBlock> blocks)
		{
			if (blocks == null)
			{
				throw new ArgumentNullException(nameof(blocks));
			}

			_definitions = new Dictionary<BlockId, PuzzleBlock>();
			_active = new HashSet<BlockId>();
			_occupancy = new Dictionary<GridPosition, BlockId>();

			foreach (var block in blocks)
			{
				if (_definitions.ContainsKey(block.Id))
				{
					throw new ArgumentException("Duplicate BlockId: " + block.Id, nameof(blocks));
				}

				if (_occupancy.ContainsKey(block.Position))
				{
					throw new ArgumentException("Duplicate grid position: " + block.Position, nameof(blocks));
				}

				_definitions.Add(block.Id, block);
				_active.Add(block.Id);
				_occupancy.Add(block.Position, block.Id);
			}
		}

		/// <summary>Number of blocks still present in the construction.</summary>
		public int ActiveCount => _active.Count;

		/// <summary>True when every block has been removed.</summary>
		public bool IsComplete => _active.Count == 0;

		/// <summary>Total blocks defined by the level (active + removed).</summary>
		public int DefinedCount => _definitions.Count;

		public bool Contains(BlockId id)
		{
			return _definitions.ContainsKey(id);
		}

		public bool IsActive(BlockId id)
		{
			return _active.Contains(id);
		}

		public bool TryGetBlock(BlockId id, out PuzzleBlock block)
		{
			return _definitions.TryGetValue(id, out block);
		}

		/// <summary>
		/// Returns a snapshot of active block definitions for presentation rebuilds.
		/// </summary>
		public IReadOnlyList<PuzzleBlock> GetActiveBlocks()
		{
			var list = new List<PuzzleBlock>(_active.Count);
			foreach (var id in _active)
			{
				list.Add(_definitions[id]);
			}

			return list;
		}

		/// <summary>
		/// Attempts to remove a block if its escape ray is clear.
		/// </summary>
		public MoveResult TryRemove(BlockId id)
		{
			if (!_definitions.TryGetValue(id, out var definition))
			{
				return MoveResult.Unknown(id);
			}

			if (!_active.Contains(id))
			{
				return MoveResult.AlreadyRemoved(id);
			}

			var status = PuzzleRules.EvaluateEscape(id, definition, _occupancy, out var blockingId);
			if (status == MoveStatus.Blocked)
			{
				return MoveResult.Blocked(id, blockingId);
			}

			_active.Remove(id);
			_occupancy.Remove(definition.Position);
			return MoveResult.Allowed(id);
		}

		/// <summary>
		/// Creates a fresh state from the original level definitions.
		/// </summary>
		public PuzzleState CloneInitial()
		{
			return new PuzzleState(_definitions.Values);
		}
	}
}
