using System;

namespace TapAway.Core
{
	/// <summary>
	/// Aspect-aware orbit framing math used by the presentation camera.
	/// Pure C# so EditMode can regress portrait / tall / wide bounds safely.
	/// </summary>
	public static class CameraFramingMath
	{
		/// <summary>
		/// Computes an orbit distance that fits a sphere of <paramref name="boundsRadius"/>
		/// inside both vertical and horizontal FOV cones, then applies padding and clamps.
		/// </summary>
		public static float ComputeOrbitDistance(
			float boundsRadius,
			float verticalFovDegrees,
			float aspect,
			float padding,
			float minDistance,
			float maxDistance)
		{
			if (boundsRadius < 0.01f)
			{
				boundsRadius = 0.01f;
			}

			if (aspect < 0.01f)
			{
				aspect = 0.01f;
			}

			if (padding < 1f)
			{
				padding = 1f;
			}

			var verticalHalf = Math.Max(0.001, verticalFovDegrees * 0.5 * (Math.PI / 180.0));
			var horizontalHalf = Math.Atan(Math.Tan(verticalHalf) * aspect);
			var limitingHalf = Math.Min(verticalHalf, horizontalHalf);
			var distance = (boundsRadius / Math.Sin(limitingHalf)) * padding;
			if (distance < minDistance)
			{
				distance = minDistance;
			}

			if (distance > maxDistance)
			{
				distance = maxDistance;
			}

			return (float)distance;
		}

		/// <summary>
		/// Radius from axis-aligned extents (half-size vector length).
		/// </summary>
		public static float RadiusFromExtents(float extentX, float extentY, float extentZ)
		{
			var r = Math.Sqrt(
				(double)extentX * extentX +
				(double)extentY * extentY +
				(double)extentZ * extentZ);
			return r < 0.01 ? 0.01f : (float)r;
		}
	}
}
