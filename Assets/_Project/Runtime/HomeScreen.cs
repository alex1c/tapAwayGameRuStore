using System;
using TapAway.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapAway.Runtime
{
	/// <summary>Phase 5 Home screen — Play / Continue / Levels / Tutorial.</summary>
	public sealed class HomeScreen : MonoBehaviour
	{
		private GameObject _root;
		private Font _font;
		private Text _subtitle;
		private GameObject _continueButton;
		private Action _onPlay;
		private Action _onContinue;
		private Action _onLevels;
		private Action _onTutorial;

		public bool IsVisible => _root != null && _root.activeSelf;

		public void Configure(
			Action onPlay,
			Action onContinue,
			Action onLevels,
			Action onTutorial)
		{
			_onPlay = onPlay;
			_onContinue = onContinue;
			_onLevels = onLevels;
			_onTutorial = onTutorial;
			EnsureUi();
			Hide();
		}

		public void Show(CampaignProgress progress)
		{
			EnsureUi();
			var hasProgress = progress != null &&
			                  (progress.TutorialCompleted ||
			                   progress.HighestUnlockedLevel > 1 ||
			                   progress.IsCompleted(1));
			if (_continueButton != null)
			{
				_continueButton.SetActive(hasProgress);
			}

			if (_subtitle != null)
			{
				if (hasProgress)
				{
					_subtitle.text = "Прогресс: уровень " + progress.HighestUnlockedLevel +
					                 " / " + CampaignDefinition.LevelCount;
				}
				else
				{
					_subtitle.text = "Собери путь и освободи блоки";
				}
			}

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

			_font = UiFactory.ResolveFont();
			var canvas = UiFactory.CreateOverlayCanvas(transform, "HomeScreen", 50);
			_root = canvas.gameObject;

			var safe = UiFactory.CreateUi("SafeArea", _root.transform);
			UiFactory.StretchFull(safe.GetComponent<RectTransform>());
			safe.AddComponent<SafeAreaFitter>();

			UiFactory.CreateText(
				safe.transform, "Title", "Tap Away", 72,
				new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.86f),
				TextAnchor.MiddleCenter, _font);

			_subtitle = UiFactory.CreateText(
				safe.transform, "Subtitle", string.Empty, 30,
				new Vector2(0.1f, 0.64f), new Vector2(0.9f, 0.72f),
				TextAnchor.MiddleCenter, _font,
				new Color(0.8f, 0.85f, 0.95f, 1f));

			UiFactory.CreateButton(
				safe.transform, "PlayButton", "Играть",
				new Color(0.2f, 0.55f, 0.85f, 1f),
				new Vector2(0.18f, 0.48f), new Vector2(0.82f, 0.58f),
				_font, () => _onPlay?.Invoke());

			_continueButton = UiFactory.CreateButton(
				safe.transform, "ContinueButton", "Продолжить",
				new Color(0.2f, 0.65f, 0.4f, 1f),
				new Vector2(0.18f, 0.36f), new Vector2(0.82f, 0.46f),
				_font, () => _onContinue?.Invoke());

			UiFactory.CreateButton(
				safe.transform, "LevelsButton", "Уровни",
				new Color(0.25f, 0.28f, 0.38f, 1f),
				new Vector2(0.18f, 0.24f), new Vector2(0.82f, 0.34f),
				_font, () => _onLevels?.Invoke());

			UiFactory.CreateButton(
				safe.transform, "TutorialButton", "Обучение",
				new Color(0.3f, 0.3f, 0.35f, 0.9f),
				new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.2f),
				_font, () => _onTutorial?.Invoke());
		}
	}
}
