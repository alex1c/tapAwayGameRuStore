using System;
using UnityEngine;
using UnityEngine.UI;

namespace TapAway.Runtime
{
	/// <summary>Minimal pause menu: Continue / Restart / Home.</summary>
	public sealed class PauseOverlay : MonoBehaviour
	{
		private GameObject _root;
		private Font _font;
		private Action _onContinue;
		private Action _onRestart;
		private Action _onHome;

		public bool IsVisible => _root != null && _root.activeSelf;

		public void Configure(Action onContinue, Action onRestart, Action onHome)
		{
			_onContinue = onContinue;
			_onRestart = onRestart;
			_onHome = onHome;
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

			_font = UiFactory.ResolveFont();
			var canvas = UiFactory.CreateOverlayCanvas(transform, "PauseOverlay", 90);
			_root = canvas.gameObject;

			var dim = UiFactory.CreateUi("Dim", _root.transform);
			var dimImage = dim.AddComponent<Image>();
			dimImage.color = new Color(0f, 0f, 0f, 0.55f);
			UiFactory.StretchFull(dim.GetComponent<RectTransform>());

			var safe = UiFactory.CreateUi("SafeArea", _root.transform);
			UiFactory.StretchFull(safe.GetComponent<RectTransform>());
			safe.AddComponent<SafeAreaFitter>();

			UiFactory.CreateText(
				safe.transform, "Title", "Пауза", 56,
				new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.74f),
				TextAnchor.MiddleCenter, _font);

			UiFactory.CreateButton(
				safe.transform, "Continue", "Продолжить",
				new Color(0.2f, 0.55f, 0.85f, 1f),
				new Vector2(0.18f, 0.48f), new Vector2(0.82f, 0.58f),
				_font, () => _onContinue?.Invoke());

			UiFactory.CreateButton(
				safe.transform, "Restart", "Заново",
				new Color(0.2f, 0.65f, 0.35f, 1f),
				new Vector2(0.18f, 0.36f), new Vector2(0.82f, 0.46f),
				_font, () => _onRestart?.Invoke());

			UiFactory.CreateButton(
				safe.transform, "Home", "На главную",
				new Color(0.3f, 0.3f, 0.38f, 1f),
				new Vector2(0.18f, 0.24f), new Vector2(0.82f, 0.34f),
				_font, () => _onHome?.Invoke());
		}
	}
}
