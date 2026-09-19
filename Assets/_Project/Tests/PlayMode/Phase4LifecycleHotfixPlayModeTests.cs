using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TapAway.Core;
using TapAway.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TapAway.PlayMode.Tests
{
	/// <summary>
	/// Critical lifecycle regressions: orphan FoundationMarker, Core/View parity,
	/// and input re-enable after Victory → Next Level.
	/// </summary>
	public sealed class Phase4LifecycleHotfixPlayModeTests
	{
		[UnityTest]
		public IEnumerator FoundationMarker_IsDisabledDuringGameplay()
		{
			yield return LoadQa(0);

			var marker = FindFoundationMarkerIncludingInactive();
			Assert.That(marker, Is.Not.Null, "FoundationMarker should still exist for diagnostics");
			Assert.That(marker.activeSelf, Is.False, "FoundationMarker must not be visible in gameplay");

			var renderer = marker.GetComponent<MeshRenderer>();
			if (renderer != null)
			{
				Assert.That(renderer.enabled, Is.False);
			}

			Assert.That(GameplayPresentationInvariant.HasOrphanGameplayLookingCube(), Is.False);
		}

		[UnityTest]
		public IEnumerator Qa1_CoreViewParity_ThroughSolution_NoOrphan()
		{
			yield return LoadQa(0);
			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();

			Assert.That(bootstrap.CurrentDescriptor.Seed, Is.EqualTo(41000));
			Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(8));
			Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(8));
			AssertParity(bootstrap);

			var solution = PuzzleSolver.Solve(bootstrap.Level);
			Assert.That(solution.IsSolvable, Is.True);

			foreach (var idValue in solution.Solution)
			{
				var id = new BlockId(idValue);
				var view = bootstrap.Presenter.GetView(id);
				Assert.That(view, Is.Not.Null, "Missing view for solution id " + idValue);
				Assert.That(bootstrap.Presenter.TrySelect(view), Is.True);

				yield return WaitUntilSettled(bootstrap, 3f);
				AssertParity(bootstrap);
				Assert.That(GameplayPresentationInvariant.HasOrphanGameplayLookingCube(), Is.False);
			}

			Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(0));
			Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(0));
			Assert.That(bootstrap.Victory.IsVisible, Is.True);
			Assert.That(bootstrap.Phase, Is.EqualTo(GameplayPhase.Victory));
			Assert.That(GameplayPresentationInvariant.HasOrphanGameplayLookingCube(), Is.False);
		}

		[UnityTest]
		public IEnumerator Tutorial_To_Qa1_ClearsStaleViews_AndEnablesInput()
		{
			LevelBootstrap.DevOverrideLevel = null;
			LevelBootstrap.DevQaIndexOverride = null;
			yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
			yield return null;
			yield return null;

			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			Assert.That(bootstrap, Is.Not.Null);
			Assert.That(bootstrap.IsTutorial, Is.True);
			Assert.That(bootstrap.Phase, Is.EqualTo(GameplayPhase.Playing));
			Assert.That(bootstrap.InputController.IsInputEnabled, Is.True);

			yield return CompleteCurrentLevel(bootstrap);

			Assert.That(bootstrap.Victory.IsVisible, Is.True);
			Assert.That(bootstrap.InputController.IsInputEnabled, Is.False);

			bootstrap.NextLevel();
			yield return null;
			yield return null;

			Assert.That(bootstrap.IsTutorial, Is.False);
			Assert.That(bootstrap.QaIndex, Is.EqualTo(0));
			Assert.That(bootstrap.CurrentDescriptor.Seed, Is.EqualTo(41000));
			Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(8));
			Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(8));
			Assert.That(bootstrap.Victory.IsVisible, Is.False);
			Assert.That(bootstrap.Phase, Is.EqualTo(GameplayPhase.Playing));
			Assert.That(bootstrap.InputController.IsInputEnabled, Is.True);
			Assert.That(GameplayPresentationInvariant.HasOrphanGameplayLookingCube(), Is.False);
			AssertParity(bootstrap);
		}

		[UnityTest]
		public IEnumerator Qa1_To_Qa2_EnablesOrbitAfterVictory()
		{
			yield return LoadQa(0);
			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();

			yield return CompleteCurrentLevel(bootstrap);
			Assert.That(bootstrap.Victory.IsVisible, Is.True);
			Assert.That(bootstrap.Phase, Is.EqualTo(GameplayPhase.Victory));
			Assert.That(bootstrap.InputController.IsInputEnabled, Is.False);

			bootstrap.NextLevel();
			yield return null;
			yield return null;

			Assert.That(bootstrap.QaIndex, Is.EqualTo(1));
			Assert.That(bootstrap.CurrentDescriptor.Seed, Is.EqualTo(42000));
			Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(10));
			Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(10));
			Assert.That(bootstrap.Victory.IsVisible, Is.False);
			Assert.That(bootstrap.Phase, Is.EqualTo(GameplayPhase.Playing));
			Assert.That(bootstrap.InputController.IsInputEnabled, Is.True);
			Assert.That(bootstrap.Presenter.IsInputLocked, Is.False);

			var orbit = bootstrap.Orbit;
			Assert.That(orbit, Is.Not.Null);
			var yawBefore = orbit.Yaw;
			orbit.ApplyDrag(new Vector2(80f, -30f));
			Assert.That(orbit.Yaw, Is.Not.EqualTo(yawBefore));
			AssertParity(bootstrap);
		}

		[UnityTest]
		public IEnumerator QaSequence_LifecycleSoak_ParityAndInput()
		{
			yield return LoadQa(0);
			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();

			for (var index = 0; index < Phase4QaLevelSet.Count; index++)
			{
				Assert.That(bootstrap.QaIndex, Is.EqualTo(index));
				Assert.That(bootstrap.Phase, Is.EqualTo(GameplayPhase.Playing));
				Assert.That(bootstrap.InputController.IsInputEnabled, Is.True);
				Assert.That(bootstrap.Victory.IsVisible, Is.False);
				Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(Phase4QaLevelSet.Entries[index].TargetBlockCount));
				Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(bootstrap.State.ActiveCount));
				AssertParity(bootstrap);
				Assert.That(GameplayPresentationInvariant.HasOrphanGameplayLookingCube(), Is.False);

				yield return CompleteCurrentLevel(bootstrap);
				Assert.That(bootstrap.Victory.IsVisible, Is.True);
				Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(0));
				Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(0));

				if (index < Phase4QaLevelSet.Count - 1)
				{
					bootstrap.NextLevel();
					yield return null;
					yield return null;
				}
			}

			Assert.That(bootstrap.QaIndex, Is.EqualTo(Phase4QaLevelSet.Count - 1));
		}

		private static IEnumerator LoadQa(int index)
		{
			LevelBootstrap.DevOverrideLevel = null;
			LevelBootstrap.DevQaIndexOverride = index;
			yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
			yield return null;
			yield return null;

			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			if (bootstrap.IsTutorial)
			{
				bootstrap.LoadQaIndex(index);
				yield return null;
				yield return null;
			}
		}

		private static IEnumerator CompleteCurrentLevel(LevelBootstrap bootstrap)
		{
			var solution = PuzzleSolver.Solve(bootstrap.Level);
			Assert.That(solution.IsSolvable, Is.True, "Level must be solvable for lifecycle soak");

			foreach (var idValue in solution.Solution)
			{
				// Views may already be gone if a previous select finished the level.
				if (bootstrap.State.IsComplete)
				{
					break;
				}

				var view = bootstrap.Presenter.GetView(new BlockId(idValue));
				if (view == null)
				{
					continue;
				}

				bootstrap.Presenter.TrySelect(view);
				yield return WaitUntilSettled(bootstrap, 3f);
			}

			var timeout = 3f;
			while (timeout > 0f && !bootstrap.Victory.IsVisible)
			{
				timeout -= Time.deltaTime;
				yield return null;
			}

			Assert.That(bootstrap.Victory.IsVisible, Is.True, "Victory must appear after solution");
		}

		private static IEnumerator WaitUntilSettled(LevelBootstrap bootstrap, float seconds)
		{
			var timeout = seconds;
			while (timeout > 0f)
			{
				if (bootstrap.Victory != null && bootstrap.Victory.IsVisible)
				{
					break;
				}

				if (bootstrap.Phase == GameplayPhase.Playing &&
				    !bootstrap.Presenter.IsInputLocked)
				{
					break;
				}

				if (bootstrap.Phase == GameplayPhase.Victory)
				{
					break;
				}

				timeout -= Time.deltaTime;
				yield return null;
			}

			// Allow Destroy() to flush so ViewCount matches Core.
			yield return null;
		}

		private static void AssertParity(LevelBootstrap bootstrap)
		{
			Assert.That(
				GameplayPresentationInvariant.TryValidateParity(
					bootstrap.State,
					bootstrap.Presenter,
					out var detail),
				Is.True,
				detail);
		}

		private static GameObject FindFoundationMarkerIncludingInactive()
		{
			var scene = SceneManager.GetActiveScene();
			foreach (var root in scene.GetRootGameObjects())
			{
				if (root.name == GameplayPresentationInvariant.FoundationMarkerName)
				{
					return root;
				}
			}

			return null;
		}
	}
}
