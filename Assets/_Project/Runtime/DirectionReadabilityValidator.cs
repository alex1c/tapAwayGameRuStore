using System.Collections.Generic;
using System.Text;
using TapAway.Core;
using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Development helper: verifies direction glyphs remain readable from
	/// representative camera angles without pixel/screenshot assertions.
	/// Checks presentation geometry (part coverage + facing), not lighting.
	/// </summary>
	public static class DirectionReadabilityValidator
	{
		/// <summary>
		/// Representative orbit look directions (from camera toward puzzle origin).
		/// Covers default-ish and late-game useful views.
		/// </summary>
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
		/// True when the indicator has multi-face coverage for one escape axis.
		/// </summary>
		public static bool HasExpectedMultiFaceCoverage(Transform indicator, EscapeDirection direction)
		{
			if (indicator == null)
			{
				return false;
			}

			EscapeDirectionUtil.GetStep(direction, out var dx, out var dy, out var dz);
			var escape = new Vector3(dx, dy, dz);
			var childCount = indicator.childCount;
			if (childCount < DirectionIndicatorBuilder.ExpectedPartCount)
			{
				return false;
			}

			var hasHead = false;
			var hasShaft = false;
			var hasTail = false;
			var lateralChevrons = 0;

			for (var i = 0; i < childCount; i++)
			{
				var child = indicator.GetChild(i);
				var name = child.name;
				if (name == "Head")
				{
					hasHead = Vector3.Dot(child.localPosition, escape) > 0.4f;
				}
				else if (name == "ThroughShaft")
				{
					hasShaft = true;
				}
				else if (name == "Tail")
				{
					hasTail = Vector3.Dot(child.localPosition, escape) < -0.4f;
				}
				else if (name == "Chevron")
				{
					lateralChevrons++;
					// Chevron tip must advance along the SAME escape vector.
					if (Vector3.Dot(child.localPosition, escape) < 0f)
					{
						return false;
					}
				}
			}

			return hasHead && hasShaft && hasTail && lateralChevrons >= 4;
		}

		/// <summary>
		/// For a given camera look direction, returns true if at least one accent
		/// part presents a readable silhouette (not fully back-facing).
		/// </summary>
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
				if (child.name != "Head" && child.name != "ThroughShaft" && child.name != "Chevron")
				{
					continue;
				}

				// Part forward is local +Z after LookRotation(escape).
				var partForward = child.forward;
				// Readable if the accent faces the camera OR presents a side silhouette.
				var facing = Vector3.Dot(partForward, toCamera);
				var sideSilhouette = 1f - Mathf.Abs(facing);
				if (facing > 0.15f || sideSilhouette > 0.55f)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Validates every active block view against representative cameras.
		/// Returns a human-readable report (empty issues => pass).
		/// </summary>
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
						"Block " + view.BlockId.Value + ": incomplete multi-face coverage for " +
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

				// Must be readable from a majority of useful angles, not only one lucky view.
				if (readableViews < (dirs.Count + 1) / 2)
				{
					sb.AppendLine(
						"Block " + view.BlockId.Value + ": readable from only " +
						readableViews + "/" + dirs.Count + " representative views");
					failCount++;
				}
			}

			if (failCount == 0)
			{
				return "PASS: all active direction indicators readable";
			}

			return "FAIL (" + failCount + "):\n" + sb;
		}

		/// <summary>
		/// Finds the DirectionIndicator child under a block root.
		/// </summary>
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
