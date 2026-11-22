
using System.Collections;
using UnityEngine;
using PlayerControllers;

[RequireComponent(typeof(Rigidbody))]
public class Obstacle : MonoBehaviour
{
    private Rigidbody rb;

    [Header("Player")]
    [SerializeField] private Transform player;
    [SerializeField] private float activationDistance = 12f;
    [SerializeField] private float explosionDistance = 1.5f;

    public float damageDistance = 1f;
    public float damage = 20f;

    [Header("Launch")]
    [SerializeField] private float launchForce = 10f;
    [SerializeField] private float upwardForce = 2f;

    [Header("Explosion")]
    [SerializeField] private GameObject explodeEffect;
    [SerializeField] private float destroyAfterExplosion = 0.1f;

    private bool launched;
    private bool exploded;

    private bool originalUseGravity;
    private bool originalIsKinematic;
    private RigidbodyConstraints originalConstraints;

    private SpawnTracker spawnTracker;
    private Coroutine returnCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spawnTracker = GetComponent<SpawnTracker>();

        originalUseGravity = rb.useGravity;
        originalIsKinematic = rb.isKinematic;
        originalConstraints = rb.constraints;
    }

    private void OnEnable()
    {
        ResetObstacle();
        FindPlayer();
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (exploded || player == null)
            return;

        float sqrDistance =
            (player.position - transform.position).sqrMagnitude;

        if (!launched &&
            sqrDistance <= activationDistance * activationDistance)
        {
            Launch();
        }

        if (launched &&
            sqrDistance <= explosionDistance * explosionDistance)
        {
            Explode();
        }
    }

    // -- FIND PLAYER
    private void FindPlayer()
    {
        if (player != null)
            return;

        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
            player = playerObject.transform;
    }

    // -- RESET OBSTACLE
    public void ResetObstacle()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }

        launched = false;
        exploded = false;

        FindPlayer();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        rb.useGravity = originalUseGravity;
        rb.isKinematic = originalIsKinematic;
        rb.constraints = originalConstraints;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // -- PREPARE FOR HEALTH EXPLOSION
    public void PrepareForHealthExplosion()
    {
        exploded = true;
        launched = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // -- LAUNCH OBSTACLE
    private void Launch()
    {
        if (launched || exploded)
            return;

        launched = true;

        Vector3 direction =
            (player.position - transform.position).normalized;

        direction.y += upwardForce;
        direction.Normalize();

        rb.AddForce(
            direction * launchForce,
            ForceMode.VelocityChange
        );
    }

    // -- EXPLODE OBSTACLE
    private void Explode()
    {
        if (exploded)
            return;

        exploded = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (player != null)
        {
            float distance =
                Vector3.Distance(player.position, transform.position);

            if (distance <= damageDistance)
            {
                FPP_PlayerController playerHealth =
                    player.GetComponent<FPP_PlayerController>();

                if (playerHealth != null)
                {
                    float damageMultiplier = damageDistance > 0f
                        ? 1f - (distance / damageDistance)
                        : 1f;

                    int finalDamage = Mathf.CeilToInt(
                        damage * Mathf.Clamp01(damageMultiplier)
                    );

                    playerHealth.PlayerTakeDamage(finalDamage);
                }
            }
        }

        // PLAY EXPLOSION EFFECT
        if (explodeEffect != null)
        {
            GameObject effect = Instantiate(
                explodeEffect,
                transform.position,
                Quaternion.identity
            );

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayExplosionClip();

            ParticleSystem particles =
                effect.GetComponent<ParticleSystem>();

            if (particles != null)
            {
                Destroy(
                    effect,
                    particles.main.duration +
                    particles.main.startLifetime.constantMax
                );
            }
            else
            {
                Destroy(effect, 3f);
            }
        }

        returnCoroutine = StartCoroutine(ReturnAfterExplosion());
    }

    // Return to pool after explosion
    private IEnumerator ReturnAfterExplosion()
    {
        yield return new WaitForSecondsRealtime(destroyAfterExplosion);

        returnCoroutine = null;

        if (spawnTracker == null)
            spawnTracker = GetComponent<SpawnTracker>();

        if (spawnTracker != null)
            spawnTracker.ReturnToPool();
        else
            gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
    }
}