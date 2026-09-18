namespace TapAway.Core
{
	/// <summary>Why a generated candidate was rejected.</summary>
	public enum LevelRejectionReason
	{
		None = 0,
		InvalidGeometry = 1,
		Unsolvable = 2,
		DisconnectedInitialShape = 3,
		PoorIntermediateTopology = 4,
		DifficultyOutOfRange = 5,
		AttemptBudgetExceeded = 6,
		SolverBudgetExceeded = 7,
		ConstructionFailed = 8
	}

	/// <summary>
	/// Configurable thresholds for content-quality rejection.
	/// Not PuzzleRules legality — visual / structural coherence only.
	/// </summary>
	public sealed class LevelQualityConfig
	{
		/// <summary>
		/// Singletons / diagonal islands are ignored when active count is
		/// at or below this value (natural endgame).
		/// </summary>
		public int EndgameActiveAllowance { get; set; } = 3;

		/// <summary>
		/// Reject if a singleton appears while active count exceeds this.
		/// </summary>
		public int MaxActiveForPrematureSingleton { get; set; } = 3;

		/// <summary>
		/// Reject if a diagonal-attachment singleton appears while active &gt; this.
		/// </summary>
		public int MaxActiveForDiagonalIsland { get; set; } = 3;

		/// <summary>
		/// Reject if face-component count exceeds this before endgame allowance.
		/// </summary>
		public int MaxComponentsBeforeEndgame { get; set; } = 2;

		/// <summary>Require initial layout to be a single face-connected component.</summary>
		public bool RequireInitialFaceConnectivity { get; set; } = true;

		/// <summary>How many alternate solver paths to sample for topology checks.</summary>
		public int AlternatePathSampleCount { get; set; } = 3;
	}

	/// <summary>Result of topology / intermediate quality analysis.</summary>
	public sealed class LevelQualityReport
	{
		public bool Passed { get; set; }
		public LevelRejectionReason RejectionReason { get; set; }
		public string Detail { get; set; }
		public int InitialComponentCount { get; set; }
		public int BoundingVolume { get; set; }
		public int OccupiedCells { get; set; }
		public double Density { get; set; }
		public int SpanX { get; set; }
		public int SpanY { get; set; }
		public int SpanZ { get; set; }
		public int WorstPrematureSingletonActiveCount { get; set; }
		public int WorstDiagonalIslandActiveCount { get; set; }
		public int PathsAnalyzed { get; set; }
	}
}
