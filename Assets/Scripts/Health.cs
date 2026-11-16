
using System.Collections;
using UnityEngine;
using PlayerControllers;

public class Health : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Explosion Damage")]
    public float damage = 30f;
    public float damageDistance = 1f;

    private float originalMaxHealth;

    private FPP_PlayerController playerController;
    private Transform player;

    private SpawnTracker spawnTracker;
    private Obstacle obstacle;

    private bool isExploding;
    private Coroutine deathRoutine;

    private void Awake()
    {
        originalMaxHealth = maxHealth;

        spawnTracker = GetComponent<SpawnTracker>();
        obstacle = GetComponent<Obstacle>();

        FindPlayer();
    }

    private void OnEnable()
    {
        ResetHealth();
    }

    // -- FIND PLAYER
    private void FindPlayer()
    {
        if (player != null)
            return;

        GameObject playerObject = GameObject.Find("Player");

        if (playerObject == null)
            playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
            return;

        player = playerObject.transform;
        playerController = playerObject.GetComponent<FPP_PlayerController>();
    }

    // -- RESET HEALTH
    public void ResetHealth()
    {
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        maxHealth = originalMaxHealth;
        isExploding = false;

        FindPlayer();
    }

    // -- TAKE DAMAGE
    public void TakeDamage(float damageAmount)
    {
        if (isExploding)
            return;

        maxHealth -= damageAmount;

        if (maxHealth <= 0f)
        {
            ExplodeAndReturn();
        }
    }

    // -- EXPLODE
    private void ExplodeAndReturn()
    {
        if (isExploding)
            return;

        isExploding = true;

        // Stop proximity behaviour if this object also has an Obstacle component.
        if (obstacle != null)
            obstacle.PrepareForHealthExplosion();

        if (playerController != null &&
            playerController.explosionEffect != null)
        {
            Instantiate(
                playerController.explosionEffect,
                transform.position,
                Quaternion.identity
            );
        }

        FindPlayer();

        if (player != null)
        {
            float sqrDistance =
                (player.position - transform.position).sqrMagnitude;

            if (sqrDistance <= damageDistance * damageDistance)
            {
                if (playerController == null)
                    playerController = player.GetComponent<FPP_PlayerController>();

                if (playerController != null)
                {
                    float distance =
                        Vector3.Distance(transform.position, player.position);

                    float damageMultiplier = damageDistance > 0f
                        ? 1f - (distance / damageDistance)
                        : 1f;

                    int finalDamage = Mathf.CeilToInt(
                        damage * Mathf.Clamp01(damageMultiplier)
                    );

                    playerController.PlayerTakeDamage(finalDamage);
                }
            }
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayExplosionClip();

        deathRoutine = StartCoroutine(Die());
    }

    // -- RETURN TO POOL
    public IEnumerator Die()
    {
        yield return new WaitForSecondsRealtime(0.1f);

        deathRoutine = null;

        if (spawnTracker != null)
            spawnTracker.ReturnToPool();
        else
            gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }
    }
}