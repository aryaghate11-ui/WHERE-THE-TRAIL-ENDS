using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueTrigger : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string text;
        public float displayTime = 3f;
        public AudioClip clip;              // optional per-line audio
    }

    [Header("Dialogue Lines")]
    public DialogueLine[] lines;

    [Header("UI")]
    public TextMeshProUGUI subtitleText;

    [Header("Typing Effect")]
    public float typingSpeed = 0.04f;

    [Header("Audio (Optional)")]
    public AudioSource audioSource;         // optional — assign or leave empty

    [Header("Settings")]
    public bool triggerOnce = true;

    private bool _triggered = false;
    private bool _isPlaying = false;
    private Coroutine _dialogueCoroutine;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && _triggered) return;
        if (_isPlaying) return;             // already running, don't restart

        _triggered = true;
        _dialogueCoroutine = StartCoroutine(PlayDialogue());
    }

    void OnTriggerExit(Collider other)
    {
        // do nothing on exit — let dialogue finish naturally
    }

    IEnumerator PlayDialogue()
    {
        _isPlaying = true;
        subtitleText.gameObject.SetActive(true);

        foreach (var line in lines)
        {
            // play audio clip if assigned
            if (audioSource != null && line.clip != null)
                audioSource.PlayOneShot(line.clip);

            yield return StartCoroutine(TypeLine(line.text));
            yield return new WaitForSeconds(line.displayTime);

            // fade out text cleanly between lines
            subtitleText.text = "";
            yield return new WaitForSeconds(0.2f);  // small gap between lines
        }

        subtitleText.gameObject.SetActive(false);
        _isPlaying = false;
    }

    IEnumerator TypeLine(string fullText)
    {
        subtitleText.text = "";
        foreach (char c in fullText)
        {
            subtitleText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }
}