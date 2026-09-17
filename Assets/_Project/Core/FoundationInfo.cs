namespace TapAway.Core
{
	/// <summary>
	/// Tiny pure-C# foundation marker used to prove Core is testable
	/// without UnityEngine / MonoBehaviour dependencies.
	/// Puzzle models will live in this assembly in later phases.
	/// </summary>
	public static class FoundationInfo
	{
		/// <summary>Working product title used across docs and debug HUD.</summary>
		public const string ProductName = "Tap Away";

		/// <summary>Alternate working title kept for branding experiments.</summary>
		public const string AlternateProductName = "Block Escape";

		/// <summary>Foundation phase marker; bump only with intentional phase changes.</summary>
		public const string Phase = "PHASE_0_FOUNDATION";

		/// <summary>
		/// Returns a stable, human-readable label for bootstrap / QA checks.
		/// Kept free of Unity types so EditMode tests can assert it directly.
		/// </summary>
		public static string GetBootstrapLabel()
		{
			return ProductName + " / " + Phase;
		}
	}
}
