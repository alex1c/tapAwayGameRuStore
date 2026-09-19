using System;
using TapAway.Core;
using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Phase 5 app shell: progress → Home → Campaign / Tutorial gameplay.
	/// Mid-level puzzles restart on Continue; campaign unlocks persist.
	/// </summary>
	public sealed class GameApp : MonoBehaviour
	{
		public static GameApp Instance { get; private set; }

		private LevelBootstrap _bootstrap;
		private HomeScreen _home;
		private LevelsScreen _levels;
		private PauseOverlay _pause;
		private ProgressRepository _repository;
		private CampaignProgress _progress;
		private AppScreen _screen = AppScreen.Home;
		private bool _paused;
		private float _pausedAt;
		private string _lastSaveDiagnostic = "none";

		public CampaignProgress Progress => _progress;
		public AppScreen CurrentScreen => _screen;
		public bool IsPaused => _paused;
		public string LastSaveDiagnostic => _lastSaveDiagnostic;
		public LevelBootstrap Bootstrap => _bootstrap;

		public enum AppScreen
		{
			Home,
			Levels,
			Gameplay,
			Tutorial
		}

		private void Awake()
		{
			Instance = this;
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}

		public void Initialize(LevelBootstrap bootstrap)
		{
			_bootstrap = bootstrap;
			EnsureUi();
			_repository = new ProgressRepository(new PersistentProgressStorage());
			var load = _repository.Load();
			_progress = load.Progress ?? CampaignProgress.CreateFresh();
			Debug.Log("[TapAway] Progress load: " + load.Diagnostic +
			          " tutorial=" + _progress.TutorialCompleted +
			          " unlocked=" + _progress.HighestUnlockedLevel);

			_bootstrap.SetCompletionHandler(OnGameplayCompleted);
			_bootstrap.SetHomeHandler(GoHome);
			_bootstrap.SetPauseHandler(TogglePause);

			// Dev/test overrides skip Home and enter gameplay directly.
			if (LevelBootstrap.DevQaIndexOverride.HasValue ||
			    LevelBootstrap.DevOverrideLevel != null)
			{
				HideAllMenus();
				_screen = AppScreen.Gameplay;
				_bootstrap.BeginLegacyAutoFlow();
				return;
			}

			ShowHome();
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				HandleAndroidBack();
			}
		}

		private void OnApplicationPause(bool pauseStatus)
		{
			if (pauseStatus && _screen == AppScreen.Gameplay && !_paused &&
			    _bootstrap != null && _bootstrap.Phase == GameplayPhase.Playing)
			{
				EnterPause();
			}
		}

		public void HandleAndroidBack()
		{
			switch (_screen)
			{
				case AppScreen.Levels:
					ShowHome();
					break;
				case AppScreen.Gameplay:
				case AppScreen.Tutorial:
					if (_bootstrap != null && _bootstrap.Victory != null && _bootstrap.Victory.IsVisible)
					{
						GoHome();
					}
					else if (_paused)
					{
						ResumePause();
					}
					else
					{
						EnterPause();
					}

					break;
				case AppScreen.Home:
				default:
					Application.Quit();
					break;
			}
		}

		public void ShowHome()
		{
			ResumePause();
			_bootstrap?.SuspendGameplayForMenu();
			_levels?.Hide();
			_pause?.Hide();
			_home.Show(_progress);
			_screen = AppScreen.Home;
		}

		public void ShowLevels()
		{
			ResumePause();
			_bootstrap?.SuspendGameplayForMenu();
			_home?.Hide();
			_pause?.Hide();
			_levels.Show(_progress);
			_screen = AppScreen.Levels;
		}

		public void OnPlayPressed()
		{
			if (_progress == null)
			{
				_progress = CampaignProgress.CreateFresh();
			}

			if (!_progress.TutorialCompleted)
			{
				StartTutorial(fromHome: true);
				return;
			}

			StartCampaignLevel(_progress.ResolveContinueLevel());
		}

		public void OnContinuePressed()
		{
			if (_progress == null)
			{
				OnPlayPressed();
				return;
			}

			if (!_progress.TutorialCompleted)
			{
				StartTutorial(fromHome: true);
				return;
			}

			StartCampaignLevel(_progress.ResolveContinueLevel());
		}

		public void StartTutorial(bool fromHome)
		{
			HideAllMenus();
			_screen = AppScreen.Tutorial;
			_bootstrap.StartTutorialReplay();
		}

		public void StartCampaignLevel(int index1Based)
		{
			if (_progress == null || !_progress.IsUnlocked(index1Based))
			{
				Debug.LogWarning("[TapAway] Level locked: " + index1Based);
				return;
			}

			HideAllMenus();
			_screen = AppScreen.Gameplay;
			_progress.SetLastPlayed(index1Based);
			SaveProgress();
			_bootstrap.StartCampaignLevel(index1Based);
		}

		public void GoHome()
		{
			ShowHome();
		}

		public void TogglePause()
		{
			if (_paused)
			{
				ResumePause();
			}
			else
			{
				EnterPause();
			}
		}

		public void EnterPause()
		{
			if (_screen != AppScreen.Gameplay && _screen != AppScreen.Tutorial)
			{
				return;
			}

			if (_bootstrap != null && _bootstrap.Phase == GameplayPhase.Victory)
			{
				return;
			}

			_paused = true;
			_pausedAt = Time.realtimeSinceStartup;
			_bootstrap?.SetPaused(true);
			_pause?.Show();
		}

		public void ResumePause()
		{
			if (!_paused)
			{
				_pause?.Hide();
				return;
			}

			var elapsed = Time.realtimeSinceStartup - _pausedAt;
			_bootstrap?.AddPausedSeconds(elapsed);
			_paused = false;
			_bootstrap?.SetPaused(false);
			_pause?.Hide();
		}

		public ProgressSaveResult SaveProgress()
		{
			if (_repository == null || _progress == null)
			{
				return new ProgressSaveResult { Success = false, Diagnostic = "not ready" };
			}

			var result = _repository.Save(_progress);
			_lastSaveDiagnostic = result.Diagnostic;
			if (!result.Success)
			{
				Debug.LogWarning("[TapAway] Save failed: " + result.Diagnostic);
			}
			else
			{
				Debug.Log("[TapAway] Progress saved");
			}

			return result;
		}

		/// <summary>Test helper: inject progress repository storage.</summary>
		public void UseTestRepository(IProgressStorage storage)
		{
			_repository = new ProgressRepository(storage);
			var load = _repository.Load();
			_progress = load.Progress;
		}

		private void OnGameplayCompleted(LevelBootstrap.CompletionInfo info)
		{
			if (info == null)
			{
				return;
			}

			if (info.IsTutorial)
			{
				_progress.MarkTutorialCompleted();
				SaveProgress();
				return;
			}

			if (info.IsCampaign && info.CampaignLevelIndex >= 1)
			{
				_progress.MarkLevelCompleted(
					info.CampaignLevelIndex,
					info.Metrics != null ? info.Metrics.ElapsedSeconds : 0f,
					info.Metrics != null ? info.Metrics.BlockedTaps : 0,
					info.Metrics != null ? info.Metrics.OrbitGestures : 0);
				// Persist BEFORE next-level navigation relies on unlock state.
				SaveProgress();
			}
		}

		private void HideAllMenus()
		{
			_home?.Hide();
			_levels?.Hide();
			_pause?.Hide();
			ResumePause();
		}

		private void EnsureUi()
		{
			_home = GetComponent<HomeScreen>() ?? gameObject.AddComponent<HomeScreen>();
			_levels = GetComponent<LevelsScreen>() ?? gameObject.AddComponent<LevelsScreen>();
			_pause = GetComponent<PauseOverlay>() ?? gameObject.AddComponent<PauseOverlay>();

			_home.Configure(OnPlayPressed, OnContinuePressed, ShowLevels, () => StartTutorial(true));
			_levels.Configure(StartCampaignLevel, ShowHome);
			_pause.Configure(ResumePause, () =>
			{
				ResumePause();
				_bootstrap?.Restart();
			}, GoHome);
		}
	}
}
