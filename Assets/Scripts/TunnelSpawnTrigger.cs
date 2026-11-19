using UnityEngine;
public class TunnelSpawnTrigger : MonoBehaviour
{
   [Header("Exit Settings")]
    [SerializeField] private int exitIndex = 1;

    private Tunnel tunnelPiece;

    private void Awake()
    {
        tunnelPiece = GetComponentInParent<Tunnel>();
    }

    // Detects when the player enters this exit
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (tunnelPiece != null)
            tunnelPiece.OnPlayerEnteredExit(exitIndex);
    }
}