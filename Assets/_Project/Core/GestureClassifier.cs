namespace TapAway.Core
{
	/// <summary>
	/// Centralized gesture thresholds for mobile tap / drag / pinch classification.
	/// Presentation feeds pixel deltas; Core stays free of Unity input APIs.
	/// </summary>
	public readonly struct GestureThresholds
	{
		/// <summary>Max movement still counted as a tap (finger jitter tolerance).</summary>
		public readonly float TapSlopPixels;

		/// <summary>Movement that promotes a pending tap into orbit drag.</summary>
		public readonly float DragStartPixels;

		/// <summary>Meaningful orbit drag observed for tutorial completion.</summary>
		public readonly float MeaningfulDragPixels;

		/// <summary>Pinch distance change that cancels a pending tap.</summary>
		public readonly float PinchCancelPixels;

		public GestureThresholds(
			float tapSlopPixels,
			float dragStartPixels,
			float meaningfulDragPixels,
			float pinchCancelPixels)
		{
			TapSlopPixels = tapSlopPixels;
			DragStartPixels = dragStartPixels;
			MeaningfulDragPixels = meaningfulDragPixels;
			PinchCancelPixels = pinchCancelPixels;
		}

		public static GestureThresholds MobileDefault =>
			new GestureThresholds(18f, 22f, 48f, 10f);
	}

	/// <summary>
	/// Pure helpers for classifying pointer gestures without UnityEngine.
	/// </summary>
	public static class GestureClassifier
	{
		public static bool ShouldStartDrag(float totalMovePixels, in GestureThresholds thresholds)
		{
			return totalMovePixels >= thresholds.DragStartPixels;
		}

		public static bool IsTapRelease(float totalMovePixels, in GestureThresholds thresholds)
		{
			return totalMovePixels < thresholds.DragStartPixels;
		}

		public static bool IsMeaningfulDrag(float totalDragPixels, in GestureThresholds thresholds)
		{
			return totalDragPixels >= thresholds.MeaningfulDragPixels;
		}

		public static bool ShouldCancelTapForPinch(float pinchDistanceDeltaPixels, in GestureThresholds thresholds)
		{
			return pinchDistanceDeltaPixels >= thresholds.PinchCancelPixels;
		}

		/// <summary>
		/// Returns zoom scale factor from previous/current finger distance.
		/// Values &gt; 1 mean zoom out (fingers apart), &lt; 1 zoom in.
		/// </summary>
		public static float PinchZoomFactor(float previousDistance, float currentDistance)
		{
			if (previousDistance < 0.01f)
			{
				return 1f;
			}

			return currentDistance / previousDistance;
		}
	}
}
