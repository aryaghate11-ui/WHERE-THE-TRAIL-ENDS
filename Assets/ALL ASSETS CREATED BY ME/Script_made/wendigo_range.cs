using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using System.Collections;

public class PostFXBlendTrigger : MonoBehaviour
{
    public PostProcessVolume normalVolume;
    public PostProcessVolume wendigoVolume;
    public GameObject bgm;
    public AudioSource BGMWight;
    public AudioSource heartbeat;
    public float blendDuration = 2f;

    [Header("BGM Volumes")]
    public float bgmVolumeNormal = 0.437f;    // BGMWight volume outside danger
    public float bgmVolumeDanger = 0f;      // BGMWight volume inside danger

    // ── static counter shared across ALL trigger instances ──
    private static int _dangerCount = 0;
    private static Coroutine _activeBlend;
    private static PostFXBlendTrigger _master;  // one instance runs the coroutine

    private bool _playerInside = false;

    void Start()
    {
        // first instance sets up the volumes
        if (_dangerCount == 0)
        {
            normalVolume.weight  = 1f;
            wendigoVolume.weight = 0f;
            wendigoVolume.enabled = false;
            if (BGMWight != null) BGMWight.volume = bgmVolumeNormal;
        }
        _master = this; // any instance can be master, doesn't matter
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || _playerInside) return;
        _playerInside = true;
        _dangerCount++;

        // only act on first entry
        if (_dangerCount == 1)
        {
            wendigoVolume.enabled = true;
            RunBlend(true);
            if (heartbeat != null) heartbeat.Play();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || !_playerInside) return;
        _playerInside = false;
        _dangerCount = Mathf.Max(0, _dangerCount - 1);

        // only blend out when fully clear of all triggers
        if (_dangerCount == 0)
            ForceExit();
    }

    // called by GhostAI.Die()
    public void ForceExit()
    {
        _dangerCount = Mathf.Max(0, _dangerCount - 1);

        if (_dangerCount == 0)
        {
            RunBlend(false);
            if (bgm != null) bgm.SetActive(true);
            if (heartbeat != null) heartbeat.Stop();
        }
    }

    void RunBlend(bool entering)
    {
        if (_activeBlend != null)
            _master.StopCoroutine(_activeBlend);
        _master = this;
        _activeBlend = StartCoroutine(BlendVolumes(entering));
    }

    IEnumerator BlendVolumes(bool entering)
    {
        // snapshot current values so interrupted blends don't jump
        float startNormal  = normalVolume.weight;
        float startWendigo = wendigoVolume.weight;
        float startBGM     = BGMWight != null ? BGMWight.volume : 0f;

        float targetNormal  = entering ? 0f : 1f;
        float targetWendigo = entering ? 1f : 0f;
        float targetBGM     = entering ? bgmVolumeDanger : bgmVolumeNormal;

        float time = 0f;
        while (time < blendDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / blendDuration);

            normalVolume.weight  = Mathf.Lerp(startNormal,  targetNormal,  t);
            wendigoVolume.weight = Mathf.Lerp(startWendigo, targetWendigo, t);
            if (BGMWight != null)
                BGMWight.volume  = Mathf.Lerp(startBGM, targetBGM, t);

            yield return null;
        }

        // snap to exact final values
        normalVolume.weight  = targetNormal;
        wendigoVolume.weight = targetWendigo;
        if (BGMWight != null) BGMWight.volume = targetBGM;

        if (!entering)
            wendigoVolume.enabled = false;

        _activeBlend = null;
    }

    // reset static state when scene unloads
    void OnDestroy()
    {
        _dangerCount = 0;
        _activeBlend = null;
    }
}