
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerControllers;

public class InfinitySpawner : MonoBehaviour
{
    // TIER CONFIGURATION
    [System.Serializable]
    public class SpawnTier
    {
        public string tierName;

        [Tooltip("Prefabs belonging to this tier.")]
        public GameObject[] prefabs;

        [Min(0f)]
        [Tooltip("Selection weight for this tier.")]
        public float spawnWeight = 1f;
    }

    [System.Serializable]
    public class SpawnArea
    {
        public string areaName;
        public Transform center;

        [Min(0.1f)]
        public float radius = 5f;

        public float minimumPlayerDistance = 10f;
        public LayerMask groundLayer;
        public LayerMask obstacleLayer;

        [Min(1)]
        public int attempts = 20;
    }

    // ENEMY TIERS
    [Header("Enemy Tiers")]
    public SpawnTier[] enemyTiers = new SpawnTier[4];

    // EXPLOSIVE TIERS
    [Header("Explosive Tiers")]
    public SpawnTier[] explosiveTiers = new SpawnTier[4];

    // SPAWN AREAS
    [Header("Enemy Spawn Areas")]
    public SpawnArea[] enemySpawnAreas;

    [Header("Explosive Spawn Areas")]
    public SpawnArea[] explosiveSpawnAreas;

    [Header("Spawn Validation")]
    [Min(0.1f)]
    public float spawnCheckRadius = 0.5f;

    [Min(1f)]
    public float groundCheckDistance = 10f;

    [Header("Player")]
    public Transform player;

    // PLAYER SPAWN DISTANCE
    [Header("Player Spawn Distance")]
    public float spawnDistanceFromPlayer = 50f;
    public float spawnDistanceVariation = 7f;

    [Header("Spawn Z Limits")]
    public float minZ = -12f;
    public float maxZ = -2f;

    // TIER UNLOCK TIMES
    [Header("Enemy Tier Unlock Times")]
    public Vector2 enemyTier2UnlockTime = new Vector2(60f, 90f);
    public Vector2 enemyTier3UnlockTime = new Vector2(90f, 150f);
    public Vector2 enemyTier4UnlockTime = new Vector2(120f, 180f);

    [Header("Explosive Tier Unlock Times")]
    public Vector2 explosiveTier2UnlockTime = new Vector2(60f, 90f);
    public Vector2 explosiveTier3UnlockTime = new Vector2(90f, 150f);
    public Vector2 explosiveTier4UnlockTime = new Vector2(120f, 180f);

    // DIFFICULTY
    [Header("Enemy Spawn Intervals")]
    public float startingEnemyInterval = 5f;
    public float minimumEnemyInterval = 1f;

    [Header("Explosive Spawn Intervals")]
    public float startingExplosiveInterval = 10f;
    public float minimumExplosiveInterval = 2f;

    [Header("Difficulty Ramp")]
    public float difficultyRampDuration = 600f;

    // SPAWN QUANTITIES
    [Header("Enemy Spawn Quantity")]
    public int startingEnemyCount = 1;
    public int maximumEnemyCountPerWave = 5;
    public float enemyQuantityIncreaseInterval = 60f;

    [Header("Explosive Spawn Quantity")]
    public int startingExplosiveCount = 1;
    public int maximumExplosiveCountPerWave = 3;
    public float explosiveQuantityIncreaseInterval = 120f;

    // ACTIVE LIMITS
    [Header("Active Object Limits")]
    public int maximumActiveEnemies = 30;
    public int maximumActiveExplosives = 15;

    // RUNTIME
    [Header("Runtime")]
    public bool spawningEnabled = true;

    [Range(1, 4)]
    public int unlockedEnemyTier = 1;

    [Range(1, 4)]
    public int unlockedExplosiveTier = 1;

    public float elapsedGameTime;

    public int activeEnemyCount;
    public int activeExplosiveCount;

    private float enemyUnlockTimer;
    private float explosiveUnlockTimer;

    private float enemyTier2Time;
    private float enemyTier3Time;
    private float enemyTier4Time;

    private float explosiveTier2Time;
    private float explosiveTier3Time;
    private float explosiveTier4Time;

    private Coroutine enemySpawnRoutine;
    private Coroutine explosiveSpawnRoutine;
    private UI_Manager uiManager;

