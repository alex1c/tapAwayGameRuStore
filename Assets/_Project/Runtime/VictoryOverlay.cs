using System;
using UnityEngine;
using UnityEngine.UI;

namespace TapAway.Runtime
{
	/// <summary>
	/// Victory overlay with Restart + optional Next Level CTAs (safe-area aware).
	/// </summary>
	public sealed class VictoryOverlay : MonoBehaviour
	{
		private GameObject _root;
		private Action _onRestart;
		private Action _onNext;
		private Font _font;
		private Text _title;
		private Text _subtitle;
		private GameObject _nextButton;
		private Text _metricsLabel;

		public bool IsVisible => _root != null && _root.activeSelf;

		public void Configure(Action onRestart, Action onNext = null)
		{
			_onRestart = onRestart;
			_onNext = onNext;
			EnsureUi();
			Hide();
		}

		public void Show(string title = null, string subtitle = null, bool showNext = false, string metrics = null)
		{
			EnsureUi();
			if (_title != null && !string.IsNullOrEmpty(title))
			{
				_title.text = title;
			}

			if (_subtitle != null)
			{
				_subtitle.text = subtitle ?? string.Empty;
				_subtitle.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
			}

			if (_nextButton != null)
			{
				_nextButton.SetActive(showNext && _onNext != null);
			}

			if (_metricsLabel != null)
			{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
				_metricsLabel.text = metrics ?? string.Empty;
				_metricsLabel.gameObject.SetActive(!string.IsNullOrEmpty(metrics));
#else
				_metricsLabel.gameObject.SetActive(false);
#endif
			}

			_root.SetActive(true);
		}

		public void Show()
		{
			Show("Уровень пройден", null, _onNext != null, null);
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
			_title = titleGo.AddComponent<Text>();
			_title.font = _font;
			_title.text = "Уровень пройден";
			_title.alignment = TextAnchor.MiddleCenter;
			_title.fontSize = 64;
			_title.color = Color.white;
			var titleRt = titleGo.GetComponent<RectTransform>();
			titleRt.anchorMin = new Vector2(0.08f, 0.58f);
			titleRt.anchorMax = new Vector2(0.92f, 0.72f);
			titleRt.offsetMin = Vector2.zero;
			titleRt.offsetMax = Vector2.zero;

			var subGo = CreateUi("Subtitle", safe.transform);
			_subtitle = subGo.AddComponent<Text>();
			_subtitle.font = _font;
			_subtitle.text = string.Empty;
			_subtitle.alignment = TextAnchor.MiddleCenter;
			_subtitle.fontSize = 32;
			_subtitle.color = new Color(0.85f, 0.9f, 1f, 1f);
			var subRt = subGo.GetComponent<RectTransform>();
			subRt.anchorMin = new Vector2(0.1f, 0.5f);
			subRt.anchorMax = new Vector2(0.9f, 0.58f);
			subRt.offsetMin = Vector2.zero;
			subRt.offsetMax = Vector2.zero;

			_nextButton = CreateButton(
				safe.transform,
				"NextButton",
				"Следующий уровень",
				new Color(0.2f, 0.55f, 0.85f, 1f),
				new Vector2(0.18f, 0.36f),
				new Vector2(0.82f, 0.46f),
				() => _onNext?.Invoke());

			CreateButton(
				safe.transform,
				"RestartButton",
				"Restart",
				new Color(0.2f, 0.65f, 0.35f, 1f),
				new Vector2(0.22f, 0.24f),
				new Vector2(0.78f, 0.34f),
				() => _onRestart?.Invoke());

			var metricsGo = CreateUi("Metrics", safe.transform);
			_metricsLabel = metricsGo.AddComponent<Text>();
			_metricsLabel.font = _font;
			_metricsLabel.fontSize = 22;
			_metricsLabel.alignment = TextAnchor.UpperCenter;
			_metricsLabel.color = new Color(0.75f, 0.8f, 0.85f, 1f);
			var mrt = metricsGo.GetComponent<RectTransform>();
			mrt.anchorMin = new Vector2(0.06f, 0.08f);
			mrt.anchorMax = new Vector2(0.94f, 0.22f);
			mrt.offsetMin = Vector2.zero;
			mrt.offsetMax = Vector2.zero;
			_metricsLabel.gameObject.SetActive(false);
		}

		private GameObject CreateButton(
			Transform parent,
			string name,
			string labelText,
			Color color,
			Vector2 anchorMin,
			Vector2 anchorMax,
			Action onClick)
		{
			var buttonGo = CreateUi(name, parent);
			var buttonImage = buttonGo.AddComponent<Image>();
			buttonImage.color = color;
			var button = buttonGo.AddComponent<Button>();
			button.targetGraphic = buttonImage;
			button.onClick.AddListener(() => onClick?.Invoke());
			var buttonRt = buttonGo.GetComponent<RectTransform>();
			buttonRt.anchorMin = anchorMin;
			buttonRt.anchorMax = anchorMax;
			buttonRt.offsetMin = Vector2.zero;
			buttonRt.offsetMax = Vector2.zero;

			var labelGo = CreateUi("Label", buttonGo.transform);
			var label = labelGo.AddComponent<Text>();
			label.font = _font;
			label.text = labelText;
			label.alignment = TextAnchor.MiddleCenter;
			label.fontSize = 40;
			label.color = Color.white;
			StretchFull(labelGo.GetComponent<RectTransform>());
			return buttonGo;
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
