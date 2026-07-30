using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

[ExecuteAlways]
public class StylizedFog : MonoBehaviour
{
    public static StylizedFog Instance;

    // ─── Single Colour Input ──────────────────────────
    // User sets ONE colour per zone
    // Script auto-derives near, mid, far from it

    [Header("Base Fog Colour")]
    [Tooltip("Set one colour — near/mid/far auto-derived")]
    public Color baseFogColor = 
        new Color(0.85f, 0.88f, 0.92f, 1f);

    [Header("Derivation Settings")]
    [Tooltip("How much darker near fog is vs base")]
    [Range(0f, 0.5f)]
    public float nearDarkenAmount = 0.25f;

    [Tooltip("How much darker mid fog is vs base")]
    [Range(0f, 0.3f)]
    public float midDarkenAmount = 0.12f;

    [Tooltip("How much to desaturate near fog")]
    [Range(0f, 0.5f)]
    public float nearDesaturateAmount = 0.15f;

    // ─── Fog Mode ─────────────────────────────────────

    //[Header("Fog Mode")]
    public enum StylizedFogMode
    {
        Exponential,
        ExponentialSquared
    }

    [Tooltip("Exponential = softer. " +
             "ExponentialSquared = denser, faster falloff")]
    public StylizedFogMode fogMode = 
        StylizedFogMode.ExponentialSquared;

    // ─── Density ──────────────────────────────────────

    [Header("Fog Density")]
    [Range(0f, 0.1f)]
    [Tooltip("Master density. " +
             "ExponentialSquared needs lower values " +
             "than Exponential for same visual result")]
    public float fogDensity = 0.025f;

    [Range(0f, 1f)]
    [Tooltip("Overall strength multiplier on top of density")]
    public float fogStrength = 1f;

    // ─── Zone Colours ─────────────────────────────────
    // One base colour per zone
    // Near/mid/far auto-derived at runtime

    [Header("Zone Base Colours")]
    public Color meadowColor = 
        new Color(0.85f, 0.90f, 0.92f, 1f);
    // cool airy white

    public Color autumnColor = 
        new Color(0.92f, 0.82f, 0.68f, 1f);
    // warm amber haze

    public Color coniferousColor = 
        new Color(0.72f, 0.80f, 0.76f, 1f);
    // cold green mist

    public Color mountainColor = 
        new Color(0.78f, 0.85f, 0.95f, 1f);
    // thin blue-white

    // dread override colours
    [Header("Mood Colours")]
    public Color dreadColor = 
        new Color(0.45f, 0.45f, 0.50f, 1f);
    // desaturated grey

    public Color dawnColor = 
        new Color(0.95f, 0.85f, 0.68f, 1f);
    // heavy warm amber

    [Header("Zone Transition Time")]
    public float zoneTransitionTime = 8f;

    // ─── Post Process Integration ─────────────────────

    [Header("Post Process Volume (optional)")]
    public Volume fogVolume;

    [Range(0f, 1f)]
    public float ppTintStrength = 0.25f;

    // ─── State ────────────────────────────────────────

    private Color _currentBase;
    private Coroutine _transitionCoroutine;
    private ColorAdjustments _colorAdj;

    // ─── Unity ────────────────────────────────────────

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(this);
    }

    void OnEnable()
    {
        CachePostProcess();
        StoryDirector.OnStoryEvent += HandleStoryEvent;
        _currentBase = baseFogColor;
        ApplyFog(_currentBase);
    }

    void OnDisable()
    {
        StoryDirector.OnStoryEvent -= HandleStoryEvent;
        RenderSettings.fog = false;
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            _currentBase = baseFogColor;
            ApplyFog(_currentBase);
        }
