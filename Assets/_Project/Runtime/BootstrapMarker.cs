using TapAway.Core;
using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Minimal scene marker confirming the Bootstrap scene loaded.
	/// Presentation-only; puzzle rules must remain in TapAway.Core.
	/// </summary>
	public sealed class BootstrapMarker : MonoBehaviour
	{
		[SerializeField]
		private string _label = FoundationInfo.ProductName;

		/// <summary>Exposes the configured label for PlayMode smoke checks.</summary>
		public string Label => _label;

		private void Awake()
		{
			// Keep startup side-effects minimal and explicit for foundation QA.
			_label = FoundationInfo.GetBootstrapLabel();
			Debug.Log("[TapAway] Bootstrap ready: " + _label);
		}
	}
}
