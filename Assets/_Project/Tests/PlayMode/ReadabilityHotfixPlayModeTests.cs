using System.Collections;
using NUnit.Framework;
using TapAway.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TapAway.PlayMode.Tests
{
	/// <summary>
	/// Presentation smoke for readability hotfix (arrows / lighting).
	/// </summary>
	public sealed class ReadabilityHotfixPlayModeTests
	{
		[UnityTest]
		public IEnumerator ArrowMaterials_AreUnlitAndIndependentOfBodyFlash()
		{
			yield return LoadBootstrap();
			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			yield return null;

			var view = bootstrap.Presenter.GetView(new TapAway.Core.BlockId(11));
			Assert.That(view, Is.Not.Null);

			var arrow = DirectionReadabilityValidator.FindIndicator(view.transform);
			Assert.That(arrow, Is.Not.Null, "Expected DirectionIndicator under block");
			Assert.That(
				DirectionReadabilityValidator.HasExpectedMultiFaceCoverage(
					arrow,
					view.EscapeDirection),
				Is.True);

			foreach (var renderer in arrow.GetComponentsInChildren<Renderer>())
			{
				Assert.That(renderer.sharedMaterial, Is.Not.Null);
				var shaderName = renderer.sharedMaterial.shader.name;
				Assert.That(
					shaderName.IndexOf("Unlit", System.StringComparison.OrdinalIgnoreCase) >= 0
					|| shaderName.IndexOf("Sprites/Default", System.StringComparison.OrdinalIgnoreCase) >= 0,
					"Arrow shader should be Unlit/independent of scene lights, got: " + shaderName);
			}

			view.PlaySelectFlash();
			yield return null;

			// Body flash must not strip arrow unlit materials.
			foreach (var renderer in arrow.GetComponentsInChildren<Renderer>())
			{
				var shaderName = renderer.sharedMaterial.shader.name;
				Assert.That(
					shaderName.IndexOf("Unlit", System.StringComparison.OrdinalIgnoreCase) >= 0
					|| shaderName.IndexOf("Sprites/Default", System.StringComparison.OrdinalIgnoreCase) >= 0,
					"Arrow shader must survive body flash, got: " + shaderName);
			}
		}

		[UnityTest]
		public IEnumerator PuzzleLighting_RaisesAmbientContribution()
		{
			yield return LoadBootstrap();
			yield return null;

			var lighting = Object.FindFirstObjectByType<PuzzleLighting>();
			Assert.That(lighting, Is.Not.Null);
			Assert.That(RenderSettings.ambientLight.grayscale, Is.GreaterThan(0.35f));
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