#endif
    }

    // ─── Core Apply ───────────────────────────────────

    void ApplyFog(Color baseColor)
    {
        RenderSettings.fog = true;

        // set mode
        RenderSettings.fogMode = fogMode ==
            StylizedFogMode.ExponentialSquared
            ? FogMode.ExponentialSquared
            : FogMode.Exponential;

        // derive the three depth colours from base
        Color far  = baseColor;
        Color mid  = DeriveColor(
            baseColor, 
            midDarkenAmount, 
            midDarkenAmount * 0.3f);
        Color near = DeriveColor(
            baseColor, 
            nearDarkenAmount, 
            nearDesaturateAmount);

        // blend near/mid/far into single fog colour
        // exponential fog doesn't support per-distance
        // colour natively so we blend into one
        // representative colour
        // weighted toward far since that's most visible
        Color blended = BlendFogColor(near, mid, far);

        RenderSettings.fogColor = blended;
        RenderSettings.fogDensity = 
            fogDensity * fogStrength;

        ApplyPostProcessTint(blended);
    }

    // ─── Colour Derivation ────────────────────────────

    // Auto-derive darker/more-saturated colour
    // for near/mid depth from one base colour
    Color DeriveColor(
        Color baseColor, 
        float darken, 
        float desaturate)
    {
        Color.RGBToHSV(
            baseColor, 
            out float h, 
            out float s, 
            out float v);

        v = Mathf.Clamp01(v - darken);
        s = Mathf.Clamp01(s - desaturate);

        return Color.HSVToRGB(h, s, v);
    }

    // Blend near/mid/far into one fog colour
    // far gets most weight since exponential
    // fog is mostly visible at distance
    Color BlendFogColor(
        Color near, Color mid, Color far)
    {
        // weights: near 20%, mid 30%, far 50%
        return near * 0.2f + mid * 0.3f + far * 0.5f;
    }

    // ─── Post Process Tint ────────────────────────────

    void CachePostProcess()
    {
        if (fogVolume == null) return;
        fogVolume.profile.TryGet(out _colorAdj);
    }

    void ApplyPostProcessTint(Color fogColor)
    {
        if (_colorAdj == null) return;

        Color tint = Color.Lerp(
            Color.white, fogColor, ppTintStrength);

        _colorAdj.colorFilter.Override(tint);
    }

    // ─── Zone Transitions ─────────────────────────────

    public void TransitionToZone(
        AudioDirector.AudioZone zone)
    {
        Color target = zone switch
        {
            AudioDirector.AudioZone.Meadow
                => meadowColor,
            AudioDirector.AudioZone.Autumn
                => autumnColor,
            AudioDirector.AudioZone.Coniferous
                => coniferousColor,
            AudioDirector.AudioZone.Mountain
                => mountainColor,
            _ => baseFogColor
        };

        TransitionToColor(target, zoneTransitionTime);
    }

    public void TriggerDreadFog(
        float transitionTime = 3f)
    {
        TransitionToColor(dreadColor, transitionTime);
    }

    public void TriggerDawnFog(
        float transitionTime = 20f)
    {
        TransitionToColor(dawnColor, transitionTime);
    }

    void TransitionToColor(
        Color target, float duration)
    {
        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);

        _transitionCoroutine = StartCoroutine(
            LerpToColor(target, duration));
    }

    IEnumerator LerpToColor(
        Color target, float duration)
    {
        Color start = _currentBase;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // smooth step
            float s = t * t * (3f - 2f * t);

            _currentBase = Color.Lerp(start, target, s);
            ApplyFog(_currentBase);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _currentBase = target;
        ApplyFog(_currentBase);
    }

    // ─── Public Manual Control ────────────────────────

    public void SetBaseColor(
        Color color, float transitionTime = 0f)
    {
        if (transitionTime <= 0f)
        {
            _currentBase = color;
            ApplyFog(_currentBase);
        }
        else
        {
            TransitionToColor(color, transitionTime);
        }
    }

    public void SetDensity(float density)
    {
        fogDensity = Mathf.Clamp(density, 0f, 0.1f);
        ApplyFog(_currentBase);
    }

    // ─── Story Events ─────────────────────────────────

    void HandleStoryEvent(StoryDirector.StoryEvent e)
    {
        switch (e)
        {
            case StoryDirector.StoryEvent
                .FirstBodyFound:
            case StoryDirector.StoryEvent
                .SecondBodyFound:
            case StoryDirector.StoryEvent
                .ConfrontationBegin:
                TriggerDreadFog();
                break;

            case StoryDirector.StoryEvent.WalkBack:
                TriggerDawnFog();
                break;
        }
    }
}