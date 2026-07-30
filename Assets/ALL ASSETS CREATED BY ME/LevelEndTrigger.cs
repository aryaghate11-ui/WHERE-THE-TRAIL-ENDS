using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelEndTrigger : MonoBehaviour
{
    [Header("Music")]
    public AudioSource currentMusic;
    public AudioSource endMusic;
    public float musicFadeDuration = 2f;

    [Header("Fog")]
    public Color endFogColor = new Color(0.05f, 0.05f, 0.1f, 1f);
    public float endFogDensity = 0.06f;
    public float fogFadeDuration = 3f;

    [Header("Fade To Black")]
    public Image fadeImage;             // drag a full-screen black UI Image here
    public float fadeDuration = 2f;

    [Header("Final Dialogue")]
    public DialogueTrigger finalDialogue;

    [Header("Next Level")]
    public string nextSceneName;
    public float delayAfterMusic = 1f;

    [Header("Settings")]
    public bool triggerOnce = true;
    private bool _triggered = false;

    void Start()
    {
        // make sure fade image starts fully transparent
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(true);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && _triggered) return;
        _triggered = true;

        // freeze rigidbody
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        StartCoroutine(EndSequence(other.gameObject));
    }

    IEnumerator EndSequence(GameObject player)
    {
        // fire final dialogue
        if (finalDialogue != null)
            finalDialogue.SendMessage("OnTriggerEnter",
                player.GetComponent<Collider>());

        // fog + music run together
        StartCoroutine(FadeFog());
        yield return StartCoroutine(FadeMusic());

        yield return new WaitForSeconds(delayAfterMusic);

        // fade to black THEN load
        yield return StartCoroutine(FadeToBlack());

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator FadeToBlack()
    {
        if (fadeImage == null) yield break;

        float t = 0f;
        Color c = fadeImage.color;

        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            c.a = Mathf.Clamp01(t);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;
    }

    IEnumerator FadeMusic()
    {
        if (currentMusic != null)
        {
            float startVol = currentMusic.volume;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / musicFadeDuration;
                currentMusic.volume = Mathf.Lerp(startVol, 0f, t);
                yield return null;
            }
            currentMusic.Stop();
        }

        if (endMusic != null)
        {
            endMusic.volume = 0f;
            endMusic.Play();

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / musicFadeDuration;
                endMusic.volume = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            float remaining = endMusic.clip.length - musicFadeDuration;
            if (remaining > 0f)
                yield return new WaitForSeconds(remaining);
        }
    }

    IEnumerator FadeFog()
    {
        RenderSettings.fog = true;
        Color startColor   = RenderSettings.fogColor;
        float startDensity = RenderSettings.fogDensity;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fogFadeDuration;
            RenderSettings.fogColor   = Color.Lerp(startColor,   endFogColor,   t);
            RenderSettings.fogDensity = Mathf.Lerp(startDensity, endFogDensity, t);
            yield return null;
        }
    }
}