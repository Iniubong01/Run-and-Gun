
using System.Collections;
using PlayerControllers;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    private NavMeshAgent agent;
    public Transform player;
    private Animator enemyAnim;
    private Rigidbody rb;

    [SerializeField] private LayerMask whatIsGround, whatIsPlayer;

    // AUDIO
    public AudioSource source;
    public AudioClip DeadClip;

    AudioSource sfxSource;
    public AudioClip shootClip;

    // PATROL
    public Vector3 walkPoint, offset, goodiesOffset;
    private bool walkPointSet;

    public float walkPointRange;

    [SerializeField] private GameObject goodies;

    // HEALTH
    public float health;
    public bool isElite;

    // ATTACK
    public float timeBetweenAttacks;
    private bool alreadyAttacked;
    private bool attacking;

    public float sightRange;
    public float attackRange;
    public float distanceFromPlayer = 5f;

    public bool playerInSightRange;
    public bool playerInAttackRange;

    public bool killEnemy;
    public bool isDead;

    // SHOOTING
    public Transform shootPoint;
    public Transform playerTarget;

    public GameObject hitEffect;
    public ParticleSystem noozleFlash;

    public float shootRange = 30f;
    public int damage = 10;

    // DEATH SLOW MOTION
    [Header("Death Slow Motion")]

    [SerializeField] private float deathTimeScale = 0.4f;
    [SerializeField] private float slowMotionDuration = 0.6f;
    [SerializeField] private float timeScaleRestoreSpeed = 3f;
    [SerializeField] private float deathAnimationDuration = 0.5f;
    [SerializeField] Transform effectParent;
    private FPP_PlayerController playerController;
    private Coroutine deathSequence;

    
    // Object pooling
    private SpawnTracker spawnTracker;

    // Original start values
    private float originalHealth;
    private float originalTimeBetweenAttacks;
    private float originalSightRange;
    private float originalAttackRange;
    private float originalDistanceFromPlayer;
    private float originalWalkPointRange;
    private float originalShootRange;
    private int originalDamage;

    private void Awake()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.Find("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        agent = GetComponent<NavMeshAgent>();
        enemyAnim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (player != null)
        {
            playerController = player.GetComponent<FPP_PlayerController>();
        }


        spawnTracker = GetComponent<SpawnTracker>();

        // Cache original values
        originalHealth = health;
        originalTimeBetweenAttacks = timeBetweenAttacks;
        originalSightRange = sightRange;
        originalAttackRange = attackRange;
        originalDistanceFromPlayer = distanceFromPlayer;
        originalWalkPointRange = walkPointRange;
        originalShootRange = shootRange;
        originalDamage = damage;
    }

    private void Start()
    {
        enemyAnim.SetTrigger("Idle");
        sfxSource = GameObject.Find("Sfx Audiosource").GetComponent<AudioSource>();
        effectParent = GameObject.Find("Effect parent").GetComponent<Transform>();
    }

    private void Update()
    {
        if (isDead)
        {
            if (agent != null)
                agent.isStopped = true;

            return;
        }

        playerInSightRange =
            Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);

        playerInAttackRange =
            Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        if (!playerInSightRange && !playerInAttackRange)
        {
            Patrol();
        }
        else if (playerInSightRange && !playerInAttackRange)
        {
            ChasePlayer();
        }
        else if (playerInSightRange && playerInAttackRange)
        {
            ShootPlayer();
        }
        else
        {
            Patrol();
        }

        if(transform.position.y < -10f)
        {
            DestroyEnemy();
        }
    }

    // Patrol
    private void Patrol()
    {
        attacking = false;

        if (!walkPointSet)
            SearchWalkPoint();

        if (walkPointSet)
            agent.SetDestination(walkPoint);

        enemyAnim.ResetTrigger("Idle");
        enemyAnim.ResetTrigger("Shoot");
        enemyAnim.ResetTrigger("Walk");

        enemyAnim.SetTrigger("Run");

        Vector3 distanceToWalkPoint =
            transform.position - walkPoint;

        if (distanceToWalkPoint.magnitude < 1f)
        {
            walkPointSet = false;

            enemyAnim.ResetTrigger("Idle");
            enemyAnim.ResetTrigger("Shoot");
            enemyAnim.ResetTrigger("Walk");
            enemyAnim.SetTrigger("Run");
        }
    }

    // Search walkpoint
    private void SearchWalkPoint()
    {
        float randomZ =
            Random.Range(-walkPointRange, walkPointRange);

        float randomX =
            Random.Range(-walkPointRange, walkPointRange);

        walkPoint = new Vector3(
            transform.position.x + randomX,
            transform.position.y,
            transform.position.z + randomZ
        );

        if (Physics.Raycast(
            walkPoint,
            -transform.up,
            2f,
            whatIsGround))
        {
            walkPointSet = true;
        }
    }

    // Health reduction
    public void ReduceEnemyHealth(int damage)
    {
        if (isDead)
            return;

        health -= damage;

        if (health <= 0)
        {
            Die();
        }
    }

    // Chase
    private void ChasePlayer()
    {
        float distanceToPlayer =
            Vector3.Distance(
                transform.position,
                player.position
            );

        if (distanceToPlayer <= distanceFromPlayer)
        {
            agent.isStopped = true;

            enemyAnim.ResetTrigger("Walk");
            enemyAnim.ResetTrigger("Run");
            enemyAnim.ResetTrigger("Shoot");

            enemyAnim.SetTrigger("Idle");

            attacking = false;

            return;
        }

        agent.isStopped = false;

        agent.SetDestination(player.position);

        enemyAnim.ResetTrigger("Walk");
        enemyAnim.ResetTrigger("Idle");
        enemyAnim.ResetTrigger("Shoot");

        enemyAnim.SetTrigger("Run");

        attacking = false;
    }

    // Shoot player
    private void ShootPlayer()
    {
        transform.rotation =
            Quaternion.Euler(
                0f,
                transform.rotation.eulerAngles.y,
                0f
            );

        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        if (!alreadyAttacked)
        {
            // enemyAnim.ResetTrigger("Walk");
            // enemyAnim.ResetTrigger("Idle");
            // enemyAnim.ResetTrigger("Run");

            enemyAnim.SetTrigger("Shoot");

            if (sfxSource != null && shootClip != null)
            {
                sfxSource.PlayOneShot(shootClip);
            }

            if (noozleFlash != null)
            {
                noozleFlash.Play();
            }

            Vector3 origin = shootPoint.position;

            Ray ray =
                new Ray(shootPoint.position, shootPoint.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, shootRange))
            {
                FPP_PlayerController hitPlayer = hit.collider.GetComponentInParent<FPP_PlayerController>();

                if (hit.collider.CompareTag("Player"))
                {
                    if (hitPlayer != null)
                    {
                        hitPlayer.PlayerTakeDamage(damage);
                    }
                }

                // Spawn effects and clean up heirachy
                if (hitEffect != null)
                {
                    GameObject effect = Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal), effectParent);
                }
            }

            attacking = true;
            alreadyAttacked = true;

            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= distanceFromPlayer)
        {
            agent.isStopped = true;
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
    }

    // Reset attack
    private void ResetAttack()
    {
        alreadyAttacked = false;

        if (!isDead)
        {
            agent.isStopped = false;
        }
    }

    // Die
    public void Die()
    {
        if (health > 0 || isDead)
            return;

        agent.isStopped = true;

        isDead = true;

        // Register player kill
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterEnemyKill();
        }

        // Stop any pending attack reset.
        CancelInvoke(nameof(ResetAttack));

        // Stop the AI from doing anything else.
        agent.ResetPath();

        enemyAnim.ResetTrigger("Walk");
        enemyAnim.ResetTrigger("Idle");
        enemyAnim.ResetTrigger("Run");
        enemyAnim.ResetTrigger("Shoot");

        enemyAnim.SetTrigger("Die");

        if (source != null && DeadClip != null)
        {
            source.PlayOneShot(DeadClip);
        }

        if (deathSequence != null)
        {
            StopCoroutine(deathSequence);
        }

        deathSequence = StartCoroutine(DeathSequence());

        this.enabled = false;
    }

    // Death sequence

    private IEnumerator DeathSequence()
    {
        bool returnToThirdPerson = false;

        if (playerController != null)
        {
            returnToThirdPerson =
                playerController.isThirdPerson;

            // Only force FPP if the player was in TPP.
            if (returnToThirdPerson)
            {
                playerController.isThirdPerson = false;
            }
        }

        // Slow motion
        Time.timeScale = deathTimeScale;

        // Use REAL time so slow motion doesn't stretch the duration indefinitely.
        yield return new WaitForSecondsRealtime(slowMotionDuration);

        // Return camera to TPP.
        if (playerController != null &&
            returnToThirdPerson)
        {
            playerController.isThirdPerson = true;
        }

        // Smoothly restore time scale.
        while (Time.timeScale < 1f)
        {
            Time.timeScale = Mathf.MoveTowards( Time.timeScale, 1f, timeScaleRestoreSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        Time.timeScale = 1f;

        // Wait for death animation.
        yield return new WaitForSecondsRealtime(deathAnimationDuration);

        DestroyEnemy();
    }


    // Return enemy to pool
    private void DestroyEnemy()
    {
        Vector3 goodiesPosition = transform.position + goodiesOffset;

        // Spawn goodies if assigned
        if (goodies != null && isElite)
        {
            Instantiate(goodies, goodiesPosition, Quaternion.identity);
        }

        // Return enemy to pool
        if (spawnTracker != null)
        {
            spawnTracker.ReturnToPool();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void GetHit()
    {
        if (isDead)
            return;

        enemyAnim.ResetTrigger("Walk");
        enemyAnim.ResetTrigger("Idle");
        enemyAnim.ResetTrigger("Run");
        enemyAnim.ResetTrigger("Shoot");

        enemyAnim.SetTrigger("GetHit");
    }

    private void OnDestroy()
    {
        // Make sure the game isn't left permanently slowed if the enemy is destroyed
        if (Mathf.Approximately(Time.timeScale, 0f) ||
            Time.timeScale < 1f)
        {
            Time.timeScale = 1f;
        }
    }

    // Reset for pooling
    public void ResetForReuse(Transform newPlayer)
    {
        StopAllCoroutines();
        CancelInvoke();

        // Restore original values
        health = originalHealth;
        timeBetweenAttacks = originalTimeBetweenAttacks;
        sightRange = originalSightRange;
        attackRange = originalAttackRange;
        distanceFromPlayer = originalDistanceFromPlayer;
        walkPointRange = originalWalkPointRange;
        shootRange = originalShootRange;
        damage = originalDamage;

        // Reset player reference
        if (newPlayer != null)
        {
            player = newPlayer;
            playerTarget = newPlayer;

            playerController =
                newPlayer.GetComponent<FPP_PlayerController>();
        }

        // Reset AI states
        walkPointSet = false;
        alreadyAttacked = false;
        attacking = false;

        playerInSightRange = false;
        playerInAttackRange = false;

        killEnemy = false;
        isDead = false;

        deathSequence = null;

        // Reset Physics
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Reset NavMeshAgent
        if (agent != null)
        {
            agent.enabled = true;

            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.ResetPath();
            }
        }

        // Reset audio
        if (source != null)
            source.Stop();

        if (sfxSource != null)
            sfxSource.Stop();

        // Reset particles
        if (noozleFlash != null)
        {
            noozleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // Reset animator
        if (enemyAnim != null)
        {
            enemyAnim.ResetTrigger("Die");
            enemyAnim.ResetTrigger("Shoot");
            enemyAnim.ResetTrigger("Attack");
            enemyAnim.ResetTrigger("Run");
            enemyAnim.ResetTrigger("Idle");
            enemyAnim.SetTrigger("Walk");
        }

        enabled = true;
    }

    // Reset enemies before returning to the pool
    public void ResetForPool()
    {
        StopAllCoroutines();
        CancelInvoke();

        // Reset AI states
        walkPointSet = false;
        alreadyAttacked = false;
        attacking = false;

        playerInSightRange = false;
        playerInAttackRange = false;

        killEnemy = false;
        isDead = false;

        // RESET PHYSICS
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // RESET NAVMESH
        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        // RESET AUDIO
        if (source != null)
            source.Stop();

        if (sfxSource != null)
            sfxSource.Stop();

        // RESET PARTICLES
        if (noozleFlash != null)
        {
            noozleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        deathSequence = null;

        enabled = true;
    }

    // -- RESET NAVMESH WHEN OBJECT IS ACTIVATED
    private void OnEnable()
    {
        if (agent == null)
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }
    }

}