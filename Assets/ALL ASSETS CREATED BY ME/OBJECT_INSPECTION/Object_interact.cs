using TMPro;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;   // for UI Text

public class Object_interact : MonoBehaviour
{
    [Header("Camera References")]
    public Camera mainCamera;
    public Camera inspectCamera;
    public Transform inspectSpot;
    public PostProcessVolume inspectPostProcessVolume;

    [Header("Interaction Settings")]
    public float rayDistance = 3f;
    public LayerMask interactableLayer;
    public float rotationSpeed = 100f;
    public float transitionTime = 0.4f;
    public TMP_Text descriptionText;

    [Header("Player Control")]
    public MonoBehaviour playerMovementScript;
    public GameObject GunMesh;
    public GameObject GunCanvas;

    [Header("UI Prompt")]
    public GameObject inspectPrompt;    // drag your Text (or TMP text) here
    
    public string promptMessage = "Press E to Inspect";

    private Transform currentObject;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private int originalLayer;
    private bool isInspecting = false;
    private Vector3 previousMousePosition;

    void Update()
{

        if (!isInspecting)
        {
            inspectCamera.enabled = false;
            HandlePromptRaycast();
        }

        else
        {
        // Disable the prompt while inspecting
        if (inspectPrompt != null && inspectPrompt.activeSelf)
            inspectPrompt.SetActive(false);
        }
    if (Input.GetKeyDown(KeyCode.E))
    {
            if (isInspecting)
                StartCoroutine(ExitInspection());
                
            else
                TryStartInspection();
    }

    // ✅ Initialize rotation start
    if (isInspecting && Input.GetMouseButtonDown(0))
        previousMousePosition = Input.mousePosition;

    // ✅ Apply rotation while holding
    if (isInspecting && Input.GetMouseButton(0))
        RotateObject();
}

void RotateObject()
{
    Vector3 deltaMouse = Input.mousePosition - previousMousePosition;

    float rotationX = deltaMouse.y * rotationSpeed * Time.deltaTime;
    float rotationY = -deltaMouse.x * rotationSpeed * Time.deltaTime;

    Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0);
    currentObject.rotation = rotation * currentObject.rotation;

    previousMousePosition = Input.mousePosition;
}
    void HandlePromptRaycast()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, interactableLayer))
        {
            if (inspectPrompt != null)
            {
                inspectPrompt.SetActive(true);
               // inspectPrompt.text = promptMessage;
            }
        }
        else
        {
    
            if (inspectPrompt != null)
                inspectPrompt.SetActive(false);
        }
    }

    void TryStartInspection()
    {
        if (inspectPrompt != null)
                inspectPrompt.SetActive(false);
         inspectCamera.enabled = false;
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, interactableLayer))
        {
            currentObject = hit.transform;
            isInspecting = true;

            originalPosition = currentObject.position;
            originalRotation = currentObject.rotation;
            originalLayer = currentObject.gameObject.layer;

            if (playerMovementScript != null)
                playerMovementScript.enabled = false;
                GunCanvas.SetActive(false);
                GunMesh.SetActive(false);

            StartCoroutine(TransitionToInspect());

            if (currentObject.TryGetComponent<InspectableObject>(out var inspectable))
            {
                descriptionText.text = inspectable.description;
                descriptionText.gameObject.SetActive(true);
                 inspectable.OnInspected();
            }
            
            
        }
        inspectPostProcessVolume.enabled = true;
    }
    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    System.Collections.IEnumerator TransitionToInspect()
    {
        inspectPrompt.SetActive(false);
        Vector3 startPos = currentObject.position;
        Quaternion startRot = currentObject.rotation;
        inspectCamera.transform.rotation = mainCamera.transform.rotation;

        // Align inspect camera rotation to main camera
        inspectCamera.transform.rotation = mainCamera.transform.rotation;

        // Move inspectSpot in front of the inspection camera
        float inspectDistance = 1.0f; // Adjust as needed
        inspectSpot.position = inspectCamera.transform.position + inspectCamera.transform.forward * inspectDistance;


        float elapsed = 0f;
        mainCamera.enabled = false;
        inspectCamera.enabled = true;

        currentObject.gameObject.layer = LayerMask.NameToLayer("Inspect");

        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionTime;
            currentObject.position = Vector3.Lerp(startPos, inspectSpot.position, t);
            currentObject.rotation = Quaternion.Slerp(startRot, inspectSpot.rotation, t);
            yield return null;
        }

        currentObject.position = inspectSpot.position;
        currentObject.rotation = inspectSpot.rotation;
        previousMousePosition = Input.mousePosition;
        UnlockCursor();
    }



    System.Collections.IEnumerator ExitInspection()
    {
        descriptionText.gameObject.SetActive(false);
        if (currentObject == null)
            yield break;

        Vector3 startPos = currentObject.position;
        Quaternion startRot = currentObject.rotation;
        float elapsed = 0f;

        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionTime;
            currentObject.position = Vector3.Lerp(startPos, originalPosition, t);
            currentObject.rotation = Quaternion.Slerp(startRot, originalRotation, t);
            yield return null;
        }

        currentObject.position = originalPosition;
        currentObject.rotation = originalRotation;
        currentObject.gameObject.layer = originalLayer;

        inspectCamera.enabled = false;
        mainCamera.enabled = true;

        if (playerMovementScript != null)
            playerMovementScript.enabled = true;
            GunCanvas.SetActive(true);
            GunMesh.SetActive(true);

        currentObject = null;
        isInspecting = false;
        LockCursor();
        inspectPostProcessVolume.enabled = false;
    }
}
