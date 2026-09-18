using System.Collections;
using NUnit.Framework;
using TapAway.Core;
using TapAway.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TapAway.PlayMode.Tests
{
	/// <summary>Smoke: generated seed can load into the live presenter.</summary>
	public sealed class GeneratedPreviewPlayModeTests
	{
		[UnityTest]
		public IEnumerator GeneratedSeed_LoadsIntoPresenter()
		{
			LevelBootstrap.DevOverrideLevel = null;
			yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
			yield return null;

			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			Assert.That(bootstrap, Is.Not.Null);

			var loaded = false;
			for (var seed = 4242; seed < 4260; seed++)
			{
				if (bootstrap.TryLoadGeneratedSeed(seed, GeneratorConfig.Small()))
				{
					loaded = true;
					break;
				}
			}

			Assert.That(loaded, Is.True, "Expected an accepted small seed in window");
			yield return null;
			Assert.That(bootstrap.State.ActiveCount, Is.GreaterThan(0));
			Assert.That(bootstrap.Presenter.ViewCount, Is.EqualTo(bootstrap.State.ActiveCount));

			bootstrap.LoadPrototype();
			yield return null;
			Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(12));
		}
	}
}
