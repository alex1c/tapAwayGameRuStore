using TapAway.Core;
using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Orbit + pinch-zoom camera for puzzle inspection. No roll; yaw wraps naturally.
	/// Framing uses <see cref="CameraFramingMath"/> (aspect-aware, Checkpoint-safe).
	/// </summary>
	public sealed class PuzzleOrbitCamera : MonoBehaviour
	{
		[SerializeField] private Transform _pivot;
		[SerializeField] private Camera _camera;
		[SerializeField] private float _yaw = 35f;
		[SerializeField] private float _pitch = 25f;
		[SerializeField] private float _distance = 8f;
		[SerializeField] private float _minPitch = -72f;
		[SerializeField] private float _maxPitch = 72f;
		[SerializeField] private float _rotateSensitivity = 0.18f;
		[SerializeField] private float _framePadding = 1.42f;
		[SerializeField] private float _minDistance = 3.5f;
		[SerializeField] private float _maxDistance = 22f;
		[SerializeField] private float _zoomSensitivity = 1f;
		[SerializeField] private float _hudVerticalReserve = 0.08f;

		private float _initialYaw;
		private float _initialPitch;
		private float _framedDistance = 8f;

		public Transform Pivot => _pivot;
		public Camera Camera => _camera != null ? _camera : Camera.main;
		public float Distance => _distance;
		public float Yaw => _yaw;
		public float Pitch => _pitch;

		private void Awake()
		{
			if (_camera == null)
			{
				_camera = Camera.main;
			}

			if (_pivot == null)
			{
				var pivotGo = new GameObject("OrbitPivot");
				_pivot = pivotGo.transform;
			}

			_initialYaw = _yaw;
			_initialPitch = _pitch;
			ApplyTransform();
		}

		/// <summary>
		/// Applies a drag delta in screen pixels to yaw/pitch.
		/// </summary>
		public void ApplyDrag(Vector2 screenDelta)
		{
			_yaw += screenDelta.x * _rotateSensitivity;
			_pitch -= screenDelta.y * _rotateSensitivity;
			_pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
			ApplyTransform();
		}

		/// <summary>
		/// Pinch/mouse-wheel zoom. Factor &gt; 1 zooms out.
		/// </summary>
		public void ApplyZoomFactor(float factor)
		{
			if (factor <= 0.01f)
			{
				return;
			}

			// Invert common pinch intuition: fingers apart → zoom out (farther).
			_distance *= Mathf.Pow(factor, _zoomSensitivity);
			_distance = Mathf.Clamp(_distance, _minDistance, _maxDistance);
			ApplyTransform();
		}

		public void ApplyScrollZoom(float scrollY)
		{
			if (Mathf.Abs(scrollY) < 0.01f)
			{
				return;
			}

			var factor = 1f - scrollY * 0.1f;
			ApplyZoomFactor(factor);
		}

		/// <summary>
		/// Frames world bounds with overview fit + inspection zoom from
		/// <see cref="MobileReadabilityMath"/> so large puzzles stay tappable.
		/// </summary>
		public void FrameBounds(Bounds bounds)
		{
			_pivot.position = bounds.center;
			var radius = CameraFramingMath.RadiusFromExtents(
				bounds.extents.x,
				bounds.extents.y,
				bounds.extents.z);

			var cam = Camera;
			var vFov = cam != null ? cam.fieldOfView : 60f;
			var aspect = cam != null ? Mathf.Max(0.01f, cam.aspect) : (9f / 16f);
			var padding = _framePadding + _hudVerticalReserve;

			MobileReadabilityMath.ComputeZoomClamps(
				radius,
				vFov,
				aspect,
				padding,
				out var minDist,
				out var maxDist,
				out var overviewDist);

			_minDistance = minDist;
			_maxDistance = maxDist;
			_distance = overviewDist;
			_framedDistance = _distance;
			ApplyTransform();
		}

		/// <summary>
		/// Restores yaw/pitch/distance captured at the last FrameBounds call.
		/// </summary>
		public void ResetToFramedView()
		{
			_yaw = _initialYaw;
			_pitch = _initialPitch;
			_distance = _framedDistance;
			ApplyTransform();
		}

		public void CaptureInitialAngles()
		{
			_initialYaw = _yaw;
			_initialPitch = _pitch;
		}

		private void ApplyTransform()
		{
			var cam = Camera;
			if (cam == null || _pivot == null)
			{
				return;
			}

			// Normalize yaw for stability without restricting orbit freedom.
			if (_yaw > 360f || _yaw < -360f)
			{
				_yaw = Mathf.Repeat(_yaw + 180f, 360f) - 180f;
			}

			var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
			var offset = rotation * new Vector3(0f, 0f, -_distance);
			cam.transform.position = _pivot.position + offset;
			cam.transform.rotation = rotation;
		}
	}
}
