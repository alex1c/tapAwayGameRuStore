using System;
using System.Collections.Generic;

namespace TapAway.Core
{
	/// <summary>
	/// Scalable active-block bitset indexed by dense block slot 0..N-1.
	/// Supports more than 64 blocks via ulong chunks — no hard 64 limit.
	/// </summary>
	public struct ActiveMask : IEquatable<ActiveMask>
	{
		private readonly ulong[] _chunks;

		public ActiveMask(int blockCount)
		{
			if (blockCount < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(blockCount));
			}

			var chunkCount = blockCount == 0 ? 0 : ((blockCount + 63) / 64);
			_chunks = chunkCount == 0 ? Array.Empty<ulong>() : new ulong[chunkCount];
		}

		private ActiveMask(ulong[] chunks)
		{
			_chunks = chunks;
		}

		public int ChunkCount => _chunks.Length;

		public static ActiveMask AllActive(int blockCount)
		{
			var mask = new ActiveMask(blockCount);
			for (var i = 0; i < blockCount; i++)
			{
				mask.Set(i);
			}

			return mask;
		}

		public ActiveMask Clone()
		{
			if (_chunks.Length == 0)
			{
				return new ActiveMask(Array.Empty<ulong>());
			}

			var copy = new ulong[_chunks.Length];
			Array.Copy(_chunks, copy, _chunks.Length);
			return new ActiveMask(copy);
		}

		public void Set(int index)
		{
			_chunks[index >> 6] |= 1UL << (index & 63);
		}

		public void Clear(int index)
		{
			_chunks[index >> 6] &= ~(1UL << (index & 63));
		}

		public bool IsSet(int index)
		{
			return (_chunks[index >> 6] & (1UL << (index & 63))) != 0;
		}

		public int CountSet(int blockCount)
		{
			var count = 0;
			for (var i = 0; i < blockCount; i++)
			{
				if (IsSet(i))
				{
					count++;
				}
			}

			return count;
		}

		public bool Equals(ActiveMask other)
		{
			if (_chunks.Length != other._chunks.Length)
			{
				return false;
			}

			for (var i = 0; i < _chunks.Length; i++)
			{
				if (_chunks[i] != other._chunks[i])
				{
					return false;
				}
			}

			return true;
		}

		public override bool Equals(object obj)
		{
			return obj is ActiveMask other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 17;
				for (var i = 0; i < _chunks.Length; i++)
				{
					hash = hash * 31 + _chunks[i].GetHashCode();
				}

				return hash;
			}
		}

		/// <summary>
		/// Stable string key for memo dictionaries (avoids Dictionary order issues).
		/// </summary>
		public string ToStableKey()
		{
			if (_chunks.Length == 0)
			{
				return "0";
			}

			var parts = new string[_chunks.Length];
			for (var i = 0; i < _chunks.Length; i++)
			{
				parts[i] = _chunks[i].ToString("X16");
			}

			return string.Join("-", parts);
		}
	}

	/// <summary>
	/// Equality comparer for ActiveMask keys in HashSet/Dictionary.
	/// </summary>
	public sealed class ActiveMaskComparer : IEqualityComparer<ActiveMask>
	{
		public static readonly ActiveMaskComparer Instance = new ActiveMaskComparer();

		public bool Equals(ActiveMask x, ActiveMask y)
		{
			return x.Equals(y);
		}

		public int GetHashCode(ActiveMask obj)
		{
			return obj.GetHashCode();
		}
	}
}
