
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using PlayerControllers;
using TMPro;

[System.Serializable]
public class CharacterEquipment
{
    public GameObject[] characterObjects;
}

[System.Serializable]
public class GunEquipment
{
    public GameObject[] gunObjects;
}

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

    // ENEMY TIERS
    [Header("Enemy Tiers")]
    public SpawnTier[] enemyTiers = new SpawnTier[4];

    // EXPLOSIVE TIERS
    [Header("Explosive Tiers")]
    public SpawnTier[] explosiveTiers = new SpawnTier[4];

    [Header("Spawn Validation")]
    [Min(0.1f)]
    public float spawnCheckRadius = 0.5f;

    [Min(1f)]
    public float groundCheckDistance = 10f;
    [Min(1f)]
    public float minimumPlayerDistance = 10f;

    [Header("Player")]
    public Transform player;

    [Header("Character Equipment")]
    [SerializeField] private CharacterEquipment[] characters;

    [Header("Gun Equipment")]
    [SerializeField] private GunEquipment[] guns;

    private const string CHARACTER_KEY = "EquippedCharacter";
    private const string GUN_KEY = "EquippedGun";

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

    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;

    public GameObject cam;
    [Header("Game Timer")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float elapsedTime;

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

    [Header("Loading UI")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private Text progressText;
    public CanvasGroup loadingCanvas;

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

    // HUD
    [Header("Item HUD Quantity")]
    [SerializeField] private TextMeshProUGUI healthQuantityText;
    [SerializeField] private TextMeshProUGUI ammoQuantityText;
    [SerializeField] private TextMeshProUGUI extraLifeQuantityText;
    [SerializeField] private TextMeshProUGUI teleportQuantityText;

    // Object pooling
    private class PooledObjectInfo
    {
        public GameObject prefab;
        public bool isExplosive;
    }

    private readonly Dictionary<GameObject, Queue<GameObject>> objectPools =
        new Dictionary<GameObject, Queue<GameObject>>();

    private readonly Dictionary<GameObject, PooledObjectInfo> activeObjects =
        new Dictionary<GameObject, PooledObjectInfo>();

    private void Awake()
    {
        elapsedTime = 0f;
    }

    private void Start()
    {
        LoadEquippedItems();

        StartCoroutine(PlayIntroSequence());
        FindPlayer();
        InitializeSpawner();
        UpdateItemHUD();
        UpdateTimerUI();
    }

    #region Loading screen
    // Intro sequence
    private IEnumerator PlayIntroSequence()
    {
        // Show loading screen.
        if (loadingCanvas != null)
        {
            loadingCanvas.gameObject.SetActive(true);
            loadingCanvas.alpha = 1f;
            loadingCanvas.interactable = true;
            loadingCanvas.blocksRaycasts = true;
        }

        if (progressBar != null)
            progressBar.value = 0f;

        if (progressText != null)
            progressText.text = "0%";

        yield return StartCoroutine(AnimateLoadingSlider());

        // Fade out loading screen.
        if (loadingCanvas != null)
        {
            yield return loadingCanvas.DOFade(0f, 0.2f)
                .SetEase(Ease.InOutSine)
                .WaitForCompletion();

            loadingCanvas.gameObject.SetActive(false);
            loadingCanvas.alpha = 0f;
            loadingCanvas.interactable = false;
            loadingCanvas.blocksRaycasts = false;
        }
        
        GameManager.Instance.isGameRunning = true;
    }

    private IEnumerator AnimateLoadingSlider()
    {
        if (progressBar == null || progressText == null)
            yield break;

        float displayedProgress = 0f;

        while (displayedProgress < 1f)
        {
            float pauseDuration = Random.Range(0.2f, 0.35f);
            yield return new WaitForSeconds(pauseDuration);

            float segmentDuration = Random.Range(0.1f, 0.3f);
            float segmentStart = displayedProgress;
            float segmentEnd = Mathf.Min(1f, segmentStart + Random.Range(0.12f, 0.28f));

            float elapsed = 0f;

            while (elapsed < segmentDuration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / segmentDuration);

                displayedProgress = Mathf.Lerp(
                    segmentStart,
                    segmentEnd,
                    Mathf.SmoothStep(0f, 1f, t)
                );

                progressBar.value = displayedProgress;
                progressText.text = Mathf.RoundToInt(displayedProgress * 100f) + "%";

                yield return null;
            }

            displayedProgress = segmentEnd;
            progressBar.value = displayedProgress;
            progressText.text = Mathf.RoundToInt(displayedProgress * 100f) + "%";
        }

        progressBar.value = 1f;
        progressText.text = "100%";

        yield return new WaitForSeconds(0.4f);
    }
    #endregion

    private void FindPlayer()
    {
        if (player != null)
            return;

        FPP_PlayerController controller = GameObject.Find("Player").GetComponent<FPP_PlayerController>();

        if (controller != null)
        {
            controller.gameObject.SetActive(true);
            cam = GameObject.Find("UI Camera");
            cam.SetActive(false);
            player = controller.transform; 
        }
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
        enemyTier2Time = Random.Range(enemyTier2UnlockTime.x, enemyTier2UnlockTime.y);
        enemyTier3Time = enemyTier2Time + Random.Range(enemyTier3UnlockTime.x, enemyTier3UnlockTime.y);
        enemyTier4Time = enemyTier3Time + Random.Range(enemyTier4UnlockTime.x, enemyTier4UnlockTime.y);

        // Independent explosive unlock schedules
        explosiveTier2Time = Random.Range(explosiveTier2UnlockTime.x, explosiveTier2UnlockTime.y);
        explosiveTier3Time = explosiveTier2Time + Random.Range(explosiveTier3UnlockTime.x, explosiveTier3UnlockTime.y);
        explosiveTier4Time = explosiveTier3Time + Random.Range(explosiveTier4UnlockTime.x, explosiveTier4UnlockTime.y);

        spawningEnabled = true;

        enemySpawnRoutine = StartCoroutine(EnemySpawner());
        explosiveSpawnRoutine = StartCoroutine(ExplosiveSpawner());

        Debug.Log("Endless Spawner Initialized!");
    }

    // Loads the equipped character and gun
    private void LoadEquippedItems()
    {
        int selectedCharacter = PlayerPrefs.GetInt(CHARACTER_KEY, 0);
        int selectedGun = PlayerPrefs.GetInt(GUN_KEY, 0);

        ActivateCharacter(selectedCharacter);
        ActivateGun(selectedGun);
    }

    // Activates the selected character
    private void ActivateCharacter(int index)
    {
        if (characters == null || characters.Length == 0)
            return;

        // Deactivates all character objects
        foreach (CharacterEquipment character in characters)
        {
            if (character == null || character.characterObjects == null)
                continue;

            foreach (GameObject obj in character.characterObjects)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        // Checks the selected character index
        if (index < 0 || index >= characters.Length)
        {
            Debug.LogWarning("Selected character index is invalid: " + index);
            return;
        }

        CharacterEquipment selected = characters[index];

        if (selected == null || selected.characterObjects == null)
            return;

        // Activates the selected character objects
        foreach (GameObject obj in selected.characterObjects)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }

    // Activates the selected gun
    private void ActivateGun(int index)
    {
        if (guns == null || guns.Length == 0)
            return;

        // Deactivates all gun objects
        foreach (GunEquipment gun in guns)
        {
            if (gun == null || gun.gunObjects == null)
                continue;

            foreach (GameObject obj in gun.gunObjects)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        // Checks the selected gun index
        if (index < 0 || index >= guns.Length)
        {
            Debug.LogWarning("Selected gun index is invalid: " + index);
            return;
        }

        GunEquipment selected = guns[index];

        if (selected == null || selected.gunObjects == null)
            return;

        // Activates the selected gun objects
        foreach (GameObject obj in selected.gunObjects)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }

    private void Update()
    {
        if (!spawningEnabled)
            return;

        elapsedGameTime += Time.deltaTime;

        UpdateEnemyTierUnlocks();
        UpdateExplosiveTierUnlocks();
    
        if (GameManager.Instance.isGameRunning)
            elapsedTime += Time.deltaTime;

        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
            timerText.text = GameManager.Instance.FormatTime(elapsedTime);
    }

    public void UpdateItemHUD()
    {
        if (healthQuantityText != null)
            healthQuantityText.text = StoreManager.Instance.GetItemCount(StoreItemType.Health).ToString();

        if (ammoQuantityText != null)
            ammoQuantityText.text = StoreManager.Instance.GetItemCount(StoreItemType.Ammo).ToString();

        if (extraLifeQuantityText != null)
            extraLifeQuantityText.text = StoreManager.Instance.GetItemCount(StoreItemType.ExtraLife).ToString();

        if (teleportQuantityText != null)
            teleportQuantityText.text = StoreManager.Instance.GetItemCount(StoreItemType.Teleport).ToString();
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
            UI_Manager.Instance.ShowAlert();
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
        SpawnTier tier = GetRandomUnlockedTier(enemyTiers, unlockedEnemyTier);

        if (tier == null)
            return;

        GameObject prefab = GetRandomPrefab(tier);

        if (prefab == null)
            return;

        if (!TryGetRandomSpawnPosition(out Vector3 position))
            return;

        GameObject enemy = GetPooledObject(prefab, position, Quaternion.identity, false);

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
    
    // Spawn obstacles
    private void SpawnExplosive()
    {
        Debug.Log("Attempting to spawn obstacle...");

        SpawnTier tier = GetRandomUnlockedTier(explosiveTiers,  unlockedExplosiveTier);

        if (tier == null)
            return;

        GameObject prefab = GetRandomPrefab(tier);

        if (prefab == null)
            return;

        if (!TryGetRandomSpawnPosition(out Vector3 position))
            return;

        GameObject explosive = GetPooledObject(prefab, position, Quaternion.identity, true);

        if (explosive == null)
        {
            Debug.LogWarning("Obstacle spawn failed: GetPooledObject returned null.");
            return;
        }

        activeExplosiveCount++;
        Debug.Log("Obstacle successfully spawned: " + explosive.name);
    }

    // Get object from pool
    private GameObject GetPooledObject(GameObject prefab, Vector3 position, Quaternion rotation, bool isExplosive)
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
            SpawnTracker newTracker = spawnedObject.GetComponent<SpawnTracker>();

            if (newTracker == null)
                newTracker = spawnedObject.AddComponent<SpawnTracker>();

            newTracker.Initialize(this, isExplosive);
        }

        spawnedObject.transform.SetPositionAndRotation(position, rotation);
        SpawnTracker tracker = spawnedObject.GetComponent<SpawnTracker>();

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
        SpawnTracker tracker = spawnedObject.GetComponent<SpawnTracker>();

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
    private bool TryGetRandomSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        if (player == null)
            return false;

        float minimumDistance = Mathf.Max(0.1f, spawnDistanceFromPlayer - spawnDistanceVariation);

        float maximumDistance = Mathf.Max(minimumDistance, spawnDistanceFromPlayer + spawnDistanceVariation );

        float lowerZ = Mathf.Min(minZ, maxZ);
        float upperZ = Mathf.Max(minZ, maxZ);

        for (int i = 0; i < 20; i++)
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

            Vector3 randomPosition = new Vector3(randomX, player.position.y, randomZ);

            // Checks for the ground
            Vector3 rayOrigin = randomPosition +
                Vector3.up * groundCheckDistance;

            if (!Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit groundHit,
                groundCheckDistance * 2f,
                groundLayer,
                QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            Vector3 candidate = groundHit.point;

            // Checks if the spawn position is occupied
            if (Physics.CheckSphere(
                candidate + Vector3.up * spawnCheckRadius,
                spawnCheckRadius,
                obstacleLayer,
                QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            // Checks minimum player distance
            if (Vector3.Distance(candidate, player.position) <
                minimumPlayerDistance)
            {
                continue;
            }

            spawnPosition = candidate;
            return true;
        }

        return false;
    }

    // Tier selection
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
        GameManager.Instance.isGameRunning = false;

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

    // Restart spawning
    public void RestartSpawning()
    {
        InitializeSpawner();
    }

    // Reset everything for a new run
    public void RestartSpawner()
    {
        InitializeSpawner();
    }

    private void OnDisable()
    {
        StopSpawning();
    }
}