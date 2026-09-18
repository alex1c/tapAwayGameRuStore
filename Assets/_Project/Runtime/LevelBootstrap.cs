using TapAway.Core;
using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Boots the prototype level and wires Core ↔ Phase 2 presentation.
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
		[SerializeField] private int _devPreviewSeed;
		[SerializeField] private bool _loadGeneratedPreview;

		/// <summary>
		/// Optional runtime override set by Editor harness / debug tools.
		/// When set, bootstrap loads this generated level instead of the prototype.
		/// </summary>
		public static GeneratedLevel DevOverrideLevel { get; set; }

		private PuzzleLevel _level;
		private PuzzleState _state;
		private TutorialState _tutorialState;

		public PuzzlePresenter Presenter => _presenter;
		public PuzzleState State => _state;
		public TutorialState Tutorial => _tutorialState;
		public VictoryOverlay Victory => _victory;
		public PuzzleLevel Level => _level;
		public int ExpectedBlockCount => _level != null ? _level.Blocks.Count : 0;
		public int DevPreviewSeed => _devPreviewSeed;

		private void Awake()
		{
			EnsureComponents();
			DisableLegacyOverlays();
			_lighting?.Apply();
			StartLevel(resetTutorial: true);
		}

		/// <summary>
		/// Reloads the same prototype puzzle and restores camera framing.
		/// </summary>
		public void Restart()
		{
			_victory?.Hide();
			_presenter?.SetInteractionEnabled(true);
			_input?.SetInputEnabled(true);
			StartLevel(resetTutorial: false);
		}

		/// <summary>
		/// Loads a generated level by seed into the live presentation (dev/QA).
		/// </summary>
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
			Restart();
			return true;
		}

		/// <summary>
		/// Clears generated override and returns to the Phase 1 prototype fixture.
		/// </summary>
		public void LoadPrototype()
		{
			DevOverrideLevel = null;
			_loadGeneratedPreview = false;
			Restart();
		}

		private void StartLevel(bool resetTutorial)
		{
			_level = ResolveLevel();
			_state = _level.CreateState();

			_presenter.Initialize(
				_state,
				OnMoveResolved,
				OnCompleted);

			_gameplayHud?.SetLevelName(_level.DisplayName);
			_gameplayHud?.SetRemaining(_state.ActiveCount, _state.DefinedCount);
			_gameplayHud?.Show();

			_orbit?.CaptureInitialAngles();
			_orbit?.FrameBounds(_presenter.ComputeBounds());
			_orbit?.ResetToFramedView();

			if (resetTutorial)
			{
				_tutorialState = new TutorialState(_showTutorial && DevOverrideLevel == null);
			}
			else if (_tutorialState == null || _tutorialState.IsCompleted || _tutorialState.IsSkipped)
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
		}

		private PuzzleLevel ResolveLevel()
		{
			if (DevOverrideLevel != null)
			{
				return DevOverrideLevel.ToPuzzleLevel();
			}

			if (_loadGeneratedPreview && _devPreviewSeed != 0)
			{
				var generated = GenerationPipeline.Generate(_devPreviewSeed, GeneratorConfig.Small());
				if (generated.Accepted)
				{
					DevOverrideLevel = generated.Level;
					return generated.Level.ToPuzzleLevel();
				}

				Debug.LogWarning("[TapAway] Serialized preview seed rejected; falling back to prototype.");
			}

			return Phase1PrototypeLevel.Create();
		}

		private void OnMoveResolved(MoveResult result)
		{
			_debugHud?.ReportMove(result);
			_gameplayHud?.SetRemaining(_state.ActiveCount, _state.DefinedCount);
			_tutorial?.NotifyMove(result);
		}

		private void OnCompleted()
		{
			_presenter?.SetInteractionEnabled(false);
			_input?.SetInputEnabled(false);
			_gameplayHud?.Hide();
			_victory?.Show();
		}

		private void DisableLegacyOverlays()
		{
			// Prevent overlapping legacy version / debug HUDs during normal play.
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
			_input.SetMeaningfulDragHandler(() => _tutorial?.NotifyMeaningfulDrag());
			_victory.Configure(Restart);
			_gameplayHud.Configure(Restart);
		}
	}
}
