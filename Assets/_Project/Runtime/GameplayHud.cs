using System;
using UnityEngine;
using UnityEngine.UI;

namespace TapAway.Runtime
{
	/// <summary>
	/// Clean Phase 2 gameplay HUD: level label, remaining blocks, restart.
	/// Uses SafeAreaFitter for bottom/top insets.
	/// </summary>
	public sealed class GameplayHud : MonoBehaviour
	{
		private GameObject _root;
		private Text _levelLabel;
		private Text _remainingLabel;
		private Text _devInfoLabel;
		private Action _onRestart;
		private Font _font;

		public void Configure(Action onRestart)
		{
			_onRestart = onRestart;
			EnsureUi();
			Show();
		}

		public void SetLevelName(string name)
		{
			EnsureUi();
			if (_levelLabel != null)
			{
				_levelLabel.text = name;
			}
		}

		public void SetRemaining(int remaining, int total)
		{
			EnsureUi();
			if (_remainingLabel != null)
			{
				_remainingLabel.text = "Осталось: " + remaining + " / " + total;
			}
		}

		/// <summary>Development-only metadata line (seed / difficulty).</summary>
		public void SetDevInfo(string info)
		{
			EnsureUi();
			if (_devInfoLabel == null)
			{
				return;
			}

#if UNITY_EDITOR || DEVELOPMENT_BUILD
			_devInfoLabel.text = info ?? string.Empty;
			_devInfoLabel.gameObject.SetActive(!string.IsNullOrEmpty(info));
#else
			_devInfoLabel.gameObject.SetActive(false);
#endif
		}

		public void Show()
		{
			EnsureUi();
			_root.SetActive(true);
		}

		public void Hide()
		{
			if (_root != null)
			{
				_root.SetActive(false);
			}
		}

		private void EnsureUi()
		{
			if (_root != null)
			{
				return;
			}

			_font = ResolveFont();
			_root = new GameObject("GameplayHud");
			_root.transform.SetParent(transform, false);

			var canvas = _root.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 40;
			var scaler = _root.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1080f, 1920f);
			_root.AddComponent<GraphicRaycaster>();

			var safe = CreateUi("SafeArea", _root.transform);
			StretchFull(safe.GetComponent<RectTransform>());
			safe.AddComponent<SafeAreaFitter>();

			_levelLabel = CreateText(safe.transform, "LevelLabel", "Tap Away", 40,
				new Vector2(0.06f, 0.88f), new Vector2(0.7f, 0.96f), TextAnchor.MiddleLeft);

			_remainingLabel = CreateText(safe.transform, "Remaining", "Осталось: 0 / 0", 34,
				new Vector2(0.06f, 0.82f), new Vector2(0.7f, 0.88f), TextAnchor.MiddleLeft);

			_devInfoLabel = CreateText(safe.transform, "DevInfo", string.Empty, 22,
				new Vector2(0.06f, 0.76f), new Vector2(0.94f, 0.82f), TextAnchor.MiddleLeft);
			_devInfoLabel.color = new Color(0.7f, 0.85f, 1f, 0.9f);
			_devInfoLabel.gameObject.SetActive(false);

			var restartGo = CreateUi("RestartButton", safe.transform);
			var img = restartGo.AddComponent<Image>();
			img.color = new Color(0.18f, 0.22f, 0.3f, 0.85f);
			var button = restartGo.AddComponent<Button>();
			button.targetGraphic = img;
			button.onClick.AddListener(() => _onRestart?.Invoke());
			var rt = restartGo.GetComponent<RectTransform>();
			rt.anchorMin = new Vector2(0.68f, 0.88f);
			rt.anchorMax = new Vector2(0.94f, 0.96f);
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;

			var restartLabel = CreateText(restartGo.transform, "Label", "Restart", 32,
				Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
			StretchFull(restartLabel.rectTransform);
		}

		private Text CreateText(
			Transform parent,
			string name,
			string value,
			int size,
			Vector2 anchorMin,
			Vector2 anchorMax,
			TextAnchor align)
		{
			var go = CreateUi(name, parent);
			var text = go.AddComponent<Text>();
			text.font = _font;
			text.text = value;
			text.fontSize = size;
			text.color = Color.white;
			text.alignment = align;
			text.horizontalOverflow = HorizontalWrapMode.Overflow;
			var rt = go.GetComponent<RectTransform>();
			rt.anchorMin = anchorMin;
			rt.anchorMax = anchorMax;
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;
			return text;
		}

		private static Font ResolveFont()
		{
			var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			if (font == null)
			{
				font = Resources.GetBuiltinResource<Font>("Arial.ttf");
			}

			if (font == null)
			{
				font = Font.CreateDynamicFontFromOSFont("Arial", 32);
			}

			return font;
		}

		private static GameObject CreateUi(string name, Transform parent)
		{
			var go = new GameObject(name, typeof(RectTransform));
			go.transform.SetParent(parent, false);
			return go;
		}

		private static void StretchFull(RectTransform rt)
		{
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;
		}
	}
}
