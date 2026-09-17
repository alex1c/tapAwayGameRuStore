namespace TapAway.Core
{
	/// <summary>
	/// Stable identity for a puzzle block. Opaque to presentation.
	/// </summary>
	public readonly struct BlockId : System.IEquatable<BlockId>
	{
		public readonly int Value;

		public BlockId(int value)
		{
			Value = value;
		}

		public bool Equals(BlockId other)
		{
			return Value == other.Value;
		}

		public override bool Equals(object obj)
		{
			return obj is BlockId other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Value;
		}

		public override string ToString()
		{
			return "B" + Value;
		}

		public static bool operator ==(BlockId a, BlockId b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(BlockId a, BlockId b)
		{
			return !a.Equals(b);
		}
	}
}
