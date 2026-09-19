using System;

namespace TapAway.Core
{
	/// <summary>
	/// Screen-space readability helpers for mobile framing/zoom.
	/// Pure math — Runtime maps these to camera distances.
	/// </summary>
	public static class MobileReadabilityMath
	{
		/// <summary>
		/// Minimum comfortable projected block height as a fraction of screen height.
		/// ~7% keeps taps readable on typical portrait phones.
		/// </summary>
		public const float MinBlockScreenFraction = 0.07f;

		/// <summary>
		/// Target overview fraction — whole puzzle readable at start without being tiny.
		/// </summary>
		public const float OverviewBlockScreenFraction = 0.045f;

		/// <summary>
		/// Soft upper fraction so tiny puzzles are not enormous.
		/// </summary>
		public const float MaxBlockScreenFraction = 0.18f;

		/// <summary>
		/// Approximate world-space block edge length (unit grid cell).
		/// </summary>
		public const float BlockWorldSize = 1f;

		/// <summary>
		/// Orbit distance that projects a world size of <paramref name="worldSize"/>
		/// to approximately <paramref name="screenFraction"/> of vertical FOV.
		/// distance ≈ (worldSize/2) / tan(vFov/2) / screenFraction
		/// </summary>
		public static float DistanceForScreenFraction(
			float worldSize,
			float verticalFovDegrees,
			float screenFraction)
		{
			if (worldSize < 0.01f)
			{
				worldSize = 0.01f;
			}

			if (screenFraction < 0.001f)
			{
				screenFraction = 0.001f;
			}

			var halfFov = Math.Max(0.001, verticalFovDegrees * 0.5 * (Math.PI / 180.0));
			var fullHeightAtUnit = 2.0 * Math.Tan(halfFov);
			var distance = worldSize / (fullHeightAtUnit * screenFraction);
			return (float)Math.Max(0.5, distance);
		}

		/// <summary>
		/// Projected screen fraction of a unit block at the given orbit distance.
		/// </summary>
		public static float ScreenFractionAtDistance(
			float worldSize,
			float verticalFovDegrees,
			float distance)
		{
			if (distance < 0.01f)
			{
				distance = 0.01f;
			}

			var halfFov = Math.Max(0.001, verticalFovDegrees * 0.5 * (Math.PI / 180.0));
			var fullHeight = 2.0 * distance * Math.Tan(halfFov);
			return (float)(worldSize / fullHeight);
		}

		/// <summary>
		/// Zoom clamps from puzzle radius + readability targets.
		/// minDistance = closest inspection (comfortable tap size)
		/// maxDistance = farthest overview (fit puzzle with padding, but not beyond readability soft floor)
		/// </summary>
		public static void ComputeZoomClamps(
			float boundsRadius,
			float verticalFovDegrees,
			float aspect,
			float framePadding,
			out float minDistance,
			out float maxDistance,
			out float overviewDistance)
		{
			var fit = CameraFramingMath.ComputeOrbitDistance(
				boundsRadius,
				verticalFovDegrees,
				aspect,
				framePadding,
				0.5f,
				80f);

			var inspect = DistanceForScreenFraction(
				BlockWorldSize,
				verticalFovDegrees,
				MinBlockScreenFraction);

			var overviewFloor = DistanceForScreenFraction(
				BlockWorldSize,
				verticalFovDegrees,
				OverviewBlockScreenFraction);

			// Closest zoom: allow inspection; never enter inside the bounding sphere.
			minDistance = Math.Max(boundsRadius * 0.55f, Math.Min(inspect, fit * 0.55f));
			minDistance = Math.Max(1.8f, minDistance);

			// Farthest: overview fit, but prefer not going so far that blocks vanish.
			maxDistance = Math.Max(fit * 1.15f, overviewFloor);
			maxDistance = Math.Max(maxDistance, minDistance + 1.5f);
			maxDistance = Math.Min(maxDistance, 48f);

			overviewDistance = Math.Min(fit, maxDistance);
			overviewDistance = Math.Max(overviewDistance, minDistance);
		}
	}
}
