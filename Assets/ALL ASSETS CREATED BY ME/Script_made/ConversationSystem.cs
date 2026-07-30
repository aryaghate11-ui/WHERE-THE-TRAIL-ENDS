using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using FPSControllerLPFP;

public class ConversationSystem : MonoBehaviour
{
    public static ConversationSystem Instance;

    // ─── Data Structures ─────────────────────────────

    public enum ConversationMode
    {
        Internal,       // Vikram thinks to himself
        // no speaker name, no chrome
        FaceToFace,     // Riya testimony
        // proximity based
        Confrontation   // Siddharth face-off
        // aim drift, movement locked
    }

    [System.Serializable]
    public class ConversationLine
    {
        [Header("Content")]
        public string speakerName;
        [TextArea(2, 5)]
        public string subtitle;
        public AudioClip voiceClip;

        [Header("Timing")]
        public float pauseBefore = 0f;
        public float pauseAfter = 0.8f;
        public float manualDuration = 0f;
        // use if no voice clip assigned
        // 0 = use default 2f

        [Header("Player")]
        public bool lockMovement = false;
        public bool lockCamera = false;

        [Header("Triggers On This Line")]
        public bool triggerSilence = false;
        public bool triggerDread = false;
        public bool triggerAimDrift = false;
        public bool stopAimDrift = false;
        public bool firesStoryEvent = false;
        public StoryDirector.StoryEvent storyEvent;
    }

    [System.Serializable]
    public class ConversationData
    {
        public string conversationID;
        public ConversationMode mode;
        public List<ConversationLine> lines;

        [Header("FaceToFace Settings")]
        public Transform speakerPosition;
        public float triggerDistance = 3f;

        [Header("On Complete")]
        public bool firesStoryEvent = false;
        public StoryDirector.StoryEvent storyEvent;
    }

    // ─── Inspector ────────────────────────────────────

    [Header("All Conversations")]
    public List<ConversationData> allConversations;

    [Header("UI")]
    public GameObject subtitlePanel;
    public TMP_Text speakerNameText;
    public TMP_Text subtitleText;
    public CanvasGroup subtitleCanvasGroup;

    [Header("Confrontation UI")]
    // confrontation has no speaker name UI
    // just centred subtitle, different style
    public GameObject confrontationSubtitlePanel;
    public TMP_Text confrontationSubtitleText;

    [Header("References")]
    public FpsControllerLPFP playerController;
    public AudioDirector audioDirector;
    public PostProcessingDirector ppDirector;

    [Header("Subtitle Fade")]
    public float subtitleFadeTime = 0.3f;

    // ─── State ───────────────────────────────────────

