using System;
using UnityEngine;
using UnityEngine.UI;

namespace TapAway.Runtime
{
	/// <summary>
	/// Victory overlay with safe-area aware Restart CTA.
	/// </summary>
	public sealed class VictoryOverlay : MonoBehaviour
	{
		private GameObject _root;
		private Action _onRestart;
		private Font _font;

		public bool IsVisible => _root != null && _root.activeSelf;

		public void Configure(Action onRestart)
		{
			_onRestart = onRestart;
			EnsureUi();
			Hide();
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
			_root = new GameObject("VictoryOverlay");
			_root.transform.SetParent(transform, false);

			var canvas = _root.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 100;
			var scaler = _root.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1080f, 1920f);
			_root.AddComponent<GraphicRaycaster>();

			EnsureEventSystem();

			var dim = CreateUi("Dim", _root.transform);
			var dimImage = dim.AddComponent<Image>();
			dimImage.color = new Color(0f, 0f, 0f, 0.55f);
			StretchFull(dim.GetComponent<RectTransform>());

			var safe = CreateUi("SafeArea", _root.transform);
			StretchFull(safe.GetComponent<RectTransform>());
			safe.AddComponent<SafeAreaFitter>();

			var titleGo = CreateUi("Title", safe.transform);
			var title = titleGo.AddComponent<Text>();
			title.font = _font;
			title.text = "Уровень пройден";
			title.alignment = TextAnchor.MiddleCenter;
			title.fontSize = 64;
			title.color = Color.white;
			var titleRt = titleGo.GetComponent<RectTransform>();
			titleRt.anchorMin = new Vector2(0.08f, 0.55f);
			titleRt.anchorMax = new Vector2(0.92f, 0.7f);
			titleRt.offsetMin = Vector2.zero;
			titleRt.offsetMax = Vector2.zero;

			var buttonGo = CreateUi("RestartButton", safe.transform);
			var buttonImage = buttonGo.AddComponent<Image>();
			buttonImage.color = new Color(0.2f, 0.65f, 0.35f, 1f);
			var button = buttonGo.AddComponent<Button>();
			button.targetGraphic = buttonImage;
			button.onClick.AddListener(() => _onRestart?.Invoke());
			var buttonRt = buttonGo.GetComponent<RectTransform>();
			// Mid-lower band inside safe area — not glued to physical bottom.
			buttonRt.anchorMin = new Vector2(0.22f, 0.28f);
			buttonRt.anchorMax = new Vector2(0.78f, 0.38f);
			buttonRt.offsetMin = Vector2.zero;
			buttonRt.offsetMax = Vector2.zero;

			var labelGo = CreateUi("RestartLabel", buttonGo.transform);
			var label = labelGo.AddComponent<Text>();
			label.font = _font;
			label.text = "Restart";
			label.alignment = TextAnchor.MiddleCenter;
			label.fontSize = 42;
			label.color = Color.white;
			StretchFull(labelGo.GetComponent<RectTransform>());
		}

		private static void EnsureEventSystem()
		{
			if (UnityEngine.EventSystems.EventSystem.current != null)
			{
				return;
			}

			var eventSystem = new GameObject("EventSystem");
			eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
			eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
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
