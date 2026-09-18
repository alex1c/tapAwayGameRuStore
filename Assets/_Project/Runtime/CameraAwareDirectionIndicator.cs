using TapAway.Core;
using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Places at most one face glyph on the best camera-facing surface.
	/// Glyph encodes the SAME world EscapeDirection (projected onto the face).
	/// Does not alter Core direction — presentation only.
	/// </summary>
	public sealed class CameraAwareDirectionIndicator : MonoBehaviour
	{
		private EscapeDirection _direction;
		private Material _plate;
		private Material _accent;
		private Transform _faceGlyphRoot;
		private Transform _glyphShaft;
		private Transform _glyphHead;
		private Camera _camera;
		private Vector3 _lastCamPos;
		private Vector3 _lastCamFwd;
		private float _scale = 1f;

		private static readonly Vector3[] FaceNormals =
		{
			Vector3.right, Vector3.left,
			Vector3.up, Vector3.down,
			Vector3.forward, Vector3.back
		};

		public void Configure(EscapeDirection direction, Material plate, Material accent)
		{
			_direction = direction;
			_plate = plate;
			_accent = accent;
			_faceGlyphRoot = transform.Find("FaceGlyph");
			if (_faceGlyphRoot == null)
			{
				var go = new GameObject("FaceGlyph");
				_faceGlyphRoot = go.transform;
				_faceGlyphRoot.SetParent(transform, false);
			}

			EnsureGlyphParts();
			_camera = Camera.main;
			ForceRefresh();
		}

		public void SetAdaptiveScale(float scale)
		{
			_scale = Mathf.Clamp(scale, 0.65f, 1.35f);
			transform.localScale = Vector3.one * _scale;
		}

		private void LateUpdate()
		{
			if (_camera == null)
			{
				_camera = Camera.main;
				if (_camera == null)
				{
					return;
				}
			}

			var camPos = _camera.transform.position;
			var camFwd = _camera.transform.forward;
			if ((camPos - _lastCamPos).sqrMagnitude < 0.0004f &&
			    (camFwd - _lastCamFwd).sqrMagnitude < 0.0004f)
			{
				return;
			}

			_lastCamPos = camPos;
			_lastCamFwd = camFwd;
			RefreshFaceGlyph(camPos);
		}

		public void ForceRefresh()
		{
			if (_camera == null)
			{
				_camera = Camera.main;
			}

			if (_camera != null)
			{
				RefreshFaceGlyph(_camera.transform.position);
			}
		}

		private void EnsureGlyphParts()
		{
			if (_glyphShaft != null)
			{
				return;
			}

			ClearChildren(_faceGlyphRoot);
			_glyphShaft = DirectionIndicatorBuilder.CreateCube(
				_faceGlyphRoot,
				"FaceShaft",
				Vector3.zero,
				Quaternion.identity,
				new Vector3(0.08f, 0.05f, 0.36f),
				_accent).transform;
			_glyphHead = DirectionIndicatorBuilder.CreateCube(
				_faceGlyphRoot,
				"FaceHead",
				new Vector3(0f, 0f, 0.22f),
				Quaternion.Euler(0f, 0f, 45f),
				new Vector3(0.2f, 0.05f, 0.2f),
				_accent).transform;
			_faceGlyphRoot.gameObject.SetActive(false);
		}

		private void RefreshFaceGlyph(Vector3 cameraWorldPos)
		{
			EnsureGlyphParts();
			EscapeDirectionUtil.GetStep(_direction, out var dx, out var dy, out var dz);
			var escape = new Vector3(dx, dy, dz);
			var blockPos = transform.position;
			var toCamera = (cameraWorldPos - blockPos).normalized;

			var bestScore = -1f;
			var bestNormal = Vector3.zero;
			var bestOnFace = Vector3.zero;

			for (var i = 0; i < FaceNormals.Length; i++)
			{
				var normal = FaceNormals[i];
				var facing = Vector3.Dot(normal, toCamera);
				if (facing < 0.35f)
				{
					continue;
				}

				if (!DirectionIndicatorBuilder.TryProjectEscapeOntoFace(escape, normal, out var onFace))
				{
					// Escape face itself: skip face glyph — primary 3D arrow is enough.
					continue;
				}

				// Prefer faces that show a long projected arrow and face the camera.
				var score = facing * 1.2f + Mathf.Abs(Vector3.Dot(onFace, escape)) * 0.5f;
				if (score > bestScore)
				{
					bestScore = score;
					bestNormal = normal;
					bestOnFace = onFace;
				}
			}

			if (bestScore < 0f)
			{
				_faceGlyphRoot.gameObject.SetActive(false);
				return;
			}

			// Place glyph slightly outside the cube face.
			var faceCenter = bestNormal * 0.52f;
			_faceGlyphRoot.localPosition = faceCenter;
			_faceGlyphRoot.localRotation = DirectionIndicatorBuilder.SafeLook(bestOnFace);
			// Keep glyph flush to face: use face normal as up when possible.
			var look = Quaternion.LookRotation(bestOnFace, bestNormal);
			_faceGlyphRoot.localRotation = look;
			_faceGlyphRoot.gameObject.SetActive(true);

			// Ensure head is along +bestOnFace (same as projected escape).
			_glyphHead.localPosition = new Vector3(0f, 0.02f, 0.2f);
			_glyphShaft.localPosition = new Vector3(0f, 0.02f, 0f);
		}

		private static void ClearChildren(Transform root)
		{
			for (var i = root.childCount - 1; i >= 0; i--)
			{
				var child = root.GetChild(i).gameObject;
				if (Application.isPlaying)
				{
					Object.Destroy(child);
				}
				else
				{
					Object.DestroyImmediate(child);
				}
			}
		}
	}
}
