namespace TapAway.Runtime
{
	/// <summary>
	/// Authoritative presentation/input lifecycle for a playable level.
	/// Orbit/tap/pinch availability derives from this phase — not ad-hoc flags.
	/// </summary>
	public enum GameplayPhase
	{
		/// <summary>Level descriptor resolving / views rebuilding.</summary>
		LoadingLevel,

		/// <summary>Player may tap, orbit, and pinch.</summary>
		Playing,

		/// <summary>Removal or blocked feedback animation in progress.</summary>
		RemovingBlock,

		/// <summary>Core complete; waiting on Victory UI.</summary>
		Victory,

		/// <summary>Between levels (Next / Restart transition).</summary>
		Transitioning
	}
}
