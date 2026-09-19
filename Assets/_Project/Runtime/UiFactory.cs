using System;
using UnityEngine;
using UnityEngine.UI;

namespace TapAway.Runtime
{
	/// <summary>Shared UI helpers for Phase 5 prototype screens.</summary>
	internal static class UiFactory
	{
		public static Font ResolveFont()
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

		public static GameObject CreateUi(string name, Transform parent)
		{
			var go = new GameObject(name, typeof(RectTransform));
			go.transform.SetParent(parent, false);
			return go;
		}

		public static void StretchFull(RectTransform rt)
		{
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;
		}

		public static Text CreateText(
			Transform parent,
			string name,
			string value,
			int size,
			Vector2 anchorMin,
			Vector2 anchorMax,
			TextAnchor align,
			Font font,
			Color? color = null)
		{
			var go = CreateUi(name, parent);
			var text = go.AddComponent<Text>();
			text.font = font;
			text.text = value;
			text.fontSize = size;
			text.color = color ?? Color.white;
			text.alignment = align;
			text.raycastTarget = false;
			text.horizontalOverflow = HorizontalWrapMode.Overflow;
			var rt = go.GetComponent<RectTransform>();
			rt.anchorMin = anchorMin;
			rt.anchorMax = anchorMax;
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;
			return text;
		}

		public static GameObject CreateButton(
			Transform parent,
			string name,
			string labelText,
			Color color,
			Vector2 anchorMin,
			Vector2 anchorMax,
			Font font,
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
			label.font = font;
			label.text = labelText;
			label.alignment = TextAnchor.MiddleCenter;
			label.fontSize = 36;
			label.color = Color.white;
			label.raycastTarget = false;
			StretchFull(labelGo.GetComponent<RectTransform>());
			return buttonGo;
		}

		public static Canvas CreateOverlayCanvas(Transform parent, string name, int sortingOrder)
		{
			var root = new GameObject(name);
			root.transform.SetParent(parent, false);
			var canvas = root.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = sortingOrder;
			var scaler = root.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1080f, 1920f);
			root.AddComponent<GraphicRaycaster>();
			var group = root.AddComponent<CanvasGroup>();
			group.blocksRaycasts = true;
			return canvas;
		}
	}
}