    // OBJECT POOLING
    private class PooledObjectInfo
    {
        public GameObject prefab;
        public bool isExplosive;
    }

    private readonly Dictionary<GameObject, Queue<GameObject>> objectPools =
        new Dictionary<GameObject, Queue<GameObject>>();

    private readonly Dictionary<GameObject, PooledObjectInfo> activeObjects =
        new Dictionary<GameObject, PooledObjectInfo>();

    // Initialization
    private void Start()
    {
        FindPlayer();
        InitializeSpawner();
        uiManager = GameObject.Find("UI Manager").GetComponent<UI_Manager>();
    }

    private void FindPlayer()
    {
        if (player != null)
            return;

        FPP_PlayerController controller = GameObject.Find("Player").GetComponent<FPP_PlayerController>();

        if (controller != null)
            player = controller.transform;
    }

    // Initialize spawner
    public void InitializeSpawner()
    {
        StopSpawning();
        ReturnAllActiveObjectsToPool();

        FindPlayer();

        elapsedGameTime = 0f;

        unlockedEnemyTier = 1;
        unlockedExplosiveTier = 1;

        enemyUnlockTimer = 0f;
        explosiveUnlockTimer = 0f;

        activeEnemyCount = 0;
        activeExplosiveCount = 0;

        // Independent enemy unlock schedules
        enemyTier2Time = Random.Range(
            enemyTier2UnlockTime.x,
            enemyTier2UnlockTime.y
        );

        enemyTier3Time = enemyTier2Time + Random.Range(
            enemyTier3UnlockTime.x,
            enemyTier3UnlockTime.y
        );

        enemyTier4Time = enemyTier3Time + Random.Range(
            enemyTier4UnlockTime.x,
            enemyTier4UnlockTime.y
        );

        // Independent explosive unlock schedules
        explosiveTier2Time = Random.Range(
            explosiveTier2UnlockTime.x,
            explosiveTier2UnlockTime.y
        );

        explosiveTier3Time = explosiveTier2Time + Random.Range(
            explosiveTier3UnlockTime.x,
            explosiveTier3UnlockTime.y
        );

        explosiveTier4Time = explosiveTier3Time + Random.Range(
            explosiveTier4UnlockTime.x,
            explosiveTier4UnlockTime.y
        );

        spawningEnabled = true;

        enemySpawnRoutine = StartCoroutine(EnemySpawner());
        explosiveSpawnRoutine = StartCoroutine(ExplosiveSpawner());

        Debug.Log("Endless Spawner Initialized!");
    }

    private void Update()
    {
        if (!spawningEnabled)
            return;

        elapsedGameTime += Time.deltaTime;

        UpdateEnemyTierUnlocks();
        UpdateExplosiveTierUnlocks();
    }

    // Enemy tier progression
    private void UpdateEnemyTierUnlocks()
    {
        enemyUnlockTimer += Time.deltaTime;

        if (unlockedEnemyTier < 2 && enemyUnlockTimer >= enemyTier2Time)
            UnlockEnemyTier(2);

        if (unlockedEnemyTier < 3 && enemyUnlockTimer >= enemyTier3Time)
            UnlockEnemyTier(3);

        if (unlockedEnemyTier < 4 && enemyUnlockTimer >= enemyTier4Time)
            UnlockEnemyTier(4);
    }

    private void UnlockEnemyTier(int tier)
    {
        unlockedEnemyTier = tier;
        Debug.Log($"ENEMY TIER {tier} UNLOCKED!");
    }

    // Explosives tier progression
    private void UpdateExplosiveTierUnlocks()
    {
        explosiveUnlockTimer += Time.deltaTime;

        if (unlockedExplosiveTier < 2 && explosiveUnlockTimer >= explosiveTier2Time)
            UnlockExplosiveTier(2);

        if (unlockedExplosiveTier < 3 && explosiveUnlockTimer >= explosiveTier3Time)
            UnlockExplosiveTier(3);

        if (unlockedExplosiveTier < 4 && explosiveUnlockTimer >= explosiveTier4Time)
            UnlockExplosiveTier(4);
    }

    private void UnlockExplosiveTier(int tier)
    {
        unlockedExplosiveTier = tier;
        Debug.Log($"EXPLOSIVE TIER {tier} UNLOCKED!");
    }

