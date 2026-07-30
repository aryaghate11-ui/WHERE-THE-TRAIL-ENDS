using UnityEngine;

public class InteractiveTriggerVolume : MonoBehaviour
{
    [Header("Trigger Settings")]
    public TriggerType triggerType;
    public string dataID;
    // dataID = cutscene ID, call ID,
    // story event name, zone name etc

    [Header("Target")]
    public Transform targetObject;
    // the object camera will zoom to
    // for cutscene triggers

    [Header("Options")]
    public bool triggerOnce = true;
    public bool requiresPlayerLook = false;
    public float lookAngleThreshold = 30f;
    public bool triggerOnEnter = true;
    // false = requires E key press

    [Header("Prompt")]
    public bool showPrompt = false;
    public string promptText = "Press E to inspect";

    // ─── State ───────────────────────────────────────
    private bool hasTriggered = false;
    private bool playerInside = false;
    private Transform playerTransform;
    private Camera mainCamera;

    void Start()
    {
        playerTransform = GameObject
            .FindGameObjectWithTag("Player")
            .transform;
        mainCamera = Camera.main;

        // make sure collider is trigger
        GetComponent<Collider>().isTrigger = true;
    }

    void Update()
    {
        if (!playerInside) return;
        if (triggerOnce && hasTriggered) return;

        if (!triggerOnEnter)
        {
            // requires E key
            if (Input.GetKeyDown(KeyCode.E))
                Fire();
        }

        if (requiresPlayerLook && targetObject != null)
        {
            // check if player looking at target
            Vector3 dirToTarget =
                (targetObject.position -
                mainCamera.transform.position).normalized;

            float angle = Vector3.Angle(
                mainCamera.transform.forward,
                dirToTarget);

            if (angle < lookAngleThreshold)
                Fire();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = true;

        if (showPrompt)
            UIManager.Instance?.ShowPrompt(promptText);

        // auto fire on enter
        if (triggerOnEnter &&
            !(triggerOnce && hasTriggered))
            Fire();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;

        if (showPrompt)
            UIManager.Instance?.HidePrompt();
    }

    void Fire()
    {
        hasTriggered = true;

        TriggerManager.Instance.FireTrigger(
            triggerType,
            dataID,
            targetObject);

        UIManager.Instance?.HidePrompt();
    }

    // call this to reset trigger
    // eg if player needs to revisit
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}