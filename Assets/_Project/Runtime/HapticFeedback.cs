using UnityEngine;

namespace TapAway.Runtime
{
	/// <summary>
	/// Thin haptic hook. Uses Handheld.Vibrate when available; never required for gameplay.
	/// </summary>
	public static class HapticFeedback
	{
		public enum Kind
		{
			LightSuccess,
			Blocked
		}

		private static bool _enabled = true;

		public static bool Enabled
		{
			get => _enabled;
			set => _enabled = value;
		}

		/// <summary>
		/// Attempts a device vibration. Safe no-op in Editor / unsupported platforms.
		/// </summary>
		public static void Play(Kind kind)
		{
			if (!_enabled)
			{
				return;
			}

#if UNITY_ANDROID && !UNITY_EDITOR
			// Built-in path only — no native plugin. Both kinds use the same short buzz;
			// distinction is reserved for a future richer API.
			try
			{
				Handheld.Vibrate();
			}
			catch (System.Exception)
			{
				// Never let haptics break gameplay.
			}
#else
			// Editor / non-Android: intentional no-op.
			_ = kind;
#endif
		}
	}
}
