using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FPSControllerLPFP;

public class CutsceneSystem : MonoBehaviour
{
    public static CutsceneSystem Instance;

    [System.Serializable]
    public class CutsceneData
    {
        public string cutsceneID;

        [Header("Camera")]
        public float zoomDuration = 1.5f;
        public float zoomFOV = 25f;
        public float holdDuration = 2f;
        // how long camera stays zoomed

        public Vector3 cameraOffset = Vector3.zero;
        // offset from target object

        [Header("Dialogue After Zoom")]
        public string conversationID;
        // fires ConversationSystem after zoom
        // leave empty for no dialogue

        [Header("Story Event")]
        public bool firesStoryEvent = false;
        public StoryDirector.StoryEvent storyEvent;

        [Header("Player")]
        public bool lockPlayerDuringCutscene = true;
        public bool lockCameraDuringCutscene = true;
    }

    [Header("All Cutscenes")]
    public List<CutsceneData> allCutscenes;

    [Header("References")]
    public Camera mainCamera;
    public FpsControllerLPFP playerController;
    public ConversationSystem conversationSystem;

    private float defaultFOV;
    private bool cutsceneActive = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (mainCamera != null)
            defaultFOV = mainCamera.fieldOfView;
    }

    public void PlayCutscene(
        string cutsceneID,
        Transform target)
    {
        if (cutsceneActive) return;

        CutsceneData data = allCutscenes.Find(
            c => c.cutsceneID == cutsceneID);

        if (data == null)
        {
            Debug.LogWarning(
                $"[Cutscene] Not found: {cutsceneID}");
            return;
        }

        StartCoroutine(RunCutscene(data, target));
    }

    IEnumerator RunCutscene(
        CutsceneData data,
        Transform target)
    {
        cutsceneActive = true;

        // lock player
        if (data.lockPlayerDuringCutscene)
            playerController?.SetMovement(false);
        if (data.lockCameraDuringCutscene)
            playerController?.SetCameraLock(true);

        // store original camera state
        Vector3 originalPos =
            mainCamera.transform.localPosition;
        Quaternion originalRot =
            mainCamera.transform.localRotation;
        float originalFOV = mainCamera.fieldOfView;

        // calculate target position
        // camera moves toward object
        Vector3 targetPos = target.position
            + data.cameraOffset;
        Quaternion targetRot = Quaternion.LookRotation(
            target.position -
            mainCamera.transform.position);

        // zoom in
        float elapsed = 0f;
        while (elapsed < data.zoomDuration)
        {
            float t = elapsed / data.zoomDuration;
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position,
                targetPos,
                smooth);

            mainCamera.transform.rotation = Quaternion.Slerp(
                mainCamera.transform.rotation,
                targetRot,
                smooth);

            mainCamera.fieldOfView = Mathf.Lerp(
                originalFOV,
                data.zoomFOV,
                smooth);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // hold on object
        yield return new WaitForSeconds(data.holdDuration);

        // fire story event
        if (data.firesStoryEvent)
            StoryDirector.Instance.TriggerEvent(
                data.storyEvent);

        // trigger dialogue if assigned
        if (!string.IsNullOrEmpty(data.conversationID))
        {
            // unlock camera for dialogue
            playerController?.SetCameraLock(false);

            conversationSystem?.TriggerConversation(
                data.conversationID);

            // wait for conversation to finish
            // simple delay — replace with callback
            // if you need exact timing
            yield return new WaitForSeconds(3f);
        }

        // zoom back out
        elapsed = 0f;
        while (elapsed < data.zoomDuration)
        {
            float t = elapsed / data.zoomDuration;
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            mainCamera.transform.localPosition = Vector3.Lerp(
                mainCamera.transform.localPosition,
                originalPos,
                smooth);

            mainCamera.transform.localRotation = Quaternion.Slerp(
                mainCamera.transform.localRotation,
                originalRot,
                smooth);

            mainCamera.fieldOfView = Mathf.Lerp(
                mainCamera.fieldOfView,
                originalFOV,
                smooth);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // restore player
        if (data.lockPlayerDuringCutscene)
            playerController?.SetMovement(true);
        if (data.lockCameraDuringCutscene)
            playerController?.SetCameraLock(false);

        cutsceneActive = false;
        Debug.Log($"[Cutscene] Complete: {data.cutsceneID}");
    }
}