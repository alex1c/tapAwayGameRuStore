using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Applies Screen.safeArea insets to a full-stretch RectTransform.
	/// Reusable for HUD, victory, and tutorial chrome.
	/// </summary>
	[RequireComponent(typeof(RectTransform))]
	public sealed class SafeAreaFitter : MonoBehaviour
	{
		[SerializeField] private RectTransform _target;
		[SerializeField] private float _extraBottomPadding;

		private Rect _lastSafeArea;
		private Vector2Int _lastScreen;

		private void Awake()
		{
			if (_target == null)
			{
				_target = GetComponent<RectTransform>();
			}

			Apply();
		}

		private void OnEnable()
		{
			Apply();
		}

		private void Update()
		{
			var safe = Screen.safeArea;
			var screen = new Vector2Int(Screen.width, Screen.height);
			if (safe != _lastSafeArea || screen != _lastScreen)
			{
				Apply();
			}
		}

		/// <summary>
		/// Forces a refresh (call after orientation / resolution changes if needed).
		/// </summary>
		public void Apply()
		{
			if (_target == null)
			{
				return;
			}

			var safe = Screen.safeArea;
			_lastSafeArea = safe;
			_lastScreen = new Vector2Int(Screen.width, Screen.height);

			var screenW = Mathf.Max(1, Screen.width);
			var screenH = Mathf.Max(1, Screen.height);

			var anchorMin = new Vector2(safe.xMin / screenW, safe.yMin / screenH);
			var anchorMax = new Vector2(safe.xMax / screenW, safe.yMax / screenH);

			// Extra bottom margin for gesture comfort beyond reported safeArea.
			var extra = _extraBottomPadding / screenH;
			anchorMin.y = Mathf.Clamp01(anchorMin.y + extra);

			_target.anchorMin = anchorMin;
			_target.anchorMax = anchorMax;
			_target.offsetMin = Vector2.zero;
			_target.offsetMax = Vector2.zero;
		}

		/// <summary>
		/// Pure helper for tests: converts a safe rect + screen size into normalized anchors.
		/// </summary>
		public static void ComputeAnchors(
			Rect safeArea,
			int screenWidth,
			int screenHeight,
			float extraBottomPadding,
			out Vector2 anchorMin,
			out Vector2 anchorMax)
		{
			var w = Mathf.Max(1, screenWidth);
			var h = Mathf.Max(1, screenHeight);
			anchorMin = new Vector2(safeArea.xMin / w, safeArea.yMin / h);
			anchorMax = new Vector2(safeArea.xMax / w, safeArea.yMax / h);
			anchorMin.y = Mathf.Clamp01(anchorMin.y + extraBottomPadding / h);
		}
	}
}
