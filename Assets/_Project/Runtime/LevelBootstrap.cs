using TapAway.Core;
using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Phase 4 bootstrap: tutorial fixture → deterministic QA generated sequence.
	/// Input/orbit availability is driven by <see cref="GameplayPhase"/>.
	/// </summary>
	public sealed class LevelBootstrap : MonoBehaviour
	{
		[SerializeField] private PuzzlePresenter _presenter;
		[SerializeField] private PuzzleOrbitCamera _orbit;
		[SerializeField] private PuzzleInputController _input;
		[SerializeField] private VictoryOverlay _victory;
		[SerializeField] private GameplayHud _gameplayHud;
		[SerializeField] private TutorialController _tutorial;
		[SerializeField] private PuzzleDebugHud _debugHud;
		[SerializeField] private PuzzleLighting _lighting;
		[SerializeField] private Camera _camera;
		[SerializeField] private bool _showTutorial = true;
		[SerializeField] private bool _playQaSequence = true;
		[SerializeField] private int _devPreviewSeed;
		[SerializeField] private bool _loadGeneratedPreview;

		/// <summary>Optional Editor override for a single generated level.</summary>
		public static GeneratedLevel DevOverrideLevel { get; set; }

		/// <summary>Editor override: jump to QA index (0-based) on next start.</summary>
		public static int? DevQaIndexOverride { get; set; }

		private HandcraftedLevelSource _tutorialSource;
		private Phase4QaLevelSource _qaSource;
		private GameLevelDescriptor _current;
		private PuzzleLevel _level;
		private PuzzleState _state;
		private TutorialState _tutorialState;
		private LevelPlayMetrics _metrics = new LevelPlayMetrics();
		private bool _inTutorial;
		private int _qaIndex;
		private bool _sequenceComplete;
		private float _levelStartTime;
		private bool _playQaSequenceFlag = true;
		private GameplayPhase _phase = GameplayPhase.LoadingLevel;

		private Phase4QaLevelSource QaSource
		{
			get
			{
				if (_qaSource == null && _playQaSequenceFlag)
				{
					_qaSource = new Phase4QaLevelSource();
				}

				return _qaSource;
			}
		}

		public PuzzlePresenter Presenter => _presenter;
		public PuzzleState State => _state;
		public TutorialState Tutorial => _tutorialState;
		public VictoryOverlay Victory => _victory;
		public PuzzleLevel Level => _level;
		public GameLevelDescriptor CurrentDescriptor => _current;
		public LevelPlayMetrics Metrics => _metrics;
		public int QaIndex => _qaIndex;
		public bool IsTutorial => _inTutorial;
		public bool IsSequenceComplete => _sequenceComplete;
		public int ExpectedBlockCount => _level != null ? _level.Blocks.Count : 0;
		public int DevPreviewSeed => _devPreviewSeed;
		public GameplayPhase Phase => _phase;
		public PuzzleInputController InputController => _input;
		public PuzzleOrbitCamera Orbit => _orbit;

		private void Awake()
		{
			EnsureComponents();
			GameplayPresentationInvariant.DisableNonGameplayMarkers();
			DisableLegacyOverlays();
			_lighting?.Apply();
			_tutorialSource = new HandcraftedLevelSource();
			_playQaSequenceFlag = _playQaSequence;

			if (DevQaIndexOverride.HasValue)
			{
				_qaIndex = Mathf.Clamp(DevQaIndexOverride.Value, 0, Phase4QaLevelSet.Count - 1);
				_showTutorial = false;
				_playQaSequenceFlag = true;
				DevQaIndexOverride = null;
			}

			StartFlow(resetTutorial: true);
		}

		private void Update()
		{
			if (_state != null && !_sequenceComplete && _phase == GameplayPhase.Playing)
			{
				_metrics.ElapsedSeconds = Time.time - _levelStartTime;
			}
		}

		public void Restart()
		{
			_phase = GameplayPhase.Transitioning;
			_victory?.Hide();
			LoadCurrentLevel(resetTutorial: false);
		}

		public void NextLevel()
		{
			_phase = GameplayPhase.Transitioning;
			_victory?.Hide();
			if (_inTutorial)
			{
				_inTutorial = false;
				_qaIndex = 0;
				LoadCurrentLevel(resetTutorial: false);
				return;
			}

			if (_qaSource == null && !_playQaSequenceFlag)
			{
				Restart();
				return;
			}

			var source = QaSource;
			if (source == null)
			{
				Restart();
				return;
			}

			if (_qaIndex >= source.Count - 1)
			{
				_sequenceComplete = true;
				EnterVictory(
					"Серия пройдена",
					"QA 1–" + source.Count + " завершены",
					showNext: false);
				return;
			}

			_qaIndex++;
			LoadCurrentLevel(resetTutorial: false);
		}

		public bool TryLoadGeneratedSeed(int seed, GeneratorConfig config = null)
		{
			var result = GenerationPipeline.Generate(seed, config ?? GeneratorConfig.Small());
			if (!result.Accepted || result.Level == null)
			{
				Debug.LogWarning("[TapAway] Generate seed " + seed + " rejected: " +
				                 result.RejectionReason + " " + result.Detail);
				return false;
			}

			DevOverrideLevel = result.Level;
			_devPreviewSeed = seed;
			_loadGeneratedPreview = true;
			_playQaSequence = false;
			_playQaSequenceFlag = false;
			_inTutorial = false;
			Restart();
			return true;
		}

		public void LoadPrototype()
		{
			DevOverrideLevel = null;
			_loadGeneratedPreview = false;
			_inTutorial = true;
			_playQaSequence = false;
			Restart();
		}

		/// <summary>Editor/QA: jump to a QA level index (0-based).</summary>
		public void LoadQaIndex(int index)
		{
			_playQaSequenceFlag = true;
			_qaIndex = Mathf.Clamp(index, 0, Phase4QaLevelSet.Count - 1);
			_inTutorial = false;
			_sequenceComplete = false;
			DevOverrideLevel = null;
			_loadGeneratedPreview = false;
			Restart();
		}

		private void StartFlow(bool resetTutorial)
		{
			_sequenceComplete = false;
			if (_loadGeneratedPreview && _devPreviewSeed != 0)
			{
				_inTutorial = false;
				LoadCurrentLevel(resetTutorial);
				return;
			}

			if (DevOverrideLevel != null)
			{
				_inTutorial = false;
				LoadCurrentLevel(resetTutorial);
				return;
			}

			_inTutorial = _showTutorial;
			if (!_inTutorial)
			{
				_qaIndex = 0;
			}

			LoadCurrentLevel(resetTutorial);
		}

		private void LoadCurrentLevel(bool resetTutorial)
		{
			_phase = GameplayPhase.LoadingLevel;
			_sequenceComplete = false;

			// Hide any leftover victory UI before rebuilding — prevents a
			// full-screen Dim from intercepting the next level's gestures.
			_victory?.Hide();
			_tutorial?.Hide();

			_current = ResolveDescriptor();
			_level = _current.Puzzle;
			_state = _level.CreateState();
			_metrics.Reset(_current);
			_levelStartTime = Time.time;

			_presenter.Initialize(
				_state,
				OnMoveResolved,
				OnCompleted);

			_gameplayHud?.SetLevelName(BuildHudTitle());
			_gameplayHud?.SetRemaining(_state.ActiveCount, _state.DefinedCount);
			_gameplayHud?.SetDevInfo(BuildDevInfo());
			_gameplayHud?.Show();

			_orbit?.CaptureInitialAngles();
			_orbit?.FrameBounds(_presenter.ComputeBounds());
			_orbit?.ResetToFramedView();

			if (resetTutorial && _inTutorial)
			{
				_tutorialState = new TutorialState(true);
			}
			else if (_tutorialState == null || !_inTutorial)
			{
				_tutorialState = new TutorialState(false);
			}

			var hintId = _level.Blocks.Count > 0 ? _level.Blocks[0].Id : new BlockId(1);
			_tutorial?.Configure(
				_tutorialState,
				_presenter,
				hintId,
				null);

			_debugHud?.Bind(_state);
			EnterPlaying();
			GameplayPresentationInvariant.AssertParityOrLog(_state, _presenter);
			Debug.Log("[TapAway] Loaded " + _current.DisplayName +
			          " phase=" + _phase +
			          " active=" + _state.ActiveCount +
			          " views=" + _presenter.ViewCount);
		}

		/// <summary>
		/// Playing is the only phase where orbit/tap/pinch are accepted.
		/// Called after every successful level load (including Tutorial → QA).
		/// </summary>
		private void EnterPlaying()
		{
			_phase = GameplayPhase.Playing;
			_presenter?.SetInteractionEnabled(true);
			_input?.SetInputEnabled(true);
			_input?.ClearGestureState();
			_victory?.Hide();
		}

		private void EnterVictory(string title, string subtitle, bool showNext)
		{
			_phase = GameplayPhase.Victory;
			_presenter?.SetInteractionEnabled(false);
			_input?.SetInputEnabled(false);
			_gameplayHud?.Hide();
			_victory?.Show(title, subtitle, showNext: showNext, metrics: _metrics.ToDebugLine());
		}

		private GameLevelDescriptor ResolveDescriptor()
		{
			if (DevOverrideLevel != null)
			{
				return new GameLevelDescriptor
				{
					Id = DevOverrideLevel.LevelId,
					DisplayName = "Generated " + DevOverrideLevel.Seed,
					Puzzle = DevOverrideLevel.ToPuzzleLevel(),
					Seed = DevOverrideLevel.Seed,
					GeneratorVersion = DevOverrideLevel.GeneratorVersion,
					BlockCount = DevOverrideLevel.Blocks.Count,
					DifficultyScore = DevOverrideLevel.Difficulty?.Score,
					DifficultyBand = DevOverrideLevel.Difficulty?.Band
				};
			}

			if (_loadGeneratedPreview && _devPreviewSeed != 0)
			{
				var generated = GenerationPipeline.Generate(_devPreviewSeed, GeneratorConfig.Small());
				if (generated.Accepted)
				{
					DevOverrideLevel = generated.Level;
					return ResolveDescriptor();
				}
			}

			if (_inTutorial)
			{
				return _tutorialSource.GetLevel(0);
			}

			if (_qaSource != null || _playQaSequenceFlag)
			{
				return QaSource.GetLevel(_qaIndex);
			}

			return _tutorialSource.GetLevel(0);
		}

		private string BuildHudTitle()
		{
			if (_inTutorial)
			{
				return "Обучение";
			}

			if (_qaSource != null || (_playQaSequenceFlag && !_inTutorial))
			{
				var count = QaSource.Count;
				return "QA " + (_qaIndex + 1) + " / " + count;
			}

			return _current != null ? _current.DisplayName : "Tap Away";
		}

		private string BuildDevInfo()
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if (_current == null)
			{
				return string.Empty;
			}

			return "seed=" + (_current.Seed ?? 0) +
			       " n=" + _current.BlockCount +
			       " " + (_current.DifficultyBand?.ToString() ?? "-") +
			       " " + (_current.DifficultyScore?.ToString("0.0") ?? "");
#else
			return string.Empty;
#endif
		}

		private void OnMoveResolved(MoveResult result)
		{
			if (_phase == GameplayPhase.Playing &&
			    (result.Status == MoveStatus.Allowed || result.Status == MoveStatus.Blocked))
			{
				_phase = GameplayPhase.RemovingBlock;
			}

			_metrics.BlockTaps++;
			if (result.Status == MoveStatus.Allowed)
			{
				_metrics.SuccessfulRemovals++;
			}
			else if (result.Status == MoveStatus.Blocked)
			{
				_metrics.BlockedTaps++;
			}

			_debugHud?.ReportMove(result);
			_gameplayHud?.SetRemaining(_state.ActiveCount, _state.DefinedCount);
			_tutorial?.NotifyMove(result);
			// Parity is checked after removal settles — Core updates before the view flies out.
		}

		private void OnCompleted()
		{
			_metrics.ElapsedSeconds = Time.time - _levelStartTime;
			// At victory Core is empty; views must also be gone (settled).
			GameplayPresentationInvariant.AssertParityOrLog(_state, _presenter);

			var hasNext = _inTutorial || (_playQaSequenceFlag && _qaIndex < Phase4QaLevelSet.Count - 1);
			var title = _inTutorial ? "Обучение пройдено" : "Уровень пройден";
			var subtitle = _inTutorial
				? "Дальше — серия QA-уровней"
				: "QA " + (_qaIndex + 1) + " / " + Phase4QaLevelSet.Count;

			EnterVictory(title, subtitle, showNext: hasNext);
			Debug.Log("[TapAway][Metrics] " + _metrics.ToDebugLine());
		}

		/// <summary>
		/// Called by presenter when a removal/blocked animation finishes and
		/// the level is not yet complete — restores Playing for the next gesture.
		/// </summary>
		public void NotifyRemovalSettled()
		{
			if (_phase == GameplayPhase.RemovingBlock && _state != null && !_state.IsComplete)
			{
				_phase = GameplayPhase.Playing;
				GameplayPresentationInvariant.AssertParityOrLog(_state, _presenter);
			}
		}

		private void OnOrbitGesture()
		{
			_metrics.OrbitGestures++;
			_tutorial?.NotifyMeaningfulDrag();
		}

		private void OnPinchGesture()
		{
			_metrics.PinchGestures++;
		}

		private void DisableLegacyOverlays()
		{
			var versionHud = GetComponent<VersionHud>();
			if (versionHud != null)
			{
				versionHud.enabled = false;
			}

			if (_debugHud != null)
			{
#if UNITY_EDITOR
				_debugHud.enabled = true;
#else
				_debugHud.enabled = false;
#endif
			}
		}

		private void EnsureComponents()
		{
			if (_camera == null)
			{
				_camera = Camera.main;
			}

			if (_presenter == null)
			{
				_presenter = gameObject.GetComponent<PuzzlePresenter>() ?? gameObject.AddComponent<PuzzlePresenter>();
			}

			if (_orbit == null)
			{
				_orbit = gameObject.GetComponent<PuzzleOrbitCamera>() ?? gameObject.AddComponent<PuzzleOrbitCamera>();
			}

			if (_input == null)
			{
				_input = gameObject.GetComponent<PuzzleInputController>() ?? gameObject.AddComponent<PuzzleInputController>();
			}

			if (_victory == null)
			{
				_victory = gameObject.GetComponent<VictoryOverlay>() ?? gameObject.AddComponent<VictoryOverlay>();
			}

			if (_gameplayHud == null)
			{
				_gameplayHud = gameObject.GetComponent<GameplayHud>() ?? gameObject.AddComponent<GameplayHud>();
			}

			if (_tutorial == null)
			{
				_tutorial = gameObject.GetComponent<TutorialController>() ?? gameObject.AddComponent<TutorialController>();
			}

			if (_debugHud == null)
			{
				_debugHud = gameObject.GetComponent<PuzzleDebugHud>();
			}

			if (_lighting == null)
			{
				_lighting = gameObject.GetComponent<PuzzleLighting>() ?? gameObject.AddComponent<PuzzleLighting>();
			}

			var blocksRoot = transform.Find("BlocksRoot");
			if (blocksRoot == null)
			{
				var rootGo = new GameObject("BlocksRoot");
				blocksRoot = rootGo.transform;
				blocksRoot.SetParent(transform, false);
			}

			_presenter.SetBlocksRoot(blocksRoot);
			_input.Configure(_camera, _presenter, _orbit);
			_input.SetMeaningfulDragHandler(OnOrbitGesture);
			_input.SetPinchHandler(OnPinchGesture);
			_victory.Configure(Restart, NextLevel);
			_gameplayHud.Configure(Restart);
		}
	}
}
