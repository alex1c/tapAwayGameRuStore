using System.Collections;
using NUnit.Framework;
using TapAway.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TapAway.PlayMode.Tests
{
	/// <summary>
	/// Minimal PlayMode smoke: Bootstrap scene loads and marker is present.
	/// </summary>
	public sealed class BootstrapSmokeTests
	{
		[UnityTest]
		public IEnumerator BootstrapScene_LoadsWithMarker()
		{
			var load = SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
			Assert.That(load, Is.Not.Null, "Bootstrap scene must be in Build Settings.");

			while (load != null && !load.isDone)
			{
				yield return null;
			}

			var marker = Object.FindFirstObjectByType<BootstrapMarker>();
			Assert.That(marker, Is.Not.Null);
			Assert.That(marker.Label, Does.Contain("Tap Away"));
		}
	}
}
