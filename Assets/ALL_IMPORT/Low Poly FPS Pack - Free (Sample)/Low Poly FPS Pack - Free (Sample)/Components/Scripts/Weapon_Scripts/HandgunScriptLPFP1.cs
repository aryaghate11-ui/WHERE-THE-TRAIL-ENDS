using UnityEngine;
using System.Collections;
using TMPro;

public class HandgunScriptLPFP1 : MonoBehaviour
{
    private Animator anim;

    [Header("Gun Cameras")]
    public Camera gunCamera;
    public Camera mainCamera;

    [Header("FOV Settings")]
    public float fovSpeed = 15.0f;
    public float defaultFov = 40.0f;
    public float aimFov = 15.0f;

    [Header("Weapon Settings")]
    public int maxAmmo = 9;
    private int currentAmmo;
    private bool isReloading;
    private bool isAiming;
    private bool isRunning;

    [Header("Muzzle Flash")]
    public ParticleSystem muzzleParticles;
    public Light muzzleflashLight;
    public float lightDuration = 0.02f;

    [Header("Audio")]
    public AudioSource shootAudioSource;
    public AudioClip shootSound;
    public AudioClip reloadSound;

    [Header("UI")]
    public TMP_Text currentAmmoText;
    public TMP_Text interactPrompt;
    public string interactMessage = "Press E to pick up ammo";

    [Header("Prefabs")]
    public Transform bulletPrefab;
    public Transform casingPrefab;
    public Transform bulletSpawnPoint;
    public Transform casingSpawnPoint;

    [Header("Camera Shake")]
    public float shakeIntensity = 0.1f;
    public float shakeDuration = 0.1f;

    [Header("Pickup")]
    public float interactDistance = 3f;

    [Header("Phone Reference")]
    public PhoneController phoneController;

    [Header("Ghost / Enemy")]
    public string enemyTag = "Ghost";

    public bool isGunDrawn = false;
    private bool isHolstering = false;

    // ── set by inspection system so gun doesn't auto-draw on exit ──
    [HideInInspector] public bool lockedByInspection = false;

    void Awake()
    {
        anim = GetComponent<Animator>();
        currentAmmo = maxAmmo;

        if (muzzleflashLight != null)
            muzzleflashLight.enabled = false;

        isGunDrawn = false;
        anim.SetBool("Holster", true);
    }

    void Update()
    {
        HandleDrawHolster();

        if (!isGunDrawn || isHolstering) return;
        if (phoneController != null && phoneController.isPhoneActive) return;

        HandleAiming();
        HandleShooting();
        HandleAmmoPickup();
        HandleAnimations();
        UpdateUI();
    }

    IEnumerator HolsterGun()
    {
        isHolstering = true;
        isGunDrawn = false;
        anim.SetBool("Holster", true);
        anim.SetBool("Run", false);     // stop run anim on holster
        yield return new WaitForSeconds(0.6f);
        isHolstering = false;
    }

    void HandleDrawHolster()
    {
        if (Input.GetKeyDown(KeyCode.G) && !isHolstering && !lockedByInspection)
        {
            if (!isGunDrawn)
            {
                if (phoneController != null && phoneController.isPhoneActive)
                    phoneController.ForceHidePhone();
                DrawGun();
            }
            else
            {
                StartCoroutine(HolsterGun());
            }
        }

        if (Input.GetKeyDown(KeyCode.Q) && !isHolstering)
        {
            if (isGunDrawn)
                StartCoroutine(HolsterThenPhone());
            else if (phoneController != null)
                phoneController.TogglePhone();
        }
    }

    void DrawGun()
    {
        isGunDrawn = true;
        anim.SetBool("Holster", false);
        anim.SetTrigger("Draw");
    }

    // called by inspection system on exit — keeps gun holstered
    public void ForceHolsterSilent()
    {
        isGunDrawn = false;
        isHolstering = false;
        anim.SetBool("Holster", true);
        anim.SetBool("Run", false);
        anim.SetBool("Aim", false);
    }

