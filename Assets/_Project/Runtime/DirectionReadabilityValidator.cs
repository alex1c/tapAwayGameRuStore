using System.Collections.Generic;
using System.Text;
using TapAway.Core;
using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Development helper for Direction UX V2 geometry checks (not pixel tests).
	/// </summary>
	public static class DirectionReadabilityValidator
	{
		public static readonly Vector3[] RepresentativeViewDirections =
		{
			new Vector3(0.55f, -0.35f, 0.75f).normalized,
			new Vector3(-0.65f, -0.25f, 0.7f).normalized,
			new Vector3(0.7f, -0.4f, -0.55f).normalized,
			new Vector3(-0.5f, -0.55f, -0.65f).normalized,
			new Vector3(0.15f, -0.85f, 0.5f).normalized,
			new Vector3(0.2f, 0.75f, 0.6f).normalized
		};

		/// <summary>
		/// V2: clear primary arrow with head ahead of tail along escape.
		/// </summary>
		public static bool HasExpectedMultiFaceCoverage(Transform indicator, EscapeDirection direction)
		{
			return DirectionIndicatorBuilder.HeadIsAlongEscape(indicator, direction)
			       && HasPrimaryArrowParts(indicator);
		}

		public static bool HasPrimaryArrowParts(Transform indicator)
		{
			if (indicator == null)
			{
				return false;
			}

			var hasShaft = false;
			var hasHead = false;
			var hasTail = false;
			for (var i = 0; i < indicator.childCount; i++)
			{
				var name = indicator.GetChild(i).name;
				if (name == "Shaft") hasShaft = true;
				if (name == "Head") hasHead = true;
				if (name == "Tail") hasTail = true;
			}

			return hasShaft && hasHead && hasTail;
		}

		public static bool IsReadableFromView(Transform indicator, Vector3 cameraLookTowardPuzzle)
		{
			if (indicator == null)
			{
				return false;
			}

			var toCamera = -cameraLookTowardPuzzle.normalized;
			for (var i = 0; i < indicator.childCount; i++)
			{
				var child = indicator.GetChild(i);
				if (child.name != "Head" && child.name != "Shaft" && child.name != "HeadWingL" &&
				    child.name != "HeadWingR")
				{
					continue;
				}

				var facing = Vector3.Dot(child.forward, toCamera);
				var sideSilhouette = 1f - Mathf.Abs(facing);
				if (facing > 0.1f || sideSilhouette > 0.5f)
				{
					return true;
				}
			}

			return false;
		}

		public static string ValidateActiveViews(
			IEnumerable<BlockView> views,
			IReadOnlyList<Vector3> viewDirections = null)
		{
			var dirs = viewDirections ?? RepresentativeViewDirections;
			var sb = new StringBuilder();
			var failCount = 0;

			foreach (var view in views)
			{
				if (view == null || !view.gameObject.activeInHierarchy)
				{
					continue;
				}

				var arrow = FindIndicator(view.transform);
				if (arrow == null)
				{
					sb.AppendLine("Block " + view.BlockId.Value + ": missing DirectionIndicator");
					failCount++;
					continue;
				}

				if (!HasExpectedMultiFaceCoverage(arrow, view.EscapeDirection))
				{
					sb.AppendLine(
						"Block " + view.BlockId.Value + ": arrow semantics failed for " +
						EscapeDirectionUtil.ToShortLabel(view.EscapeDirection));
					failCount++;
				}

				var readableViews = 0;
				for (var i = 0; i < dirs.Count; i++)
				{
					if (IsReadableFromView(arrow, dirs[i]))
					{
						readableViews++;
					}
				}

				if (readableViews < (dirs.Count + 1) / 2)
				{
					sb.AppendLine(
						"Block " + view.BlockId.Value + ": readable from only " +
						readableViews + "/" + dirs.Count + " views");
					failCount++;
				}
			}

			if (failCount == 0)
			{
				return "PASS: all active direction indicators readable";
			}

			return "FAIL (" + failCount + "):\n" + sb;
		}

		public static Transform FindIndicator(Transform blockRoot)
		{
			if (blockRoot == null)
			{
				return null;
			}

			for (var i = 0; i < blockRoot.childCount; i++)
			{
				var child = blockRoot.GetChild(i);
				if (child.name == "DirectionIndicator" || child.name == "Arrow")
				{
					return child;
				}
			}

			return null;
		}
	}
}
