using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;

public class deer_ai : MonoBehaviour
{
    public BlinkEffectManager blinkManager;
    [Header("Deer Settings")]
    public float health = 100f;
    public float walkRadius = 15f;
    public float detectRange = 10f;
    public float runSpeed = 5f;
    public float walkSpeed = 2f;
    public float obstacleAvoidDistance = 3f;

    [Header("References")]
    public Animator anim;
    public NavMeshAgent agent;
    public Transform player;
    public ParticleSystem bloodHitEffect;
    public ParticleSystem deathBloodEffect;

    [Header("Fade Effect")]
    public CanvasGroup fadeCanvas;
    public float fadeDuration = 1.5f; // Adjustable in Inspector

    private bool isDead = false;
    private bool isRunning = false;
    private bool isFading = false;

    void Start()
    {
        EnableRagdoll(false);

        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!anim) anim = GetComponent<Animator>();
        if (!player) player = GameObject.FindGameObjectWithTag("Player").transform;

        SetRandomDestination();
        agent.speed = walkSpeed;
    }

    void Update()
    {
        if (isDead) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < detectRange)
        {
            RunAwayFromPlayer();
        }
        else if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            SetRandomDestination();
        }

        // Handle obstacle avoidance
        AvoidWalls();

        // Update animation blend
        float speedPercent = agent.velocity.magnitude / agent.speed;
        anim.SetFloat("State", speedPercent);
    }

    void SetRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * walkRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, walkRadius, 1))
        {
            agent.SetDestination(hit.position);
        }
    }

    void RunAwayFromPlayer()
    {
        if (!isRunning)
        {
            agent.speed = runSpeed;
            isRunning = true;
        }

        Vector3 dirToPlayer = transform.position - player.position;
        Vector3 newPos = transform.position + dirToPlayer.normalized * walkRadius;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(newPos, out hit, walkRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void AvoidWalls()
    {
        Ray ray = new Ray(transform.position + Vector3.up, transform.forward);
        if (Physics.Raycast(ray, obstacleAvoidDistance))
        {
            Vector3 newDir = Quaternion.Euler(0, Random.Range(-120, 120), 0) * transform.forward;
            agent.SetDestination(transform.position + newDir * 3f);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        health -= amount;
        if (bloodHitEffect) Instantiate(bloodHitEffect, transform.position + Vector3.up * 1f, Quaternion.identity);

        if (health <= 0f)
        {
            StartCoroutine(Die());
        }
        else
        {
            RunAwayFromPlayer();
        }
    }

    IEnumerator Die()
{
    if (isDead) yield break;
    isDead = true;

    // Stop moving
    if (agent) agent.isStopped = true;

    // Play blood effect
    if (deathBloodEffect) 
        Instantiate(deathBloodEffect, transform.position + Vector3.up * 0.5f, Quaternion.identity);

    // Enable ragdoll physics
    EnableRagdoll(true);

    // Slow blink when deer dies
    if (blinkManager != null)
        blinkManager.BlinkSlow();

    yield break; // no animation blend needed
}


    IEnumerator FadeEffect()
    {
        isFading = true;

        // Fade to black
        float t = 0f;
        while (t < fadeDuration)
        {
            fadeCanvas.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(1f);

        // Fade back to normal
        t = 0f;
        while (t < fadeDuration)
        {
            fadeCanvas.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }

        fadeCanvas.alpha = 0f;
        isFading = false;
    }

    void EnableRagdoll(bool enable)
{

    foreach (var rb in GetComponentsInChildren<Rigidbody>())
        rb.isKinematic = !enable;

    foreach (var c in GetComponentsInChildren<Collider>())
        if (c.gameObject != gameObject)
            c.enabled = enable;
    // All colliders and rigidbodies in child bones
    var bodies = GetComponentsInChildren<Rigidbody>();
    var colliders = GetComponentsInChildren<Collider>();

    foreach (var rb in bodies)
    {
        rb.isKinematic = !enable; // ragdoll physics ON
        rb.detectCollisions = enable;
    }

    foreach (var col in colliders)
    {
        if (col.gameObject != this.gameObject) col.enabled = enable; 
    }

    // Disable NavMesh + Animator when ragdoll starts
    if (enable)
    {
        if (agent) agent.enabled = false;
        if (anim) anim.enabled = false;
    }
}

}