    IEnumerator HolsterThenPhone()
    {
        isHolstering = true;
        isGunDrawn = false;
        anim.SetBool("Holster", true);
        anim.SetBool("Run", false);
        yield return new WaitForSeconds(0.6f);
        isHolstering = false;
        if (phoneController != null)
            phoneController.ShowPhone();
    }

    public void ForceHolster()
    {
        if (!isGunDrawn) return;
        StartCoroutine(HolsterThenPhone());
    }

    void HandleAiming()
    {
        if (Input.GetButton("Fire2") && !isReloading && !isRunning)
        {
            gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView, aimFov, fovSpeed * Time.deltaTime);
            isAiming = true;
            anim.SetBool("Aim", true);
        }
        else
        {
            gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView, defaultFov, fovSpeed * Time.deltaTime);
            isAiming = false;
            anim.SetBool("Aim", false);
        }
    }

    void HandleShooting()
    {
        if (Input.GetMouseButtonDown(0) && currentAmmo > 0 && !isReloading)
        {
            currentAmmo--;

            anim.Play(isAiming ? "Aim Fire" : "Fire", 0, 0f);
            muzzleParticles.Emit(1);
            shootAudioSource.PlayOneShot(shootSound);
            StartCoroutine(MuzzleFlashLight());
            StartCoroutine(CameraShake());

            // ── QueryTriggerInteraction.Ignore skips ALL trigger colliders ──
            // so ghost detection spheres are ignored, only body colliders hit
            Ray ray = new Ray(
                mainCamera.transform.position,
                mainCamera.transform.forward);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))   // <-- THE FIX
            {
                // ghost hit
                if (hit.collider.CompareTag(enemyTag))
                {
                    HumanFollower ghost = hit.collider.GetComponentInParent<HumanFollower>();
                    if (ghost != null) ghost.Die();
                }

                // deer hit
                if (hit.collider.CompareTag("Deer"))
                {
                    deer_ai deer = hit.collider.GetComponentInParent<deer_ai>();
                    if (deer != null) deer.TakeDamage(25f);
                }

                EvidenceObject evidence = hit.collider.GetComponent<EvidenceObject>();
                if (evidence != null && !evidence.photographed)
                    evidence.OnInspected();
            }

            Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
            Instantiate(casingPrefab, casingSpawnPoint.position, casingSpawnPoint.rotation);

            UpdateUI();
            StoryDirector.Instance?.TriggerEvent(StoryDirector.StoryEvent.GameStart);
        }
    }

    void HandleAmmoPickup()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            if (hit.collider.CompareTag("Ammo"))
            {
                if (currentAmmo < maxAmmo)
                {
                    if (interactPrompt != null) interactPrompt.text = interactMessage;
                    if (Input.GetKeyDown(KeyCode.E) && !isReloading)
                    {
                        StartCoroutine(PerformReload(hit.collider.gameObject));
                        Destroy(hit.collider.gameObject);
                    }
                }
                else
                {
                    if (interactPrompt != null) interactPrompt.text = "Ammo Full";
                }
            }
            else
            {
                if (interactPrompt != null) interactPrompt.text = "";
            }
        }
        else
        {
            if (interactPrompt != null) interactPrompt.text = "";
        }
    }

    IEnumerator PerformReload(GameObject ammoObject)
    {
        isReloading = true;
        if (anim != null) anim.Play("Reload", 0, 0f);
        if (reloadSound != null) shootAudioSource.PlayOneShot(reloadSound);
        yield return new WaitForSeconds(1.3f);
        currentAmmo = maxAmmo;
        UpdateUI();
        isReloading = false;
    }

    void HandleAnimations()
    {
        isRunning = Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.LeftShift);
        anim.SetBool("Run", isRunning);
    }

    IEnumerator MuzzleFlashLight()
    {
        muzzleflashLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        muzzleflashLight.enabled = false;
    }

    IEnumerator CameraShake()
    {
        if (mainCamera == null) yield break;
        Vector3 originalPos = mainCamera.transform.localPosition;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;
            mainCamera.transform.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        mainCamera.transform.localPosition = originalPos;
    }

    void UpdateUI()
    {
        if (currentAmmoText != null)
            currentAmmoText.text = currentAmmo + " / " + maxAmmo;
    }
}