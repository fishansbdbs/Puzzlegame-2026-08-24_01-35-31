using UnityEngine;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Player-facing effect intensity options. All juice (shake, hit stop,
    /// flashes, long reveal animations) must scale through these values so
    /// the reduced-motion requirement holds everywhere.
    /// </summary>
    public static class MotionSettings
    {
        const string IntensityKey = "pg.fx.intensity";
        const string ReducedKey = "pg.fx.reduced";

        static float _intensity = -1f;
        static int _reduced = -1;

        /// <summary>0.0 (minimal) .. 1.0 (full juice). Default 1.</summary>
        public static float Intensity
        {
            get
            {
                if (_intensity < 0f) _intensity = PlayerPrefs.GetFloat(IntensityKey, 1f);
                return _intensity;
            }
            set
            {
                _intensity = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(IntensityKey, _intensity);
            }
        }

        /// <summary>Reduced-motion accessibility switch: disables shake/hit-stop and shortens reveals.</summary>
        public static bool ReducedMotion
        {
            get
            {
                if (_reduced < 0) _reduced = PlayerPrefs.GetInt(ReducedKey, 0);
                return _reduced == 1;
            }
            set
            {
                _reduced = value ? 1 : 0;
                PlayerPrefs.SetInt(ReducedKey, _reduced);
            }
        }

        /// <summary>Effective multiplier for shake magnitude, flash alpha, etc.</summary>
        public static float Juice => ReducedMotion ? 0f : Intensity;

        /// <summary>Scale an animation duration; reveals get snappier under reduced motion.</summary>
        public static int Ms(int fullMilliseconds)
        {
            return ReducedMotion ? Mathf.Max(60, fullMilliseconds / 3) : fullMilliseconds;
        }
    }
}
