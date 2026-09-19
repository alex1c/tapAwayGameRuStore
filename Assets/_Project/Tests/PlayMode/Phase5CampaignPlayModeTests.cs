using System.Collections;
using NUnit.Framework;
using TapAway.Core;
using TapAway.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TapAway.PlayMode.Tests
{
	public sealed class Phase5CampaignPlayModeTests
	{
		[UnitySetUp]
		public IEnumerator SetUp()
		{
			LevelBootstrap.DevOverrideLevel = null;
			LevelBootstrap.DevQaIndexOverride = null;
			yield return null;
		}

		[UnityTest]
		public IEnumerator Startup_ShowsHome()
		{
			yield return LoadBootstrap();
			var app = Object.FindFirstObjectByType<GameApp>();
			Assert.That(app, Is.Not.Null);
			Assert.That(app.CurrentScreen, Is.EqualTo(GameApp.AppScreen.Home));
			var home = Object.FindFirstObjectByType<HomeScreen>();
			Assert.That(home, Is.Not.Null);
			Assert.That(home.IsVisible, Is.True);
		}

		[UnityTest]
		public IEnumerator Play_WithoutTutorial_StartsTutorial()
		{
			yield return LoadBootstrap();
			var app = Object.FindFirstObjectByType<GameApp>();
			app.UseTestRepository(new MemoryProgressStorage());
			app.ShowHome();
			app.OnPlayPressed();
			yield return null;
			yield return null;
			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			Assert.That(bootstrap.IsTutorial, Is.True);
			Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(12));
		}

		[UnityTest]
		public IEnumerator CampaignLevel_LoadsExpectedCount()
		{
			yield return LoadBootstrap();
			var app = Object.FindFirstObjectByType<GameApp>();
			var storage = new MemoryProgressStorage();
			app.UseTestRepository(storage);
			app.Progress.MarkTutorialCompleted();
			app.Progress.HighestUnlockedLevel = 6;
			app.StartCampaignLevel(6);
			yield return null;
			yield return null;
			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			Assert.That(bootstrap.IsCampaign, Is.True);
			Assert.That(bootstrap.CampaignIndex, Is.EqualTo(6));
			Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(8));
			Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(8));
			Assert.That(bootstrap.CurrentDescriptor.Seed, Is.EqualTo(41000));
		}

		[UnityTest]
		public IEnumerator Victory_UnlocksNext_AndPersists()
		{
			yield return LoadBootstrap();
			var app = Object.FindFirstObjectByType<GameApp>();
			var storage = new MemoryProgressStorage();
			app.UseTestRepository(storage);
			app.Progress.MarkTutorialCompleted();
			app.StartCampaignLevel(1);
			yield return null;
			yield return null;

			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			yield return CompleteCurrent(bootstrap);
			Assert.That(app.Progress.IsCompleted(1), Is.True);
			Assert.That(app.Progress.IsUnlocked(2), Is.True);
			Assert.That(storage.Exists(ProgressRepository.PrimaryFileName), Is.True);

			bootstrap.NextLevel();
			yield return null;
			yield return null;
			Assert.That(bootstrap.CampaignIndex, Is.EqualTo(2));
			Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(6));
			AssertParity(bootstrap);
		}

		[UnityTest]
		public IEnumerator LevelsScreen_OpensAndSelectsUnlocked()
		{
			yield return LoadBootstrap();
			var app = Object.FindFirstObjectByType<GameApp>();
			app.UseTestRepository(new MemoryProgressStorage());
			app.Progress.MarkTutorialCompleted();
			app.ShowLevels();
			yield return null;
			var levels = Object.FindFirstObjectByType<LevelsScreen>();
			Assert.That(levels.IsVisible, Is.True);
			app.StartCampaignLevel(1);
			yield return null;
			Assert.That(app.CurrentScreen, Is.EqualTo(GameApp.AppScreen.Gameplay));
		}

		[UnityTest]
		public IEnumerator Gameplay_Home_ClearsViews()
		{
			yield return LoadBootstrap();
			var app = Object.FindFirstObjectByType<GameApp>();
			app.UseTestRepository(new MemoryProgressStorage());
			app.Progress.MarkTutorialCompleted();
			app.StartCampaignLevel(1);
			yield return null;
			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(6));
			app.GoHome();
			yield return null;
			Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(0));
			Assert.That(app.CurrentScreen, Is.EqualTo(GameApp.AppScreen.Home));
		}

		[UnityTest]
		public IEnumerator LargeCampaignLevel_FramingAllowsCloserInspection()
		{
			yield return LoadBootstrap();
			var app = Object.FindFirstObjectByType<GameApp>();
			app.UseTestRepository(new MemoryProgressStorage());
			app.Progress.MarkTutorialCompleted();
			app.Progress.HighestUnlockedLevel = 30;
			app.StartCampaignLevel(30);
			yield return null;
			yield return null;
			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(20));
			var orbit = bootstrap.Orbit;
			Assert.That(orbit.Distance, Is.GreaterThan(1f));
			var before = orbit.Distance;
			orbit.ApplyZoomFactor(0.5f);
			Assert.That(orbit.Distance, Is.LessThan(before));
		}

		private static IEnumerator LoadBootstrap()
		{
			yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
			yield return null;
			yield return null;
		}

		private static IEnumerator CompleteCurrent(LevelBootstrap bootstrap)
		{
			var solution = PuzzleSolver.Solve(bootstrap.Level);
			Assert.That(solution.IsSolvable, Is.True);
			foreach (var idValue in solution.Solution)
			{
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
				var timeout = 3f;
				while (timeout > 0f &&
				       bootstrap.Phase != GameplayPhase.Playing &&
				       bootstrap.Phase != GameplayPhase.Victory)
				{
					timeout -= Time.deltaTime;
					yield return null;
				}

				yield return null;
			}

			var wait = 3f;
			while (wait > 0f && !bootstrap.Victory.IsVisible)
			{
				wait -= Time.deltaTime;
				yield return null;
			}

			Assert.That(bootstrap.Victory.IsVisible, Is.True);
		}

		private static void AssertParity(LevelBootstrap bootstrap)
		{
			Assert.That(
				GameplayPresentationInvariant.TryValidateParity(
					bootstrap.State, bootstrap.Presenter, out var detail),
				Is.True,
				detail);
		}
	}
}
