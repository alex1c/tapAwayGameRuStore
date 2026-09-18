using System.Collections;
using NUnit.Framework;
using TapAway.Core;
using TapAway.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TapAway.PlayMode.Tests
{
	/// <summary>Phase 4 generated QA flow smoke tests.</summary>
	public sealed class Phase4QaPlayModeTests
	{
		[UnityTest]
		public IEnumerator QaLevel_LoadsExpectedBlockCount()
		{
			LevelBootstrap.DevOverrideLevel = null;
			LevelBootstrap.DevQaIndexOverride = 0;
			yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
			yield return null;
			yield return null;

			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			Assert.That(bootstrap, Is.Not.Null);
			// Skip tutorial by override — should be on QA-1.
			if (bootstrap.IsTutorial)
			{
				bootstrap.LoadQaIndex(0);
				yield return null;
			}

			Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(8));
			Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(8));
			Assert.That(bootstrap.CurrentDescriptor.Seed, Is.EqualTo(41000));
		}

		[UnityTest]
		public IEnumerator NextLevel_ReplacesViews()
		{
			LevelBootstrap.DevOverrideLevel = null;
			LevelBootstrap.DevQaIndexOverride = 0;
			yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
			yield return null;
			yield return null;

			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			if (bootstrap.IsTutorial)
			{
				bootstrap.LoadQaIndex(0);
				yield return null;
			}

			Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(8));
			bootstrap.NextLevel();
			yield return null;
			Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(10));
			Assert.That(bootstrap.QaIndex, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator Restart_RestoresSameSeedState()
		{
			LevelBootstrap.DevOverrideLevel = null;
			LevelBootstrap.DevQaIndexOverride = 2;
			yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
			yield return null;
			yield return null;

			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			if (bootstrap.IsTutorial)
			{
				bootstrap.LoadQaIndex(2);
				yield return null;
			}

			var seed = bootstrap.CurrentDescriptor.Seed;
			var count = bootstrap.State.ActiveCount;
			var view = bootstrap.Presenter.GetView(new BlockId(1));
			if (view != null)
			{
				bootstrap.Presenter.TrySelect(view);
				var timeout = 2f;
				while (timeout > 0f && bootstrap.Presenter.IsInputLocked)
				{
					timeout -= Time.deltaTime;
					yield return null;
				}
			}

			bootstrap.Restart();
			yield return null;
			Assert.That(bootstrap.CurrentDescriptor.Seed, Is.EqualTo(seed));
			Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(count));
			Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(count));
		}

		[UnityTest]
		public IEnumerator DirectionIndicators_ExistOnQaBlocks()
		{
			LevelBootstrap.DevOverrideLevel = null;
			LevelBootstrap.DevQaIndexOverride = 0;
			yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
			yield return null;
			yield return null;

			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			if (bootstrap.IsTutorial)
			{
				bootstrap.LoadQaIndex(0);
				yield return null;
			}

			foreach (var block in bootstrap.State.GetActiveBlocks())
			{
				var view = bootstrap.Presenter.GetView(block.Id);
				Assert.That(view, Is.Not.Null);
				var indicator = DirectionReadabilityValidator.FindIndicator(view.transform);
				Assert.That(indicator, Is.Not.Null);
				Assert.That(
					DirectionIndicatorBuilder.HeadIsAlongEscape(indicator, view.EscapeDirection),
					Is.True);
			}
		}

		[UnityTest]
		public IEnumerator CameraFraming_HasPositiveDistance()
		{
			LevelBootstrap.DevQaIndexOverride = 6;
			yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
			yield return null;
			yield return null;

			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			if (bootstrap.IsTutorial)
			{
				bootstrap.LoadQaIndex(6);
				yield return null;
			}

			var orbit = Object.FindFirstObjectByType<PuzzleOrbitCamera>();
			Assert.That(orbit, Is.Not.Null);
			Assert.That(orbit.Distance, Is.GreaterThan(1f));
			var bounds = bootstrap.Presenter.ComputeBounds();
			Assert.That(bounds.size.sqrMagnitude, Is.GreaterThan(0.01f));
		}
	}
}
