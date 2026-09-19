using System;
using System.Collections.Generic;

namespace TapAway.Core
{
	/// <summary>
	/// One stable campaign slot. Runtime reconstructs the level from seed + config
	/// via generator 1.1.0 — never randomizes at play time.
	/// </summary>
	public readonly struct CampaignLevelEntry
	{
		public readonly int Index;
		public readonly int Seed;
		public readonly int TargetBlockCount;
		public readonly string Id;
		public readonly string Label;

		public CampaignLevelEntry(int index, int seed, int targetBlockCount)
		{
			Index = index;
			Seed = seed;
			TargetBlockCount = targetBlockCount;
			Id = "campaign_" + index.ToString("00") + "_" + seed;
			Label = "Уровень " + index;
		}
	}

	/// <summary>
	/// Phase 5 deterministic campaign (30 levels). Content curated offline;
	/// Difficulty V1 is advisory only — selection used block count, shape, and
	/// mobile readability rather than V1 band labels.
	/// </summary>
	public static class CampaignDefinition
	{
		public const string GeneratorVersion = GeneratorConfig.GeneratorVersion;
		public const int LevelCount = 30;

		/// <summary>Comfortable mobile ceiling — OPPO QA showed ~28 blocks too small.</summary>
		public const int MaxComfortableBlockCount = 20;

		/// <summary>
		/// Soft radius proxy ceiling for campaign acceptance (grid extents sphere).
		/// </summary>
		public const float MaxComfortableRadiusProxy = 5.0f;

		/// <summary>
		/// Locked accepted seeds (generator 1.1.0). Includes Phase 4 QA seeds
		/// where counts match: 41000/8, 42000/10, 1000/12, 43000/15, 44000/18.
		/// </summary>
		public static readonly CampaignLevelEntry[] Entries =
		{
			new CampaignLevelEntry(1, 50000, 6),
			new CampaignLevelEntry(2, 50001, 6),
			new CampaignLevelEntry(3, 50002, 6),
			new CampaignLevelEntry(4, 51000, 7),
			new CampaignLevelEntry(5, 51001, 7),
			new CampaignLevelEntry(6, 41000, 8),
			new CampaignLevelEntry(7, 52000, 8),
			new CampaignLevelEntry(8, 53000, 9),
			new CampaignLevelEntry(9, 53001, 9),
			new CampaignLevelEntry(10, 53002, 9),
			new CampaignLevelEntry(11, 42000, 10),
			new CampaignLevelEntry(12, 54000, 10),
			new CampaignLevelEntry(13, 1000, 12),
			new CampaignLevelEntry(14, 55000, 12),
			new CampaignLevelEntry(15, 55001, 12),
			new CampaignLevelEntry(16, 56000, 14),
			new CampaignLevelEntry(17, 56001, 14),
			new CampaignLevelEntry(18, 56002, 14),
			new CampaignLevelEntry(19, 43000, 15),
			new CampaignLevelEntry(20, 57000, 15),
			new CampaignLevelEntry(21, 57001, 15),
			new CampaignLevelEntry(22, 58000, 16),
			new CampaignLevelEntry(23, 58001, 16),
			new CampaignLevelEntry(24, 58002, 16),
			new CampaignLevelEntry(25, 44000, 18),
			new CampaignLevelEntry(26, 59000, 18),
			new CampaignLevelEntry(27, 59001, 18),
			new CampaignLevelEntry(28, 60000, 20),
			new CampaignLevelEntry(29, 60001, 20),
			new CampaignLevelEntry(30, 60002, 20)
		};

		public static CampaignLevelEntry GetEntry(int index1Based)
		{
			if (index1Based < 1 || index1Based > Entries.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(index1Based));
			}

			return Entries[index1Based - 1];
		}

		public static GeneratorConfig ConfigForEntry(CampaignLevelEntry entry)
		{
			return Phase4QaLevelSet.ConfigForCount(entry.TargetBlockCount);
		}

		public static GenerationResult Generate(CampaignLevelEntry entry)
		{
			return GenerationPipeline.Generate(entry.Seed, ConfigForEntry(entry));
		}

		public static GameLevelDescriptor ToDescriptor(CampaignLevelEntry entry, GeneratedLevel generated)
		{
			var puzzle = new PuzzleLevel(
				entry.Id,
				entry.Label + " (" + generated.Blocks.Count + ")",
				generated.Blocks,
				generated.CanonicalSolution);

			return new GameLevelDescriptor
			{
				Id = entry.Id,
				DisplayName = entry.Label,
				Puzzle = puzzle,
				Seed = entry.Seed,
				GeneratorVersion = GeneratorVersion,
				BlockCount = generated.Blocks.Count,
				DifficultyScore = generated.Difficulty != null ? generated.Difficulty.Score : (double?)null,
				DifficultyBand = generated.Difficulty != null ? generated.Difficulty.Band : (DifficultyBand?)null,
				IsTutorialFixture = false
			};
		}

		/// <summary>Rebuilds a campaign level descriptor from the locked seed/config.</summary>
		public static GameLevelDescriptor LoadLevel(int index1Based)
		{
			var entry = GetEntry(index1Based);
			var generated = Generate(entry);
			if (!generated.Accepted || generated.Level == null)
			{
				throw new InvalidOperationException(
					"Campaign level " + index1Based + " seed " + entry.Seed +
					" rejected: " + generated.RejectionReason + " " + generated.Detail);
			}

			return ToDescriptor(entry, generated.Level);
		}

		public static bool TryGetEntryById(string id, out CampaignLevelEntry entry)
		{
			for (var i = 0; i < Entries.Length; i++)
			{
				if (Entries[i].Id == id)
				{
					entry = Entries[i];
					return true;
				}
			}

			entry = default;
			return false;
		}

		public static IReadOnlyList<int> AllSeeds()
		{
			var seeds = new int[Entries.Length];
			for (var i = 0; i < Entries.Length; i++)
			{
				seeds[i] = Entries[i].Seed;
			}

			return seeds;
		}
	}
}
