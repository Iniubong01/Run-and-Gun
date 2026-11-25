
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public enum StoreCategory
{
    Gun,
    CharacterSkin,
    Item
}

public enum StoreItemType
{
    Health,
    Teleport,
    ExtraLife,
    Ammo
}

public class PlayerSkin : MonoBehaviour, ISelectHandler, IDeselectHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Upgrade Settings")]
    [SerializeField] private StoreCategory category;
    [SerializeField] public StoreItemType itemType;
    [SerializeField] private int slotIndex;

    [Header("Upgrade Objects")]
    [SerializeField] private GameObject[] upgradeObjects;
    [SerializeField] private GameObject[] itemSlots;

    [Header("Sprite Settings")]
    [SerializeField] private GameObject outline;
    private CanvasGroup canvasGroup;

    public int price;

    [Header("Button Settings")]
    [SerializeField] private Button useButton;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private GameObject priceIconGroup;
    [SerializeField] private GameObject useButtonGroup;
    [SerializeField] private Image inUseImage;
    [SerializeField] private GameObject slotFilledVisual;

    [Header("Upgrade Display")]
    [SerializeField] private GameObject displayImage;

    [Header("Upgrade State")]
    public bool isLocked;
    public bool canBeBought;
    public bool isDeployed;

    private bool isOwned;
    private bool isSlotFilled;
    private bool initiallyOwned;
    private int itemQuantity;

    public StoreCategory Category => category;
    public StoreItemType ItemType => itemType;
    public int SlotIndex => slotIndex;
    public bool IsOwned => isOwned;
    public bool IsSlotFilled => isSlotFilled;
    public bool InitiallyOwned => initiallyOwned;
    public int ItemQuantity => itemQuantity;

    public string upgradeName;
    public string upgradeDescription;

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        initiallyOwned = isDeployed;
    }

    private void Start()
    {
        UpdateLockUI();
    }

    // Selects this upgrade
    private void SelectUpgrade()
    {
        if (StoreManager.Instance == null)
            return;

        StoreManager.Instance.SelectUpgrade(this);
    }

    // Gamepad / Keyboard Logic
    public void OnSelect(BaseEventData eventData)
    {
        SelectUpgrade();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        UpdateLockUI();
    }

    #region Selection Logic

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EventSystem.current == null)
            return;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (EventSystem.current == null)
            return;
    }


    // Updates the upgrade display
    public void UpdateDisplay(bool selected)
    {
        if (outline != null)
            outline.SetActive(selected);

        if (displayImage != null)
            displayImage.SetActive(selected);

        if (selected)
        {
            if (nameText != null)
                nameText.text = upgradeName;

            if (descriptionText != null)
                descriptionText.text = upgradeDescription;
        }
        else
        {
            if (nameText != null)
                nameText.text = "";

            if (descriptionText != null)
                descriptionText.text = "";
        }
    }

    #endregion

    #region Upgrade UI Logic

    // Updates the upgrade state
    public void SetUpgradeState(bool owned, bool deployed, bool filled)
    {
        isOwned = owned;
        isDeployed = deployed;
        isSlotFilled = filled;

        if (category == StoreCategory.Item)
        {
            // isLocked = false;
            canBeBought = itemQuantity < 6;
        }
        else
        {
            // isLocked = !owned;
            canBeBought = !owned;
        }

        UpdateLockUI();
    }

    // Updates the individual upgrade UI
    public void UpdateLockUI()
    {
        bool isItem = category == StoreCategory.Item;

        bool isAvailableToBuy = isItem
            ? itemQuantity < 6
            : canBeBought;

        bool isAvailableToEquip = !isItem && isOwned && !isDeployed;

        if (useButton != null)
        {
            useButton.interactable =
                isAvailableToBuy || isAvailableToEquip;
        }

        if (priceText != null)
            priceText.text = price.ToString();

        if (priceIconGroup != null)
            priceIconGroup.SetActive(isAvailableToBuy);

        if (useButtonGroup != null)
        {
            useButtonGroup.SetActive(
                isAvailableToEquip || (isItem && itemQuantity > 0));
        }

        if (inUseImage != null)
            inUseImage.gameObject.SetActive(!isItem && isDeployed);

        if (slotFilledVisual != null)
            slotFilledVisual.SetActive(isItem && isSlotFilled);
    }

    // Updates the item inventory slots
    public void SetItemSlots(int quantity)
    {
        itemQuantity = Mathf.Clamp(quantity, 0, 6);
        isSlotFilled = itemQuantity >= 6;

        if (itemSlots != null)
        {
            for (int i = 0; i < itemSlots.Length; i++)
            {
                if (itemSlots[i] != null)
                    itemSlots[i].SetActive(i < itemQuantity);
            }
        }

        SetUpgradeState(false, false, isSlotFilled);
    }

    // Activates or deactivates the actual upgrade objects
    public void SetUpgradeObjects(bool active)
    {
        if (upgradeObjects == null)
            return;

        foreach (GameObject obj in upgradeObjects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }

    #endregion
}