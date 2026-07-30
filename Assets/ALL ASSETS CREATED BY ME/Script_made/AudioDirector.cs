using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class AudioDirector : MonoBehaviour
{
    public static AudioDirector Instance;

    // ─── Zone System ──────────────────────────────────

    public enum AudioZone
    {
        Meadow,
        Autumn,
        Coniferous,
        Mountain,
        Silence
    }

    [System.Serializable]
    public class ZoneAudioSettings
    {
        public AudioZone zone;
        public AudioMixerSnapshot snapshot;
        public float transitionTime = 8f;

        [Header("Zone Ambience Clips")]
        public AudioClip[] ambienceClips;
        // multiple clips = random selection

        [Header("Bird Settings")]
        public float birdVolume = 1f;
        // 0 = no birds in this zone

        [Header("Wind Settings")]
        public float windVolume = 0.5f;
        public float windPitch = 1f;
    }

    // ─── Inspector ────────────────────────────────────

    [Header("Audio Mixer")]
    public AudioMixer masterMixer;

    [Header("Zones")]
    public List<ZoneAudioSettings> zones;

    [Header("Audio Sources")]
    public AudioSource ambienceSource;
    public AudioSource birdSource;
    public AudioSource windSource;
    public AudioSource musicSource;
    public AudioSource stingerSource;
    // for one-shot narrative stingers

    [Header("Bird Clips")]
    public AudioClip[] meadowBirds;
    public AudioClip[] autumnBirds;
    // coniferous and mountain have no birds

    [Header("Wind Clips")]
    public AudioClip windLight;
    public AudioClip windHeavy;
    public AudioClip windMountain;

    [Header("Music")]
    public AudioClip openingTheme;
    // sparse, warm, plays first 2 min
    public AudioClip endingTheme;
    // bittersweet, ending sequence

    [Header("Stingers")]
    public AudioClip bodyDiscoveryStinger;
    // subtle, not horror
    public AudioClip siddharthCallStinger;

    [Header("Silence Settings")]
    public float silenceFadeTime = 3f;
    // how long birds take to go silent

    [Header("Dawn Return Settings")]
    public float dawnSilenceDuration = 30f;
    // how long silence holds at walk back
    public float birdReturnFadeTime = 20f;
    // how slowly birds return

    // ─── State ───────────────────────────────────────

    private AudioZone currentZone = AudioZone.Meadow;
    private bool silenceActive = false;
    private Coroutine silenceCoroutine;
    private Coroutine dawnCoroutine;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // start in meadow
        TransitionToZone("Meadow");
        PlayOpeningTheme();
    }

    void OnEnable()
    {
        StoryDirector.OnStoryEvent += HandleStoryEvent;
    }

    void OnDisable()
    {
        StoryDirector.OnStoryEvent -= HandleStoryEvent;
    }

    // ─── Zone Transitions ─────────────────────────────

    public void TransitionToZone(string zoneName)
    {
        AudioZone zone = (AudioZone)System.Enum.Parse(
            typeof(AudioZone), zoneName);
        TransitionToZone(zone);
    }

    public void TransitionToZone(AudioZone zone)
    {
        if (zone == currentZone) return;

        currentZone = zone;

        ZoneAudioSettings settings = zones.Find(
            z => z.zone == zone);

        if (settings == null)
        {
            Debug.LogWarning(
                $"[AudioDirector] Zone not found: {zone}");
            return;
        }

        // mixer snapshot transition
        if (settings.snapshot != null)
            settings.snapshot.TransitionTo(
                settings.transitionTime);

        // swap ambience clip
        if (settings.ambienceClips != null &&
            settings.ambienceClips.Length > 0)
        {
            AudioClip clip = settings.ambienceClips[
                Random.Range(0,
                settings.ambienceClips.Length)];
            StartCoroutine(CrossfadeAmbience(clip));
        }

        // adjust bird volume for zone
        if (!silenceActive)
            StartCoroutine(FadeSource(
                birdSource,
                settings.birdVolume,
                settings.transitionTime));

        // adjust wind
        StartCoroutine(FadeSource(
            windSource,
            settings.windVolume,
            settings.transitionTime));

        Debug.Log($"[AudioDirector] Zone: {zone}");
    }

    // ─── Silence System ───────────────────────────────

    public void TriggerSilence()
    {
        if (silenceCoroutine != null)
            StopCoroutine(silenceCoroutine);

        silenceCoroutine = StartCoroutine(
            FadeTotalSilence());
    }

    IEnumerator FadeTotalSilence()
    {
        silenceActive = true;

        // fade birds out slowly
        float elapsed = 0f;
        float startVolume = birdSource.volume;

        while (elapsed < silenceFadeTime)
        {
            birdSource.volume = Mathf.Lerp(
                startVolume, 0f,
                elapsed / silenceFadeTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        birdSource.volume = 0f;

        // fade ambience to near zero
        // not completely — dead silence is wrong
        // a tiny amount of wind remains
        elapsed = 0f;
        startVolume = ambienceSource.volume;
        float targetVolume = 0.05f;

        while (elapsed < silenceFadeTime)
        {
            ambienceSource.volume = Mathf.Lerp(
                startVolume, targetVolume,
                elapsed / silenceFadeTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        ambienceSource.volume = targetVolume;

        Debug.Log("[AudioDirector] Silence active");
    }

    // ─── Dawn Return ──────────────────────────────────
    // called when Vikram walks back after confrontation

    public void TriggerDawnReturn()
    {
        if (dawnCoroutine != null)
            StopCoroutine(dawnCoroutine);

        dawnCoroutine = StartCoroutine(DawnReturn());
    }

    IEnumerator DawnReturn()
    {
        Debug.Log("[AudioDirector] Dawn return starting");

        // silence holds for a while
        yield return new WaitForSeconds(
            dawnSilenceDuration);

        // first bird — single, tentative
        // use a single quiet bird clip here
        if (meadowBirds.Length > 0)
        {
            birdSource.clip = meadowBirds[0];
            birdSource.volume = 0.05f;
            birdSource.Play();
        }

        yield return new WaitForSeconds(10f);

        // more birds gradually return
        float elapsed = 0f;
        while (elapsed < birdReturnFadeTime)
        {
            birdSource.volume = Mathf.Lerp(
                0.05f, 0.6f,
                elapsed / birdReturnFadeTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // ambience returns
        elapsed = 0f;
        while (elapsed < birdReturnFadeTime)
        {
            ambienceSource.volume = Mathf.Lerp(
                0.05f, 0.7f,
                elapsed / birdReturnFadeTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        silenceActive = false;

        // ending theme fades in last
        yield return new WaitForSeconds(15f);
        PlayEndingTheme();

        Debug.Log("[AudioDirector] Dawn return complete");
    }

    // ─── Music ────────────────────────────────────────

    void PlayOpeningTheme()
    {
        if (openingTheme == null) return;

        musicSource.clip = openingTheme;
        musicSource.volume = 0.4f;
        musicSource.loop = false;
        musicSource.Play();

        // fade out after clip ends naturally
        StartCoroutine(FadeOutMusicAfter(
            openingTheme.length - 3f, 3f));
    }

    void PlayEndingTheme()
    {
        if (endingTheme == null) return;

        StartCoroutine(FadeInMusic(
            endingTheme, 0f, 0.5f, 8f));
    }

    IEnumerator FadeInMusic(
        AudioClip clip,
        float startVol,
        float targetVol,
        float fadeTime)
    {
        musicSource.clip = clip;
        musicSource.volume = startVol;
        musicSource.loop = false;
        musicSource.Play();

        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            musicSource.volume = Mathf.Lerp(
                startVol, targetVol,
                elapsed / fadeTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        musicSource.volume = targetVol;
    }

    IEnumerator FadeOutMusicAfter(
        float delay, float fadeTime)
    {
        yield return new WaitForSeconds(delay);

        float startVol = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            musicSource.volume = Mathf.Lerp(
                startVol, 0f,
                elapsed / fadeTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
    }

    // ─── Stingers ─────────────────────────────────────

    public void PlayStinger(AudioClip stinger)
    {
        if (stinger == null) return;
        stingerSource.PlayOneShot(stinger);
    }

    // ─── Crossfade Ambience ───────────────────────────

    IEnumerator CrossfadeAmbience(AudioClip newClip)
    {
        float fadeTime = 3f;
        float startVol = ambienceSource.volume;

        // fade out current
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            ambienceSource.volume = Mathf.Lerp(
                startVol, 0f,
                elapsed / fadeTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // swap clip
        ambienceSource.clip = newClip;
        ambienceSource.loop = true;
        ambienceSource.Play();

        // fade in new
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            ambienceSource.volume = Mathf.Lerp(
                0f, startVol,
                elapsed / fadeTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        ambienceSource.volume = startVol;
    }

    // ─── Generic Fade ─────────────────────────────────

    IEnumerator FadeSource(
        AudioSource source,
        float targetVolume,
        float fadeTime)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            source.volume = Mathf.Lerp(
                startVolume, targetVolume,
                elapsed / fadeTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        source.volume = targetVolume;
    }

    // ─── Story Director Listener ──────────────────────

    void HandleStoryEvent(StoryDirector.StoryEvent e)
    {
        switch (e)
        {
            case StoryDirector.StoryEvent.GameStart:
                TransitionToZone(AudioZone.Meadow);
                break;

            case StoryDirector.StoryEvent
                .MorningRoundComplete:
                TransitionToZone(AudioZone.Autumn);
                break;

            case StoryDirector.StoryEvent.FirstBodyFound:
                TriggerSilence();
                PlayStinger(bodyDiscoveryStinger);
                break;

            case StoryDirector.StoryEvent.SecondBodyFound:
                TriggerSilence();
                break;

            case StoryDirector.StoryEvent.SiddharthCalls:
                TriggerSilence();
                PlayStinger(siddharthCallStinger);
                break;

            case StoryDirector.StoryEvent
                .ConfrontationBegin:
                // full silence for confrontation
                TriggerSilence();
                break;

            case StoryDirector.StoryEvent.WalkBack:
                TriggerDawnReturn();
                break;

            case StoryDirector.StoryEvent.Ending:
                // dawn return handles ending theme
                break;
        }
    }

    // ─── Public for TriggerManager ────────────────────

    // called when player enters zone trigger volume
    public void OnZoneTriggerEnter(string zoneName)
    {
        TransitionToZone(zoneName);
    }
}