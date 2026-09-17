using TapAway.Core;
using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Boots the Phase 1 prototype level and wires Core ↔ presentation.
	/// </summary>
	public sealed class LevelBootstrap : MonoBehaviour
	{
		[SerializeField] private PuzzlePresenter _presenter;
		[SerializeField] private PuzzleOrbitCamera _orbit;
		[SerializeField] private PuzzleInputController _input;
		[SerializeField] private VictoryOverlay _victory;
		[SerializeField] private PuzzleDebugHud _debugHud;
		[SerializeField] private Camera _camera;

		private PuzzleLevel _level;
		private PuzzleState _state;

		public PuzzlePresenter Presenter => _presenter;
		public PuzzleState State => _state;
		public int ExpectedBlockCount => _level != null ? _level.Blocks.Count : 0;

		private void Awake()
		{
			EnsureComponents();
			StartLevel();
		}

		/// <summary>
		/// Reloads the same Phase 1 prototype puzzle.
		/// </summary>
		public void Restart()
		{
			_victory?.Hide();
			StartLevel();
		}

		private void StartLevel()
		{
			_level = Phase1PrototypeLevel.Create();
			_state = _level.CreateState();

			_presenter.Initialize(
				_state,
				OnMoveResolved,
				OnCompleted);

			_debugHud?.Bind(_state);
			_orbit?.FrameBounds(_presenter.ComputeBounds());
		}

		private void OnMoveResolved(MoveResult result)
		{
			_debugHud?.ReportMove(result);
		}

		private void OnCompleted()
		{
			_victory?.Show();
		}

		private void EnsureComponents()
		{
			if (_camera == null)
			{
				_camera = Camera.main;
			}

			if (_presenter == null)
			{
				_presenter = gameObject.GetComponent<PuzzlePresenter>();
				if (_presenter == null)
				{
					_presenter = gameObject.AddComponent<PuzzlePresenter>();
				}
			}

			if (_orbit == null)
			{
				_orbit = gameObject.GetComponent<PuzzleOrbitCamera>();
				if (_orbit == null)
				{
					_orbit = gameObject.AddComponent<PuzzleOrbitCamera>();
				}
			}

			if (_input == null)
			{
				_input = gameObject.GetComponent<PuzzleInputController>();
				if (_input == null)
				{
					_input = gameObject.AddComponent<PuzzleInputController>();
				}
			}

			if (_victory == null)
			{
				_victory = gameObject.GetComponent<VictoryOverlay>();
				if (_victory == null)
				{
					_victory = gameObject.AddComponent<VictoryOverlay>();
				}
			}

			if (_debugHud == null)
			{
				_debugHud = gameObject.GetComponent<PuzzleDebugHud>();
				if (_debugHud == null)
				{
					_debugHud = gameObject.AddComponent<PuzzleDebugHud>();
				}
			}

			var blocksRoot = transform.Find("BlocksRoot");
			if (blocksRoot == null)
			{
				var rootGo = new GameObject("BlocksRoot");
				blocksRoot = rootGo.transform;
				blocksRoot.SetParent(transform, false);
			}

			// Inject blocks root before presenter initialize.
			_presenter.SetBlocksRoot(blocksRoot);
			_input.Configure(_camera, _presenter, _orbit);
			_victory.Configure(Restart);
		}
	}
}
