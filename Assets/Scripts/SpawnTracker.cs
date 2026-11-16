
using UnityEngine;

public class SpawnTracker : MonoBehaviour
{
    private InfinitySpawner spawner;

    private bool isExplosive;
    private bool initialized;
    private bool isInPool;

    public void Initialize(InfinitySpawner owner, bool explosive)
    {
        spawner = owner;
        isExplosive = explosive;
        initialized = true;
        isInPool = false;
    }

    public void ReturnToPool()
    {
        if (!initialized || isInPool || spawner == null)
            return;

        isInPool = true;
        spawner.ReturnToPool(gameObject, isExplosive);
    }

    public void MarkReturnedToPool()
    {
        isInPool = true;
    }

    // Check pool state
    public bool IsInPool()
    {
        return isInPool;
    }

    private void OnDestroy()
    {
        if (!initialized || isInPool || spawner == null)
            return;

        spawner.OnSpawnedObjectDestroyed(isExplosive);
    }
}