using System;
using TapAway.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapAway.Runtime
{
	/// <summary>
	/// Minimal interactive tutorial chrome driven by <see cref="TutorialState"/>.
	/// </summary>
	public sealed class TutorialController : MonoBehaviour
	{
		private TutorialState _state;
		private PuzzlePresenter _presenter;
		private GameObject _root;
		private Text _prompt;
		private Font _font;
		private BlockId _hintBlockId = new BlockId(1);
		private float _blockedPromptSeconds;
		private Action _onCompleted;

		public TutorialState State => _state;
		public bool IsBlockingPuzzle => false;

		public void Configure(
			TutorialState state,
			PuzzlePresenter presenter,
			BlockId hintBlockId,
			Action onCompleted)
		{
			_state = state;
			_presenter = presenter;
			_hintBlockId = hintBlockId;
			_onCompleted = onCompleted;
			EnsureUi();
			Refresh();
		}

		public void NotifyMove(MoveResult result)
		{
			if (_state == null || !_state.IsActive)
			{
				return;
			}

			if (result.Status == MoveStatus.Allowed)
			{
				_state.NotifyAllowedRemoval();
				_blockedPromptSeconds = 0f;
			}
			else if (result.Status == MoveStatus.Blocked)
			{
				_state.NotifyBlockedAttempt();
			}

			Refresh();
		}

		public void NotifyMeaningfulDrag()
		{
			if (_state == null)
			{
				return;
			}

			_state.NotifyMeaningfulDrag();
			Refresh();
			if (_state.IsCompleted)
			{
				_onCompleted?.Invoke();
			}
		}

		public void Skip()
		{
			_state?.Skip();
			_presenter?.ClearTutorialHighlight();
			Hide();
			_onCompleted?.Invoke();
		}

		private void Update()
		{
			if (_state == null || !_state.IsActive)
			{
				return;
			}

			// Soft advance: after showing blocked prompt briefly, allow rotate step
			// even if the player doesn't tap a blocked block.
			if (_state.Step == TutorialStep.ExplainBlocked)
			{
				_blockedPromptSeconds += Time.deltaTime;
				if (_blockedPromptSeconds >= 2.5f)
				{
					_state.NotifyContinueFromBlockedPrompt();
					Refresh();
				}
			}
		}

		public void Hide()
		{
			if (_root != null)
			{
				_root.SetActive(false);
			}
		}

		private void Refresh()
		{
			EnsureUi();
			if (_state == null || !_state.IsActive)
			{
				if (_state != null && _state.IsCompleted)
				{
					_prompt.text = _state.GetPromptRu();
					_root.SetActive(true);
					_presenter?.ClearTutorialHighlight();
					CancelInvoke();
					Invoke(nameof(Hide), 1.0f);
				}
				else
				{
					Hide();
				}

				return;
			}

			_root.SetActive(true);
			_prompt.text = _state.GetPromptRu();

			if (_state.Step == TutorialStep.TapRemovable)
			{
				_presenter?.SetTutorialHighlight(_hintBlockId);
			}
			else
			{
				_presenter?.ClearTutorialHighlight();
			}
		}

		private void EnsureUi()
		{
			if (_root != null)
			{
				return;
			}

			_font = ResolveFont();
			_root = new GameObject("TutorialOverlay");
			_root.transform.SetParent(transform, false);

			var canvas = _root.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 60;
			var scaler = _root.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1080f, 1920f);
			_root.AddComponent<GraphicRaycaster>();

			var safe = new GameObject("SafeArea", typeof(RectTransform));
			safe.transform.SetParent(_root.transform, false);
			var safeRt = safe.GetComponent<RectTransform>();
			safeRt.anchorMin = Vector2.zero;
			safeRt.anchorMax = Vector2.one;
			safeRt.offsetMin = Vector2.zero;
			safeRt.offsetMax = Vector2.zero;
			safe.AddComponent<SafeAreaFitter>();

			var panel = new GameObject("PromptPanel", typeof(RectTransform));
			panel.transform.SetParent(safe.transform, false);
			var panelImg = panel.AddComponent<Image>();
			panelImg.color = new Color(0f, 0f, 0f, 0.55f);
			var panelRt = panel.GetComponent<RectTransform>();
			panelRt.anchorMin = new Vector2(0.08f, 0.14f);
			panelRt.anchorMax = new Vector2(0.92f, 0.26f);
			panelRt.offsetMin = Vector2.zero;
			panelRt.offsetMax = Vector2.zero;

			var promptGo = new GameObject("Prompt", typeof(RectTransform));
			promptGo.transform.SetParent(panel.transform, false);
			_prompt = promptGo.AddComponent<Text>();
			_prompt.font = _font;
			_prompt.fontSize = 36;
			_prompt.alignment = TextAnchor.MiddleCenter;
			_prompt.color = Color.white;
			var promptRt = promptGo.GetComponent<RectTransform>();
			promptRt.anchorMin = Vector2.zero;
			promptRt.anchorMax = Vector2.one;
			promptRt.offsetMin = new Vector2(16f, 8f);
			promptRt.offsetMax = new Vector2(-16f, -8f);

			var skipGo = new GameObject("Skip", typeof(RectTransform));
			skipGo.transform.SetParent(safe.transform, false);
			var skipImg = skipGo.AddComponent<Image>();
			skipImg.color = new Color(0.2f, 0.2f, 0.25f, 0.7f);
			var skipBtn = skipGo.AddComponent<Button>();
			skipBtn.targetGraphic = skipImg;
			skipBtn.onClick.AddListener(Skip);
			var skipRt = skipGo.GetComponent<RectTransform>();
			skipRt.anchorMin = new Vector2(0.72f, 0.08f);
			skipRt.anchorMax = new Vector2(0.94f, 0.13f);
			skipRt.offsetMin = Vector2.zero;
			skipRt.offsetMax = Vector2.zero;

			var skipLabelGo = new GameObject("SkipLabel", typeof(RectTransform));
			skipLabelGo.transform.SetParent(skipGo.transform, false);
			var skipLabel = skipLabelGo.AddComponent<Text>();
			skipLabel.font = _font;
			skipLabel.text = "Пропустить";
			skipLabel.fontSize = 28;
			skipLabel.alignment = TextAnchor.MiddleCenter;
			skipLabel.color = Color.white;
			var skipLabelRt = skipLabelGo.GetComponent<RectTransform>();
			skipLabelRt.anchorMin = Vector2.zero;
			skipLabelRt.anchorMax = Vector2.one;
			skipLabelRt.offsetMin = Vector2.zero;
			skipLabelRt.offsetMax = Vector2.zero;
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
	}
}
