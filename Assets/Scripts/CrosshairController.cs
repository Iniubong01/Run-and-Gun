
using UnityEngine;
using DG.Tweening;

public class CrosshairController : MonoBehaviour
{
    // Crosshair Pieces
    [Header("Crosshair Pieces")]
    [SerializeField] private RectTransform top;
    [SerializeField] private RectTransform bottom;
    [SerializeField] private RectTransform left;
    [SerializeField] private RectTransform right;

    // Crosshair Distances
    [Header("Distances")]
    [SerializeField] private float normalDistance = 35f;
    [SerializeField] private float narrowDistance = 10f;

    // Animation
    [Header("Animation")]
    [SerializeField] private float narrowDuration = 0.15f;
    [SerializeField] private float expandDuration = 0.15f;

    // Variables
    private bool isTargetingEnemy;

    // Start
    private void Start()
    {
        SetCrosshairPositions(normalDistance);
    }

    // Set Crosshair Positions
    private void SetCrosshairPositions(float distance)
    {
        top.anchoredPosition = new Vector2(top.anchoredPosition.x, distance);
        bottom.anchoredPosition = new Vector2(bottom.anchoredPosition.x, -distance);

        left.anchoredPosition = new Vector2(-distance, left.anchoredPosition.y);
        right.anchoredPosition = new Vector2(distance, right.anchoredPosition.y);
    }

    // Set Enemy Target
    public void SetEnemyTarget(bool targeting)
    {
        if (isTargetingEnemy == targeting)
            return;

        isTargetingEnemy = targeting;

        KillTweens();

        if (isTargetingEnemy)
        {
            NarrowCrosshair();
        }
        else
        {
            ExpandCrosshair();
        }
    }

    // Narrow Crosshair
    private void NarrowCrosshair()
    {
        top.DOAnchorPosY(narrowDistance, narrowDuration)
            .SetEase(Ease.OutQuad);

        bottom.DOAnchorPosY(-narrowDistance, narrowDuration)
            .SetEase(Ease.OutQuad);

        left.DOAnchorPosX(-narrowDistance, narrowDuration)
            .SetEase(Ease.OutQuad);

        right.DOAnchorPosX(narrowDistance, narrowDuration)
            .SetEase(Ease.OutQuad);
    }

    // Expand Crosshair
    private void ExpandCrosshair()
    {
        top.DOAnchorPosY(normalDistance, expandDuration)
            .SetEase(Ease.OutQuad);

        bottom.DOAnchorPosY(-normalDistance, expandDuration)
            .SetEase(Ease.OutQuad);

        left.DOAnchorPosX(-normalDistance, expandDuration)
            .SetEase(Ease.OutQuad);

        right.DOAnchorPosX(normalDistance, expandDuration)
            .SetEase(Ease.OutQuad);
    }

    // Kill Tweens
    private void KillTweens()
    {
        top.DOKill();
        bottom.DOKill();
        left.DOKill();
        right.DOKill();
    }

    // On Destroy
    private void OnDestroy()
    {
        KillTweens();
    }
}
