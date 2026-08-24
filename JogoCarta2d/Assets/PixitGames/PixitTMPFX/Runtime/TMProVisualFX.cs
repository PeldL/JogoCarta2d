using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Pixit.TMProVisualFX
{
    [AddComponentMenu("Pixit/TMPro Visual FX/TMPro Visual FX")]
    [DisallowMultipleComponent]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest")]
    public sealed class TMProVisualFX : MonoBehaviour
    {
        public enum ColorMode
        {
            Original,
            Solid,
            Gradient,
            Rainbow
        }

        public enum MotionMode
        {
            None,
            Bounce,
            Wave,
            Shake,
            Pulse,
            Jitter,
            Swing,
            Jump,
            Wiggle,
            Glitch,
            Breath,
            Explode
        }

        public enum TypeAnimationMode
        {
            None,
            Typewriter,
            PopIn,
            FadeInWave,
            Spiral
        }

        public enum EffectRangeMode
        {
            AllText,
            CustomRanges
        }

        private struct MotionState
        {
            public static readonly MotionState None = new MotionState(Vector3.zero, 1f, 0f, 1f);

            public readonly Vector3 offset;
            public readonly float scale;
            public readonly float rotation;
            public readonly float alpha;

            public MotionState(Vector3 offset, float scale, float rotation, float alpha)
            {
                this.offset = offset;
                this.scale = Mathf.Max(0.01f, scale);
                this.rotation = rotation;
                this.alpha = Mathf.Clamp01(alpha);
            }

            public static MotionState WithOffset(Vector3 offset)
            {
                return new MotionState(offset, 1f, 0f, 1f);
            }

            public static MotionState WithScale(float scale)
            {
                return new MotionState(Vector3.zero, scale, 0f, 1f);
            }

            public static MotionState WithRotation(float rotation)
            {
                return new MotionState(Vector3.zero, 1f, rotation, 1f);
            }

            public static MotionState WithAlpha(float alpha)
            {
                return new MotionState(Vector3.zero, 1f, 0f, alpha);
            }

            public MotionState Combine(MotionState other)
            {
                return new MotionState(
                    offset + other.offset,
                    scale * other.scale,
                    rotation + other.rotation,
                    alpha * other.alpha);
            }
        }

        [Serializable]
        public sealed class CharacterRange
        {
            [Min(0)] public int startIndex;
            [Min(0)] public int endIndex;

            public bool Contains(int visibleCharacterIndex)
            {
                int min = Mathf.Min(startIndex, endIndex);
                int max = Mathf.Max(startIndex, endIndex);
                return visibleCharacterIndex >= min && visibleCharacterIndex <= max;
            }
        }

        [Header("Text")]
        [Tooltip("Leave empty to use the TextMeshPro component on this GameObject.")]
        [SerializeField] private TMP_Text targetText;

        [Header("Effect Range")]
        [Tooltip("Choose whether effects apply to all visible characters or only selected index ranges.")]
        [SerializeField] private EffectRangeMode effectRangeMode = EffectRangeMode.AllText;
        [Tooltip("Visible character ranges. Spaces and rich text tags are not counted as visible characters.")]
        [SerializeField] private List<CharacterRange> effectRanges = new List<CharacterRange>();

        [Header("Color FX")]
        [Tooltip("Controls how this component colors the text characters.")]
        [SerializeField] private ColorMode colorMode = ColorMode.Gradient;
        [SerializeField] private Color solidColor = Color.white;
        [SerializeField] private Gradient gradient = DefaultGradient();
        [SerializeField, Min(0f)] private float rainbowSpeed = 1f;
        [SerializeField, Range(0f, 1f)] private float rainbowSaturation = 0.85f;
        [SerializeField, Range(0f, 1f)] private float rainbowValue = 1f;

        [Header("Motion FX")]
        [Tooltip("Controls how this component moves the text characters.")]
        [SerializeField] private MotionMode motionMode = MotionMode.Bounce;
        [Tooltip("Movement height in local text units.")]
        [SerializeField, Min(0f)] private float motionAmount = 8f;
        [Tooltip("How quickly the animation moves.")]
        [SerializeField, Min(0f)] private float motionSpeed = 3f;
        [Tooltip("Time offset between each visible character.")]
        [SerializeField, Min(0f)] private float characterDelay = 0.08f;
        [Tooltip("Scale strength used by Pulse, Pop In, Breath, Spiral, and Explode.")]
        [SerializeField, Min(0f)] private float scaleAmount = 0.25f;
        [Tooltip("Rotation strength in degrees used by Swing, Wiggle, type Spiral, and Explode.")]
        [SerializeField, Min(0f)] private float rotationAmount = 12f;

        [Header("Type Animation")]
        [Tooltip("Controls how characters reveal when Play or Restart is called.")]
        [SerializeField] private TypeAnimationMode typeAnimationMode = TypeAnimationMode.None;
        [Tooltip("Visible characters revealed per second for type animation modes.")]
        [SerializeField, Min(0.01f)] private float typeCharactersPerSecond = 24f;
        [Tooltip("Soft fade-in duration for each revealed character.")]
        [SerializeField, Min(0f)] private float typeFadeDuration = 0.06f;

        [Header("Playback")]
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool useUnscaledTime;

        private bool isPlaying;
        private float playStartTime;

        public TMP_Text Text => targetText;
        public bool IsPlaying => isPlaying;

        private void Reset()
        {
            targetText = GetComponent<TMP_Text>();
        }

        private void Awake()
        {
            CacheTextComponent();
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            RestoreText();
        }

        private void OnValidate()
        {
            CacheTextComponent();

            if (enabled && targetText != null && targetText.isActiveAndEnabled)
            {
                ApplyEffects(CurrentTime());
            }
        }

        private void LateUpdate()
        {
            if (!isPlaying || targetText == null)
            {
                return;
            }

            ApplyEffects(CurrentTime());
        }

        public void Play()
        {
            isPlaying = true;
            playStartTime = CurrentTime();
            ApplyEffects(CurrentTime());
        }

        public void Restart()
        {
            Play();
        }

        public void Stop(bool restoreOriginal = true)
        {
            isPlaying = false;

            if (restoreOriginal)
            {
                RestoreText();
            }
        }

        public void SetSolidColor(Color color)
        {
            solidColor = color;
            colorMode = ColorMode.Solid;
            ApplyEffects(CurrentTime());
        }

        public void SetGradient(Gradient textGradient)
        {
            gradient = textGradient;
            colorMode = ColorMode.Gradient;
            ApplyEffects(CurrentTime());
        }

        public void AddEffectRange(int startIndex, int endIndex)
        {
            effectRangeMode = EffectRangeMode.CustomRanges;

            if (effectRanges == null)
            {
                effectRanges = new List<CharacterRange>();
            }

            effectRanges.Add(new CharacterRange
            {
                startIndex = Mathf.Max(0, startIndex),
                endIndex = Mathf.Max(0, endIndex)
            });

            ApplyEffects(CurrentTime());
        }

        public void ClearEffectRanges()
        {
            if (effectRanges == null)
            {
                effectRanges = new List<CharacterRange>();
            }

            effectRanges.Clear();
            ApplyEffects(CurrentTime());
        }

        private void ApplyEffects(float time)
        {
            CacheTextComponent();

            if (targetText == null)
            {
                return;
            }

            targetText.ForceMeshUpdate();

            TMP_TextInfo textInfo = targetText.textInfo;

            if (textInfo == null || textInfo.characterInfo == null || textInfo.meshInfo == null)
            {
                return;
            }

            int characterCount = textInfo.characterCount;

            if (characterCount == 0)
            {
                return;
            }

            int visibleCharacterIndex = 0;
            int affectedCharacterCount = CountAffectedCharacters(textInfo, characterCount);
            int affectedCharacterIndex = 0;

            for (int i = 0; i < characterCount; i++)
            {
                TMP_CharacterInfo character = textInfo.characterInfo[i];

                if (!character.isVisible)
                {
                    continue;
                }

                bool isInEffectRange = IsInEffectRange(visibleCharacterIndex);
                int materialIndex = character.materialReferenceIndex;
                int vertexIndex = character.vertexIndex;

                if (materialIndex < 0 || materialIndex >= textInfo.meshInfo.Length)
                {
                    visibleCharacterIndex++;
                    continue;
                }

                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
                Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

                if (vertices == null || colors == null || vertexIndex < 0 || vertexIndex + 3 >= vertices.Length || vertexIndex + 3 >= colors.Length)
                {
                    visibleCharacterIndex++;
                    continue;
                }

                MotionState motionState = isInEffectRange
                    ? GetMotionState(affectedCharacterIndex, affectedCharacterCount, time)
                    : MotionState.None;
                MotionState typeState = isInEffectRange
                    ? GetTypeAnimationState(affectedCharacterIndex, time)
                    : MotionState.None;
                MotionState combinedState = motionState.Combine(typeState);
                Color32 color = GetColor(affectedCharacterIndex, affectedCharacterCount, time, colors[vertexIndex]);
                Vector3 center = (vertices[vertexIndex] + vertices[vertexIndex + 2]) * 0.5f;
                Quaternion rotation = Quaternion.Euler(0f, 0f, combinedState.rotation);

                for (int vertex = 0; vertex < 4; vertex++)
                {
                    int currentVertexIndex = vertexIndex + vertex;
                    Vector3 relativePosition = vertices[currentVertexIndex] - center;
                    vertices[currentVertexIndex] = center + (rotation * (relativePosition * combinedState.scale)) + combinedState.offset;

                    if (isInEffectRange && colorMode != ColorMode.Original)
                    {
                        colors[currentVertexIndex] = color;
                    }

                    if (isInEffectRange && combinedState.alpha < 1f)
                    {
                        Color32 vertexColor = colors[currentVertexIndex];
                        vertexColor.a = (byte)Mathf.RoundToInt(vertexColor.a * combinedState.alpha);
                        colors[currentVertexIndex] = vertexColor;
                    }
                }

                if (isInEffectRange)
                {
                    affectedCharacterIndex++;
                }

                visibleCharacterIndex++;
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                TMP_MeshInfo meshInfo = textInfo.meshInfo[i];

                if (meshInfo.mesh == null || meshInfo.vertices == null || meshInfo.colors32 == null)
                {
                    continue;
                }

                meshInfo.mesh.vertices = meshInfo.vertices;
                meshInfo.mesh.colors32 = meshInfo.colors32;
                targetText.UpdateGeometry(meshInfo.mesh, i);
            }
        }

        private MotionState GetMotionState(int characterIndex, int characterCount, float time)
        {
            if (motionMode == MotionMode.None)
            {
                return MotionState.None;
            }

            float phase = (time * motionSpeed) + (characterIndex * characterDelay);
            float playElapsed = Mathf.Max(0f, time - playStartTime);

            switch (motionMode)
            {
                case MotionMode.Bounce:
                    return MotionState.WithOffset(Vector3.up * Mathf.Abs(Mathf.Sin(phase)) * motionAmount);
                case MotionMode.Wave:
                    return MotionState.WithOffset(Vector3.up * Mathf.Sin(phase) * motionAmount);
                case MotionMode.Shake:
                    return MotionState.WithOffset(GetShakeOffset(characterIndex, time, motionAmount));
                case MotionMode.Pulse:
                    return MotionState.WithScale(1f + (Mathf.Sin(phase) * scaleAmount));
                case MotionMode.Jitter:
                    return MotionState.WithOffset(GetShakeOffset(characterIndex, time * 1.7f, motionAmount * 0.55f));
                case MotionMode.Swing:
                    return MotionState.WithRotation(Mathf.Sin(phase) * rotationAmount);
                case MotionMode.Jump:
                    return MotionState.WithOffset(Vector3.up * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(phase)), 2f) * motionAmount);
                case MotionMode.Wiggle:
                    return new MotionState(
                        new Vector3(Mathf.Sin(phase * 1.3f), Mathf.Cos(phase * 1.7f), 0f) * motionAmount * 0.35f,
                        1f,
                        Mathf.Sin(phase * 1.9f) * rotationAmount,
                        1f);
                case MotionMode.Glitch:
                    return GetGlitchState(characterIndex, time);
                case MotionMode.Breath:
                    return MotionState.WithScale(1f + ((Mathf.Sin(phase) + 1f) * 0.5f * scaleAmount));
                case MotionMode.Explode:
                    return GetExplodeState(characterIndex, characterCount, playElapsed);
                default:
                    return MotionState.None;
            }
        }

        private MotionState GetTypeAnimationState(int characterIndex, float time)
        {
            float playElapsed = Mathf.Max(0f, time - playStartTime);

            switch (typeAnimationMode)
            {
                case TypeAnimationMode.Typewriter:
                    return MotionState.WithAlpha(GetRevealAlpha(characterIndex, playElapsed));
                case TypeAnimationMode.PopIn:
                    return GetPopInState(characterIndex, playElapsed);
                case TypeAnimationMode.FadeInWave:
                    return GetFadeInWaveState(characterIndex, playElapsed);
                case TypeAnimationMode.Spiral:
                    return GetSpiralState(characterIndex, playElapsed);
                default:
                    return MotionState.None;
            }
        }

        private Color32 GetColor(int characterIndex, int characterCount, float time, Color32 originalColor)
        {
            switch (colorMode)
            {
                case ColorMode.Solid:
                    return solidColor;
                case ColorMode.Gradient:
                    float gradientPosition = characterCount <= 1 ? 0f : characterIndex / (float)(characterCount - 1);
                    return gradient.Evaluate(gradientPosition);
                case ColorMode.Rainbow:
                    float hue = Mathf.Repeat((characterIndex * 0.08f) + (time * rainbowSpeed * 0.1f), 1f);
                    return Color.HSVToRGB(hue, rainbowSaturation, rainbowValue);
                default:
                    return originalColor;
            }
        }

        private float GetRevealAlpha(int characterIndex, float elapsed)
        {
            float safeCharactersPerSecond = Mathf.Max(0.01f, typeCharactersPerSecond);
            float characterStartTime = characterIndex / safeCharactersPerSecond;
            float characterElapsed = elapsed - characterStartTime;

            if (characterElapsed <= 0f)
            {
                return 0f;
            }

            if (typeFadeDuration <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01(characterElapsed / typeFadeDuration);
        }

        private MotionState GetPopInState(int characterIndex, float elapsed)
        {
            float alpha = GetRevealAlpha(characterIndex, elapsed);
            float overshoot = Mathf.Sin(alpha * Mathf.PI) * scaleAmount;
            return new MotionState(Vector3.zero, Mathf.Lerp(0.15f, 1f, alpha) + overshoot, 0f, alpha);
        }

        private MotionState GetFadeInWaveState(int characterIndex, float elapsed)
        {
            float alpha = GetRevealAlpha(characterIndex, elapsed);
            Vector3 offset = Vector3.up * Mathf.Sin(alpha * Mathf.PI) * motionAmount;
            return new MotionState(offset, 1f, 0f, alpha);
        }

        private MotionState GetGlitchState(int characterIndex, float time)
        {
            float burst = Mathf.PerlinNoise(characterIndex * 13.7f, time * motionSpeed * 4f);

            if (burst < 0.58f)
            {
                return MotionState.None;
            }

            float direction = PseudoRandom(characterIndex, Mathf.FloorToInt(time * motionSpeed * 12f)) > 0.5f ? 1f : -1f;
            Vector3 offset = Vector3.right * direction * motionAmount * Mathf.Lerp(0.35f, 1f, burst);
            float alpha = burst > 0.86f ? 0.45f : 1f;
            return new MotionState(offset, 1f, 0f, alpha);
        }

        private MotionState GetSpiralState(int characterIndex, float elapsed)
        {
            float alpha = GetRevealAlpha(characterIndex, elapsed);
            float angle = (1f - alpha) * 360f;
            float radians = (angle + (characterIndex * 32f)) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * motionAmount * (1f - alpha);
            float scale = Mathf.Lerp(0.2f, 1f, alpha);
            float rotation = (1f - alpha) * rotationAmount * 8f;
            return new MotionState(offset, scale, rotation, alpha);
        }

        private MotionState GetExplodeState(int characterIndex, int characterCount, float elapsed)
        {
            float progress = Mathf.Clamp01(elapsed * Mathf.Max(0.01f, motionSpeed));
            float centeredIndex = characterCount <= 1 ? 0f : (characterIndex / (float)(characterCount - 1)) - 0.5f;
            Vector3 direction = new Vector3(centeredIndex * 2f, Mathf.Sin((characterIndex + 1) * 1.91f), 0f).normalized;
            Vector3 offset = direction * motionAmount * 6f * progress;
            float scale = 1f + (scaleAmount * progress);
            float rotation = rotationAmount * 4f * progress * Mathf.Sign(centeredIndex == 0f ? 1f : centeredIndex);
            return new MotionState(offset, scale, rotation, 1f - progress);
        }

        private Vector3 GetShakeOffset(int characterIndex, float time, float amount)
        {
            int step = Mathf.FloorToInt(time * Mathf.Max(0.01f, motionSpeed) * 18f);
            float x = (PseudoRandom(characterIndex, step) - 0.5f) * 2f;
            float y = (PseudoRandom(characterIndex + 37, step + 11) - 0.5f) * 2f;
            return new Vector3(x, y, 0f) * amount;
        }

        private float PseudoRandom(int seedA, int seedB)
        {
            float value = Mathf.Sin((seedA * 12.9898f) + (seedB * 78.233f)) * 43758.5453f;
            return value - Mathf.Floor(value);
        }

        private int CountAffectedCharacters(TMP_TextInfo textInfo, int characterCount)
        {
            int visibleCharacterIndex = 0;
            int affectedCharacterCount = 0;

            for (int i = 0; i < characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                {
                    continue;
                }

                if (IsInEffectRange(visibleCharacterIndex))
                {
                    affectedCharacterCount++;
                }

                visibleCharacterIndex++;
            }

            return affectedCharacterCount;
        }

        private bool IsInEffectRange(int visibleCharacterIndex)
        {
            if (effectRangeMode == EffectRangeMode.AllText)
            {
                return true;
            }

            if (effectRanges == null)
            {
                return false;
            }

            for (int i = 0; i < effectRanges.Count; i++)
            {
                CharacterRange range = effectRanges[i];

                if (range != null && range.Contains(visibleCharacterIndex))
                {
                    return true;
                }
            }

            return false;
        }

        private float CurrentTime()
        {
            return useUnscaledTime ? Time.unscaledTime : Time.time;
        }

        private void CacheTextComponent()
        {
            if (targetText == null)
            {
                targetText = GetComponent<TMP_Text>();
            }
        }

        private void RestoreText()
        {
            if (targetText == null)
            {
                return;
            }

            targetText.ForceMeshUpdate();
        }

        private static Gradient DefaultGradient()
        {
            Gradient defaultGradient = new Gradient();
            defaultGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.22f, 0.35f), 0f),
                    new GradientColorKey(new Color(0.2f, 0.65f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });

            return defaultGradient;
        }
    }
}
