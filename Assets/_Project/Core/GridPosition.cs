namespace TapAway.Core
{
	/// <summary>
	/// Integer cell on the puzzle grid. Core never uses Unity Vector3.
	/// </summary>
	public readonly struct GridPosition : System.IEquatable<GridPosition>
	{
		public readonly int X;
		public readonly int Y;
		public readonly int Z;

		public GridPosition(int x, int y, int z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public GridPosition Offset(int dx, int dy, int dz)
		{
			return new GridPosition(X + dx, Y + dy, Z + dz);
		}

		public bool Equals(GridPosition other)
		{
			return X == other.X && Y == other.Y && Z == other.Z;
		}

		public override bool Equals(object obj)
		{
			return obj is GridPosition other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = X;
				hash = (hash * 397) ^ Y;
				hash = (hash * 397) ^ Z;
				return hash;
			}
		}

		public override string ToString()
		{
			return "(" + X + "," + Y + "," + Z + ")";
		}

		public static bool operator ==(GridPosition a, GridPosition b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(GridPosition a, GridPosition b)
		{
			return !a.Equals(b);
		}
	}
}
