using System.Collections.Generic;
using UnityEngine;

public class TunnelPoolManager : MonoBehaviour
{
    [Header("Tunnel Settings")]
    [SerializeField] private Tunnel tunnelPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private Transform startingTunnel;

    [Header("Pool Settings")]
    [SerializeField] private int poolSize = 10;

    [Header("Return Settings")]
    [SerializeField] public float distanceFromTunnelBeforeReturning = 100f;

    private readonly Queue<Tunnel> tunnelPool = new Queue<Tunnel>();
    private readonly List<Tunnel> activeTunnels = new List<Tunnel>();

    private void Start()
    {
        CreatePool();

        // Registers the starting tunnel
        if (startingTunnel != null)
        {
            Tunnel startingPiece = startingTunnel.GetComponent<Tunnel>();

            if (startingPiece != null)
            {
                startingPiece.Initialize(this);
                startingPiece.ResetTunnel();

                if (!startingPiece.gameObject.activeSelf)
                    startingPiece.gameObject.SetActive(true);

                activeTunnels.Add(startingPiece);
            }
        }
    }

    private void Update()
    {
        ReturnDistantTunnels();
    }

    // Creates the tunnel pool
    private void CreatePool()
    {
        if (tunnelPrefab == null)
        {
            Debug.LogError("Tunnel prefab is missing!");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            Tunnel tunnel = Instantiate(tunnelPrefab, transform);

            tunnel.Initialize(this);
            tunnel.ResetTunnel();
            tunnel.gameObject.SetActive(false);

            tunnelPool.Enqueue(tunnel);
        }
    }

    // Spawns a tunnel from the pool
    public void SpawnTunnel(Tunnel sourceTunnel, int exitIndex)
    {
        if (sourceTunnel == null)
            return;

        Transform spawnPosition = sourceTunnel.GetSpawnPosition(exitIndex);

        if (spawnPosition == null)
        {
            Debug.LogWarning("Tunnel spawn position is missing!");
            return;
        }

        Tunnel newTunnel = GetPooledTunnel();

        if (newTunnel == null)
        {
            Debug.LogWarning("No tunnel available in the pool!");
            return;
        }

        newTunnel.transform.SetPositionAndRotation(
            spawnPosition.position,
            spawnPosition.rotation
        );

        newTunnel.Initialize(this);
        newTunnel.ResetTunnel();

        newTunnel.gameObject.SetActive(true);

        activeTunnels.Add(newTunnel);
    }

    // Gets an available tunnel from the pool
    private Tunnel GetPooledTunnel()
    {
        if (tunnelPool.Count > 0)
            return tunnelPool.Dequeue();

        return null;
    }

    // Returns tunnels that are too far from the player
    private void ReturnDistantTunnels()
    {
        if (player == null)
            return;

        for (int i = activeTunnels.Count - 1; i >= 0; i--)
        {
            Tunnel tunnel = activeTunnels[i];

            if (tunnel == null)
            {
                activeTunnels.RemoveAt(i);
                continue;
            }

            float distance = Vector3.Distance(
                player.position,
                tunnel.transform.position
            );

            if (distance > distanceFromTunnelBeforeReturning)
                ReturnTunnel(tunnel);
        }
    }

    // Returns a tunnel to the pool
    private void ReturnTunnel(Tunnel tunnel)
    {
        if (tunnel == null)
            return;

        activeTunnels.Remove(tunnel);

        tunnel.ResetTunnel();
        tunnel.gameObject.SetActive(false);

        tunnelPool.Enqueue(tunnel);
    }
}