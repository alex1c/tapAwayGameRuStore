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
	/// PlayMode smoke for the Phase 1 prototype scene.
	/// </summary>
	public sealed class PrototypeGameplaySmokeTests
	{
		[UnityTest]
		public IEnumerator BootstrapScene_LoadsWithExpectedBlockViews()
		{
			yield return LoadBootstrap();

			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			Assert.That(bootstrap, Is.Not.Null);

			// Allow Awake/Start wiring to finish.
			yield return null;

			Assert.That(bootstrap.Presenter, Is.Not.Null);
			Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(12));
			Assert.That(bootstrap.State, Is.Not.Null);
			Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(12));
		}

		[UnityTest]
		public IEnumerator AllowedMove_UpdatesCoreAndRemovesView()
		{
			yield return LoadBootstrap();
			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			yield return null;

			var id = new BlockId(1);
			var view = bootstrap.Presenter.GetView(id);
			Assert.That(view, Is.Not.Null);

			var handled = bootstrap.Presenter.TrySelect(view);
			Assert.That(handled, Is.True);
			Assert.That(bootstrap.State.IsActive(id), Is.False);

			// Wait for fly animation to complete and destroy the view.
			var timeout = 2f;
			while (timeout > 0f && bootstrap.Presenter.GetView(id) != null)
			{
				timeout -= Time.deltaTime;
				yield return null;
			}

			Assert.That(bootstrap.Presenter.GetView(id), Is.Null);
			Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(11));
		}

		[UnityTest]
		public IEnumerator BlockedMove_KeepsBlockActive()
		{
			yield return LoadBootstrap();
			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			yield return null;

			var id = new BlockId(3);
			var view = bootstrap.Presenter.GetView(id);
			Assert.That(view, Is.Not.Null);

			bootstrap.Presenter.TrySelect(view);
			yield return new WaitForSeconds(0.35f);

			Assert.That(bootstrap.State.IsActive(id), Is.True);
			Assert.That(bootstrap.Presenter.GetView(id), Is.Not.Null);
		}

		[UnityTest]
		public IEnumerator Restart_RestoresFullPuzzle()
		{
			yield return LoadBootstrap();
			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			yield return null;

			var view = bootstrap.Presenter.GetView(new BlockId(1));
			Assert.That(view, Is.Not.Null);
			bootstrap.Presenter.TrySelect(view);

			// Core removes immediately; wait until presentation finishes the fly-out.
			var timeout = 2f;
			while (timeout > 0f && bootstrap.Presenter.ViewCount == 12)
			{
				timeout -= Time.deltaTime;
				yield return null;
			}

			Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(11));
			Assert.That(bootstrap.Presenter.IsInputLocked, Is.False);

			bootstrap.Restart();
			yield return null;

			Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(12));
			Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(12));
			Assert.That(bootstrap.Presenter.GetView(new BlockId(1)), Is.Not.Null);
		}

		private static IEnumerator LoadBootstrap()
		{
			var load = SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
			Assert.That(load, Is.Not.Null);
			while (!load.isDone)
			{
				yield return null;
			}
		}
	}
}
