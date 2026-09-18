using System.Collections.Generic;
using System.Text;

namespace TapAway.Core
{
	/// <summary>Human-facing difficulty band (V1 — not player-calibrated).</summary>
	public enum DifficultyBand
	{
		Tutorial = 0,
		Easy = 1,
		Medium = 2,
		Hard = 3,
		Expert = 4
	}

	/// <summary>Extracted features feeding Difficulty Model V1.</summary>
	public sealed class DifficultyFeatures
	{
		public int BlockCount { get; set; }
		public int InitialLegalMoves { get; set; }
		public int ForcedMoveCount { get; set; }
		public int ChoicePointCount { get; set; }
		public double ForcedMoveRatio { get; set; }
		public int MaxBranching { get; set; }
		public double AverageBranching { get; set; }
		public int SolutionLength { get; set; }
		public int SpanX { get; set; }
		public int SpanY { get; set; }
		public int SpanZ { get; set; }
		public double Density { get; set; }
		public int DirectionDiversity { get; set; }
		public double AverageLegalMovesAlongSolution { get; set; }
		public int OcclusionProxy { get; set; }
	}

	/// <summary>Difficulty Model V1 result with explainable breakdown.</summary>
	public sealed class DifficultyResult
	{
		public double Score { get; set; }
		public DifficultyBand Band { get; set; }
		public DifficultyFeatures Features { get; set; }
		public string Explanation { get; set; }
	}

	/// <summary>
	/// First-pass objective difficulty scoring. Weights are provisional.
	/// </summary>
	public static class DifficultyAnalyzer
	{
		public static DifficultyResult Analyze(
			IReadOnlyList<PuzzleBlock> blocks,
			SolverResult solver,
			LevelQualityReport quality = null)
		{
			var features = ExtractFeatures(blocks, solver, quality);
			// V1 weights: size-led with mild choice/structure modifiers.
			// Not player-calibrated — thresholds are provisional.
			var score = 0.0;
			score += features.BlockCount * 0.8;
			score += features.ChoicePointCount * 0.4;
			score += features.MaxBranching * 0.8;
			score += features.AverageBranching * 1.0;
			score += (1.0 - features.ForcedMoveRatio) * 4.0;
			score += features.DirectionDiversity * 0.5;
			score += features.Density * 4.0;
			score += System.Math.Min(features.OcclusionProxy, 40) * 0.1;
			score += System.Math.Max(features.SpanX, System.Math.Max(features.SpanY, features.SpanZ)) * 0.3;

			var band = ScoreToBand(score);
			var explanation = BuildExplanation(score, band, features);
			return new DifficultyResult
			{
				Score = score,
				Band = band,
				Features = features,
				Explanation = explanation
			};
		}

		public static DifficultyFeatures ExtractFeatures(
			IReadOnlyList<PuzzleBlock> blocks,
			SolverResult solver,
			LevelQualityReport quality = null)
		{
			var features = new DifficultyFeatures();
			if (blocks == null)
			{
				return features;
			}

			features.BlockCount = blocks.Count;
			features.SolutionLength = solver != null ? solver.SolutionLength : 0;
			features.InitialLegalMoves = solver != null ? solver.LegalMovesAtStart.Count : 0;
			features.ForcedMoveCount = solver != null ? solver.ForcedMoveCount : 0;
			features.ChoicePointCount = solver != null ? solver.ChoicePointCount : 0;
			features.MaxBranching = solver != null ? solver.MaxBranching : 0;
			features.AverageBranching = solver != null ? solver.AverageBranching : 0;
			features.ForcedMoveRatio = features.SolutionLength <= 0
				? 1.0
				: (double)features.ForcedMoveCount / features.SolutionLength;

			if (solver != null && solver.LegalMoveCountsAlongSolution.Count > 0)
			{
				long sum = 0;
				for (var i = 0; i < solver.LegalMoveCountsAlongSolution.Count; i++)
				{
					sum += solver.LegalMoveCountsAlongSolution[i];
				}

				features.AverageLegalMovesAlongSolution =
					(double)sum / solver.LegalMoveCountsAlongSolution.Count;
			}

			var dirs = new HashSet<EscapeDirection>();
			var occlusion = 0;
			for (var i = 0; i < blocks.Count; i++)
			{
				dirs.Add(blocks[i].EscapeDirection);
				for (var j = 0; j < blocks.Count; j++)
				{
					if (i == j)
					{
						continue;
					}

					if (IsOnEscapeRay(blocks[i], blocks[j].Position))
					{
						occlusion++;
					}
				}
			}

			features.DirectionDiversity = dirs.Count;
			features.OcclusionProxy = occlusion;

			if (quality != null)
			{
				features.SpanX = quality.SpanX;
				features.SpanY = quality.SpanY;
				features.SpanZ = quality.SpanZ;
				features.Density = quality.Density;
			}
			else
			{
				var q = LevelQualityAnalyzer.Analyze(blocks, solver != null ? solver.Solution : null,
					new LevelQualityConfig { AlternatePathSampleCount = 0, RequireInitialFaceConnectivity = false });
				features.SpanX = q.SpanX;
				features.SpanY = q.SpanY;
				features.SpanZ = q.SpanZ;
				features.Density = q.Density;
			}

			return features;
		}

		public static DifficultyBand ScoreToBand(double score)
		{
			if (score < 16) return DifficultyBand.Tutorial;
			if (score < 36) return DifficultyBand.Easy;
			if (score < 50) return DifficultyBand.Medium;
			if (score < 65) return DifficultyBand.Hard;
			return DifficultyBand.Expert;
		}

		private static bool IsOnEscapeRay(PuzzleBlock from, GridPosition other)
		{
			var a = from.Position;
			switch (from.EscapeDirection)
			{
				case EscapeDirection.PosX:
					return other.Y == a.Y && other.Z == a.Z && other.X > a.X;
				case EscapeDirection.NegX:
					return other.Y == a.Y && other.Z == a.Z && other.X < a.X;
				case EscapeDirection.PosY:
					return other.X == a.X && other.Z == a.Z && other.Y > a.Y;
				case EscapeDirection.NegY:
					return other.X == a.X && other.Z == a.Z && other.Y < a.Y;
				case EscapeDirection.PosZ:
					return other.X == a.X && other.Y == a.Y && other.Z > a.Z;
				case EscapeDirection.NegZ:
					return other.X == a.X && other.Y == a.Y && other.Z < a.Z;
				default:
					return false;
			}
		}

		private static string BuildExplanation(double score, DifficultyBand band, DifficultyFeatures f)
		{
			var sb = new StringBuilder();
			sb.Append("V1 score=").Append(score.ToString("0.00"));
			sb.Append(" band=").Append(band);
			sb.Append(" blocks=").Append(f.BlockCount);
			sb.Append(" choices=").Append(f.ChoicePointCount);
			sb.Append(" forcedRatio=").Append(f.ForcedMoveRatio.ToString("0.00"));
			sb.Append(" maxBranch=").Append(f.MaxBranching);
			sb.Append(" dirs=").Append(f.DirectionDiversity);
			sb.Append(" density=").Append(f.Density.ToString("0.00"));
			sb.Append(" (not player-calibrated)");
			return sb.ToString();
		}
	}
}
