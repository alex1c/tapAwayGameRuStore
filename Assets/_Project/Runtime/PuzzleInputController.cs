using System;
using TapAway.Core;
using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Mobile-first input: one-finger tap/orbit, two-finger pinch zoom.
	/// Pinch cancels pending taps; drag never becomes a removal on release.
	/// </summary>
	public sealed class PuzzleInputController : MonoBehaviour
	{
		[SerializeField] private Camera _camera;
		[SerializeField] private PuzzlePresenter _presenter;
		[SerializeField] private PuzzleOrbitCamera _orbit;
		[SerializeField] private LayerMask _blockMask = ~0;
		[SerializeField] private float _tapSlopPixels = 18f;
		[SerializeField] private float _dragStartPixels = 22f;
		[SerializeField] private float _meaningfulDragPixels = 48f;
		[SerializeField] private float _pinchCancelPixels = 10f;

		private GestureThresholds _thresholds;
		private bool _pointerDown;
		private bool _dragging;
		private bool _pinchActive;
		private bool _tapCancelled;
		private Vector2 _pointerStart;
		private Vector2 _pointerLast;
		private float _accumulatedDrag;
		private float _prevPinchDistance;
		private Action _onMeaningfulDrag;
		private bool _inputEnabled = true;

		public void Configure(Camera camera, PuzzlePresenter presenter, PuzzleOrbitCamera orbit)
		{
			_camera = camera;
			_presenter = presenter;
			_orbit = orbit;
			_thresholds = new GestureThresholds(
				_tapSlopPixels,
				_dragStartPixels,
				_meaningfulDragPixels,
				_pinchCancelPixels);
		}

		public void SetMeaningfulDragHandler(Action handler)
		{
			_onMeaningfulDrag = handler;
		}

		public void SetInputEnabled(bool enabled)
		{
			_inputEnabled = enabled;
			if (!enabled)
			{
				ResetGestureState();
			}
		}

		private void Update()
		{
			if (!_inputEnabled)
			{
				ResetGestureState();
				return;
			}

			HandleMouseWheel();

			if (Input.touchCount >= 2)
			{
				HandlePinch();
				return;
			}

			if (_pinchActive && Input.touchCount < 2)
			{
				// Leaving pinch must not fire a tap.
				_pinchActive = false;
				_tapCancelled = true;
				_pointerDown = false;
				_dragging = false;
			}

			if (_presenter != null && _presenter.IsInputLocked)
			{
				if (!IsPrimaryHeld())
				{
					ResetGestureState();
				}
				else if (_dragging)
				{
					ContinueDrag();
				}

				return;
			}

			if (WasPrimaryPressed())
			{
				_pointerDown = true;
				_dragging = false;
				_tapCancelled = false;
				_accumulatedDrag = 0f;
				_pointerStart = PrimaryPosition();
				_pointerLast = _pointerStart;
			}
			else if (_pointerDown && IsPrimaryHeld())
			{
				var pos = PrimaryPosition();
				var total = Vector2.Distance(pos, _pointerStart);
				if (!_dragging && GestureClassifier.ShouldStartDrag(total, _thresholds))
				{
					_dragging = true;
				}

				if (_dragging)
				{
					ContinueDrag();
				}
				else
				{
					_pointerLast = pos;
				}
			}
			else if (_pointerDown && (WasPrimaryReleased() || !IsPrimaryHeld()))
			{
				var total = Vector2.Distance(_pointerLast, _pointerStart);
				if (!_dragging && !_tapCancelled && GestureClassifier.IsTapRelease(total, _thresholds))
				{
					TryTap(_pointerStart);
				}

				if (_dragging && GestureClassifier.IsMeaningfulDrag(_accumulatedDrag, _thresholds))
				{
					_onMeaningfulDrag?.Invoke();
				}

				ResetGestureState();
			}
		}

		private void HandlePinch()
		{
			var t0 = Input.GetTouch(0).position;
			var t1 = Input.GetTouch(1).position;
			var dist = Vector2.Distance(t0, t1);

			if (!_pinchActive)
			{
				_pinchActive = true;
				_tapCancelled = true;
				_pointerDown = false;
				_dragging = false;
				_prevPinchDistance = dist;
				return;
			}

			var delta = Mathf.Abs(dist - _prevPinchDistance);
			if (GestureClassifier.ShouldCancelTapForPinch(delta, _thresholds))
			{
				_tapCancelled = true;
			}

			var factor = GestureClassifier.PinchZoomFactor(_prevPinchDistance, dist);
			_orbit?.ApplyZoomFactor(factor);
			_prevPinchDistance = dist;
		}

		private void HandleMouseWheel()
		{
			if (Input.touchCount > 0)
			{
				return;
			}

			var scroll = Input.mouseScrollDelta.y;
			_orbit?.ApplyScrollZoom(scroll);
		}

		private void ContinueDrag()
		{
			var pos = PrimaryPosition();
			var delta = pos - _pointerLast;
			_accumulatedDrag += delta.magnitude;
			_pointerLast = pos;
			_orbit?.ApplyDrag(delta);
		}

		private void TryTap(Vector2 screenPos)
		{
			var cam = _camera != null ? _camera : Camera.main;
			if (cam == null || _presenter == null)
			{
				return;
			}

			var ray = cam.ScreenPointToRay(screenPos);
			if (!Physics.Raycast(ray, out var hit, 500f, _blockMask, QueryTriggerInteraction.Ignore))
			{
				return;
			}

			var view = hit.collider.GetComponentInParent<BlockView>();
			if (view != null)
			{
				_presenter.TrySelect(view);
			}
		}

		private void ResetGestureState()
		{
			_pointerDown = false;
			_dragging = false;
			_pinchActive = false;
			_tapCancelled = false;
			_accumulatedDrag = 0f;
		}

		private static bool WasPrimaryPressed()
		{
			if (Input.touchCount == 1)
			{
				return Input.GetTouch(0).phase == TouchPhase.Began;
			}

			if (Input.touchCount > 1)
			{
				return false;
			}

			return Input.GetMouseButtonDown(0);
		}

		private static bool WasPrimaryReleased()
		{
			if (Input.touchCount == 1)
			{
				var phase = Input.GetTouch(0).phase;
				return phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
			}

			if (Input.touchCount == 0)
			{
				return Input.GetMouseButtonUp(0);
			}

			return false;
		}

		private static bool IsPrimaryHeld()
		{
			if (Input.touchCount == 1)
			{
				var phase = Input.GetTouch(0).phase;
				return phase == TouchPhase.Moved || phase == TouchPhase.Stationary || phase == TouchPhase.Began;
			}

			if (Input.touchCount > 1)
			{
				return false;
			}

			return Input.GetMouseButton(0);
		}

		private static Vector2 PrimaryPosition()
		{
			if (Input.touchCount >= 1)
			{
				return Input.GetTouch(0).position;
			}

			return Input.mousePosition;
		}
	}
}
