using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class HumanFollower : MonoBehaviour
{
    [Header("Follow Settings")]
    public float minSpeed = 1.2f;
    public float maxSpeed = 2.8f;
    public float stopDistance = 1.8f;

    [Header("Death")]
    public GameObject deathParticlePrefab;
    public float destroyDelay = 2f;

    [Header("Post Processing")]
    public PostFXBlendTrigger postFXTrigger;  // drag your PostFXBlendTrigger here

    private NavMeshAgent _agent;
    private Transform _player;
    private bool _following = false;
    private bool _dead = false;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = Random.Range(minSpeed, maxSpeed);
        _agent.stoppingDistance = stopDistance;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            _following = true;
    }

    void Update()
    {
        if (_dead || !_following || _player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > stopDistance)
            _agent.SetDestination(_player.position);
        else
            _agent.ResetPath();
    }

    public void Die()
    {
        if (_dead) return;
        _dead = true;

        _agent.isStopped = true;
        _agent.enabled = false;

        // spawn death particle
        if (deathParticlePrefab != null)
            Instantiate(deathParticlePrefab,
                transform.position, Quaternion.identity);

        // force PP back to normal — simulates OnTriggerExit
        if (postFXTrigger != null)
            postFXTrigger.ForceExit();

        StartCoroutine(DestroyAfterDelay());
    }

    IEnumerator DestroyAfterDelay()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}