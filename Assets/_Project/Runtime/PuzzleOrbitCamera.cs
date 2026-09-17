using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Orbit camera around the puzzle pivot. Drag rotates; tap is handled elsewhere.
	/// Avoids uncontrolled roll by separating yaw/pitch.
	/// </summary>
	public sealed class PuzzleOrbitCamera : MonoBehaviour
	{
		[SerializeField] private Transform _pivot;
		[SerializeField] private Camera _camera;
		[SerializeField] private float _yaw = 35f;
		[SerializeField] private float _pitch = 25f;
		[SerializeField] private float _distance = 8f;
		[SerializeField] private float _minPitch = -80f;
		[SerializeField] private float _maxPitch = 80f;
		[SerializeField] private float _rotateSensitivity = 0.2f;
		[SerializeField] private float _framePadding = 1.35f;

		public Transform Pivot => _pivot;
		public Camera Camera => _camera != null ? _camera : Camera.main;

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
		/// Frames the given world bounds in a portrait-friendly orbit distance.
		/// </summary>
		public void FrameBounds(Bounds bounds)
		{
			_pivot.position = bounds.center;
			var extents = bounds.extents.magnitude;
			if (extents < 0.01f)
			{
				extents = 2f;
			}

			var cam = Camera;
			var vFov = cam != null ? cam.fieldOfView : 60f;
			var verticalHalf = Mathf.Max(0.1f, vFov * 0.5f) * Mathf.Deg2Rad;
			var aspect = cam != null ? Mathf.Max(0.01f, cam.aspect) : 1f;
			var horizontalHalf = Mathf.Atan(Mathf.Tan(verticalHalf) * aspect);
			var half = Mathf.Min(verticalHalf, horizontalHalf);
			// Extra padding leaves room for future HUD / bottom safe area.
			_distance = (extents / Mathf.Sin(half)) * _framePadding;
			_distance = Mathf.Clamp(_distance, 4f, 24f);
			ApplyTransform();
		}

		private void ApplyTransform()
		{
			var cam = Camera;
			if (cam == null || _pivot == null)
			{
				return;
			}

			var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
			var offset = rotation * new Vector3(0f, 0f, -_distance);
			cam.transform.position = _pivot.position + offset;
			cam.transform.rotation = rotation;
		}
	}
}
