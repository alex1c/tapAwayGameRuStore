using System;

namespace TapAway.Core
{
	/// <summary>
	/// Deterministic xorshift64* PRNG for seeded generation.
	/// Not cryptographic — only for reproducible puzzle content.
	/// </summary>
	public sealed class DeterministicRng
	{
		private ulong _state;

		public DeterministicRng(int seed)
		{
			// Avoid zero state which would stick at zero for xorshift.
			_state = (uint)seed == 0u ? 0xA5A5A5A5u : (ulong)(uint)seed;
			_state |= 1UL;
		}

		public DeterministicRng(ulong seed)
		{
			_state = seed == 0UL ? 0x9E3779B97F4A7C15UL : seed;
		}

		/// <summary>Current internal state (for diagnostics / fork).</summary>
		public ulong State => _state;

		public uint NextUInt32()
		{
			_state ^= _state >> 12;
			_state ^= _state << 25;
			_state ^= _state >> 27;
			return (uint)((_state * 0x2545F4914F6CDD1DUL) >> 32);
		}

		public int NextInt(int minInclusive, int maxExclusive)
		{
			if (maxExclusive <= minInclusive)
			{
				throw new ArgumentOutOfRangeException(nameof(maxExclusive));
			}

			var range = (uint)(maxExclusive - minInclusive);
			return minInclusive + (int)(NextUInt32() % range);
		}

		public int NextIndex(int count)
		{
			if (count <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}

			return (int)(NextUInt32() % (uint)count);
		}

		public bool NextBool()
		{
			return (NextUInt32() & 1u) != 0;
		}
	}
}
