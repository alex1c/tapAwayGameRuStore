using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Separates tap selection from orbit drag using a small pixel threshold.
	/// Works with mouse (Editor) and single-touch (Android).
	/// </summary>
	public sealed class PuzzleInputController : MonoBehaviour
	{
		[SerializeField] private Camera _camera;
		[SerializeField] private PuzzlePresenter _presenter;
		[SerializeField] private PuzzleOrbitCamera _orbit;
		[SerializeField] private float _dragThresholdPixels = 12f;
		[SerializeField] private LayerMask _blockMask = ~0;

		private bool _pointerDown;
		private bool _dragging;
		private Vector2 _pointerStart;
		private Vector2 _pointerLast;

		public void Configure(Camera camera, PuzzlePresenter presenter, PuzzleOrbitCamera orbit)
		{
			_camera = camera;
			_presenter = presenter;
			_orbit = orbit;
		}

		private void Update()
		{
			if (_presenter != null && _presenter.IsInputLocked)
			{
				// Keep pointer state coherent if lock begins mid-gesture.
				if (!IsPointerHeld())
				{
					_pointerDown = false;
					_dragging = false;
				}

				if (_dragging && IsPointerHeld())
				{
					var pos = PointerPosition();
					var delta = pos - _pointerLast;
					_pointerLast = pos;
					_orbit?.ApplyDrag(delta);
				}

				return;
			}

			if (WasPointerPressed())
			{
				_pointerDown = true;
				_dragging = false;
				_pointerStart = PointerPosition();
				_pointerLast = _pointerStart;
			}
			else if (_pointerDown && IsPointerHeld())
			{
				var pos = PointerPosition();
				var total = pos - _pointerStart;
				if (!_dragging && total.magnitude >= _dragThresholdPixels)
				{
					_dragging = true;
				}

				if (_dragging)
				{
					var delta = pos - _pointerLast;
					_pointerLast = pos;
					_orbit?.ApplyDrag(delta);
				}
			}
			else if (_pointerDown && (WasPointerReleased() || !IsPointerHeld()))
			{
				if (!_dragging)
				{
					TryTap(_pointerStart);
				}

				_pointerDown = false;
				_dragging = false;
			}
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

		private static bool WasPointerPressed()
		{
			if (Input.touchCount > 0)
			{
				return Input.GetTouch(0).phase == TouchPhase.Began;
			}

			return Input.GetMouseButtonDown(0);
		}

		private static bool WasPointerReleased()
		{
			if (Input.touchCount > 0)
			{
				var phase = Input.GetTouch(0).phase;
				return phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
			}

			return Input.GetMouseButtonUp(0);
		}

		private static bool IsPointerHeld()
		{
			if (Input.touchCount > 0)
			{
				var phase = Input.GetTouch(0).phase;
				return phase == TouchPhase.Moved || phase == TouchPhase.Stationary || phase == TouchPhase.Began;
			}

			return Input.GetMouseButton(0);
		}

		private static Vector2 PointerPosition()
		{
			if (Input.touchCount > 0)
			{
				return Input.GetTouch(0).position;
			}

			return Input.mousePosition;
		}
	}
}
