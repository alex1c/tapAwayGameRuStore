using System;
using UnityEngine;
using UnityEngine.UI;

namespace TapAway.Runtime
{
	/// <summary>
	/// Minimal victory overlay: message + Restart. No production UI polish.
	/// </summary>
	public sealed class VictoryOverlay : MonoBehaviour
	{
		private GameObject _root;
		private Action _onRestart;

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

			_root = new GameObject("VictoryOverlay");
			_root.transform.SetParent(transform, false);

			var canvas = _root.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 100;
			_root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			_root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080f, 1920f);
			_root.AddComponent<GraphicRaycaster>();

			if (UnityEngine.EventSystems.EventSystem.current == null)
			{
				var eventSystem = new GameObject("EventSystem");
				eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
				eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
			}

			var panel = CreateUiObject("Panel", _root.transform);
			var panelImage = panel.AddComponent<Image>();
			panelImage.color = new Color(0f, 0f, 0f, 0.55f);
			StretchFull(panel.GetComponent<RectTransform>());

			var title = CreateUiObject("Title", panel.transform);
			var titleText = title.AddComponent<Text>();
			titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			if (titleText.font == null)
			{
				titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
			}

			titleText.text = "Уровень пройден";
			titleText.alignment = TextAnchor.MiddleCenter;
			titleText.fontSize = 64;
			titleText.color = Color.white;
			var titleRt = title.GetComponent<RectTransform>();
			titleRt.anchorMin = new Vector2(0.1f, 0.55f);
			titleRt.anchorMax = new Vector2(0.9f, 0.7f);
			titleRt.offsetMin = Vector2.zero;
			titleRt.offsetMax = Vector2.zero;

			var buttonGo = CreateUiObject("RestartButton", panel.transform);
			var buttonImage = buttonGo.AddComponent<Image>();
			buttonImage.color = new Color(0.2f, 0.65f, 0.35f, 1f);
			var button = buttonGo.AddComponent<Button>();
			button.targetGraphic = buttonImage;
			button.onClick.AddListener(() => _onRestart?.Invoke());
			var buttonRt = buttonGo.GetComponent<RectTransform>();
			buttonRt.anchorMin = new Vector2(0.25f, 0.38f);
			buttonRt.anchorMax = new Vector2(0.75f, 0.48f);
			buttonRt.offsetMin = Vector2.zero;
			buttonRt.offsetMax = Vector2.zero;

			// Keep CTA above typical gesture/nav insets (playbook safe-area note).
			buttonRt.anchoredPosition += new Vector2(0f, 40f);

			var labelGo = CreateUiObject("RestartLabel", buttonGo.transform);
			var label = labelGo.AddComponent<Text>();
			label.font = titleText.font;
			label.text = "Restart";
			label.alignment = TextAnchor.MiddleCenter;
			label.fontSize = 42;
			label.color = Color.white;
			StretchFull(labelGo.GetComponent<RectTransform>());
		}

		private static GameObject CreateUiObject(string name, Transform parent)
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
