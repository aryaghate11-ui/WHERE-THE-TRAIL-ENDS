using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class Credits : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup thankYouGroup;
    public CanvasGroup teamNameGroup;
    public CanvasGroup teamMembersGroup;

    [Header("Timing")]
    public float fadeDuration = 1.5f;
    public float holdDuration = 2.5f;
    public float delayBetween = 0.5f;

    [Header("Next Scene (leave blank if none)")]
    public string nextSceneName = "";

    void Start()
    {
        // Make everything invisible at start
        thankYouGroup.alpha = 0;
        teamNameGroup.alpha = 0;
        teamMembersGroup.alpha = 0;

        StartCoroutine(PlayCredits());
    }

    IEnumerator PlayCredits()
    {
        // Fade in "Thank you for playing"
        yield return StartCoroutine(FadeIn(thankYouGroup));
        yield return new WaitForSeconds(holdDuration);
        yield return StartCoroutine(FadeOut(thankYouGroup));
        yield return new WaitForSeconds(delayBetween);

        // Fade in "Team NewGen"
        yield return StartCoroutine(FadeIn(teamNameGroup));
        yield return new WaitForSeconds(holdDuration);
        yield return StartCoroutine(FadeOut(teamNameGroup));
        yield return new WaitForSeconds(delayBetween);

        // Fade in team members
        yield return StartCoroutine(FadeIn(teamMembersGroup));
        yield return new WaitForSeconds(holdDuration + 1f);
        yield return StartCoroutine(FadeOut(teamMembersGroup));

        // Load next scene or quit
        yield return new WaitForSeconds(1f);
        if (nextSceneName != "")
            SceneManager.LoadScene(nextSceneName);
        else
            Application.Quit();
    }

    IEnumerator FadeIn(CanvasGroup group)
    {
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        group.alpha = 1;
    }

    IEnumerator FadeOut(CanvasGroup group)
    {
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            group.alpha = 1 - Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        group.alpha = 0;
    }
}