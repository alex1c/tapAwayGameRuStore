namespace TapAway.Core
{
	/// <summary>
	/// Tiny pure-C# foundation marker used to prove Core is testable
	/// without UnityEngine / MonoBehaviour dependencies.
	/// </summary>
	public static class FoundationInfo
	{
		/// <summary>Working product title used across docs and debug HUD.</summary>
		public const string ProductName = "Tap Away";

		/// <summary>Alternate working title kept for branding experiments.</summary>
		public const string AlternateProductName = "Block Escape";

		/// <summary>Current content-pipeline phase marker.</summary>
		public const string Phase = "PHASE_4_GENERATED_GAMEPLAY";

		/// <summary>
		/// Returns a stable, human-readable label for bootstrap / QA checks.
		/// </summary>
		public static string GetBootstrapLabel()
		{
			return ProductName + " / " + Phase;
		}
	}
}
