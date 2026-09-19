using System;
using System.Collections;
using System.Collections.Generic;
using TapAway.Core;
using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Builds and drives BlockViews from authoritative PuzzleState results.
	/// </summary>
	public sealed class PuzzlePresenter : MonoBehaviour
	{
		[SerializeField] private Transform _blocksRoot;
		[SerializeField] private float _cellSize = 1f;
		[SerializeField] private float _bodyScale = 0.96f;
		[SerializeField] private float _flyDistance = 9f;
		[SerializeField] private float _flyDuration = 0.32f;
		[SerializeField] private float _bumpDistance = 0.22f;
		[SerializeField] private float _bumpDuration = 0.11f;
		[SerializeField] private float _selectAckSeconds = 0.05f;

		private static Material _arrowPlateUnlit;
		private static Material _arrowAccentUnlit;

		private readonly Dictionary<int, BlockView> _views = new Dictionary<int, BlockView>();
		private PuzzleState _state;
		private bool _inputLocked;
		private bool _interactionEnabled = true;
		private Action<MoveResult> _onMoveResolved;
		private Action _onCompleted;
		private BlockId _highlightId;
		private LevelBootstrap _bootstrap;

		public PuzzleState State => _state;
		public bool IsInputLocked => _inputLocked || !_interactionEnabled;
		public int ViewCount => _views.Count;
		public Transform BlocksRoot => _blocksRoot != null ? _blocksRoot : transform;

		/// <summary>
		/// Active tracked views only (ignores Destroy-deferred inactive leftovers).
		/// </summary>
		public int CountActiveViews()
		{
			var count = 0;
			foreach (var pair in _views)
			{
				if (pair.Value != null && pair.Value.gameObject.activeInHierarchy)
				{
					count++;
				}
			}

			return count;
		}

		public void SetBlocksRoot(Transform root)
		{
			_blocksRoot = root;
		}

		public void SetInteractionEnabled(bool enabled)
		{
			_interactionEnabled = enabled;
		}

		public void Initialize(
			PuzzleState state,
			Action<MoveResult> onMoveResolved,
			Action onCompleted)
		{
			StopAllCoroutines();
			_state = state ?? throw new ArgumentNullException(nameof(state));
			_onMoveResolved = onMoveResolved;
			_onCompleted = onCompleted;
			_inputLocked = false;
			_interactionEnabled = true;
			_highlightId = default;
			_bootstrap = GetComponent<LevelBootstrap>() ??
			             FindFirstObjectByType<LevelBootstrap>();
			RebuildViews();
			GameplayPresentationInvariant.AssertParityOrLog(_state, this);
		}

		/// <summary>
		/// Attempts a Core remove for the tapped view and plays feedback.
		/// </summary>
		public bool TrySelect(BlockView view)
		{
			if (IsInputLocked || view == null || _state == null || view.IsAnimating)
			{
				return false;
			}

			if (!_state.IsActive(view.BlockId))
			{
				return false;
			}

			// Immediate visual acknowledgement of WHICH block was hit.
			view.PlaySelectFlash();
			HapticFeedback.Play(HapticFeedback.Kind.LightSuccess);

			var result = _state.TryRemove(view.BlockId);
			_onMoveResolved?.Invoke(result);

			if (result.Status == MoveStatus.Allowed)
			{
				_inputLocked = true;
				StartCoroutine(PlayAllowedRoutine(view));
				return true;
			}

			if (result.Status == MoveStatus.Blocked)
			{
				_inputLocked = true;
				HapticFeedback.Play(HapticFeedback.Kind.Blocked);
				StartCoroutine(PlayBlockedRoutine(view));
				return true;
			}

			view.RestoreBaseColor();
			return false;
		}

		public BlockView GetView(BlockId id)
		{
			_views.TryGetValue(id.Value, out var view);
			return view;
		}

		public void SetTutorialHighlight(BlockId id)
		{
			ClearTutorialHighlight();
			_highlightId = id;
			var view = GetView(id);
			view?.SetHighlighted(true);
		}

		public void ClearTutorialHighlight()
		{
			if (_highlightId.Value != 0)
			{
				var previous = GetView(_highlightId);
				previous?.SetHighlighted(false);
			}

			_highlightId = default;
		}

		public Bounds ComputeBounds()
		{
			var bound = new Bounds(BlocksRoot.position, Vector3.zero);
			var has = false;
			foreach (var pair in _views)
			{
				if (pair.Value == null || !pair.Value.gameObject.activeInHierarchy)
				{
					continue;
				}

				var renderer = pair.Value.GetComponentInChildren<Renderer>();
				if (renderer == null)
				{
					continue;
				}

				if (!has)
				{
					bound = renderer.bounds;
					has = true;
				}
				else
				{
					bound.Encapsulate(renderer.bounds);
				}
			}

			if (!has)
			{
				bound = new Bounds(BlocksRoot.position, Vector3.one * 2f);
			}

			return bound;
		}

		private void RebuildViews()
		{
			ClearViews();
			BlocksRoot.localScale = Vector3.one * _cellSize;

			foreach (var block in _state.GetActiveBlocks())
			{
				var view = CreateBlockView(block);
				_views[block.Id.Value] = view;
			}
		}

		private BlockView CreateBlockView(PuzzleBlock block)
		{
			var root = new GameObject("Block_" + block.Id.Value);
			root.transform.SetParent(BlocksRoot, false);

			var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
			body.name = "Body";
			body.transform.SetParent(root.transform, false);
			// Nearly fill the unit cell so face-adjacent blocks read as face-joined,
			// not edge/corner-only due to large visual gaps.
			body.transform.localScale = Vector3.one * _bodyScale;

			var bodyCollider = body.GetComponent<Collider>();
			if (bodyCollider != null)
			{
				Destroy(bodyCollider);
			}

			// Slightly forgiving pick target without giant surprising colliders.
			var pick = root.AddComponent<BoxCollider>();
			pick.size = Vector3.one * 0.98f;

			var arrow = DirectionIndicatorBuilder.Build(
				root.transform,
				block.EscapeDirection,
				GetArrowPlateMaterial(),
				GetArrowAccentMaterial());

			// Keep arrows readable across small/large generated puzzles.
			var scale = ComputeArrowScale();
			var aware = arrow.GetComponent<CameraAwareDirectionIndicator>();
			aware?.SetAdaptiveScale(scale);

			var view = root.AddComponent<BlockView>();
			view.SetVisualParts(arrow, body.GetComponent<Renderer>());
			view.Bind(block, ColorFor(block.Id.Value));
			return view;
		}

		private float ComputeArrowScale()
		{
			var n = _state != null ? _state.DefinedCount : 12;
			if (n <= 10)
			{
				return 1.15f;
			}

			if (n <= 16)
			{
				return 1f;
			}

			if (n <= 24)
			{
				return 0.9f;
			}

			return 0.8f;
		}

		private static Material GetArrowPlateMaterial()
		{
			EnsureArrowMaterials();
			return _arrowPlateUnlit;
		}

		private static Material GetArrowAccentMaterial()
		{
			EnsureArrowMaterials();
			return _arrowAccentUnlit;
		}

		/// <summary>
		/// Shared Unlit arrow materials — geometry conveys direction; color is contrast only.
		/// </summary>
		private static void EnsureArrowMaterials()
		{
			if (_arrowPlateUnlit != null && _arrowAccentUnlit != null)
			{
				return;
			}

			var shader = Shader.Find("Unlit/Color");
			if (shader == null)
			{
				shader = Shader.Find("Legacy Shaders/Unlit/Color");
			}

			if (shader == null)
			{
				shader = Shader.Find("Sprites/Default");
			}

			_arrowPlateUnlit = new Material(shader)
			{
				name = "TapAway_ArrowPlate_Unlit",
				color = new Color(0.05f, 0.05f, 0.07f, 1f)
			};
			_arrowAccentUnlit = new Material(shader)
			{
				name = "TapAway_ArrowAccent_Unlit",
				color = new Color(1f, 0.92f, 0.12f, 1f)
			};
		}

		private IEnumerator PlayAllowedRoutine(BlockView view)
		{
			if (view == null)
			{
				_inputLocked = false;
				yield break;
			}

			view.SetAnimating(true);
			if (_selectAckSeconds > 0f)
			{
				yield return new WaitForSeconds(_selectAckSeconds);
			}

			var dir = BlockView.DirectionToWorld(view.EscapeDirection);
			var start = view.transform.position;
			var end = start + dir.normalized * _flyDistance;
			var t = 0f;
			while (t < 1f)
			{
				if (view == null)
				{
					_inputLocked = false;
					yield break;
				}

				t += Time.deltaTime / Mathf.Max(0.01f, _flyDuration);
				// Ease-in cubic: snappy start, clear exit.
				var u = Mathf.Clamp01(t);
				var eased = u * u * u;
				view.transform.position = Vector3.Lerp(start, end, eased);
				yield return null;
			}

			if (view != null)
			{
				view.Hide();
				_views.Remove(view.BlockId.Value);
				Destroy(view.gameObject);
			}

			_inputLocked = false;
			if (_state != null && _state.IsComplete)
			{
				_onCompleted?.Invoke();
			}
			else
			{
				_bootstrap?.NotifyRemovalSettled();
				GameplayPresentationInvariant.AssertParityOrLog(_state, this);
			}
		}

		private IEnumerator PlayBlockedRoutine(BlockView view)
		{
			view.SetAnimating(true);
			if (_selectAckSeconds > 0f)
			{
				yield return new WaitForSeconds(_selectAckSeconds);
			}

			var dir = BlockView.DirectionToWorld(view.EscapeDirection);
			var start = view.transform.localPosition;
			var bump = start + dir.normalized * _bumpDistance;
			view.ApplyPulseColor(new Color(1f, 0.32f, 0.32f));

			// Total blocked feedback ~220ms (two halves).
			var half = Mathf.Max(0.01f, _bumpDuration);
			var t = 0f;
			while (t < 1f)
			{
				t += Time.deltaTime / half;
				view.transform.localPosition = Vector3.Lerp(start, bump, Mathf.Clamp01(t));
				yield return null;
			}

			t = 0f;
			while (t < 1f)
			{
				t += Time.deltaTime / half;
				view.transform.localPosition = Vector3.Lerp(bump, start, Mathf.Clamp01(t));
				yield return null;
			}

			view.transform.localPosition = start;
			view.RestoreBaseColor();
			view.SetAnimating(false);
			_inputLocked = false;
			_bootstrap?.NotifyRemovalSettled();
		}

		private void ClearViews()
		{
			foreach (var pair in _views)
			{
				if (pair.Value != null)
				{
					// Deactivate immediately — Destroy() is deferred to end of frame,
					// and orphan/parity checks must not see stale gameplay cubes.
					pair.Value.gameObject.SetActive(false);
					Destroy(pair.Value.gameObject);
				}
			}

			_views.Clear();

			if (_blocksRoot != null)
			{
				for (var i = _blocksRoot.childCount - 1; i >= 0; i--)
				{
					var child = _blocksRoot.GetChild(i).gameObject;
					child.SetActive(false);
					Destroy(child);
				}
			}
		}

		private static Color ColorFor(int id)
		{
			// Slightly higher value keeps underside faces readable under ambient fill.
			var hue = (id * 0.137f) % 1f;
			return Color.HSVToRGB(hue, 0.48f, 0.95f);
		}
	}
}
