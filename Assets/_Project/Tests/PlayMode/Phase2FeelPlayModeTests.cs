using System.Collections;
using NUnit.Framework;
using TapAway.Core;
using TapAway.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TapAway.PlayMode.Tests
{
	/// <summary>
	/// Phase 2 PlayMode coverage for victory, restart framing, and safe-area helper.
	/// </summary>
	public sealed class Phase2FeelPlayModeTests
	{
		[UnityTest]
		public IEnumerator Victory_AppearsOnlyAfterFinalBlock()
		{
			yield return LoadBootstrap();
			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			yield return null;

			Assert.That(bootstrap.Victory, Is.Not.Null);
			Assert.That(bootstrap.Victory.IsVisible, Is.False);

			foreach (var idValue in Phase1PrototypeLevel.DocumentedSolution)
			{
				var id = new BlockId(idValue);
				var view = bootstrap.Presenter.GetView(id);
				Assert.That(view, Is.Not.Null, "Missing view for " + id);
				bootstrap.Presenter.TrySelect(view);

				var timeout = 2f;
				while (timeout > 0f && bootstrap.Presenter.IsInputLocked)
				{
					timeout -= Time.deltaTime;
					yield return null;
				}

				if (!bootstrap.State.IsComplete)
				{
					Assert.That(bootstrap.Victory.IsVisible, Is.False);
				}
			}

			Assert.That(bootstrap.State.IsComplete, Is.True);
			Assert.That(bootstrap.Victory.IsVisible, Is.True);
		}

		[UnityTest]
		public IEnumerator Restart_RestoresCameraFramingAndBlocks()
		{
			yield return LoadBootstrap();
			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			yield return null;

			var orbit = Object.FindFirstObjectByType<PuzzleOrbitCamera>();
			Assert.That(orbit, Is.Not.Null);
			var framedDistance = orbit.Distance;

			orbit.ApplyDrag(new Vector2(120f, -40f));
			orbit.ApplyZoomFactor(1.25f);
			Assert.That(orbit.Distance, Is.Not.EqualTo(framedDistance));

			var view = bootstrap.Presenter.GetView(new BlockId(1));
			bootstrap.Presenter.TrySelect(view);
			var timeout = 2f;
			while (timeout > 0f && bootstrap.Presenter.ViewCount == 12)
			{
				timeout -= Time.deltaTime;
				yield return null;
			}

			bootstrap.Restart();
			yield return null;

			Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(12));
			Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(12));
			Assert.That(bootstrap.Victory.IsVisible, Is.False);
			Assert.That(orbit.Distance, Is.EqualTo(framedDistance).Within(0.05f));
		}

		[Test]
		public void SafeAreaFitter_ComputesNormalizedAnchors()
		{
			var safe = new Rect(0f, 80f, 1080f, 1840f);
			SafeAreaFitter.ComputeAnchors(safe, 1080, 1920, 24f, out var min, out var max);
			Assert.That(min.x, Is.EqualTo(0f).Within(0.001f));
			Assert.That(max.x, Is.EqualTo(1f).Within(0.001f));
			Assert.That(min.y, Is.GreaterThan(80f / 1920f));
			Assert.That(max.y, Is.EqualTo(1920f / 1920f).Within(0.001f));
		}

		private static IEnumerator LoadBootstrap()
		{
			var load = SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
			Assert.That(load, Is.Not.Null);
			while (!load.isDone)
			{
				yield return null;
			}

			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			bootstrap.LoadPrototype();
			yield return null;
			yield return null;
		}
	}
}
