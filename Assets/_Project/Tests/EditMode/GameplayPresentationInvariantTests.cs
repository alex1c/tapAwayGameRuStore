using NUnit.Framework;
using TapAway.Core;

namespace TapAway.Core.Tests
{
	/// <summary>EditMode coverage for Phase 4 hotfix Core facts.</summary>
	public sealed class GameplayPresentationInvariantTests
	{
		[Test]
		public void Qa1_Seed_ProducesExpectedBlockCount()
		{
			var entry = Phase4QaLevelSet.Entries[0];
			var generated = Phase4QaLevelSet.GenerateEntry(entry);
			Assert.That(generated.Accepted, Is.True);
			Assert.That(generated.Level.Seed, Is.EqualTo(41000));
			Assert.That(generated.Level.Blocks.Count, Is.EqualTo(8));
			Assert.That(PuzzleSolver.Solve(generated.Level.Blocks).IsSolvable, Is.True);
		}

		[Test]
		public void Qa1_HasNoOriginOnlyOrphanRequirement_AllBlocksParticipate()
		{
			// Documents that the grey orphan was FoundationMarker, not a Core block.
			var entry = Phase4QaLevelSet.Entries[0];
			var generated = Phase4QaLevelSet.GenerateEntry(entry);
			Assert.That(generated.Accepted, Is.True);
			var state = new PuzzleState(generated.Level.Blocks);
			Assert.That(state.ActiveCount, Is.EqualTo(generated.Level.Blocks.Count));
			Assert.That(PuzzleSolver.TryReplaySolution(
				generated.Level.Blocks,
				generated.Level.CanonicalSolution), Is.True);
		}
	}
}
