using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────
//  PostProcessingDirector
//  Blend multiple named Post Processing Volumes by weight.
//  No direct ColorAdjustments access — Unity handles the blend.
// ─────────────────────────────────────────────────────────────

public class PostProcessingDirector : MonoBehaviour
{
    public static PostProcessingDirector Instance;

    // ─── Named Volume Slots ───────────────────────────────────
    // Assign each Volume in the Inspector.
    // Set its Weight to 0 on the Volume component itself.
    // This script drives the weights at runtime.

    [Header("Zone Volumes")]
    public Volume meadowVolume;
    public Volume autumnVolume;
    public Volume coniferousVolume;
    public Volume mountainVolume;

    [Header("Mood / Event Volumes")]
    public Volume dreadVolume;          // body discovery, silence
    public Volume confrontationVolume;  // siddharth face-off
    public Volume dawnVolume;           // walk-back ending

    [Header("Always-On Base Volume")]
    public Volume baseVolume;           // global colour grade, vignette etc.

    [Header("Transition Settings")]
    public float defaultTransitionTime = 6f;
    public float dreadTransitionTime   = 3f;
    public float dawnTransitionTime    = 20f;

    // ─── State ────────────────────────────────────────────────

    private Volume currentZoneVolume;
    private Coroutine dreadCoroutine;
    private Coroutine zoneCoroutine;
    private Coroutine dawnCoroutine;

    // ─── Unity ────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // zero everything out at startup
        SetWeightImmediate(meadowVolume,       0f);
        SetWeightImmediate(autumnVolume,       0f);
        SetWeightImmediate(coniferousVolume,   0f);
        SetWeightImmediate(mountainVolume,     0f);
        SetWeightImmediate(dreadVolume,        0f);
        SetWeightImmediate(confrontationVolume,0f);
        SetWeightImmediate(dawnVolume,         0f);

        // base always on
        SetWeightImmediate(baseVolume, 1f);

        // default zone
        TransitionZoneVolume(meadowVolume, defaultTransitionTime);
    }

    void OnEnable()
    {
        StoryDirector.OnStoryEvent += HandleStoryEvent;
    }

    void OnDisable()
    {
        StoryDirector.OnStoryEvent -= HandleStoryEvent;
    }

    // ─── Public Zone API ──────────────────────────────────────

    /// <summary>Call from AudioDirector.TransitionToZone or TriggerManager.</summary>
    public void TransitionToZone(AudioDirector.AudioZone zone)
    {
        Volume target = zone switch
        {
            AudioDirector.AudioZone.Meadow      => meadowVolume,
            AudioDirector.AudioZone.Autumn      => autumnVolume,
            AudioDirector.AudioZone.Coniferous  => coniferousVolume,
            AudioDirector.AudioZone.Mountain    => mountainVolume,
            _                                   => meadowVolume
        };

        TransitionZoneVolume(target, defaultTransitionTime);
    }

    // ─── Public Mood API ──────────────────────────────────────

    /// <summary>Fades dread volume in. Called on body discovery / silence trigger.</summary>
    public void TriggerDread()
    {
        if (dreadCoroutine != null) StopCoroutine(dreadCoroutine);
        dreadCoroutine = StartCoroutine(
            FadeVolume(dreadVolume, dreadVolume.weight, 1f, dreadTransitionTime));
    }

    public void ClearDread()
    {
        if (dreadCoroutine != null) StopCoroutine(dreadCoroutine);
        dreadCoroutine = StartCoroutine(
            FadeVolume(dreadVolume, dreadVolume.weight, 0f, dreadTransitionTime));
    }

    /// <summary>Confrontation — blend confrontation volume in on top of dread.</summary>
    public void TriggerConfrontation()
    {
        StartCoroutine(FadeVolume(confrontationVolume, 0f, 1f, dreadTransitionTime));
    }

    public void ClearConfrontation()
    {
        StartCoroutine(FadeVolume(confrontationVolume, confrontationVolume.weight, 0f, dreadTransitionTime));
    }

    /// <summary>Dawn return — slowly fades in warm dawn grade while clearing dread.</summary>
    public void TriggerDawn()
    {
        if (dawnCoroutine != null) StopCoroutine(dawnCoroutine);
        dawnCoroutine = StartCoroutine(DawnSequence());
    }

    IEnumerator DawnSequence()
    {
        // simultaneously clear dread, clear confrontation, bring in dawn
        StartCoroutine(FadeVolume(dreadVolume,         dreadVolume.weight,         0f, dawnTransitionTime));
        StartCoroutine(FadeVolume(confrontationVolume, confrontationVolume.weight, 0f, dawnTransitionTime));
        yield return StartCoroutine(FadeVolume(dawnVolume, 0f, 1f, dawnTransitionTime));
    }

    // ─── Manual Weight Control (call from anywhere) ───────────

    /// <summary>Set any volume's weight instantly.</summary>
    public void SetWeight(Volume vol, float weight)
    {
        if (vol != null) vol.weight = Mathf.Clamp01(weight);
    }

    /// <summary>Fade any volume to a target weight over time.</summary>
    public void FadeWeightTo(Volume vol, float target, float duration)
    {
        StartCoroutine(FadeVolume(vol, vol.weight, target, duration));
    }

    // ─── Zone Crossfade ───────────────────────────────────────

    void TransitionZoneVolume(Volume incoming, float duration)
    {
        if (zoneCoroutine != null) StopCoroutine(zoneCoroutine);
        zoneCoroutine = StartCoroutine(
            CrossfadeZone(currentZoneVolume, incoming, duration));
        currentZoneVolume = incoming;
    }

    IEnumerator CrossfadeZone(Volume outgoing, Volume incoming, float duration)
    {
        float elapsed = 0f;
        float outStart = outgoing != null ? outgoing.weight : 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            if (outgoing != null) outgoing.weight = Mathf.Lerp(outStart, 0f, t);
            if (incoming != null) incoming.weight = Mathf.Lerp(0f, 1f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (outgoing != null) outgoing.weight = 0f;
        if (incoming != null) incoming.weight = 1f;
    }

    // ─── Core Fade Coroutine ──────────────────────────────────

    IEnumerator FadeVolume(Volume vol, float from, float to, float duration)
    {
        if (vol == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            vol.weight = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        vol.weight = to;
    }

    // ─── Helpers ──────────────────────────────────────────────

    void SetWeightImmediate(Volume vol, float weight)
    {
        if (vol != null) vol.weight = weight;
    }

    // ─── Story Event Listener ─────────────────────────────────

    void HandleStoryEvent(StoryDirector.StoryEvent e)
    {
        switch (e)
        {
            case StoryDirector.StoryEvent.GameStart:
                TransitionToZone(AudioDirector.AudioZone.Meadow);
                break;

            case StoryDirector.StoryEvent.MorningRoundComplete:
                TransitionToZone(AudioDirector.AudioZone.Autumn);
                break;

            case StoryDirector.StoryEvent.FirstBodyFound:
            case StoryDirector.StoryEvent.SecondBodyFound:
            case StoryDirector.StoryEvent.SiddharthCalls:
                TriggerDread();
                break;

            case StoryDirector.StoryEvent.ConfrontationBegin:
                TriggerConfrontation();
                break;

            case StoryDirector.StoryEvent.WalkBack:
                TriggerDawn();
                break;
        }
    }
}