using NUnit.Framework;
using TapAway.Core;

namespace TapAway.Core.Tests
{
	/// <summary>
	/// Trivial EditMode proof that TapAway.Core can be tested without a scene.
	/// </summary>
	public sealed class FoundationInfoTests
	{
		[Test]
		public void GetBootstrapLabel_ContainsProductNameAndPhase()
		{
			var label = FoundationInfo.GetBootstrapLabel();

			Assert.That(label, Does.Contain(FoundationInfo.ProductName));
			Assert.That(label, Does.Contain(FoundationInfo.Phase));
		}

		[Test]
		public void Phase_IsFoundationMarker()
		{
			Assert.That(FoundationInfo.Phase, Is.EqualTo("PHASE_0_FOUNDATION"));
		}
	}
}
