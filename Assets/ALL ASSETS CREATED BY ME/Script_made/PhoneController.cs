using UnityEngine;

public class PhoneController : MonoBehaviour
{
    [Header("Phone GameObject")]
    public GameObject phoneGameObject;

    [Header("Flashlight")]
    public Light phoneFlashlight;

    [Header("Gun Reference")]
    public HandgunScriptLPFP1 gunScript;

    [Header("Raycast")]
    public float photoDistance = 10f;
    public Camera mainCamera;

    public bool isPhoneActive = false;
    private bool isFlashlightOn = false;

    void Start()
    {
        phoneGameObject.SetActive(false);

        if (phoneFlashlight != null)
            phoneFlashlight.enabled = false;
    }

    void Update()
    {
        if (!isPhoneActive) return;

        HandleFlashlight();
        HandlePhoto();
    }

    // ─── Toggle ───────────────────────────────────────
    public void TogglePhone()
    {
        if (isPhoneActive)
            HidePhone();
        else
            ShowPhone();
    }

    public void ShowPhone()
    {
        isPhoneActive = true;
        phoneGameObject.SetActive(true);
        Debug.Log("[Phone] Phone raised");
    }

    public void HidePhone()
    {
        isPhoneActive = false;
        phoneGameObject.SetActive(false);

        if (phoneFlashlight != null)
            phoneFlashlight.enabled = false;

        isFlashlightOn = false;
        Debug.Log("[Phone] Phone lowered");
    }

    // called by gun script when G pressed
    public void ForceHidePhone()
    {
        HidePhone();
    }

    // called by ConversationSystem
    // forces phone up for Siddharth call
    public void ForcePhoneUpForCall()
    {
        if (gunScript != null && gunScript.isGunDrawn)
            gunScript.ForceHolster();
        else
            ShowPhone();
    }

    // ─── Flashlight ───────────────────────────────────
    void HandleFlashlight()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isFlashlightOn = !isFlashlightOn;

            if (phoneFlashlight != null)
                phoneFlashlight.enabled = isFlashlightOn;

            Debug.Log($"[Phone] Flashlight: {isFlashlightOn}");
        }
    }

    // ─── Photo ────────────────────────────────────────
    void HandlePhoto()
    {
        if (Input.GetMouseButtonDown(1))
            TakePhoto();
    }

    void TakePhoto()
    {
        if (mainCamera == null)
        {
            Debug.LogWarning(
                "[Phone] No camera assigned for raycast");
            return;
        }

        Ray ray = new Ray(
            mainCamera.transform.position,
            mainCamera.transform.forward);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, photoDistance))
        {
            EvidenceObject evidence = hit.collider
                .GetComponent<EvidenceObject>();

            if (evidence != null)
            {
                if (!evidence.photographed)
                {
                    evidence.OnPhotographed();
                    Debug.Log(
                        $"[Phone] Photo taken: " +
                        $"{evidence.evidenceName}");
                }
                else
                {
                    Debug.Log(
                        $"[Phone] Already photographed: " +
                        $"{evidence.evidenceName}");
                }
            }
        }
    }
}