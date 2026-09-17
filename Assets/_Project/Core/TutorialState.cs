namespace TapAway.Core
{
	/// <summary>
	/// Lightweight interactive tutorial state machine.
	/// Does not affect puzzle legality — presentation drives prompts only.
	/// </summary>
	public enum TutorialStep
	{
		None = 0,
		TapRemovable = 1,
		ExplainBlocked = 2,
		RotateView = 3,
		Completed = 4
	}

	/// <summary>
	/// Tracks onboarding progress for a single play session.
	/// </summary>
	public sealed class TutorialState
	{
		public TutorialStep Step { get; private set; }
		public bool IsActive => Step != TutorialStep.None && Step != TutorialStep.Completed;
		public bool IsCompleted => Step == TutorialStep.Completed;
		public bool IsSkipped { get; private set; }

		public TutorialState(bool startActive)
		{
			Step = startActive ? TutorialStep.TapRemovable : TutorialStep.None;
		}

		public string GetPromptRu()
		{
			switch (Step)
			{
				case TutorialStep.TapRemovable:
					return "Нажмите на блок со стрелкой";
				case TutorialStep.ExplainBlocked:
					return "Блок может уйти только по стрелке";
				case TutorialStep.RotateView:
					return "Поверните фигуру";
				case TutorialStep.Completed:
					return "Отлично!";
				default:
					return string.Empty;
			}
		}

		public void Skip()
		{
			IsSkipped = true;
			Step = TutorialStep.Completed;
		}

		public void NotifyAllowedRemoval()
		{
			if (Step == TutorialStep.TapRemovable)
			{
				Step = TutorialStep.ExplainBlocked;
			}
		}

		public void NotifyBlockedAttempt()
		{
			if (Step == TutorialStep.ExplainBlocked)
			{
				Step = TutorialStep.RotateView;
			}
		}

		/// <summary>
		/// Advances from blocked explanation if the player skips blocked practice
		/// by continuing (optional soft path after first allowed move + any block attempt).
		/// </summary>
		public void NotifyContinueFromBlockedPrompt()
		{
			if (Step == TutorialStep.ExplainBlocked)
			{
				Step = TutorialStep.RotateView;
			}
		}

		public void NotifyMeaningfulDrag()
		{
			if (Step == TutorialStep.RotateView)
			{
				Step = TutorialStep.Completed;
			}
		}
	}
}
