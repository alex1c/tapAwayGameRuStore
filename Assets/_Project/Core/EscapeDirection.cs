namespace TapAway.Core
{
	/// <summary>
	/// Axis-aligned escape direction on the integer grid.
	/// </summary>
	public enum EscapeDirection
	{
		PosX = 0,
		NegX = 1,
		PosY = 2,
		NegY = 3,
		PosZ = 4,
		NegZ = 5
	}

	/// <summary>
	/// Helpers for converting escape directions into grid steps.
	/// </summary>
	public static class EscapeDirectionUtil
	{
		/// <summary>
		/// Returns the unit step along the escape ray for the given direction.
		/// </summary>
		public static void GetStep(EscapeDirection direction, out int dx, out int dy, out int dz)
		{
			dx = 0;
			dy = 0;
			dz = 0;

			switch (direction)
			{
				case EscapeDirection.PosX:
					dx = 1;
					break;
				case EscapeDirection.NegX:
					dx = -1;
					break;
				case EscapeDirection.PosY:
					dy = 1;
					break;
				case EscapeDirection.NegY:
					dy = -1;
					break;
				case EscapeDirection.PosZ:
					dz = 1;
					break;
				case EscapeDirection.NegZ:
					dz = -1;
					break;
				default:
					dx = 0;
					dy = 0;
					dz = 0;
					break;
			}
		}

		/// <summary>
		/// Short label used by debug overlays and tests.
		/// </summary>
		public static string ToShortLabel(EscapeDirection direction)
		{
			switch (direction)
			{
				case EscapeDirection.PosX: return "+X";
				case EscapeDirection.NegX: return "-X";
				case EscapeDirection.PosY: return "+Y";
				case EscapeDirection.NegY: return "-Y";
				case EscapeDirection.PosZ: return "+Z";
				case EscapeDirection.NegZ: return "-Z";
				default: return "?";
			}
		}
	}
}
