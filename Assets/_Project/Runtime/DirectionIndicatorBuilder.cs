using TapAway.Core;
using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Builds a direction glyph readable from multiple viewing angles.
	/// All parts encode the SAME world-space <see cref="EscapeDirection"/>.
	/// Uses Unlit materials so readability does not depend on lighting.
	/// </summary>
	public static class DirectionIndicatorBuilder
	{
		/// <summary>Expected child part count: shaft, head, plate, 4×(chevron+plate), tail.</summary>
		public const int ExpectedPartCount = 12;

		/// <summary>
		/// Creates a through-shaft + escape-face head + lateral face chevrons.
		/// Local block axes match the puzzle grid (no block yaw).
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

			// Through-shaft: silhouette readable from side views even if head faces away.
			CreateCube(
				root.transform,
				"ThroughShaft",
				escape * 0.08f,
				look,
				new Vector3(0.14f, 0.14f, 0.92f),
				accentMaterial);

			// Head on the escape face (primary 3D arrow tip).
			CreateCube(
				root.transform,
				"Head",
				escape * 0.58f,
				look * Quaternion.Euler(0f, 0f, 45f),
				new Vector3(0.34f, 0.12f, 0.34f),
				accentMaterial);

			// Raised plate behind head for contrast on the escape face.
			CreateCube(
				root.transform,
				"EscapePlate",
				escape * 0.52f,
				look,
				new Vector3(0.42f, 0.08f, 0.42f),
				plateMaterial);

			// Lateral face chevrons: each points toward the SAME escape axis.
			foreach (var lateral in GetLateralAxes(escape))
			{
				CreateLateralChevron(root.transform, escape, lateral, plateMaterial, accentMaterial);
			}

			// Opposite-face tail mark so the back side still shows orientation.
			CreateCube(
				root.transform,
				"Tail",
				-escape * 0.52f,
				look,
				new Vector3(0.2f, 0.2f, 0.08f),
				plateMaterial);

			return root.transform;
		}

		/// <summary>
		/// LookRotation that stays stable when escape is parallel to world up.
		/// </summary>
		public static Quaternion SafeLook(Vector3 forward)
		{
			var f = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
			var up = Mathf.Abs(Vector3.Dot(f, Vector3.up)) > 0.99f
				? Vector3.forward
				: Vector3.up;
			return Quaternion.LookRotation(f, up);
		}

		private static void CreateLateralChevron(
			Transform parent,
			Vector3 escape,
			Vector3 lateral,
			Material plateMaterial,
			Material accentMaterial)
		{
			var faceCenter = lateral * 0.52f;
			// Use lateral as the local "up" so the chevron sits on that face.
			var look = Quaternion.LookRotation(escape, lateral);

			CreateCube(
				parent,
				"ChevronPlate",
				faceCenter,
				look,
				new Vector3(0.36f, 0.06f, 0.5f),
				plateMaterial);

			// Wedge tip toward escape on this face.
			CreateCube(
				parent,
				"Chevron",
				faceCenter + escape * 0.12f + lateral * 0.02f,
				look * Quaternion.Euler(0f, 0f, 45f),
				new Vector3(0.22f, 0.08f, 0.22f),
				accentMaterial);
		}

		private static Vector3[] GetLateralAxes(Vector3 escape)
		{
			if (Mathf.Abs(escape.x) > 0.5f)
			{
				return new[] { Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
			}

			if (Mathf.Abs(escape.y) > 0.5f)
			{
				return new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
			}

			return new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down };
		}

		private static GameObject CreateCube(
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