    // Enemy spawner
    private IEnumerator EnemySpawner()
    {
        if(activeEnemyCount > 7)
        {
            uiManager.ShowAlert();
        }

        while (spawningEnabled)
        {
            yield return new WaitForSeconds(GetEnemySpawnInterval());

            if (!spawningEnabled)
                yield break;

            if (activeEnemyCount >= maximumActiveEnemies)
                continue;

            int spawnCount = GetEnemySpawnCount();

            for (int i = 0; i < spawnCount; i++)
            {
                if (activeEnemyCount >= maximumActiveEnemies)
                    break;

                SpawnEnemy();
            }
        }
    }

    private void SpawnEnemy()
    {
        SpawnTier tier = GetRandomUnlockedTier(
            enemyTiers,
            unlockedEnemyTier
        );

        if (tier == null)
            return;

        GameObject prefab = GetRandomPrefab(tier);

        if (prefab == null)
            return;

        SpawnArea area = GetRandomSpawnArea(enemySpawnAreas);

        if (area == null)
            return;

        if (!TryGetRandomSpawnPosition(area, out Vector3 position))
            return;

        GameObject enemy = GetPooledObject(prefab, position, area.center.rotation, false);

        if (enemy == null)
            return;

        activeEnemyCount++;
    }

    // Explosives spawner
    private IEnumerator ExplosiveSpawner()
    {
        while (spawningEnabled)
        {
            yield return new WaitForSeconds(GetExplosiveSpawnInterval());

            if (!spawningEnabled)
                yield break;

            if (activeExplosiveCount >= maximumActiveExplosives)
                continue;

            int spawnCount = GetExplosiveSpawnCount();

            for (int i = 0; i < spawnCount; i++)
            {
                if (activeExplosiveCount >= maximumActiveExplosives)
                    break;

                SpawnExplosive();
            }
        }
    }

    private void SpawnExplosive()
    {
        SpawnTier tier = GetRandomUnlockedTier(
            explosiveTiers,
            unlockedExplosiveTier
        );

        if (tier == null)
            return;

        GameObject prefab = GetRandomPrefab(tier);

        if (prefab == null)
            return;

        SpawnArea area = GetRandomSpawnArea(explosiveSpawnAreas);

        if (area == null)
            return;

        if (!TryGetRandomSpawnPosition(area, out Vector3 position))
            return;

        GameObject explosive = GetPooledObject(
            prefab,
            position,
            area.center.rotation,
            true
        );

        if (explosive == null)
            return;

        activeExplosiveCount++;
    }

    // Get object from pool
    private GameObject GetPooledObject(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        bool isExplosive)
    {
        if (prefab == null)
            return null;

        if (!objectPools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            objectPools.Add(prefab, pool);
        }

        GameObject spawnedObject = null;

        while (pool.Count > 0 && spawnedObject == null)
        {
            spawnedObject = pool.Dequeue();
        }

        if (spawnedObject == null)
        {
            spawnedObject = Instantiate(prefab, position, rotation, transform);

            spawnedObject.SetActive(false);

            SpawnTracker newTracker =
                spawnedObject.GetComponent<SpawnTracker>();

            if (newTracker == null)
                newTracker = spawnedObject.AddComponent<SpawnTracker>();

            newTracker.Initialize(this, isExplosive);
        }

        spawnedObject.transform.SetPositionAndRotation(position, rotation);

        SpawnTracker tracker =
            spawnedObject.GetComponent<SpawnTracker>();

        if (tracker == null)
            tracker = spawnedObject.AddComponent<SpawnTracker>();

        tracker.Initialize(this, isExplosive);

        activeObjects[spawnedObject] = new PooledObjectInfo
        {
            prefab = prefab,
            isExplosive = isExplosive
        };

        // Reset pooled component states before activating.
        Enemy enemy = spawnedObject.GetComponent<Enemy>();

        if (enemy != null)
            enemy.ResetForReuse(player);

        Obstacle obstacle = spawnedObject.GetComponent<Obstacle>();

        if (obstacle != null)
            obstacle.ResetObstacle();

        Health health = spawnedObject.GetComponent<Health>();

        if (health != null)
            health.ResetHealth();

        spawnedObject.SetActive(true);

        return spawnedObject;
    }

