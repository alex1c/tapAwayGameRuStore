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

		private PuzzleLevel _level;
		private PuzzleState _state;
		private TutorialState _tutorialState;

		public PuzzlePresenter Presenter => _presenter;
		public PuzzleState State => _state;
		public TutorialState Tutorial => _tutorialState;
		public VictoryOverlay Victory => _victory;
		public int ExpectedBlockCount => _level != null ? _level.Blocks.Count : 0;

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

		private void StartLevel(bool resetTutorial)
		{
			_level = Phase1PrototypeLevel.Create();
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
				_tutorialState = new TutorialState(_showTutorial);
			}
			else if (_tutorialState == null || _tutorialState.IsCompleted || _tutorialState.IsSkipped)
			{
				_tutorialState = new TutorialState(false);
			}

			_tutorial?.Configure(
				_tutorialState,
				_presenter,
				new BlockId(1),
				null);

			_debugHud?.Bind(_state);
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
