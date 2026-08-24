using System.Collections;
using UnityEngine;
using PuzzleGame.Presentation.UI;

namespace PuzzleGame.Presentation.VFX
{
    /// <summary>
    /// World-space battle juice for when the battle field renders with
    /// sprites/2.5D. Components are self-installing and prefab-free so core
    /// scenes can adopt them with one AddComponent/one static call.
    /// All effects respect <see cref="MotionSettings"/>.
    /// </summary>
    public class WorldFx : MonoBehaviour
    {
        static WorldFx _instance;

        public static WorldFx Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("WorldFx");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<WorldFx>();
                }
                return _instance;
            }
        }

        // ---------------------------- Screen shake -------------------------

        Vector3 _shakeOffset;
        float _shakeMagnitude;
        float _shakeRemaining;
        float _shakeDuration;
        Transform _shakeTarget;
        Vector3 _shakeBasePos;

        /// <summary>Shake a camera (or any transform). Magnitude in world units.</summary>
        public void ShakeCamera(Transform target, float magnitude = 0.25f, float duration = 0.3f)
        {
            float juice = MotionSettings.Juice;
            if (juice <= 0f || target == null) return;
            if (_shakeTarget != target)
            {
                RestoreShakeTarget();
                _shakeTarget = target;
                _shakeBasePos = target.localPosition;
            }
            _shakeMagnitude = Mathf.Max(_shakeMagnitude, magnitude * juice);
            _shakeDuration = duration;
            _shakeRemaining = Mathf.Max(_shakeRemaining, duration);
        }

        void RestoreShakeTarget()
        {
            if (_shakeTarget != null)
            {
                _shakeTarget.localPosition = _shakeBasePos;
                _shakeTarget = null;
            }
        }

        void LateUpdate()
        {
            if (_shakeTarget == null) return;
            if (_shakeRemaining <= 0f)
            {
                RestoreShakeTarget();
                _shakeMagnitude = 0f;
                return;
            }
            _shakeRemaining -= Time.unscaledDeltaTime;
            float decay = _shakeDuration > 0f ? Mathf.Clamp01(_shakeRemaining / _shakeDuration) : 0f;
            _shakeOffset = new Vector3(
                (Mathf.PerlinNoise(Time.unscaledTime * 35f, 0f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(0f, Time.unscaledTime * 35f) - 0.5f) * 2f,
                0f) * (_shakeMagnitude * decay);
            _shakeTarget.localPosition = _shakeBasePos + _shakeOffset;
        }

        // ------------------------------ Hit stop ----------------------------

        Coroutine _hitStop;

        /// <summary>Brief global time dip that makes big hits land. Skipped under reduced motion.</summary>
        public void HitStop(float duration = 0.07f, float timeScale = 0.05f)
        {
            if (MotionSettings.Juice <= 0f) return;
            if (_hitStop != null) StopCoroutine(_hitStop);
            _hitStop = StartCoroutine(HitStopRoutine(duration, timeScale));
        }

        IEnumerator HitStopRoutine(float duration, float timeScale)
        {
            float previous = Time.timeScale;
            if (previous <= timeScale) yield break;
            Time.timeScale = timeScale;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            _hitStop = null;
        }

        // --------------------------- Particle bursts ------------------------

        /// <summary>
        /// Element-tinted impact burst at a world position. Particle systems
        /// are built in code, pooled per element, and mobile-cheap.
        /// </summary>
        public void ElementalImpact(Vector3 worldPos, ElementId element, float scale = 1f)
        {
            float juice = MotionSettings.Juice;
            if (juice <= 0f) return;
            var ps = GetPooledSystem(element);
            ps.transform.position = worldPos;
            var main = ps.main;
            main.startSizeMultiplier = 0.22f * scale;
            ps.Emit(Mathf.RoundToInt(14 * juice * scale));
        }

        readonly System.Collections.Generic.Dictionary<ElementId, ParticleSystem> _systems =
            new System.Collections.Generic.Dictionary<ElementId, ParticleSystem>();

        ParticleSystem GetPooledSystem(ElementId element)
        {
            if (_systems.TryGetValue(element, out var existing) && existing != null) return existing;
            var go = new GameObject("ImpactFx_" + element);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                Theme.ElementColor(element),
                Color.Lerp(Theme.ElementColor(element), Color.white, 0.6f));
            main.gravityModifier = 0.4f;
            main.maxParticles = 128;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = ParticleMaterial();
            renderer.sortingOrder = 500;
            _systems[element] = ps;
            return ps;
        }

        static Material _particleMaterial;

        static Material ParticleMaterial()
        {
            if (_particleMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                _particleMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
            }
            return _particleMaterial;
        }

        // ------------------------------ Trails ------------------------------

        /// <summary>
        /// Fire a glowing projectile trail from A to B, then invoke onHit.
        /// Used for character attack presentation toward enemies.
        /// </summary>
        public void ProjectileTrail(Vector3 from, Vector3 to, ElementId element, float duration, System.Action onHit)
        {
            if (MotionSettings.Juice <= 0f)
            {
                onHit?.Invoke();
                return;
            }
            StartCoroutine(TrailRoutine(from, to, element, duration, onHit));
        }

        IEnumerator TrailRoutine(Vector3 from, Vector3 to, ElementId element, float duration, System.Action onHit)
        {
            var go = new GameObject("TrailFx");
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.22f;
            trail.startWidth = 0.14f;
            trail.endWidth = 0.01f;
            trail.material = ParticleMaterial();
            trail.startColor = Color.Lerp(Theme.ElementColor(element), Color.white, 0.3f);
            trail.endColor = Theme.ElementColor(element).WithAlpha(0f);
            trail.sortingOrder = 490;
            go.transform.position = from;
            float t = 0f;
            Vector3 mid = (from + to) * 0.5f + Vector3.up * 0.6f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.05f, duration);
                float k = Mathf.Clamp01(t);
                // Quadratic bezier arc for a satisfying curve.
                Vector3 p = Vector3.Lerp(Vector3.Lerp(from, mid, k), Vector3.Lerp(mid, to, k), k);
                go.transform.position = p;
                yield return null;
            }
            onHit?.Invoke();
            ElementalImpact(to, element);
            Destroy(go, trail.time);
        }
    }
}
