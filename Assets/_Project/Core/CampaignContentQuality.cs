using System.Collections.Generic;

namespace TapAway.Core
{
	/// <summary>
	/// Campaign content quality — presentation/readability, not PuzzleRules legality.
	/// </summary>
	public static class CampaignContentQuality
	{
		public sealed class Report
		{
			public bool Accepted { get; set; }
			public string Detail { get; set; }
			public int BlockCount { get; set; }
			public int SpanX { get; set; }
			public int SpanY { get; set; }
			public int SpanZ { get; set; }
			public double Density { get; set; }
			public float RadiusProxy { get; set; }
			public float OverviewBlockScreenFractionProxy { get; set; }
		}

		public static Report Evaluate(
			IReadOnlyList<PuzzleBlock> blocks,
			DifficultyFeatures features = null,
			float verticalFovDegrees = 60f,
			float aspect = 9f / 16f)
		{
			var report = new Report { Accepted = true, Detail = "ok" };
			if (blocks == null || blocks.Count == 0)
			{
				report.Accepted = false;
				report.Detail = "empty";
				return report;
			}

			report.BlockCount = blocks.Count;
			if (report.BlockCount > CampaignDefinition.MaxComfortableBlockCount)
			{
				report.Accepted = false;
				report.Detail = "block count " + report.BlockCount +
				                " exceeds mobile comfort " + CampaignDefinition.MaxComfortableBlockCount;
			}

			int minX = int.MaxValue, maxX = int.MinValue;
			int minY = int.MaxValue, maxY = int.MinValue;
			int minZ = int.MaxValue, maxZ = int.MinValue;
			foreach (var block in blocks)
			{
				var p = block.Position;
				if (p.X < minX) minX = p.X;
				if (p.X > maxX) maxX = p.X;
				if (p.Y < minY) minY = p.Y;
				if (p.Y > maxY) maxY = p.Y;
				if (p.Z < minZ) minZ = p.Z;
				if (p.Z > maxZ) maxZ = p.Z;
			}

			report.SpanX = maxX - minX + 1;
			report.SpanY = maxY - minY + 1;
			report.SpanZ = maxZ - minZ + 1;
			var volume = System.Math.Max(1, report.SpanX * report.SpanY * report.SpanZ);
			report.Density = (double)report.BlockCount / volume;

			var extX = report.SpanX * 0.5f;
			var extY = report.SpanY * 0.5f;
			var extZ = report.SpanZ * 0.5f;
			report.RadiusProxy = CameraFramingMath.RadiusFromExtents(extX, extY, extZ);

			if (report.RadiusProxy > CampaignDefinition.MaxComfortableRadiusProxy)
			{
				report.Accepted = false;
				report.Detail = "radius proxy " + report.RadiusProxy.ToString("0.00") +
				                " exceeds " + CampaignDefinition.MaxComfortableRadiusProxy.ToString("0.00");
			}

			MobileReadabilityMath.ComputeZoomClamps(
				report.RadiusProxy,
				verticalFovDegrees,
				aspect,
				1.42f,
				out _,
				out _,
				out var overviewDistance);

			report.OverviewBlockScreenFractionProxy = MobileReadabilityMath.ScreenFractionAtDistance(
				MobileReadabilityMath.BlockWorldSize,
				verticalFovDegrees,
				overviewDistance);

			// Extreme skinny shapes that force tiny overview blocks.
			if (report.OverviewBlockScreenFractionProxy < 0.028f)
			{
				report.Accepted = false;
				report.Detail = "overview block screen fraction too small: " +
				                report.OverviewBlockScreenFractionProxy.ToString("0.000");
			}

			if (features != null && features.Density < 0.08 && report.BlockCount >= 16)
			{
				report.Accepted = false;
				report.Detail = "sparse large construction dens=" + features.Density.ToString("0.00");
			}

			return report;
		}
	}
}
