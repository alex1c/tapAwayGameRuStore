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
		[SerializeField] private float _cellSize = 1.05f;
		[SerializeField] private float _flyDistance = 8f;
		[SerializeField] private float _flyDuration = 0.35f;
		[SerializeField] private float _bumpDistance = 0.18f;
		[SerializeField] private float _bumpDuration = 0.12f;

		private readonly Dictionary<int, BlockView> _views = new Dictionary<int, BlockView>();
		private PuzzleState _state;
		private bool _inputLocked;
		private Action<MoveResult> _onMoveResolved;
		private Action _onCompleted;

		public PuzzleState State => _state;
		public bool IsInputLocked => _inputLocked;
		public int ViewCount => _views.Count;
		public Transform BlocksRoot => _blocksRoot != null ? _blocksRoot : transform;

		/// <summary>
		/// Assigns the transform that will parent spawned BlockViews.
		/// </summary>
		public void SetBlocksRoot(Transform root)
		{
			_blocksRoot = root;
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
			RebuildViews();
		}

		/// <summary>
		/// Attempts a Core remove for the tapped view and plays feedback.
		/// </summary>
		public bool TrySelect(BlockView view)
		{
			if (_inputLocked || view == null || _state == null || view.IsAnimating)
			{
				return false;
			}

			if (!_state.IsActive(view.BlockId))
			{
				return false;
			}

			var result = _state.TryRemove(view.BlockId);
			_onMoveResolved?.Invoke(result);

			if (result.Status == MoveStatus.Allowed)
			{
				_inputLocked = true;
				StartCoroutine(PlayAllowedRoutine(view, result));
				return true;
			}

			if (result.Status == MoveStatus.Blocked)
			{
				_inputLocked = true;
				StartCoroutine(PlayBlockedRoutine(view));
				return true;
			}

			return false;
		}

		public BlockView GetView(BlockId id)
		{
			_views.TryGetValue(id.Value, out var view);
			return view;
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
			body.transform.localScale = Vector3.one * 0.9f;

			// Remove physics authority — collider is pick-only.
			var bodyCollider = body.GetComponent<Collider>();
			if (bodyCollider != null)
			{
				Destroy(bodyCollider);
			}

			var pick = root.AddComponent<BoxCollider>();
			pick.size = Vector3.one * 0.95f;

			var arrow = CreateArrow(root.transform);
			var view = root.AddComponent<BlockView>();
			view.SetVisualParts(arrow, body.GetComponent<Renderer>());
			view.Bind(block, ColorFor(block.Id.Value));
			return view;
		}

		private static Transform CreateArrow(Transform parent)
		{
			var arrowRoot = new GameObject("Arrow");
			arrowRoot.transform.SetParent(parent, false);

			var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
			shaft.name = "Shaft";
			shaft.transform.SetParent(arrowRoot.transform, false);
			shaft.transform.localPosition = new Vector3(0f, 0f, 0.55f);
			shaft.transform.localScale = new Vector3(0.12f, 0.12f, 0.55f);
			Destroy(shaft.GetComponent<Collider>());
			shaft.GetComponent<Renderer>().material.color = new Color(1f, 0.92f, 0.2f);

			var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
			head.name = "Head";
			head.transform.SetParent(arrowRoot.transform, false);
			head.transform.localPosition = new Vector3(0f, 0f, 0.95f);
			head.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
			head.transform.localScale = new Vector3(0.28f, 0.08f, 0.28f);
			Destroy(head.GetComponent<Collider>());
			head.GetComponent<Renderer>().material.color = new Color(1f, 0.75f, 0.1f);

			return arrowRoot.transform;
		}

		private IEnumerator PlayAllowedRoutine(BlockView view, MoveResult result)
		{
			if (view == null)
			{
				_inputLocked = false;
				yield break;
			}

			view.SetAnimating(true);
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
				var eased = t * t * (3f - 2f * t);
				view.transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(eased));
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
		}

		private IEnumerator PlayBlockedRoutine(BlockView view)
		{
			view.SetAnimating(true);

			var dir = BlockView.DirectionToWorld(view.EscapeDirection);
			var start = view.transform.localPosition;
			var bump = start + dir.normalized * _bumpDistance;
			view.ApplyPulseColor(new Color(1f, 0.35f, 0.35f));

			var t = 0f;
			var half = Mathf.Max(0.01f, _bumpDuration);
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
		}

		private void ClearViews()
		{
			foreach (var pair in _views)
			{
				if (pair.Value != null)
				{
					Destroy(pair.Value.gameObject);
				}
			}

			_views.Clear();

			if (_blocksRoot != null)
			{
				for (var i = _blocksRoot.childCount - 1; i >= 0; i--)
				{
					Destroy(_blocksRoot.GetChild(i).gameObject);
				}
			}
		}

		private static Color ColorFor(int id)
		{
			// Distinct, readable hues without relying on art assets.
			var hue = (id * 0.137f) % 1f;
			return Color.HSVToRGB(hue, 0.55f, 0.92f);
		}
	}
}
