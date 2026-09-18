using NUnit.Framework;
using TapAway.Core;

namespace TapAway.Core.Tests
{
	/// <summary>Phase 4 QA set + metrics + direction math coverage.</summary>
	public sealed class Phase4QaLevelSetTests
	{
		[Test]
		public void QaEntries_HaveExpectedCounts()
		{
			Assert.That(Phase4QaLevelSet.Count, Is.EqualTo(7));
			var expected = new[] { 8, 10, 12, 15, 18, 22, 28 };
			for (var i = 0; i < expected.Length; i++)
			{
				Assert.That(Phase4QaLevelSet.Entries[i].TargetBlockCount, Is.EqualTo(expected[i]));
			}
		}

		[Test]
		public void EachQaSeed_IsAcceptedAndReproducible()
		{
			for (var i = 0; i < Phase4QaLevelSet.Entries.Length; i++)
			{
				var entry = Phase4QaLevelSet.Entries[i];
				var a = Phase4QaLevelSet.GenerateEntry(entry);
				var b = Phase4QaLevelSet.GenerateEntry(entry);
				Assert.That(a.Accepted, Is.True, entry.Label + " " + a.RejectionReason + " " + a.Detail);
				Assert.That(b.Accepted, Is.True);
				Assert.That(a.Level.Blocks.Count, Is.EqualTo(entry.TargetBlockCount));
				Assert.That(a.Level.Blocks.Count, Is.EqualTo(b.Level.Blocks.Count));
				for (var bIdx = 0; bIdx < a.Level.Blocks.Count; bIdx++)
				{
					Assert.That(a.Level.Blocks[bIdx].Position, Is.EqualTo(b.Level.Blocks[bIdx].Position));
					Assert.That(a.Level.Blocks[bIdx].EscapeDirection, Is.EqualTo(b.Level.Blocks[bIdx].EscapeDirection));
				}

				Assert.That(PuzzleSolver.TryReplaySolution(a.Level.Blocks, a.Level.CanonicalSolution), Is.True);
			}
		}

		[Test]
		public void RestartDescriptor_MatchesGetLevel()
		{
			var source = new Phase4QaLevelSource();
			var first = source.GetLevel(0);
			var again = source.GetLevel(0);
			Assert.That(first.Seed, Is.EqualTo(again.Seed));
			Assert.That(first.BlockCount, Is.EqualTo(again.BlockCount));
			for (var i = 0; i < first.Puzzle.Blocks.Count; i++)
			{
				Assert.That(first.Puzzle.Blocks[i].Position, Is.EqualTo(again.Puzzle.Blocks[i].Position));
			}
		}

		[Test]
		public void Sequence_IsDeterministicOrder()
		{
			Assert.That(Phase4QaLevelSet.Entries[0].Index, Is.EqualTo(1));
			Assert.That(Phase4QaLevelSet.Entries[6].Index, Is.EqualTo(7));
			for (var i = 1; i < Phase4QaLevelSet.Entries.Length; i++)
			{
				Assert.That(
					Phase4QaLevelSet.Entries[i].TargetBlockCount,
					Is.GreaterThan(Phase4QaLevelSet.Entries[i - 1].TargetBlockCount));
			}
		}

		[Test]
		public void Metrics_ResetCopiesDescriptor()
		{
			var metrics = new LevelPlayMetrics();
			var desc = new GameLevelDescriptor
			{
				Seed = 42,
				BlockCount = 8,
				DifficultyScore = 12.5,
				DifficultyBand = DifficultyBand.Easy
			};
			metrics.Reset(desc);
			metrics.BlockedTaps = 3;
			Assert.That(metrics.Seed, Is.EqualTo(42));
			Assert.That(metrics.ToDebugLine(), Does.Contain("blocked=3"));
		}

		[Test]
		public void ConfigForCount_TargetsExactCount()
		{
			Assert.That(Phase4QaLevelSet.ConfigForCount(15).TargetBlockCount, Is.EqualTo(15));
			Assert.That(Phase4QaLevelSet.ConfigForCount(28).TargetBlockCount, Is.EqualTo(28));
		}
	}
}
