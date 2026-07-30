using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BlinkEffectManager : MonoBehaviour
{
    [Header("Blink Overlay")]
    public CanvasGroup blinkCanvas; // UI overlay with black image
    public float fastBlinkInSpeed = 0.2f;
    public float fastBlinkOutSpeed = 0.2f;
    public float slowBlinkInSpeed = 1.2f;
    public float slowBlinkOutSpeed = 1.5f;

    private Coroutine blinkRoutine;

    void Start()
    {
        if (blinkCanvas != null)
        {
            blinkCanvas.alpha = 0;
            blinkCanvas.gameObject.SetActive(true);
        }
    }

    public void BlinkFast()
    {
        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);

        blinkRoutine = StartCoroutine(BlinkRoutine(fastBlinkInSpeed, fastBlinkOutSpeed));
    }

    public void BlinkSlow()
    {
        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);

        blinkRoutine = StartCoroutine(BlinkRoutine(slowBlinkInSpeed, slowBlinkOutSpeed));
    }

    private IEnumerator BlinkRoutine(float inSpeed, float outSpeed)
    {
        if (blinkCanvas == null) yield break;

        blinkCanvas.gameObject.SetActive(true);

        // Fade in
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / inSpeed;
            blinkCanvas.alpha = Mathf.Lerp(0, 1, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        // Fade out
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / outSpeed;
            blinkCanvas.alpha = Mathf.Lerp(1, 0, t);
            yield return null;
        }

        blinkCanvas.alpha = 0;
    }
}
