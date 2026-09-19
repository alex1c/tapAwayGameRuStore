using System.Collections.Generic;
using NUnit.Framework;
using TapAway.Core;

namespace TapAway.Core.Tests
{
	public sealed class CampaignDefinitionTests
	{
		[Test]
		public void Campaign_HasExactly30SequentialUniqueEntries()
		{
			Assert.That(CampaignDefinition.Entries.Length, Is.EqualTo(30));
			Assert.That(CampaignDefinition.LevelCount, Is.EqualTo(30));
			var seeds = new HashSet<int>();
			var ids = new HashSet<string>();
			for (var i = 0; i < CampaignDefinition.Entries.Length; i++)
			{
				var e = CampaignDefinition.Entries[i];
				Assert.That(e.Index, Is.EqualTo(i + 1));
				Assert.That(seeds.Add(e.Seed), Is.True, "duplicate seed " + e.Seed);
				Assert.That(ids.Add(e.Id), Is.True, "duplicate id " + e.Id);
				Assert.That(e.TargetBlockCount, Is.LessThanOrEqualTo(CampaignDefinition.MaxComfortableBlockCount));
			}

			Assert.That(CampaignDefinition.GeneratorVersion, Is.EqualTo(GeneratorConfig.GeneratorVersion));
		}

		[Test]
		public void EveryCampaignLevel_IsAcceptedSolvableAndReproducible()
		{
			for (var i = 0; i < CampaignDefinition.Entries.Length; i++)
			{
				var entry = CampaignDefinition.Entries[i];
				var a = CampaignDefinition.Generate(entry);
				var b = CampaignDefinition.Generate(entry);
				Assert.That(a.Accepted, Is.True, entry.Id + " " + a.RejectionReason + " " + a.Detail);
				Assert.That(b.Accepted, Is.True);
				Assert.That(a.Level.Blocks.Count, Is.EqualTo(entry.TargetBlockCount));
				Assert.That(a.Level.GeneratorVersion, Is.EqualTo(GeneratorConfig.GeneratorVersion));
				Assert.That(PuzzleSolver.Solve(a.Level.Blocks).IsSolvable, Is.True);
				Assert.That(PuzzleSolver.TryReplaySolution(a.Level.Blocks, a.Level.CanonicalSolution), Is.True);

				for (var bIdx = 0; bIdx < a.Level.Blocks.Count; bIdx++)
				{
					Assert.That(a.Level.Blocks[bIdx].Position, Is.EqualTo(b.Level.Blocks[bIdx].Position));
					Assert.That(a.Level.Blocks[bIdx].EscapeDirection, Is.EqualTo(b.Level.Blocks[bIdx].EscapeDirection));
				}

				var quality = CampaignContentQuality.Evaluate(a.Level.Blocks, a.Level.Difficulty?.Features);
				Assert.That(quality.Accepted, Is.True, entry.Id + " " + quality.Detail);
				Assert.That(quality.RadiusProxy, Is.LessThanOrEqualTo(CampaignDefinition.MaxComfortableRadiusProxy + 0.01f));
			}
		}

		[Test]
		public void LoadLevel_MatchesEntryMetadata()
		{
			var desc = CampaignDefinition.LoadLevel(1);
			Assert.That(desc.Seed, Is.EqualTo(50000));
			Assert.That(desc.BlockCount, Is.EqualTo(6));
			Assert.That(desc.GeneratorVersion, Is.EqualTo(GeneratorConfig.GeneratorVersion));
		}
	}

	public sealed class CampaignProgressTests
	{
		[Test]
		public void FreshProgress_OnlyLevel1Unlocked()
		{
			var p = CampaignProgress.CreateFresh();
			Assert.That(p.IsUnlocked(1), Is.True);
			Assert.That(p.IsUnlocked(2), Is.False);
			Assert.That(p.TutorialCompleted, Is.False);
		}

		[Test]
		public void CompleteLevel1_UnlocksLevel2()
		{
			var p = CampaignProgress.CreateFresh();
			p.MarkLevelCompleted(1, 12f, 1, 2);
			Assert.That(p.IsCompleted(1), Is.True);
			Assert.That(p.IsUnlocked(2), Is.True);
			Assert.That(p.HighestUnlockedLevel, Is.EqualTo(2));
		}

