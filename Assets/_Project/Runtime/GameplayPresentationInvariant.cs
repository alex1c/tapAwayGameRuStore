using System.Collections.Generic;
using TapAway.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TapAway.Runtime
{
	/// <summary>
	/// Presentation invariants: Core active blocks must match gameplay BlockViews.
	/// Non-gameplay cubes (e.g. FoundationMarker) must not look like puzzle blocks.
	/// </summary>
	public static class GameplayPresentationInvariant
	{
		public const string FoundationMarkerName = "FoundationMarker";

		/// <summary>
		/// Disables the Phase 0 foundation diagnostic cube so it cannot appear
		/// as an orphan grey "block" during gameplay.
		/// </summary>
		public static void DisableNonGameplayMarkers()
		{
			var marker = GameObject.Find(FoundationMarkerName);
			if (marker == null)
			{
				// Find does not return inactive objects — search roots explicitly.
				marker = FindIncludingInactive(FoundationMarkerName);
			}

			if (marker == null)
			{
				return;
			}

			// Keep the object for Editor diagnostics, but strip gameplay resemblance.
			var renderer = marker.GetComponent<MeshRenderer>();
			if (renderer != null)
			{
				renderer.enabled = false;
			}

			var collider = marker.GetComponent<Collider>();
			if (collider != null)
			{
				collider.enabled = false;
			}

			marker.SetActive(false);
		}

		/// <summary>
		/// Counts BlockViews tracked by the presenter that are still active.
		/// Prefer this over scene-wide searches — Destroy() is end-of-frame deferred.
		/// </summary>
		public static int CountActiveGameplayViews(PuzzlePresenter presenter)
		{
			if (presenter == null)
			{
				return 0;
			}

			return presenter.CountActiveViews();
		}

		/// <summary>
		/// True when Core ActiveCount equals active gameplay BlockView count
		/// and every active Core id has exactly one view.
		/// </summary>
		public static bool TryValidateParity(
			PuzzleState state,
			PuzzlePresenter presenter,
			out string detail)
		{
			detail = null;
			if (state == null || presenter == null)
			{
				detail = "null state or presenter";
				return false;
			}

			var coreActive = state.ActiveCount;
			var viewCount = CountActiveGameplayViews(presenter);
			if (coreActive != viewCount)
			{
				detail = "ActiveCount=" + coreActive + " views=" + viewCount;
				return false;
			}

			if (presenter.ViewCount != viewCount)
			{
				detail = "Presenter.ViewCount=" + presenter.ViewCount +
				         " activeViews=" + viewCount;
				return false;
			}

			foreach (var block in state.GetActiveBlocks())
			{
				var view = presenter.GetView(block.Id);
				if (view == null || !view.gameObject.activeInHierarchy)
				{
					detail = "Missing view for BlockId " + block.Id.Value;
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Development assertion: logs an error when Core/View parity breaks.
		/// </summary>
		public static void AssertParityOrLog(PuzzleState state, PuzzlePresenter presenter)
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if (!TryValidateParity(state, presenter, out var detail))
			{
				Debug.LogError("[TapAway] Core/View parity broken: " + detail);
			}
#endif
		}

		/// <summary>
		/// True when a visible mesh cube exists that is not owned by a BlockView
		/// (the historic FoundationMarker failure mode).
		/// </summary>
		public static bool HasOrphanGameplayLookingCube()
		{
			var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			for (var i = 0; i < renderers.Length; i++)
			{
				var renderer = renderers[i];
				if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
				{
					continue;
				}

				// Direction arrows are cubes too — they belong to a BlockView.
				if (renderer.GetComponentInParent<BlockView>() != null)
				{
					continue;
				}

				if (renderer.gameObject.name == FoundationMarkerName)
				{
					return true;
				}

				var filter = renderer.GetComponent<MeshFilter>();
				if (filter == null || filter.sharedMesh == null)
				{
					continue;
				}

				// Unity built-in cube mesh is the gameplay lookalike.
				if (filter.sharedMesh.name.IndexOf("Cube", System.StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}

			return false;
		}

		public static IEnumerable<BlockView> EnumerateActiveViews(PuzzlePresenter presenter)
		{
			if (presenter == null || presenter.BlocksRoot == null)
			{
				yield break;
			}

			var views = presenter.BlocksRoot.GetComponentsInChildren<BlockView>(true);
			for (var i = 0; i < views.Length; i++)
			{
				var view = views[i];
				if (view != null && view.gameObject.activeInHierarchy)
				{
					yield return view;
				}
			}
		}

		private static GameObject FindIncludingInactive(string name)
		{
			var scene = SceneManager.GetActiveScene();
			if (!scene.IsValid())
			{
				return null;
			}

			var roots = scene.GetRootGameObjects();
			for (var i = 0; i < roots.Length; i++)
			{
				if (roots[i].name == name)
				{
					return roots[i];
				}

				var child = FindInChildren(roots[i].transform, name);
				if (child != null)
				{
					return child;
				}
			}

			return null;
		}

		private static GameObject FindInChildren(Transform parent, string name)
		{
			for (var i = 0; i < parent.childCount; i++)
			{
				var child = parent.GetChild(i);
				if (child.name == name)
				{
					return child.gameObject;
				}

				var nested = FindInChildren(child, name);
				if (nested != null)
				{
					return nested;
				}
			}

			return null;
		}
	}
}
