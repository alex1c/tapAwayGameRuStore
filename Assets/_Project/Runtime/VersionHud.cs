using TapAway.Core;
using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Lightweight on-screen version / phase text for foundation smoke runs.
	/// Uses IMGUI to avoid uGUI scene wiring on Phase 0.
	/// </summary>
	public sealed class VersionHud : MonoBehaviour
	{
		[SerializeField]
		private int _fontSize = 22;

		private string _text = string.Empty;

		private void Awake()
		{
			_text = FoundationInfo.GetBootstrapLabel()
				+ " | Unity "
				+ Application.unityVersion;
		}

		private void OnGUI()
		{
			var style = new GUIStyle(GUI.skin.label)
			{
				fontSize = _fontSize,
				normal = { textColor = Color.white }
			};

			// Slight padding keeps text readable above system gesture areas later.
			GUI.Label(new Rect(16f, 16f, Screen.width - 32f, 64f), _text, style);
		}
	}
}
