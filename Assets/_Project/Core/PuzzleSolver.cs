using System;
using System.Collections.Generic;

namespace TapAway.Core
{
	/// <summary>
	/// Deterministic Tap Away solver operating only on Core block definitions.
	/// Exploits monotonic removal: legal removals never re-block a clear ray,
	/// so lowest-BlockId greedy search is a complete solvability decision procedure.
	/// </summary>
	public static class PuzzleSolver
	{
		/// <summary>
		/// Under current ray rules, removing a block only clears occupancy.
		/// Therefore any legal removal from a solvable state leaves a solvable state.
		/// </summary>
		public const string MonotonicInvariant =
			"Legal removals only clear cells; they cannot place new ray blockers. " +
			"Any legal move from a solvable state preserves solvability. " +
			"Therefore lowest-BlockId greedy search decides solvability without backtracking.";

		public static SolverResult Solve(PuzzleLevel level, SolverConfig config = null)
		{
			if (level == null)
			{
				return Invalid("level is null");
			}

			return Solve(level.Blocks, config);
		}

		public static SolverResult Solve(IReadOnlyList<PuzzleBlock> blocks, SolverConfig config = null)
		{
			config = config ?? new SolverConfig();
			if (blocks == null)
			{
				return Invalid("blocks is null");
			}

			if (blocks.Count == 0)
			{
				return new SolverResult
				{
					Status = SolverStatus.Empty,
					Solution = Array.Empty<int>(),
					LegalMovesAtStart = Array.Empty<int>(),
					LegalMoveCountsAlongSolution = Array.Empty<int>()
				};
			}

			if (!TryNormalize(blocks, out var ordered, out var error))
			{
				return Invalid(error);
			}

			var model = new SolverModel(ordered);
			return SolveGreedy(model, ordered, config);
		}

		/// <summary>
		/// Samples up to <paramref name="maxAlternates"/> additional solutions by
		/// taking the 2nd/3rd/... legal move at the first choice point, then greedy.
		/// Deterministic given stable BlockId ordering.
		/// </summary>
		public static List<IReadOnlyList<int>> SampleAlternateSolutions(
			IReadOnlyList<PuzzleBlock> blocks,
			int maxAlternates)
		{
			var results = new List<IReadOnlyList<int>>();
			if (blocks == null || blocks.Count == 0 || maxAlternates <= 0)
			{
				return results;
			}

			if (!TryNormalize(blocks, out var ordered, out _))
			{
				return results;
			}

			var model = new SolverModel(ordered);
			var full = ActiveMask.AllActive(ordered.Length);
			var legal = model.ListLegalIndices(full);
			if (legal.Count <= 1)
			{
				return results;
			}

			var limit = Math.Min(maxAlternates, legal.Count - 1);
			for (var sample = 0; sample < limit; sample++)
			{
				var mask = full.Clone();
				var solution = new List<int>(ordered.Length);
				// Spread a bounded sample across the complete first-move choice set.
				// The old contiguous 2nd/3rd/... sample could miss poor paths later
				// in a wide choice set while still reporting the level as quality-safe.
				var pickOffset = limit == 1
					? 1
					: 1 + (int)System.Math.Round(
						(legal.Count - 2) * (double)sample / (limit - 1),
						MidpointRounding.AwayFromZero);
				var pick = legal[pickOffset];
				solution.Add(ordered[pick].Id.Value);
				mask.Clear(pick);

				var failed = false;
				while (mask.CountSet(ordered.Length) > 0)
				{
					var nextLegal = model.ListLegalIndices(mask);
					if (nextLegal.Count == 0)
					{
						failed = true;
						break;
					}

					var idx = nextLegal[0];
					solution.Add(ordered[idx].Id.Value);
					mask.Clear(idx);
				}

				if (!failed)
				{
					results.Add(solution);
				}
			}

			return results;
		}

		public static bool TryReplaySolution(
			IReadOnlyList<PuzzleBlock> blocks,
			IReadOnlyList<int> solutionIds)
		{
			if (blocks == null || solutionIds == null)
			{
				return false;
			}

			if (blocks.Count == 0)
			{
				return solutionIds.Count == 0;
			}

			try
			{
				var state = new PuzzleState(blocks);
				for (var i = 0; i < solutionIds.Count; i++)
				{
					var result = state.TryRemove(new BlockId(solutionIds[i]));
					if (result.Status != MoveStatus.Allowed)
					{
						return false;
					}
				}

				return state.IsComplete;
			}
			catch (ArgumentException)
			{
				return false;
			}
		}

		/// <summary>
		/// Empirically checks that every legal first move preserves solvability.
		/// </summary>
		public static bool LegalRemovalPreservesSolvability(IReadOnlyList<PuzzleBlock> blocks)
		{
			var root = Solve(blocks);
			if (!root.IsSolvable || blocks.Count == 0)
			{
				return true;
			}

			if (!TryNormalize(blocks, out var ordered, out _))
			{
				return false;
			}

			var model = new SolverModel(ordered);
			var full = ActiveMask.AllActive(ordered.Length);
			var legal = model.ListLegalIndices(full);
			for (var i = 0; i < legal.Count; i++)
			{
				var next = full.Clone();
				next.Clear(legal[i]);
				if (!IsSolvableMask(model, ordered.Length, next))
				{
					return false;
				}
			}

			return true;
		}