    private bool conversationActive = false;
    public bool IsActive => conversationActive;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        subtitlePanel.SetActive(false);
        confrontationSubtitlePanel.SetActive(false);
    }

    void OnEnable()
    {
        StoryDirector.OnStoryEvent += HandleStoryEvent;
    }

    void OnDisable()
    {
        StoryDirector.OnStoryEvent -= HandleStoryEvent;
    }

    // ─── Public Entry Point ───────────────────────────

    public void TriggerConversation(string conversationID)
    {
        ConversationData data = allConversations.Find(
            c => c.conversationID == conversationID);

        if (data == null)
        {
            Debug.LogWarning(
                $"[Conversation] Not found: " +
                $"{conversationID}");
            return;
        }

        if (conversationActive)
        {
            Debug.Log(
                $"[Conversation] Already active, " +
                $"queuing: {conversationID}");
            StartCoroutine(QueueConversation(data));
            return;
        }

        StartCoroutine(RunConversation(data));
    }

    IEnumerator QueueConversation(ConversationData data)
    {
        while (conversationActive)
            yield return new WaitForSeconds(0.5f);

        StartCoroutine(RunConversation(data));
    }

    // ─── Main Runner ──────────────────────────────────

    IEnumerator RunConversation(ConversationData data)
    {
        conversationActive = true;

        Debug.Log(
            $"[Conversation] Starting: " +
            $"{data.conversationID} " +
            $"Mode: {data.mode}");

        // mode specific setup
        switch (data.mode)
        {
            case ConversationMode.FaceToFace:
                yield return StartCoroutine(
                    WaitForProximity(data));
                break;

            case ConversationMode.Confrontation:
                // aim drift starts at confrontation begin
                playerController?.SetMovement(false);
                playerController?.StartAimDrift();
                break;

            case ConversationMode.Internal:
                // full player control, no chrome
                break;
        }

        // open correct UI
        OpenSubtitleUI(data.mode);

        // run all lines
        foreach (ConversationLine line in data.lines)
            yield return StartCoroutine(RunLine(
                line, data.mode));

        // close UI
        yield return StartCoroutine(
            FadeSubtitleUI(false));

        CloseSubtitleUI(data.mode);

        // mode specific teardown
        switch (data.mode)
        {
            case ConversationMode.Confrontation:
                playerController?.SetMovement(true);
                playerController?.StopAimDrift();
                break;
        }

        // fire completion event
        if (data.firesStoryEvent)
            StoryDirector.Instance.TriggerEvent(
                data.storyEvent);

        conversationActive = false;

        Debug.Log(
            $"[Conversation] Complete: " +
            $"{data.conversationID}");
    }

    // ─── Line Runner ──────────────────────────────────

    IEnumerator RunLine(
        ConversationLine line,
        ConversationMode mode)
    {
        // pre line pause
        if (line.pauseBefore > 0)
            yield return new WaitForSeconds(
                line.pauseBefore);

        // player locks
        if (line.lockMovement)
            playerController?.SetMovement(false);
        if (line.lockCamera)
            playerController?.SetCameraLock(true);

        // audio triggers
        if (line.triggerSilence)
            audioDirector?.TriggerSilence();
        if (line.triggerDread)
            ppDirector?.TriggerDread();
        if (line.triggerAimDrift)
            playerController?.StartAimDrift();
        if (line.stopAimDrift)
            playerController?.StopAimDrift();

        // show subtitle
        SetSubtitleText(line, mode);

        // fade in
        yield return StartCoroutine(
            FadeSubtitleUI(true));

        // play voice
        float duration = line.manualDuration > 0
            ? line.manualDuration
            : 2f;

        if (line.voiceClip != null)
        {
            AudioSource.PlayClipAtPoint(
                line.voiceClip,
                Camera.main.transform.position);
            duration = line.voiceClip.length;
        }

        yield return new WaitForSeconds(duration);

        // fade out
        yield return StartCoroutine(
            FadeSubtitleUI(false));

        // post line pause
        yield return new WaitForSeconds(line.pauseAfter);

        // unlock player
        if (line.lockMovement)
            playerController?.SetMovement(true);
        if (line.lockCamera)
            playerController?.SetCameraLock(false);

        // mid line story event
        if (line.firesStoryEvent)
            StoryDirector.Instance.TriggerEvent(
                line.storyEvent);
    }

    // ─── FaceToFace Proximity Wait ────────────────────

    IEnumerator WaitForProximity(ConversationData data)
    {
        if (data.speakerPosition == null)
            yield break;

        Transform player =
            playerController?.transform;

        if (player == null) yield break;

        while (Vector3.Distance(
            player.position,
            data.speakerPosition.position)
            > data.triggerDistance)
        {
            yield return null;
        }
    }

    // ─── UI Helpers ───────────────────────────────────

    void OpenSubtitleUI(ConversationMode mode)
    {
        switch (mode)
        {
            case ConversationMode.Internal:
            case ConversationMode.FaceToFace:
                subtitlePanel.SetActive(true);
                if (subtitleCanvasGroup != null)
                    subtitleCanvasGroup.alpha = 0f;
                break;

            case ConversationMode.Confrontation:
                confrontationSubtitlePanel.SetActive(true);
                break;
        }
    }

    void CloseSubtitleUI(ConversationMode mode)
    {
        switch (mode)
        {
            case ConversationMode.Internal:
            case ConversationMode.FaceToFace:
                subtitlePanel.SetActive(false);
                break;

            case ConversationMode.Confrontation:
                confrontationSubtitlePanel
                    .SetActive(false);
                break;
        }
    }

    void SetSubtitleText(
        ConversationLine line,
        ConversationMode mode)
    {
        switch (mode)
        {
            case ConversationMode.Internal:
                // no speaker name
                speakerNameText.text = "";
                subtitleText.text = line.subtitle;
                break;

            case ConversationMode.FaceToFace:
                speakerNameText.text = line.speakerName;
                subtitleText.text = line.subtitle;
                break;

            case ConversationMode.Confrontation:
                // confrontation panel
                // speaker name inline with subtitle
                confrontationSubtitleText.text =
                    string.IsNullOrEmpty(line.speakerName)
                    ? line.subtitle
                    : $"<b>{line.speakerName}</b>\n" +
                      line.subtitle;
                break;
        }
    }

    IEnumerator FadeSubtitleUI(bool fadeIn)
    {
        if (subtitleCanvasGroup == null)
            yield break;

        float start = fadeIn ? 0f : 1f;
        float end = fadeIn ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < subtitleFadeTime)
        {
            subtitleCanvasGroup.alpha = Mathf.Lerp(
                start, end,
                elapsed / subtitleFadeTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        subtitleCanvasGroup.alpha = end;
    }

    // ─── Story Director Listener ──────────────────────

    void HandleStoryEvent(StoryDirector.StoryEvent e)
    {
        switch (e)
        {
            case StoryDirector.StoryEvent.GameStart:
                TriggerConversation(
                    "vikram_morning_internal");
                break;

            case StoryDirector.StoryEvent.TrekkersMissing:
                TriggerConversation(
                    "vikram_trekkers_missing");
                break;

            case StoryDirector.StoryEvent.FirstBodyFound:
                TriggerConversation(
                    "vikram_first_body_reaction");
                break;

            case StoryDirector.StoryEvent.RiyaFound:
                TriggerConversation(
                    "riya_testimony");
                break;

            case StoryDirector.StoryEvent.ConfrontationBegin:
                TriggerConversation(
                    "siddharth_confrontation");
                break;

            case StoryDirector.StoryEvent.WalkBack:
                TriggerConversation(
                    "vikram_walkback_internal");
                break;
        }
    }
}