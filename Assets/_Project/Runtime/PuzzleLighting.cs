using UnityEngine;
using UnityEngine.Rendering;

namespace TapAway.Runtime
{
	/// <summary>
	/// Lightweight mobile-friendly lighting for the prototype construction.
	/// Raises underside readability without an expensive lighting setup.
	/// </summary>
	public sealed class PuzzleLighting : MonoBehaviour
	{
		[SerializeField] private Color _ambient = new Color(0.42f, 0.45f, 0.52f, 1f);
		[SerializeField] private Color _keyColor = new Color(1f, 0.97f, 0.92f, 1f);
		[SerializeField] private float _keyIntensity = 1.05f;
		[SerializeField] private Color _fillColor = new Color(0.55f, 0.65f, 0.85f, 1f);
		[SerializeField] private float _fillIntensity = 0.55f;
		[SerializeField] private Color _bottomFillColor = new Color(0.7f, 0.75f, 0.8f, 1f);
		[SerializeField] private float _bottomFillIntensity = 0.4f;

		private Light _keyLight;
		private Light _fillLight;
		private Light _bottomFillLight;

		/// <summary>
		/// Applies ambient + key/fill lights suitable for inspecting all sides.
		/// </summary>
		public void Apply()
		{
			RenderSettings.ambientMode = AmbientMode.Flat;
			RenderSettings.ambientLight = _ambient;
			RenderSettings.subtractiveShadowColor = new Color(0.35f, 0.38f, 0.42f);

			_keyLight = EnsureLight("PuzzleKeyLight", ref _keyLight);
			_keyLight.type = LightType.Directional;
			_keyLight.color = _keyColor;
			_keyLight.intensity = _keyIntensity;
			_keyLight.shadows = LightShadows.Soft;
			_keyLight.transform.rotation = Quaternion.Euler(42f, -35f, 0f);

			_fillLight = EnsureLight("PuzzleFillLight", ref _fillLight);
			_fillLight.type = LightType.Directional;
			_fillLight.color = _fillColor;
			_fillLight.intensity = _fillIntensity;
			_fillLight.shadows = LightShadows.None;
			// Opposite-ish fill so vertical sides stay readable.
			_fillLight.transform.rotation = Quaternion.Euler(25f, 140f, 0f);

			_bottomFillLight = EnsureLight("PuzzleBottomFillLight", ref _bottomFillLight);
			_bottomFillLight.type = LightType.Directional;
			_bottomFillLight.color = _bottomFillColor;
			_bottomFillLight.intensity = _bottomFillIntensity;
			_bottomFillLight.shadows = LightShadows.None;
			// From below-front so underside blocks/arrows are not gameplay-black.
			_bottomFillLight.transform.rotation = Quaternion.Euler(-55f, 20f, 0f);

			// Soften any pre-existing scene sun that would crush ambient.
			foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
			{
				if (light == _keyLight || light == _fillLight || light == _bottomFillLight)
				{
					continue;
				}

				if (light.type == LightType.Directional && light.gameObject.scene.IsValid())
				{
					light.enabled = false;
				}
			}
		}

		private Light EnsureLight(string name, ref Light cached)
		{
			if (cached != null)
			{
				return cached;
			}

			var existing = transform.Find(name);
			GameObject go;
			if (existing != null)
			{
				go = existing.gameObject;
			}
			else
			{
				go = new GameObject(name);
				go.transform.SetParent(transform, false);
			}

			cached = go.GetComponent<Light>();
			if (cached == null)
			{
				cached = go.AddComponent<Light>();
			}

			return cached;
		}
	}
}