    // Return object to pool
    public void ReturnToPool(GameObject spawnedObject, bool isExplosive)
    {
        if (spawnedObject == null)
            return;

        if (!activeObjects.TryGetValue(spawnedObject, out PooledObjectInfo info))
            return;

        activeObjects.Remove(spawnedObject);

        SpawnTracker tracker =
            spawnedObject.GetComponent<SpawnTracker>();

        if (tracker != null)
            tracker.MarkReturnedToPool();

        if (info.isExplosive)
        {
            activeExplosiveCount = Mathf.Max(0, activeExplosiveCount - 1);
        }
        else
        {
            activeEnemyCount = Mathf.Max(0, activeEnemyCount - 1);
        }

        Enemy enemy = spawnedObject.GetComponent<Enemy>();

        if (enemy != null)
            enemy.ResetForPool();

        Obstacle obstacle = spawnedObject.GetComponent<Obstacle>();

        if (obstacle != null)
            obstacle.ResetObstacle();

        Health health = spawnedObject.GetComponent<Health>();

        if (health != null)
            health.ResetHealth();

        spawnedObject.SetActive(false);

        if (!objectPools.TryGetValue(info.prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            objectPools.Add(info.prefab, pool);
        }

        pool.Enqueue(spawnedObject);
    }

    // RETURN ALL ACTIVE OBJECTS
    public void ReturnAllActiveObjectsToPool()
    {
        if (activeObjects.Count == 0)
        {
            activeEnemyCount = 0;
            activeExplosiveCount = 0;
            return;
        }

        List<GameObject> objectsToReturn =
            new List<GameObject>(activeObjects.Keys);

        foreach (GameObject obj in objectsToReturn)
        {
            if (obj == null)
                continue;

            PooledObjectInfo info = activeObjects[obj];

            SpawnTracker tracker = obj.GetComponent<SpawnTracker>();

            if (tracker != null)
                tracker.MarkReturnedToPool();

            activeObjects.Remove(obj);

            Enemy enemy = obj.GetComponent<Enemy>();

            if (enemy != null)
                enemy.ResetForPool();

            Obstacle obstacle = obj.GetComponent<Obstacle>();

            if (obstacle != null)
                obstacle.ResetObstacle();

            Health health = obj.GetComponent<Health>();

            if (health != null)
                health.ResetHealth();

            obj.SetActive(false);

            if (!objectPools.TryGetValue(info.prefab, out Queue<GameObject> pool))
            {
                pool = new Queue<GameObject>();
                objectPools.Add(info.prefab, pool);
            }

            pool.Enqueue(obj);
        }

        activeEnemyCount = 0;
        activeExplosiveCount = 0;
    }

    // Random spawn positions from the player's current x position
    private bool TryGetRandomSpawnPosition(
        SpawnArea area,
        out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        if (area == null || player == null)
            return false;

        float minimumDistance = Mathf.Max(
            0.1f,
            spawnDistanceFromPlayer - spawnDistanceVariation
        );

        float maximumDistance = Mathf.Max(
            minimumDistance,
            spawnDistanceFromPlayer + spawnDistanceVariation
        );

        float lowerZ = Mathf.Min(minZ, maxZ);
        float upperZ = Mathf.Max(minZ, maxZ);

        for (int i = 0; i < area.attempts; i++)
        {
            // Randomizes the distance from the player
            float randomDistance = Random.Range(
                minimumDistance,
                maximumDistance
            );

            // Randomizes whether to spawn in front or behind
            float direction = Random.value < 0.5f ? -1f : 1f;

            // Calculates the spawn position
            float randomX = player.position.x +
                (randomDistance * direction);

            float randomZ = Random.Range(lowerZ, upperZ);

            Vector3 randomPosition = new Vector3(
                randomX,
                player.position.y,
                randomZ
            );

            // Checks for the ground
            Vector3 rayOrigin = randomPosition +
                Vector3.up * groundCheckDistance;

            if (!Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit groundHit,
                groundCheckDistance * 2f,
                area.groundLayer,
                QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            Vector3 candidate = groundHit.point;

            // Checks if the spawn position is occupied
            if (Physics.CheckSphere(
                candidate + Vector3.up * spawnCheckRadius,
                spawnCheckRadius,
                area.obstacleLayer,
                QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            // Checks minimum player distance
            if (Vector3.Distance(candidate, player.position) <
                area.minimumPlayerDistance)
            {
                continue;
            }

            spawnPosition = candidate;
            return true;
        }

        return false;
    }

    // SPAWN AREA SELECTION
    private SpawnArea GetRandomSpawnArea(SpawnArea[] areas)
    {
        if (areas == null || areas.Length == 0)
            return null;

        return areas[Random.Range(0, areas.Length)];
    }

    // TIER SELECTION
    private SpawnTier GetRandomUnlockedTier(
        SpawnTier[] tiers,
        int unlockedTier)
    {
        if (tiers == null || tiers.Length == 0)
            return null;

        float totalWeight = 0f;

        for (int i = 0; i < unlockedTier; i++)
        {
            if (i >= tiers.Length || tiers[i] == null)
                continue;

            if (tiers[i].prefabs == null || tiers[i].prefabs.Length == 0)
                continue;

            totalWeight += Mathf.Max(0f, tiers[i].spawnWeight);
        }

        if (totalWeight <= 0f)
            return null;

        float randomValue = Random.Range(0f, totalWeight);

        for (int i = 0; i < unlockedTier; i++)
        {
            if (i >= tiers.Length || tiers[i] == null)
                continue;

            if (tiers[i].prefabs == null || tiers[i].prefabs.Length == 0)
                continue;

            randomValue -= Mathf.Max(0f, tiers[i].spawnWeight);

            if (randomValue <= 0f)
                return tiers[i];
        }

        return null;
    }

    private GameObject GetRandomPrefab(SpawnTier tier)
    {
        if (tier == null || tier.prefabs == null || tier.prefabs.Length == 0)
            return null;

        return tier.prefabs[Random.Range(0, tier.prefabs.Length)];
    }

    // DIFFICULTY
    private float GetDifficultyProgress()
    {
        if (difficultyRampDuration <= 0f)
            return 1f;

        return Mathf.Clamp01(elapsedGameTime / difficultyRampDuration);
    }

    private float GetEnemySpawnInterval()
    {
        return Mathf.Lerp(
            startingEnemyInterval,
            minimumEnemyInterval,
            GetDifficultyProgress()
        );
    }

    private float GetExplosiveSpawnInterval()
    {
        return Mathf.Lerp(
            startingExplosiveInterval,
            minimumExplosiveInterval,
            GetDifficultyProgress()
        );
    }

    // SPAWN QUANTITY
    private int GetEnemySpawnCount()
    {
        int increase = enemyQuantityIncreaseInterval > 0f
            ? Mathf.FloorToInt(elapsedGameTime / enemyQuantityIncreaseInterval)
            : 0;

        return Mathf.Clamp(
            startingEnemyCount + increase,
            startingEnemyCount,
            maximumEnemyCountPerWave
        );
    }

    private int GetExplosiveSpawnCount()
    {
        int increase = explosiveQuantityIncreaseInterval > 0f
            ? Mathf.FloorToInt(elapsedGameTime / explosiveQuantityIncreaseInterval)
            : 0;

        return Mathf.Clamp(
            startingExplosiveCount + increase,
            startingExplosiveCount,
            maximumExplosiveCountPerWave
        );
    }

    // HANDLE PERMANENT DESTRUCTION
    public void OnSpawnedObjectDestroyed(bool isExplosive)
    {
        if (isExplosive)
            activeExplosiveCount = Mathf.Max(0, activeExplosiveCount - 1);
        else
            activeEnemyCount = Mathf.Max(0, activeEnemyCount - 1);
    }

    // STOP SPAWNING
    public void StopSpawning()
    {
        spawningEnabled = false;

        if (enemySpawnRoutine != null)
        {
            StopCoroutine(enemySpawnRoutine);
            enemySpawnRoutine = null;
        }

        if (explosiveSpawnRoutine != null)
        {
            StopCoroutine(explosiveSpawnRoutine);
            explosiveSpawnRoutine = null;
        }
    }

    // RESTART SPAWNING
    public void RestartSpawning()
    {
        InitializeSpawner();
    }

    // RESET EVERYTHING FOR A NEW RUN
    public void RestartSpawner()
    {
        InitializeSpawner();
    }

    private void OnDisable()
    {
        StopSpawning();
    }
}