		[Test]
		public void ReplayCompletedLevel_DoesNotCorruptUnlocks()
		{
			var p = CampaignProgress.CreateFresh();
			p.MarkLevelCompleted(1, 20f, 3, 4);
			p.MarkLevelCompleted(1, 10f, 0, 1);
			Assert.That(p.HighestUnlockedLevel, Is.EqualTo(2));
			Assert.That(p.GetOrCreate(1).BestTimeSeconds, Is.EqualTo(10f));
			Assert.That(p.GetOrCreate(1).BestBlockedTaps, Is.EqualTo(0));
		}

		[Test]
		public void OutOfOrderReplay_DoesNotJumpUnlockChain()
		{
			var p = CampaignProgress.CreateFresh();
			p.MarkLevelCompleted(1, 5f, 0, 0);
			p.MarkLevelCompleted(1, 4f, 0, 0);
			Assert.That(p.HighestUnlockedLevel, Is.EqualTo(2));
			Assert.That(p.IsUnlocked(5), Is.False);
		}

		[Test]
		public void Level30Completion_IsSafe()
		{
			var p = CampaignProgress.CreateFresh();
			p.HighestUnlockedLevel = 30;
			p.MarkLevelCompleted(30, 40f, 2, 5);
			Assert.That(p.IsCompleted(30), Is.True);
			Assert.That(p.HighestUnlockedLevel, Is.EqualTo(30));
		}

		[Test]
		public void Json_RoundTrip()
		{
			var p = CampaignProgress.CreateFresh();
			p.MarkTutorialCompleted();
			p.MarkLevelCompleted(1, 11.5f, 2, 3);
			var json = CampaignProgressJson.Serialize(p);
			Assert.That(CampaignProgressJson.TryDeserialize(json, out var loaded, out var err), Is.True, err);
			Assert.That(loaded.TutorialCompleted, Is.True);
			Assert.That(loaded.HighestUnlockedLevel, Is.EqualTo(2));
			Assert.That(loaded.IsCompleted(1), Is.True);
		}

		[Test]
		public void Repository_SaveLoad_AndBackupRecovery()
		{
			var storage = new MemoryProgressStorage();
			var repo = new ProgressRepository(storage);
			var p = CampaignProgress.CreateFresh();
			p.MarkLevelCompleted(1, 9f, 1, 1);
			Assert.That(repo.Save(p).Success, Is.True);

			var load = repo.Load();
			Assert.That(load.Progress.IsCompleted(1), Is.True);

			storage.Corrupt(ProgressRepository.PrimaryFileName, "{not-json");
			var recovered = repo.Load();
			Assert.That(recovered.UsedBackup || recovered.Progress.IsCompleted(1) || recovered.UsedFreshDefault, Is.True);
		}

		[Test]
		public void Repository_CorruptBoth_UsesFreshDefault()
		{
			var storage = new MemoryProgressStorage();
			var repo = new ProgressRepository(storage);
			storage.Corrupt(ProgressRepository.PrimaryFileName, "@@@");
			storage.Corrupt(ProgressRepository.BackupFileName, "@@@");
			var load = repo.Load();
			Assert.That(load.UsedFreshDefault, Is.True);
			Assert.That(load.Progress.HighestUnlockedLevel, Is.EqualTo(1));
		}

		[Test]
		public void Sanitize_ClampsInvalidIndices()
		{
			var p = CampaignProgress.CreateFresh();
			p.HighestUnlockedLevel = 999;
			p.LastPlayedLevel = -3;
			p.Levels[99] = new LevelProgressRecord { Completed = true };
			p.Sanitize();
			Assert.That(p.HighestUnlockedLevel, Is.EqualTo(CampaignDefinition.LevelCount));
			Assert.That(p.LastPlayedLevel, Is.EqualTo(1));
			Assert.That(p.Levels.ContainsKey(99), Is.False);
		}
	}

	public sealed class MobileReadabilityMathTests
	{
		[Test]
		public void CloserDistance_IncreasesScreenFraction()
		{
			var far = MobileReadabilityMath.ScreenFractionAtDistance(1f, 60f, 20f);
			var near = MobileReadabilityMath.ScreenFractionAtDistance(1f, 60f, 5f);
			Assert.That(near, Is.GreaterThan(far));
		}

		[Test]
		public void ZoomClamps_AllowInspectionCloserThanOverview()
		{
			MobileReadabilityMath.ComputeZoomClamps(
				4f, 60f, 9f / 16f, 1.42f,
				out var min, out var max, out var overview);
			Assert.That(min, Is.LessThan(overview));
			Assert.That(overview, Is.LessThanOrEqualTo(max));
			Assert.That(min, Is.GreaterThan(1f));
		}
	}
}
