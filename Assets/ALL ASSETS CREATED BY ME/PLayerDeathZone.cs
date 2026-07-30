using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerDeathZone : MonoBehaviour
{
    [Header("Fade")]
    public Image fadeImage;
    public float fadeDuration = 1.5f;

    [Header("Death Music")]
    public AudioSource deathMusic;
    public float deathMusicDuration = 3f;

    [Header("Ghost Tag")]
    public string ghostTag = "Ghost";

    private bool _dead = false;

    void Start()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
        else
            Debug.LogWarning("[PlayerDeathZone] fadeImage is NOT assigned!");

        if (deathMusic == null)
            Debug.LogWarning("[PlayerDeathZone] deathMusic is NOT assigned!");
    }

    void OnTriggerEnter(Collider other)
    {
        if (_dead) return;
        if (!other.CompareTag(ghostTag)) return;
        if (other.isTrigger) return;

        Debug.Log("[PlayerDeathZone] Ghost touched player — dying");
        _dead = true;
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // lock rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // play death sound
        if (deathMusic != null)
        {
            deathMusic.Play();
            Debug.Log("[PlayerDeathZone] Death music playing");
        }

        // fade to black — runs regardless of audio
        if (fadeImage != null)
        {
            Debug.Log("[PlayerDeathZone] Fading to black");
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
        else
        {
            // no image — just wait the fade duration
            yield return new WaitForSeconds(fadeDuration);
        }

        // wait for death music to finish
        yield return new WaitForSeconds(deathMusicDuration);

        Debug.Log("[PlayerDeathZone] Restarting scene");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}