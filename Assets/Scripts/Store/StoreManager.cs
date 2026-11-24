
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class StoreManager : MonoBehaviour
{
    public static StoreManager Instance;

    [Header("Purchase / Use Buttons and Images")]
    [SerializeField] private Button gunPurchaseButton;
    [SerializeField] private Button characterPurchaseButton;
    [SerializeField] private Button itemPurchaseButton;

    [SerializeField] private Image gunInUseImage;
    [SerializeField] private Image characterInUseImage;
    [SerializeField] private Image itemInUseImage;

    [Header("Buy / Purchase / Use Button")]
    // Gun
    [SerializeField] private TextMeshProUGUI gunUseButtonText;
    [SerializeField] private TextMeshProUGUI gunPriceText;
    [SerializeField] private GameObject gunPricePanel;

    // Character
    [SerializeField] private TextMeshProUGUI characterUseButtonText;
    [SerializeField] private TextMeshProUGUI characterPriceText;
    [SerializeField] private GameObject characterPricePanel;

    // Item
    [SerializeField] private TextMeshProUGUI itemUseButtonText;
    [SerializeField] private TextMeshProUGUI itemPriceText;
    [SerializeField] private GameObject itemPricePanel;

    [Header("Coins")]
    [SerializeField] private int totalCoins;

    [SerializeField] private TextMeshProUGUI coinText;

    [Header("Insufficient Coins Popup")]
    [SerializeField] private CanvasGroup insufficientCoinsPopup, upgradeUnavailablePopup;
    [SerializeField] private float popupFadeDuration = 0.2f;

    // Int for tracking which gun, character, and item is currently selected
    // Upgrade Buttons
    [Header("Upgrade Buttons")]
    [SerializeField] private PlayerSkin[] gunUpgradeButtons;
    [SerializeField] private PlayerSkin[] characterUpgradeButtons;
    [SerializeField] private PlayerSkin[] itemUpgradeButtons;

    // Selected Upgrade Indices
    [Header("Selected Upgrade Indices")]
    [SerializeField] private int selectedGun;
    [SerializeField] private int selectedCharacter;
    [SerializeField] private int selectedItem;

    // Player Prefs Keys
    private const string GUN_KEY = "SelectedGun";
    private const string CHARACTER_KEY = "SelectedCharacter";
    private const string ITEM_KEY = "SelectedItem";
    private const string COINS_KEY = "PlayerCoins";

    private Tween popupTween;

    private const int MAX_ITEM_QUANTITY = 6;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Load the saved coin balance
        totalCoins = PlayerPrefs.GetInt(COINS_KEY, totalCoins);

        selectedGun = PlayerPrefs.GetInt(GUN_KEY, 0);
        selectedCharacter = PlayerPrefs.GetInt(CHARACTER_KEY, 0);
        selectedItem = PlayerPrefs.GetInt(ITEM_KEY, 0);

        if (insufficientCoinsPopup != null)
        {
            insufficientCoinsPopup.alpha = 0f;
            insufficientCoinsPopup.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        InitializePurchaseButtonListeners();
        UpdatePurchaseButtons();
        LoadUpgradeSelections();

        ActivateSelectedUpgrades();
        // UpdateItemHUD();
        UpdateCoinUI();
    }

    private void OnDestroy()
    {
        if (popupTween != null)
            popupTween.Kill();

        if (Instance == this)
            Instance = null;
    }

    // Initializes the purchase button listeners
    private void InitializePurchaseButtonListeners()
    {
        if (gunPurchaseButton != null)
            gunPurchaseButton.onClick.AddListener(PurchaseOrEquipGun);

        if (characterPurchaseButton != null)
            characterPurchaseButton.onClick.AddListener(PurchaseOrEquipCharacter);

        if (itemPurchaseButton != null)
            itemPurchaseButton.onClick.AddListener(PurchaseItem);

        foreach(PlayerSkin upgrade in gunUpgradeButtons)
        {
            if (upgrade != null)
            
                upgrade.GetComponent<Button>().onClick.AddListener(() => SelectUpgrade(upgrade));
                upgrade.GetComponent<Button>().onClick.AddListener(() => RefreshButton(upgrade));
        }

        foreach(PlayerSkin upgrade in characterUpgradeButtons)
        {
            if (upgrade != null)

                upgrade.GetComponent<Button>().onClick.AddListener(() => SelectUpgrade(upgrade));
                upgrade.GetComponent<Button>().onClick.AddListener(() => RefreshButton(upgrade));
        }

        foreach(PlayerSkin upgrade in itemUpgradeButtons)
        {
            if (upgrade != null)
            
                upgrade.GetComponent<Button>().onClick.AddListener(() => SelectUpgrade(upgrade));
                upgrade.GetComponent<Button>().onClick.AddListener(() => RefreshButton(upgrade));
        }
    }

    #region Upgrade Selection

    // Selects an upgrade
    public void SelectUpgrade(PlayerSkin upgrade)
    {
        if (upgrade == null)
            return;

        switch (upgrade.Category)
        {
            case StoreCategory.Gun:
                selectedGun = GetUpgradeIndex(gunUpgradeButtons, upgrade);

                if (selectedGun == -1)
                    return;

                UpdateUpgradeSelection(gunUpgradeButtons, upgrade);
                break;

            case StoreCategory.CharacterSkin:
                selectedCharacter = GetUpgradeIndex(characterUpgradeButtons, upgrade);

                if (selectedCharacter == -1)
                    return;

                UpdateUpgradeSelection(characterUpgradeButtons, upgrade);
                break;

            case StoreCategory.Item:
                selectedItem = GetUpgradeIndex(itemUpgradeButtons, upgrade);

                if (selectedItem == -1)
                    return;

                UpdateUpgradeSelection(itemUpgradeButtons, upgrade);
                break;
        }

        // Updates the purchase buttons immediately
        UpdatePurchaseButtons();
    }

    // saves the selected upgrades
    public void SaveStoreSelections()
    {
        PlayerPrefs.SetInt(GUN_KEY, selectedGun);
        PlayerPrefs.SetInt(CHARACTER_KEY, selectedCharacter);
        PlayerPrefs.SetInt(ITEM_KEY, selectedItem);
        PlayerPrefs.SetInt(COINS_KEY, totalCoins);

        // UpdateItemHUD();
        PlayerPrefs.Save();
    }

    // Activates the selected upgrade outlines and display images
    private void ActivateSelectedUpgrades()
    {
        if (gunUpgradeButtons != null && gunUpgradeButtons.Length > 0)
        {
            selectedGun = Mathf.Clamp(selectedGun, 0, gunUpgradeButtons.Length - 1);

            PlayerSkin gun = gunUpgradeButtons[selectedGun];

            if (gun != null)
            {
                SelectUpgrade(gun);
                gun.UpdateDisplay(true);
            }
        }

        if (characterUpgradeButtons != null && characterUpgradeButtons.Length > 0)
        {
            selectedCharacter = Mathf.Clamp(selectedCharacter, 0, characterUpgradeButtons.Length - 1);

            PlayerSkin character = characterUpgradeButtons[selectedCharacter];

            if (character != null)
            {
                SelectUpgrade(character);
                character.UpdateDisplay(true);
            }
        }

        if (itemUpgradeButtons != null && itemUpgradeButtons.Length > 0)
        {
            selectedItem = Mathf.Clamp(selectedItem, 0, itemUpgradeButtons.Length - 1);

            PlayerSkin item = itemUpgradeButtons[selectedItem];

            if (item != null)
            {
                SelectUpgrade(item);
                item.UpdateDisplay(true);
            }
        }
    }

    // Gets the position of an upgrade inside its category
    private int GetUpgradeIndex(PlayerSkin[] upgrades, PlayerSkin upgrade)
    {
        if (upgrades == null)
            return -1;

        for (int i = 0; i < upgrades.Length; i++)
        {
            if (upgrades[i] == upgrade)
                return i;
        }

        return -1;
    }

    // Updates the selected upgrade outline and display
    private void UpdateUpgradeSelection(PlayerSkin[] upgrades, PlayerSkin selectedUpgrade)
    {
        if (upgrades == null)
            return;

        foreach (PlayerSkin upgrade in upgrades)
        {
            if (upgrade == null)
                continue;

            bool isSelected = upgrade == selectedUpgrade;

            upgrade.UpdateDisplay(isSelected);
        }
    }

    // Refreshes an upgrade button
    public void RefreshButton(PlayerSkin upgrade)
    {
        if (upgrade == null)
            return;

        PlayerSkin[] upgrades = GetUpgradeArray(upgrade.Category);
        int selectedIndex = GetSelectedIndex(upgrade.Category);

        if (upgrades == null || upgrades.Length == 0)
        {
            upgrade.UpdateLockUI();
            return;
        }

        selectedIndex = Mathf.Clamp(selectedIndex, 0, upgrades.Length - 1);

        bool isSelected = upgrades[selectedIndex];

        upgrade.UpdateDisplay(isSelected);

        LoadUpgradeState(upgrade);
    }


    // Loads saved upgrade selections
    private void LoadUpgradeSelections()
    {
        selectedGun = PlayerPrefs.GetInt(GUN_KEY, 0);
        selectedCharacter = PlayerPrefs.GetInt(CHARACTER_KEY, 0);
        selectedItem = PlayerPrefs.GetInt(ITEM_KEY, 0);
    }

    // Gets the upgrade array for a category
    private PlayerSkin[] GetUpgradeArray(StoreCategory category)
    {
        switch (category)
        {
            case StoreCategory.Gun:
                return gunUpgradeButtons;

            case StoreCategory.CharacterSkin:
                return characterUpgradeButtons;

            case StoreCategory.Item:
                return itemUpgradeButtons;

            default:
                return null;
        }
    }

    // Gets the selected index for a category
    private int GetSelectedIndex(StoreCategory category)
    {
        switch (category)
        {
            case StoreCategory.Gun:
                return selectedGun;

            case StoreCategory.CharacterSkin:
                return selectedCharacter;

            case StoreCategory.Item:
                return selectedItem;

            default:
                return 0;
        }
    }

    // Gets the selected upgrade
    private PlayerSkin GetSelectedUpgrade(StoreCategory category)
    {
        PlayerSkin[] upgrades = GetUpgradeArray(category);
        int index = GetSelectedIndex(category);

        if (upgrades == null || upgrades.Length == 0)
            return null;

        index = Mathf.Clamp(index, 0, upgrades.Length - 1);

        return upgrades[index];
    }

    #endregion

    #region Upgrade Handling

    // Purchases or equips the selected gun
    public void PurchaseOrEquipGun()
    {
        PlayerSkin upgrade = GetSelectedUpgrade(StoreCategory.Gun);

        if (upgrade == null)
            return;

        if (!upgrade.IsOwned)
        {
            PurchaseUpgrade(upgrade);
            return;
        }

        EquipUpgrade(upgrade);
    }

    // Purchases or equips the selected character
    public void PurchaseOrEquipCharacter()
    {
        PlayerSkin upgrade = GetSelectedUpgrade(StoreCategory.CharacterSkin);

        if (upgrade == null)
            return;

        if (!upgrade.IsOwned)
        {
            PurchaseUpgrade(upgrade);
            return;
        }

        EquipUpgrade(upgrade);
    }

    // Purchases the selected item
    public void PurchaseItem()
    {
        if (itemUpgradeButtons == null ||
            selectedItem < 0 ||
            selectedItem >= itemUpgradeButtons.Length)
            return;

        PlayerSkin upgrade = itemUpgradeButtons[selectedItem];

        if (upgrade == null)
            return;

        int quantity = GetItemQuantity(upgrade);

        if (quantity >= MAX_ITEM_QUANTITY)
        {
            upgrade.SetItemSlots(quantity);
            UpdatePurchaseButtons();
            return;
        }

        PurchaseUpgrade(upgrade);
    }

    // Purchases an upgrade using coins
    private void PurchaseUpgrade(PlayerSkin upgrade)
    {
        if (upgrade == null)
            return;
        
        if (upgrade.isLocked)
        {
            ShowUnavailablePopup();
            return;
        }

        if (totalCoins < upgrade.price)
        {
            ShowInsufficientPopup();
            return;
        }

        BuyFromBalance(upgrade.price);

        if (upgrade.Category == StoreCategory.Item)
        {
            int quantity = GetItemQuantity(upgrade);
            quantity++;

            SaveItemQuantity(upgrade, quantity);
            upgrade.SetItemSlots(quantity);

            // UpdateItemHUD();
        }
        else
        {
            SaveOwnership(upgrade, true);
            upgrade.SetUpgradeState(true, false, false);
        }

        PlayerPrefs.Save();

        UpdatePurchaseButtons();
    }

    // Equips an owned upgrade
    private void EquipUpgrade(PlayerSkin upgrade)
    {
        if (upgrade == null || !upgrade.IsOwned)
            return;

        PlayerSkin[] upgrades = GetUpgradeArray(upgrade.Category);

        if (upgrades == null)
            return;

        foreach (PlayerSkin otherUpgrade in upgrades)
        {
            if (otherUpgrade == null)
                continue;

            bool isEquipped = otherUpgrade == upgrade;

            otherUpgrade.SetUpgradeObjects(isEquipped);
            otherUpgrade.SetUpgradeState(
                otherUpgrade.IsOwned,
                isEquipped,
                false);
        }

        SaveEquippedUpgrade(upgrade.Category, GetUpgradeIndex(upgrades, upgrade));

        PlayerPrefs.Save();

        UpdatePurchaseButtons();
    }

    // Saves upgrade ownership
    private void SaveOwnership(PlayerSkin upgrade, bool owned)
    {
        string key = GetOwnershipKey(upgrade);

        if (!string.IsNullOrEmpty(key))
            PlayerPrefs.SetInt(key, owned ? 1 : 0);
    }

    // Loads upgrade ownership and item quantity
    private void LoadUpgradeState(PlayerSkin upgrade)
    {
        if (upgrade.Category == StoreCategory.Item)
        {
            int quantity = GetItemQuantity(upgrade);
            upgrade.SetItemSlots(quantity);
            return;
        }

        string key = GetOwnershipKey(upgrade);

        bool owned = upgrade.InitiallyOwned ||
                     PlayerPrefs.GetInt(key, 0) == 1;

        int equippedIndex = GetSelectedEquippedIndex(upgrade.Category);
        int currentIndex = GetUpgradeIndex(
            GetUpgradeArray(upgrade.Category), upgrade);

        bool deployed = owned && equippedIndex == currentIndex;

        upgrade.SetUpgradeState(owned, deployed, false);

        if (deployed)
            upgrade.SetUpgradeObjects(true);
    }

    // Gets the ownership preference key
    private string GetOwnershipKey(PlayerSkin upgrade)
    {
        switch (upgrade.Category)
        {
            case StoreCategory.Gun:
                return "GunOwned_" + GetUpgradeIndex(gunUpgradeButtons, upgrade);

            case StoreCategory.CharacterSkin:
                return "CharacterOwned_" + GetUpgradeIndex(characterUpgradeButtons, upgrade);

            default:
                return string.Empty;
        }
    }

    // Gets the equipped upgrade index
    private int GetSelectedEquippedIndex(StoreCategory category)
    {
        return PlayerPrefs.GetInt(
            category == StoreCategory.Gun
                ? "EquippedGun"
                : "EquippedCharacter",
            -1);
    }

    // Saves the equipped upgrade index
    private void SaveEquippedUpgrade(StoreCategory category, int index)
    {
        PlayerPrefs.SetInt(
            category == StoreCategory.Gun
                ? "EquippedGun"
                : "EquippedCharacter",
            index);
    }

    // Gets the item quantity
    private int GetItemQuantity(PlayerSkin upgrade)
    {
        int index = GetUpgradeIndex(itemUpgradeButtons, upgrade);

        return PlayerPrefs.GetInt("ItemQuantity_" + index, 0);
    }

    // Saves the item quantity
    private void SaveItemQuantity(PlayerSkin upgrade, int quantity)
    {
        int index = GetUpgradeIndex(itemUpgradeButtons, upgrade);

        PlayerPrefs.SetInt(
            "ItemQuantity_" + index,
            Mathf.Clamp(quantity, 0, MAX_ITEM_QUANTITY));
    }

    // Updates the purchase buttons
    private void UpdatePurchaseButtons()
    {
        PlayerSkin gun = GetSelectedUpgrade(StoreCategory.Gun);
        PlayerSkin character = GetSelectedUpgrade(StoreCategory.CharacterSkin);
        PlayerSkin item = GetSelectedUpgrade(StoreCategory.Item);

        UpdateCategoryButton(gunPurchaseButton, gunUseButtonText, gunPriceText, gunPricePanel, gun, false);

        UpdateCategoryButton(characterPurchaseButton, characterUseButtonText, characterPriceText,
            characterPricePanel, character, false);

        UpdateCategoryButton(itemPurchaseButton, itemUseButtonText, itemPriceText, itemPricePanel, item, true);
    }

    // Updates a category purchase button
    private void UpdateCategoryButton(Button button, TextMeshProUGUI buttonText, TextMeshProUGUI priceText, GameObject pricePanel, PlayerSkin upgrade, bool isItem)
    {
        if (upgrade == null)
        {
            if (button != null)
                button.interactable = false;

            if (pricePanel != null)
                pricePanel.SetActive(false);

            return;
        }

        int quantity = isItem ? GetItemQuantity(upgrade) : 0;

        if (priceText != null)
            priceText.text = upgrade.price.ToString();

        if (pricePanel != null)
            pricePanel.SetActive(
                isItem ? quantity < MAX_ITEM_QUANTITY : !upgrade.IsOwned);

        if (buttonText != null)
        {
            if (isItem)
                buttonText.text = quantity >= MAX_ITEM_QUANTITY
                    ? "Full"
                    : "Buy";
            else if (!upgrade.IsOwned)
                buttonText.text = "Use";
            else if (upgrade.isDeployed)
                buttonText.text = "";
            else
                buttonText.text = "Use";
        }

        if (button != null)
            button.interactable = isItem ? quantity < MAX_ITEM_QUANTITY : !upgrade.IsOwned || !upgrade.isDeployed;
    }

    // Updates the coin display
    public void UpdateCoinUI()
    {
        if (coinText != null)
            coinText.text = totalCoins.ToString();
    }

    // Deducts coins from the player balance
    public void BuyFromBalance(int purchaseAmount)
    {
        if (purchaseAmount < 0 || totalCoins < purchaseAmount)
            return;

        totalCoins -= purchaseAmount;

        PlayerPrefs.SetInt(COINS_KEY, totalCoins);
        PlayerPrefs.Save();

        UpdateCoinUI();
    }

    // Adds coins to the player's total coin balance
    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        totalCoins += amount;

        PlayerPrefs.SetInt(COINS_KEY, totalCoins);
        PlayerPrefs.Save();

        UpdateCoinUI();
    }

    // Shows the insufficient coins / Unavailable popup
    private void ShowInsufficientPopup()
    {
        if (insufficientCoinsPopup == null)
            return;

        if (popupTween != null)
            popupTween.Kill();

        insufficientCoinsPopup.gameObject.SetActive(true);
        insufficientCoinsPopup.alpha = 0f;

        popupTween = insufficientCoinsPopup
            .DOFade(1f, popupFadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                popupTween = insufficientCoinsPopup
                    .DOFade(0f, popupFadeDuration)
                    .SetDelay(1.5f)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        insufficientCoinsPopup.gameObject.SetActive(false);
                    });
            });
    }

    private void ShowUnavailablePopup()
    {
        if (upgradeUnavailablePopup == null)
            return;

        if (popupTween != null)
            popupTween.Kill();

        upgradeUnavailablePopup.gameObject.SetActive(true);
        upgradeUnavailablePopup.alpha = 0f;

        popupTween = upgradeUnavailablePopup
            .DOFade(1f, popupFadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                popupTween = upgradeUnavailablePopup
                    .DOFade(0f, popupFadeDuration)
                    .SetDelay(1.5f)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        upgradeUnavailablePopup.gameObject.SetActive(false);
                    });
            });
    }
    #endregion


    #region Item Usa

    // Uses an item from the player's inventory
    public bool UseItem(StoreItemType itemType)
    {
        if (itemUpgradeButtons == null)
            return false;

        foreach (PlayerSkin upgrade in itemUpgradeButtons)
        {
            if (upgrade == null || upgrade.ItemType != itemType)
                continue;

            int quantity = GetItemQuantity(upgrade);

            if (quantity <= 0)
                return false;

            quantity--;

            SaveItemQuantity(upgrade, quantity);
            upgrade.SetItemSlots(quantity);
            PlayerPrefs.Save();

            UpdatePurchaseButtons();
            // UpdateItemHUD();

            return true;
        }

        return false;
    }

    #endregion

    // Gets the quantity of a specific item
    public int GetItemCount(StoreItemType itemType)
    {
        if (itemUpgradeButtons == null)
            return 0;

        foreach (PlayerSkin upgrade in itemUpgradeButtons)
        {
            if (upgrade == null || upgrade.ItemType != itemType)
                continue;

            return GetItemQuantity(upgrade);
        }

        return 0;
    }

    // Add item quantity
    public void AddItemQuantity(StoreItemType itemType, int amount)
    {
        if (amount <= 0)
            return;

        PlayerSkin upgrade = null;

        // Find the item upgrade
        foreach (PlayerSkin item in itemUpgradeButtons)
        {
            if (item != null && item.itemType == itemType)
            {
                upgrade = item;
                break;
            }
        }

        if (upgrade == null)
            return;

        // Get current item quantity
        int quantity = GetItemQuantity(upgrade);

        // Increase item quantity
        quantity += amount;

        // Save item quantity
        SaveItemQuantity(upgrade, quantity);

        // Save PlayerPrefs
        PlayerPrefs.Save();

        // Update item HUD
        // UpdateItemHUD();
    }

    // Loads and updates the purchase / use button states
    public void LoadStoreButtonStates()
    {
        // Load saved selected upgrade indices
        selectedGun = PlayerPrefs.GetInt(GUN_KEY, 0);
        selectedCharacter = PlayerPrefs.GetInt(CHARACTER_KEY, 0);
        selectedItem = PlayerPrefs.GetInt(ITEM_KEY, 0);

        // Update selected upgrade displays
        ActivateSelectedUpgrades();

        // Load the saved upgrade states
        foreach (PlayerSkin upgrade in gunUpgradeButtons)
        {
            if (upgrade != null)
                LoadUpgradeState(upgrade);
        }

        foreach (PlayerSkin upgrade in characterUpgradeButtons)
        {
            if (upgrade != null)
                LoadUpgradeState(upgrade);
        }

        foreach (PlayerSkin upgrade in itemUpgradeButtons)
        {
            if (upgrade != null)
                LoadUpgradeState(upgrade);
        }

        // Update purchase / use buttons
        UpdatePurchaseButtons();

        // Update item HUD
        // UpdateItemHUD();

        // Update coin UI
        UpdateCoinUI();
    }
}