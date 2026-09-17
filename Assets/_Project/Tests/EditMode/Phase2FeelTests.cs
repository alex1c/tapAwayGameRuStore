using NUnit.Framework;
using TapAway.Core;

namespace TapAway.Core.Tests
{
	public sealed class CameraFramingMathTests
	{
		[Test]
		public void PortraitAspect_UsesHorizontalFovLimit()
		{
			var portrait = CameraFramingMath.ComputeOrbitDistance(2f, 60f, 9f / 16f, 1.4f, 3.5f, 22f);
			var landscape = CameraFramingMath.ComputeOrbitDistance(2f, 60f, 16f / 9f, 1.4f, 3.5f, 22f);
			Assert.That(portrait, Is.GreaterThan(landscape));
		}

		[TestCase(0.5f, 0.5f, 0.5f, TestName = "SmallCubic")]
		[TestCase(1f, 4f, 1f, TestName = "Tall")]
		[TestCase(4f, 1f, 1f, TestName = "Wide")]
		[TestCase(2f, 2f, 2f, TestName = "DeepCubic")]
		public void SyntheticBounds_ProduceClampedPositiveDistance(float ex, float ey, float ez)
		{
			var radius = CameraFramingMath.RadiusFromExtents(ex, ey, ez);
			var distance = CameraFramingMath.ComputeOrbitDistance(radius, 60f, 9f / 16f, 1.42f, 3.5f, 22f);
			Assert.That(distance, Is.InRange(3.5f, 22f));
		}

		[Test]
		public void LargerRadius_RequiresLargerOrEqualDistance()
		{
			var small = CameraFramingMath.ComputeOrbitDistance(1f, 60f, 0.56f, 1.4f, 3.5f, 22f);
			var large = CameraFramingMath.ComputeOrbitDistance(3f, 60f, 0.56f, 1.4f, 3.5f, 22f);
			Assert.That(large, Is.GreaterThanOrEqualTo(small));
		}
	}

	public sealed class GestureClassifierTests
	{
		private static readonly GestureThresholds Thresholds = GestureThresholds.MobileDefault;

		[Test]
		public void SmallFingerJitter_RemainsTap()
		{
			Assert.That(GestureClassifier.IsTapRelease(10f, Thresholds), Is.True);
			Assert.That(GestureClassifier.ShouldStartDrag(10f, Thresholds), Is.False);
		}

		[Test]
		public void MeaningfulMovement_StartsDrag()
		{
			Assert.That(GestureClassifier.ShouldStartDrag(30f, Thresholds), Is.True);
			Assert.That(GestureClassifier.IsTapRelease(30f, Thresholds), Is.False);
		}

		[Test]
		public void PinchDelta_CancelsTap()
		{
			Assert.That(GestureClassifier.ShouldCancelTapForPinch(12f, Thresholds), Is.True);
			Assert.That(GestureClassifier.ShouldCancelTapForPinch(2f, Thresholds), Is.False);
		}

		[Test]
		public void PinchZoomFactor_ScalesWithFingerDistance()
		{
			Assert.That(GestureClassifier.PinchZoomFactor(100f, 120f), Is.EqualTo(1.2f).Within(0.001f));
			Assert.That(GestureClassifier.PinchZoomFactor(100f, 80f), Is.EqualTo(0.8f).Within(0.001f));
		}

		[Test]
		public void MeaningfulDrag_UsesDedicatedThreshold()
		{
			Assert.That(GestureClassifier.IsMeaningfulDrag(20f, Thresholds), Is.False);
			Assert.That(GestureClassifier.IsMeaningfulDrag(50f, Thresholds), Is.True);
		}
	}

	public sealed class TutorialStateTests
	{
		[Test]
		public void HappyPath_ReachesCompleted()
		{
			var tutorial = new TutorialState(true);
			Assert.That(tutorial.Step, Is.EqualTo(TutorialStep.TapRemovable));
			Assert.That(tutorial.GetPromptRu(), Does.Contain("Нажмите"));

			tutorial.NotifyAllowedRemoval();
			Assert.That(tutorial.Step, Is.EqualTo(TutorialStep.ExplainBlocked));

			tutorial.NotifyBlockedAttempt();
			Assert.That(tutorial.Step, Is.EqualTo(TutorialStep.RotateView));

			tutorial.NotifyMeaningfulDrag();
			Assert.That(tutorial.IsCompleted, Is.True);
			Assert.That(tutorial.GetPromptRu(), Is.EqualTo("Отлично!"));
		}

		[Test]
		public void Skip_CompletesImmediately()
		{
			var tutorial = new TutorialState(true);
			tutorial.Skip();
			Assert.That(tutorial.IsSkipped, Is.True);
			Assert.That(tutorial.IsCompleted, Is.True);
			Assert.That(tutorial.IsActive, Is.False);
		}

		[Test]
		public void SoftContinue_FromBlockedPrompt()
		{
			var tutorial = new TutorialState(true);
			tutorial.NotifyAllowedRemoval();
			tutorial.NotifyContinueFromBlockedPrompt();
			Assert.That(tutorial.Step, Is.EqualTo(TutorialStep.RotateView));
		}
	}
}