		private static SolverResult SolveGreedy(
			SolverModel model,
			PuzzleBlock[] ordered,
			SolverConfig config)
		{
			var mask = ActiveMask.AllActive(ordered.Length);
			var startLegal = model.ListLegalIndices(mask);
			var startLegalIds = IndicesToIds(ordered, startLegal);
			var solution = new List<int>(ordered.Length);
			var along = new List<int>(ordered.Length);
			var explored = 0;
			var forced = 0;
			var choices = 0;
			var maxBranch = 0;
			long branchSum = 0;
			var branchSamples = 0;

			while (mask.CountSet(ordered.Length) > 0)
			{
				explored++;
				if (config.MaxExploredStates > 0 && explored > config.MaxExploredStates)
				{
					return new SolverResult
					{
						Status = SolverStatus.BudgetExceeded,
						ExploredStates = explored,
						LegalMovesAtStart = startLegalIds,
						FailureReason = "MaxExploredStates exceeded"
					};
				}

				var legal = model.ListLegalIndices(mask);
				var branching = legal.Count;
				maxBranch = Math.Max(maxBranch, branching);
				branchSum += branching;
				branchSamples++;

				if (branching == 0)
				{
					return new SolverResult
					{
						Status = SolverStatus.Unsolvable,
						ExploredStates = explored,
						DeadEndStates = 1,
						MaxBranching = maxBranch,
						AverageBranching = branchSamples == 0 ? 0 : (double)branchSum / branchSamples,
						ForcedMoveCount = forced,
						ChoicePointCount = choices,
						LegalMovesAtStart = startLegalIds,
						FailureReason = "No legal moves with blocks remaining"
					};
				}

				if (branching == 1)
				{
					forced++;
				}
				else
				{
					choices++;
				}

				along.Add(branching);
				var pick = legal[0];
				solution.Add(ordered[pick].Id.Value);
				mask.Clear(pick);
			}

			return new SolverResult
			{
				Status = SolverStatus.Solved,
				Solution = solution,
				ExploredStates = explored,
				MaxBranching = maxBranch,
				AverageBranching = branchSamples == 0 ? 0 : (double)branchSum / branchSamples,
				ForcedMoveCount = forced,
				ChoicePointCount = choices,
				LegalMovesAtStart = startLegalIds,
				LegalMoveCountsAlongSolution = along
			};
		}

		private static bool IsSolvableMask(SolverModel model, int blockCount, ActiveMask mask)
		{
			while (mask.CountSet(blockCount) > 0)
			{
				var legal = model.ListLegalIndices(mask);
				if (legal.Count == 0)
				{
					return false;
				}

				mask.Clear(legal[0]);
			}

			return true;
		}

		internal static bool TryNormalize(
			IReadOnlyList<PuzzleBlock> blocks,
			out PuzzleBlock[] ordered,
			out string error)
		{
			ordered = null;
			error = null;
			var byId = new SortedDictionary<int, PuzzleBlock>();
			var positions = new HashSet<GridPosition>();
			for (var i = 0; i < blocks.Count; i++)
			{
				var block = blocks[i];
				if (byId.ContainsKey(block.Id.Value))
				{
					error = "Duplicate BlockId: " + block.Id.Value;
					return false;
				}

				if (!positions.Add(block.Position))
				{
					error = "Duplicate position: " + block.Position;
					return false;
				}

				byId.Add(block.Id.Value, block);
			}

			ordered = new PuzzleBlock[byId.Count];
			var idx = 0;
			foreach (var pair in byId)
			{
				ordered[idx++] = pair.Value;
			}

			return true;
		}

		private static List<int> IndicesToIds(PuzzleBlock[] ordered, List<int> indices)
		{
			var ids = new List<int>(indices.Count);
			for (var i = 0; i < indices.Count; i++)
			{
				ids.Add(ordered[indices[i]].Id.Value);
			}

			return ids;
		}

		private static SolverResult Invalid(string reason)
		{
			return new SolverResult
			{
				Status = SolverStatus.InvalidInput,
				FailureReason = reason
			};
		}

		private sealed class SolverModel
		{
			private readonly PuzzleBlock[] _blocks;

			public SolverModel(PuzzleBlock[] blocks)
			{
				_blocks = blocks;
			}

			public List<int> ListLegalIndices(ActiveMask mask)
			{
				var occupancy = new Dictionary<GridPosition, BlockId>();
				for (var i = 0; i < _blocks.Length; i++)
				{
					if (!mask.IsSet(i))
					{
						continue;
					}

					occupancy[_blocks[i].Position] = _blocks[i].Id;
				}

				var legal = new List<int>();
				for (var i = 0; i < _blocks.Length; i++)
				{
					if (!mask.IsSet(i))
					{
						continue;
					}

					var status = PuzzleRules.EvaluateEscape(
						_blocks[i].Id,
						_blocks[i],
						occupancy,
						out _);
					if (status == MoveStatus.Allowed)
					{
						legal.Add(i);
					}
				}

				return legal;
			}
		}
	}
}
