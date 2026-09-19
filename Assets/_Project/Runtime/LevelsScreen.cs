using System;
using System.Collections.Generic;
using TapAway.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapAway.Runtime
{
	/// <summary>Scrollable 30-level campaign select with locked/unlocked/completed states.</summary>
	public sealed class LevelsScreen : MonoBehaviour
	{
		private GameObject _root;
		private Font _font;
		private Transform _content;
		private readonly List<Button> _buttons = new List<Button>();
		private readonly List<Text> _labels = new List<Text>();
		private Action<int> _onSelect;
		private Action _onBack;

		public bool IsVisible => _root != null && _root.activeSelf;

		public void Configure(Action<int> onSelect, Action onBack)
		{
			_onSelect = onSelect;
			_onBack = onBack;
			EnsureUi();
			Hide();
		}

		public void Show(CampaignProgress progress)
		{
			EnsureUi();
			Refresh(progress);
			_root.SetActive(true);
		}

		public void Hide()
		{
			if (_root != null)
			{
				_root.SetActive(false);
			}
		}

		private void Refresh(CampaignProgress progress)
		{
			for (var i = 0; i < CampaignDefinition.LevelCount; i++)
			{
				var level = i + 1;
				var unlocked = progress != null && progress.IsUnlocked(level);
				var completed = progress != null && progress.IsCompleted(level);
				var current = progress != null && progress.HighestUnlockedLevel == level && !completed;

				var button = _buttons[i];
				button.interactable = unlocked;
				var img = button.targetGraphic as Image;
				if (img != null)
				{
					if (!unlocked)
					{
						img.color = new Color(0.18f, 0.18f, 0.22f, 0.85f);
					}
					else if (completed)
					{
						img.color = new Color(0.18f, 0.5f, 0.32f, 1f);
					}
					else if (current)
					{
						img.color = new Color(0.2f, 0.5f, 0.8f, 1f);
					}
					else
					{
						img.color = new Color(0.28f, 0.32f, 0.42f, 1f);
					}
				}

				var label = _labels[i];
				if (!unlocked)
				{
					label.text = level + " 🔒";
				}
				else if (completed)
				{
					label.text = level + " ✓";
				}
				else
				{
					label.text = level.ToString();
				}
			}
		}

		private void EnsureUi()
		{
			if (_root != null)
			{
				return;
			}

			_font = UiFactory.ResolveFont();
			var canvas = UiFactory.CreateOverlayCanvas(transform, "LevelsScreen", 55);
			_root = canvas.gameObject;

			var safe = UiFactory.CreateUi("SafeArea", _root.transform);
			UiFactory.StretchFull(safe.GetComponent<RectTransform>());
			safe.AddComponent<SafeAreaFitter>();

			UiFactory.CreateText(
				safe.transform, "Title", "Уровни", 56,
				new Vector2(0.1f, 0.9f), new Vector2(0.7f, 0.98f),
				TextAnchor.MiddleLeft, _font);

			UiFactory.CreateButton(
				safe.transform, "BackButton", "Назад",
				new Color(0.25f, 0.28f, 0.35f, 1f),
				new Vector2(0.72f, 0.9f), new Vector2(0.94f, 0.98f),
				_font, () => _onBack?.Invoke());

			var scrollGo = UiFactory.CreateUi("Scroll", safe.transform);
			var scrollRt = scrollGo.GetComponent<RectTransform>();
			scrollRt.anchorMin = new Vector2(0.05f, 0.06f);
			scrollRt.anchorMax = new Vector2(0.95f, 0.88f);
			scrollRt.offsetMin = Vector2.zero;
			scrollRt.offsetMax = Vector2.zero;
			var scroll = scrollGo.AddComponent<ScrollRect>();
			scroll.horizontal = false;
			scroll.vertical = true;
			scroll.movementType = ScrollRect.MovementType.Clamped;

			var viewport = UiFactory.CreateUi("Viewport", scrollGo.transform);
			UiFactory.StretchFull(viewport.GetComponent<RectTransform>());
			viewport.AddComponent<RectMask2D>();
			scroll.viewport = viewport.GetComponent<RectTransform>();

			var content = UiFactory.CreateUi("Content", viewport.transform);
			_content = content.transform;
			var contentRt = content.GetComponent<RectTransform>();
			contentRt.anchorMin = new Vector2(0f, 1f);
			contentRt.anchorMax = new Vector2(1f, 1f);
			contentRt.pivot = new Vector2(0.5f, 1f);
			var grid = content.AddComponent<GridLayoutGroup>();
			grid.cellSize = new Vector2(150f, 150f);
			grid.spacing = new Vector2(18f, 18f);
			grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			grid.constraintCount = 4;
			grid.padding = new RectOffset(12, 12, 12, 12);
			grid.childAlignment = TextAnchor.UpperCenter;
			var fitter = content.AddComponent<ContentSizeFitter>();
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			scroll.content = contentRt;

			for (var i = 0; i < CampaignDefinition.LevelCount; i++)
			{
				var level = i + 1;
				var btnGo = UiFactory.CreateUi("Level_" + level, _content);
				var img = btnGo.AddComponent<Image>();
				img.color = new Color(0.28f, 0.32f, 0.42f, 1f);
				var button = btnGo.AddComponent<Button>();
				button.targetGraphic = img;
				var captured = level;
				button.onClick.AddListener(() => _onSelect?.Invoke(captured));
				_buttons.Add(button);

				var labelGo = UiFactory.CreateUi("Label", btnGo.transform);
				var label = labelGo.AddComponent<Text>();
				label.font = _font;
				label.text = level.ToString();
				label.fontSize = 40;
				label.alignment = TextAnchor.MiddleCenter;
				label.color = Color.white;
				label.raycastTarget = false;
				UiFactory.StretchFull(labelGo.GetComponent<RectTransform>());
				_labels.Add(label);
			}
		}
	}
}
