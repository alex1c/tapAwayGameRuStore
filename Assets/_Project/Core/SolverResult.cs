using System.Collections.Generic;

namespace TapAway.Core
{
	/// <summary>High-level solver outcome.</summary>
	public enum SolverStatus
	{
		Solved = 0,
		Unsolvable = 1,
		Empty = 2,
		InvalidInput = 3,
		BudgetExceeded = 4
	}

	/// <summary>
	/// Structured solver diagnostics for difficulty / generation pipelines.
	/// </summary>
	public sealed class SolverResult
	{
		public SolverStatus Status { get; set; }
		public bool IsSolvable => Status == SolverStatus.Solved || Status == SolverStatus.Empty;
		public IReadOnlyList<int> Solution { get; set; } = System.Array.Empty<int>();
		public int SolutionLength => Solution.Count;
		public int ExploredStates { get; set; }
		public int DeadEndStates { get; set; }
		public int MaxBranching { get; set; }
		public double AverageBranching { get; set; }
		public int ForcedMoveCount { get; set; }
		public int ChoicePointCount { get; set; }
		public string FailureReason { get; set; }
		public IReadOnlyList<int> LegalMovesAtStart { get; set; } = System.Array.Empty<int>();

		/// <summary>
		/// Per-step legal-move counts along the returned solution (after each prior removal).
		/// Length equals SolutionLength; entry i is branching before removing Solution[i].
		/// </summary>
		public IReadOnlyList<int> LegalMoveCountsAlongSolution { get; set; } = System.Array.Empty<int>();
	}

	/// <summary>Optional solver work limits.</summary>
	public sealed class SolverConfig
	{
		/// <summary>Max distinct active-masks visited (0 = unlimited).</summary>
		public int MaxExploredStates { get; set; }

		/// <summary>
		/// When true, explore choice points for branching diagnostics and prefer
		/// a full search; when false, use monotonic greedy for solvability first.
		/// </summary>
		public bool CollectFullDiagnostics { get; set; } = true;

		/// <summary>Max alternate solutions to sample for quality checks (0 = none).</summary>
		public int MaxAlternateSolutions { get; set; } = 3;
	}
}
