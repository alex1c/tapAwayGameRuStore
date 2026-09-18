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
	/// Direction readability coverage: multi-face glyphs stay obvious before tap.
	/// </summary>
	public sealed class DirectionReadabilityPlayModeTests
	{
		[UnityTest]
		public IEnumerator Builder_CreatesMultiFaceGlyph_ForAllSixDirections()
		{
			var parent = new GameObject("IndicatorTestRoot").transform;
			var plate = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"))
			{
				color = Color.black
			};
			var accent = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"))
			{
				color = Color.yellow
			};

			foreach (EscapeDirection direction in System.Enum.GetValues(typeof(EscapeDirection)))
			{
				var indicator = DirectionIndicatorBuilder.Build(parent, direction, plate, accent);
				Assert.That(
					DirectionReadabilityValidator.HasExpectedMultiFaceCoverage(indicator, direction),
					Is.True,
					"Coverage failed for " + direction);
				Assert.That(indicator.childCount, Is.EqualTo(DirectionIndicatorBuilder.ExpectedPartCount));

				Object.Destroy(indicator.gameObject);
			}

			Object.Destroy(parent.gameObject);
			Object.Destroy(plate);
			Object.Destroy(accent);
			yield return null;
		}

		[UnityTest]
		public IEnumerator LateGameFiveBlocks_DirectionsReadableFromRepresentativeViews()
		{
			yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
			yield return null;

			var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
			Assert.That(bootstrap, Is.Not.Null);
			yield return null;

			var prefix = new[] { 1, 6, 7, 8, 9, 11, 2 };
			foreach (var idValue in prefix)
			{
				var view = bootstrap.Presenter.GetView(new BlockId(idValue));
				Assert.That(view, Is.Not.Null, "Missing view " + idValue);
				Assert.That(bootstrap.Presenter.TrySelect(view), Is.True);

				var timeout = 2f;
				while (timeout > 0f && bootstrap.Presenter.IsInputLocked)
				{
					timeout -= Time.deltaTime;
					yield return null;
				}
			}

			Assert.That(bootstrap.State.ActiveCount, Is.EqualTo(5));

			var activeViews = new List<BlockView>();
			foreach (var block in bootstrap.State.GetActiveBlocks())
			{
				var view = bootstrap.Presenter.GetView(block.Id);
				Assert.That(view, Is.Not.Null);
				activeViews.Add(view);
			}

			var report = DirectionReadabilityValidator.ValidateActiveViews(activeViews);
			Assert.That(report, Does.StartWith("PASS"), report);
		}

		[Test]
		public void SafeLook_StableForVerticalEscapes()
		{
			var up = DirectionIndicatorBuilder.SafeLook(Vector3.up);
			var down = DirectionIndicatorBuilder.SafeLook(Vector3.down);
			Assert.That((up * Vector3.forward - Vector3.up).sqrMagnitude, Is.LessThan(0.0001f));
			Assert.That((down * Vector3.forward - Vector3.down).sqrMagnitude, Is.LessThan(0.0001f));
		}
	}
}
