using UnityEngine;

public class Tunnel : MonoBehaviour
{
    [Header("Tunnel Exits")]
    [SerializeField] private Collider exit1;
    [SerializeField] private Collider exit2;

    [Header("Tunnel Spawn Positions")]
    [SerializeField] private Transform pos1;
    [SerializeField] private Transform pos2;

    private TunnelPoolManager poolManager;

    private bool exit1Used;
    private bool exit2Used;

    // Sets up this tunnel
    public void Initialize(TunnelPoolManager manager)
    {
        poolManager = manager;
    }

    // Resets the tunnel before it is reused
    public void ResetTunnel()
    {
        exit1Used = false;
        exit2Used = false;
    }

    // Handles player entering an exit
    public void OnPlayerEnteredExit(int exitIndex)
    {
        if (poolManager == null)
            return;

        if (exitIndex == 1)
        {
            if (exit1Used)
                return;

            exit1Used = true;
            poolManager.SpawnTunnel(this, 1);
        }
        else if (exitIndex == 2)
        {
            if (exit2Used)
                return;

            exit2Used = true;
            poolManager.SpawnTunnel(this, 2);
        }
    }

    // Gets the correct spawn position
    public Transform GetSpawnPosition(int exitIndex)
    {
        return exitIndex == 1 ? pos1 : pos2;
    }
}