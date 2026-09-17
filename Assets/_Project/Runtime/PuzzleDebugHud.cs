using TapAway.Core;
using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Lightweight development HUD. Does not clutter production UI paths.
	/// </summary>
	public sealed class PuzzleDebugHud : MonoBehaviour
	{
		[SerializeField] private bool _visible = true;

		private PuzzleState _state;
		private MoveResult _lastResult;
		private string _lastLine = "-";

		public void Bind(PuzzleState state)
		{
			_state = state;
			_lastResult = default;
			_lastLine = "ready";
		}

		public void ReportMove(MoveResult result)
		{
			_lastResult = result;
			_lastLine = result.ToString();
		}

		private void OnGUI()
		{
			if (!_visible || _state == null)
			{
				return;
			}

			var style = new GUIStyle(GUI.skin.box)
			{
				fontSize = 22,
				alignment = TextAnchor.UpperLeft,
				normal = { textColor = Color.white }
			};

			var text =
				"Tap Away Phase 1\n" +
				"Active: " + _state.ActiveCount + " / " + _state.DefinedCount + "\n" +
				"Last: " + _lastLine + "\n" +
				"Drag=orbit  Tap=select";

			// Top-left, clear of future bottom safe-area CTAs.
			GUI.Box(new Rect(12f, 12f, 420f, 130f), text, style);
		}
	}
}
