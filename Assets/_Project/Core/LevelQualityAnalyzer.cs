using System;
using System.Collections.Generic;

namespace TapAway.Core
{
	/// <summary>
	/// Content-quality analysis for authored / generated levels.
	/// Rejects mathematically valid but visually poor intermediate states.
	/// </summary>
	public static class LevelQualityAnalyzer
	{
		public static LevelQualityReport Analyze(
			IReadOnlyList<PuzzleBlock> blocks,
			IReadOnlyList<int> canonicalSolution,
			LevelQualityConfig config = null)
		{
			config = config ?? new LevelQualityConfig();
			var report = new LevelQualityReport();
			if (blocks == null)
			{
				report.Passed = false;
				report.RejectionReason = LevelRejectionReason.InvalidGeometry;
				report.Detail = "blocks is null";
				return report;
			}

			FillBounds(blocks, report);

			if (blocks.Count == 0)
			{
				report.Passed = true;
				report.RejectionReason = LevelRejectionReason.None;
				report.Detail = "empty";
				return report;
			}

			report.InitialComponentCount = LevelTopology.CountFaceConnectedComponents(blocks);
			if (config.RequireInitialFaceConnectivity && report.InitialComponentCount != 1)
			{
				report.Passed = false;
				report.RejectionReason = LevelRejectionReason.DisconnectedInitialShape;
				report.Detail = "Initial face components=" + report.InitialComponentCount;
				return report;
			}

			var paths = new List<IReadOnlyList<int>>();
			if (canonicalSolution != null && canonicalSolution.Count > 0)
			{
				paths.Add(canonicalSolution);
			}

			var alts = PuzzleSolver.SampleAlternateSolutions(blocks, config.AlternatePathSampleCount);
			for (var i = 0; i < alts.Count; i++)
			{
				paths.Add(alts[i]);
			}

			report.PathsAnalyzed = paths.Count;
			if (paths.Count == 0)
			{
				report.Passed = false;
				report.RejectionReason = LevelRejectionReason.Unsolvable;
				report.Detail = "No solution path to analyze";
				return report;
			}

			for (var p = 0; p < paths.Count; p++)
			{
				var pathReport = AnalyzePath(blocks, paths[p], config);
				if (!pathReport.Passed)
				{
					return pathReport;
				}

				report.WorstPrematureSingletonActiveCount = Math.Max(
					report.WorstPrematureSingletonActiveCount,
					pathReport.WorstPrematureSingletonActiveCount);
				report.WorstDiagonalIslandActiveCount = Math.Max(
					report.WorstDiagonalIslandActiveCount,
					pathReport.WorstDiagonalIslandActiveCount);
			}

			report.Passed = true;
			report.RejectionReason = LevelRejectionReason.None;
			report.Detail = "ok paths=" + report.PathsAnalyzed;
			return report;
		}

		private static LevelQualityReport AnalyzePath(
			IReadOnlyList<PuzzleBlock> blocks,
			IReadOnlyList<int> solution,
			LevelQualityConfig config)
		{
			var report = new LevelQualityReport();
			FillBounds(blocks, report);
			report.InitialComponentCount = LevelTopology.CountFaceConnectedComponents(blocks);
			report.PathsAnalyzed = 1;

			var stateReports = LevelTopology.AnalyzeSolutionStates(blocks, solution);
			for (var i = 0; i < stateReports.Count; i++)
			{
				var snap = stateReports[i];
				if (snap.ActiveCount <= config.EndgameActiveAllowance)
				{
					continue;
				}

				if (snap.SingletonComponentCount > 0 &&
				    snap.ActiveCount > config.MaxActiveForPrematureSingleton)
				{
					report.Passed = false;
					report.RejectionReason = LevelRejectionReason.PoorIntermediateTopology;
					report.Detail = "Premature singleton at active=" + snap.ActiveCount +
					                " step=" + snap.StepIndex;
					report.WorstPrematureSingletonActiveCount = snap.ActiveCount;
					return report;
				}

				if (snap.DiagonalAttachmentSingletonCount > 0 &&
				    snap.ActiveCount > config.MaxActiveForDiagonalIsland)
				{
					report.Passed = false;
					report.RejectionReason = LevelRejectionReason.PoorIntermediateTopology;
					report.Detail = "Diagonal island at active=" + snap.ActiveCount +
					                " step=" + snap.StepIndex;
					report.WorstDiagonalIslandActiveCount = snap.ActiveCount;
					return report;
				}

				if (snap.ComponentCount > config.MaxComponentsBeforeEndgame)
				{
					report.Passed = false;
					report.RejectionReason = LevelRejectionReason.PoorIntermediateTopology;
					report.Detail = "Fragmentation components=" + snap.ComponentCount +
					                " active=" + snap.ActiveCount;
					return report;
				}

				if (snap.SingletonComponentCount > 0)
				{
					report.WorstPrematureSingletonActiveCount = Math.Max(
						report.WorstPrematureSingletonActiveCount,
						snap.ActiveCount);
				}

				if (snap.DiagonalAttachmentSingletonCount > 0)
				{
					report.WorstDiagonalIslandActiveCount = Math.Max(
						report.WorstDiagonalIslandActiveCount,
						snap.ActiveCount);
				}
			}

			report.Passed = true;
			report.RejectionReason = LevelRejectionReason.None;
			return report;
		}

		private static void FillBounds(IReadOnlyList<PuzzleBlock> blocks, LevelQualityReport report)
		{
			report.OccupiedCells = blocks.Count;
			if (blocks.Count == 0)
			{
				return;
			}

			var minX = int.MaxValue;
			var minY = int.MaxValue;
			var minZ = int.MaxValue;
			var maxX = int.MinValue;
			var maxY = int.MinValue;
			var maxZ = int.MinValue;
			for (var i = 0; i < blocks.Count; i++)
			{
				var p = blocks[i].Position;
				if (p.X < minX) minX = p.X;
				if (p.Y < minY) minY = p.Y;
				if (p.Z < minZ) minZ = p.Z;
				if (p.X > maxX) maxX = p.X;
				if (p.Y > maxY) maxY = p.Y;
				if (p.Z > maxZ) maxZ = p.Z;
			}

			report.SpanX = maxX - minX + 1;
			report.SpanY = maxY - minY + 1;
			report.SpanZ = maxZ - minZ + 1;
			report.BoundingVolume = report.SpanX * report.SpanY * report.SpanZ;
			report.Density = report.BoundingVolume <= 0
				? 0
				: (double)report.OccupiedCells / report.BoundingVolume;
		}
	}
}
