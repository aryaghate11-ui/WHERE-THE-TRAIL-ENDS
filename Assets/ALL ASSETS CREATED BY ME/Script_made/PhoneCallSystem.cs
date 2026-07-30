using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using FPSControllerLPFP;

public class PhoneCallSystem : MonoBehaviour
{
    public static PhoneCallSystem Instance;

    [System.Serializable]
    public class CallLine
    {
        public string speaker;
        [TextArea(2, 4)]
        public string subtitle;
        public AudioClip voiceClip;
        public float pauseAfter = 0.8f;
        public float pauseBefore = 0f;
    }

    [System.Serializable]
    public class PhoneCallData
    {
        public string callID;
        public string callerName;
        public CallerType callerType;
        public Sprite callerIcon;

        [Header("Behaviour")]
        public int ringsBeforeHangup = 4;
        public bool canDelayAnswer = true;
        public bool playerCanWalkDuringCall = true;
        public bool forcePhoneUp = true;

        [Header("Lines")]
        public List<CallLine> lines;

        [Header("On Complete")]
        public bool firesStoryEvent = false;
        public StoryDirector.StoryEvent storyEvent;
    }

    public enum CallerType
    {
        Priya,
        Aryan,
        Unknown  // Siddharth
    }

    [Header("All Calls")]
    public List<PhoneCallData> allCalls;

    [Header("UI")]
    public GameObject incomingCallUI;
    public TMP_Text callerNameText;
    public TMP_Text subtitleText;
    public GameObject subtitlePanel;

    [Header("Audio")]
    public AudioSource ringtoneSource;
    public AudioClip ringtoneClip;

    [Header("References")]
    public PhoneController phoneController;
    public FpsControllerLPFP playerController;

    private bool callActive = false;
    private bool playerAnswered = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void TriggerCall(string callID)
    {
        PhoneCallData data = allCalls.Find(
            c => c.callID == callID);

        if (data == null)
        {
            Debug.LogWarning(
                $"[PhoneCall] Not found: {callID}");
            return;
        }

        if (callActive)
        {
            Debug.Log(
                "[PhoneCall] Call already active, queuing");
            StartCoroutine(QueueCall(data));
            return;
        }

        StartCoroutine(RunCall(data));
    }

    IEnumerator QueueCall(PhoneCallData data)
    {
        while (callActive)
            yield return new WaitForSeconds(1f);

        StartCoroutine(RunCall(data));
    }

    IEnumerator RunCall(PhoneCallData data)
    {
        callActive = true;
        playerAnswered = false;

        // force phone up
        if (data.forcePhoneUp)
            phoneController?.ForcePhoneUpForCall();

        // show incoming call UI
        incomingCallUI.SetActive(true);
        callerNameText.text = data.callerName;

        // ring
        ringtoneSource.clip = ringtoneClip;
        ringtoneSource.loop = true;
        ringtoneSource.Play();

        // wait for answer or timeout
        float ringTimer = 0f;
        float maxRingTime = data.ringsBeforeHangup * 3f;

        if (data.canDelayAnswer)
        {
            while (!playerAnswered)
            {
                ringTimer += Time.deltaTime;

                // Siddharth hangs up after max rings
                if (ringTimer >= maxRingTime &&
                    data.callerType == CallerType.Unknown)
                {
                    // hang up — callback in 60 seconds
                    ringtoneSource.Stop();
                    incomingCallUI.SetActive(false);
                    callActive = false;
                    StartCoroutine(SiddharthCallback(
                        data.callID));
                    yield break;
                }

                yield return null;
            }
        }
        else
        {
            // auto answer
            playerAnswered = true;
        }

        // call connected
        ringtoneSource.Stop();
        incomingCallUI.SetActive(false);
        subtitlePanel.SetActive(true);

        if (!data.playerCanWalkDuringCall)
            playerController?.SetMovement(false);

        // run lines
        foreach (CallLine line in data.lines)
        {
            if (line.pauseBefore > 0)
                yield return new WaitForSeconds(
                    line.pauseBefore);

            subtitleText.text =
                $"<b>{line.speaker}:</b> {line.subtitle}";

            float duration = 2f;
            if (line.voiceClip != null)
            {
                AudioSource.PlayClipAtPoint(
                    line.voiceClip,
                    Camera.main.transform.position);
                duration = line.voiceClip.length;
            }

            yield return new WaitForSeconds(duration);
            yield return new WaitForSeconds(line.pauseAfter);
        }

        // end call
        subtitlePanel.SetActive(false);

        if (!data.playerCanWalkDuringCall)
            playerController?.SetMovement(true);

        callActive = false;

        // fire story event
        if (data.firesStoryEvent)
            StoryDirector.Instance.TriggerEvent(
                data.storyEvent);

        Debug.Log($"[PhoneCall] Complete: {data.callID}");
    }

    IEnumerator SiddharthCallback(string callID)
    {
        Debug.Log("[PhoneCall] Siddharth callback in 60s");
        yield return new WaitForSeconds(60f);
        TriggerCall(callID);
    }

    // called by player pressing E
    public void AnswerCall()
    {
        if (!callActive) return;
        playerAnswered = true;
    }
}