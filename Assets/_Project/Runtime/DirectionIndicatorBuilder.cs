using TapAway.Core;
using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Direction UX V2: clear asymmetric arrow (shaft + triangular head).
	/// Optionally places one camera-facing face glyph encoding the SAME EscapeDirection.
	/// </summary>
	public static class DirectionIndicatorBuilder
	{
		/// <summary>Primary 3D arrow parts: Shaft, Head, HeadWingL, HeadWingR, Tail.</summary>
		public const int PrimaryPartCount = 5;

		/// <summary>
		/// Builds a clear ----&gt; style arrow along world escape, plus a face-glyph holder.
		/// </summary>
		public static Transform Build(
			Transform parent,
			EscapeDirection direction,
			Material plateMaterial,
			Material accentMaterial)
		{
			var root = new GameObject("DirectionIndicator");
			root.transform.SetParent(parent, false);
			root.transform.localPosition = Vector3.zero;
			root.transform.localRotation = Quaternion.identity;

			EscapeDirectionUtil.GetStep(direction, out var dx, out var dy, out var dz);
			var escape = new Vector3(dx, dy, dz);
			var look = SafeLook(escape);

			// Thin shaft — tail end toward -escape, head end toward +escape.
			CreateCube(
				root.transform,
				"Shaft",
				escape * 0.05f,
				look,
				new Vector3(0.11f, 0.11f, 0.55f),
				accentMaterial);

			// Triangular head: center wedge + two wings (strong head/tail asymmetry).
			var headBase = escape * 0.42f;
			CreateCube(
				root.transform,
				"Head",
				headBase + escape * 0.08f,
				look,
				new Vector3(0.08f, 0.08f, 0.22f),
				accentMaterial);

			CreateCube(
				root.transform,
				"HeadWingL",
				headBase,
				look * Quaternion.Euler(0f, 0f, 35f),
				new Vector3(0.28f, 0.07f, 0.14f),
				accentMaterial);

			CreateCube(
				root.transform,
				"HeadWingR",
				headBase,
				look * Quaternion.Euler(0f, 0f, -35f),
				new Vector3(0.28f, 0.07f, 0.14f),
				accentMaterial);

			// Small dark tail stub so reverse end is obvious.
			CreateCube(
				root.transform,
				"Tail",
				-escape * 0.28f,
				look,
				new Vector3(0.16f, 0.16f, 0.08f),
				plateMaterial);

			// Empty holder for optional camera-facing face glyph (updated at runtime).
			var faceGlyph = new GameObject("FaceGlyph");
			faceGlyph.transform.SetParent(root.transform, false);

			var aware = root.AddComponent<CameraAwareDirectionIndicator>();
			aware.Configure(direction, plateMaterial, accentMaterial);

			return root.transform;
		}

		/// <summary>
		/// LookRotation stable when escape is parallel to world up.
		/// </summary>
		public static Quaternion SafeLook(Vector3 forward)
		{
			var f = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
			var up = Mathf.Abs(Vector3.Dot(f, Vector3.up)) > 0.99f
				? Vector3.forward
				: Vector3.up;
			return Quaternion.LookRotation(f, up);
		}

		/// <summary>
		/// Projects world escape onto a face plane; returns false if nearly parallel to normal.
		/// </summary>
		public static bool TryProjectEscapeOntoFace(
			Vector3 escape,
			Vector3 faceNormal,
			out Vector3 onFace)
		{
			escape = escape.normalized;
			faceNormal = faceNormal.normalized;
			onFace = escape - faceNormal * Vector3.Dot(escape, faceNormal);
			if (onFace.sqrMagnitude < 0.05f)
			{
				onFace = Vector3.zero;
				return false;
			}

			onFace.Normalize();
			return true;
		}

		/// <summary>
		/// True when head lies further along escape than tail (semantic check).
		/// </summary>
		public static bool HeadIsAlongEscape(Transform indicator, EscapeDirection direction)
		{
			if (indicator == null)
			{
				return false;
			}

			EscapeDirectionUtil.GetStep(direction, out var dx, out var dy, out var dz);
			var escape = new Vector3(dx, dy, dz);
			Transform head = null;
			Transform tail = null;
			for (var i = 0; i < indicator.childCount; i++)
			{
				var child = indicator.GetChild(i);
				if (child.name == "Head")
				{
					head = child;
				}
				else if (child.name == "Tail")
				{
					tail = child;
				}
			}

			if (head == null || tail == null)
			{
				return false;
			}

			return Vector3.Dot(head.localPosition, escape) > Vector3.Dot(tail.localPosition, escape);
		}

		public static GameObject CreateCube(
			Transform parent,
			string name,
			Vector3 localPos,
			Quaternion localRot,
			Vector3 localScale,
			Material material)
		{
			var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
			go.name = name;
			go.transform.SetParent(parent, false);
			go.transform.localPosition = localPos;
			go.transform.localRotation = localRot;
			go.transform.localScale = localScale;
			Object.Destroy(go.GetComponent<Collider>());
			go.GetComponent<Renderer>().sharedMaterial = material;
			return go;
		}
	}
}
