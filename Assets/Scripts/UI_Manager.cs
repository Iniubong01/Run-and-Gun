
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using PlayerControllers;

public class UI_Manager : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private CanvasGroup splashCanvas;
    [SerializeField] private CanvasGroup loadingCanvas;
    [SerializeField] private CanvasGroup mainMenuCanvas;
    [SerializeField] private CanvasGroup GameOverCanvasI, GameOverCanvasII;

    [Header("Loading UI")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private Text progressText;

    [Header("Splash Settings")]
    [SerializeField] private float splashDuration = 2.5f;
    [SerializeField] private float splashFadeDuration = 1f, fadeDuration = 0.4f;

    [Header("Loading Settings")]
    [SerializeField] private float loadingDuration = 4f;
    [SerializeField] private float loadingFadeDuration = 0.8f;
    [SerializeField] private float minimumPause = 0.25f;
    [SerializeField] private float maximumPause = 0.7f;

    [Header("Main Menu")]
    [SerializeField] private RectTransform[] menuButtons;
    [SerializeField] private CanvasGroup settingsButton;
    [SerializeField] private float buttonSlideDuration = 0.45f;
    [SerializeField] private float buttonDelay = 0.18f;
    [SerializeField] private float buttonOvershoot = 30f;
    [SerializeField] private float settingsFadeDuration = 0.6f;

    [Header("Settings Menu")]
    [SerializeField] private CanvasGroup settingsMenuCanvas;
    [SerializeField] private float screenFadeDuration = 0.4f;

    [Header("Store")]
    [SerializeField] private CanvasGroup storeCanvas;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip swooshSound;

    [Header("Audio Settings")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider fxSlider;
    [SerializeField] private TextMeshProUGUI musicSliderPerc;
    [SerializeField] private TextMeshProUGUI fxSliderPerc;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource[] fxSources;

    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string FX_VOLUME_KEY = "FXVolume";

    private float musicVolume = 1f;
    private float fxVolume = 1f;

    private bool isTransitioning;

    private bool sequenceStarted;
    public bool isUIMode;
    public GameObject blackBg;

    public GameObject player, spawner;

    [Header("Notifications")]
    public CanvasGroup critical, doubleKill, incoming;
    public CanvasGroup tapToTeleport, teleportFull, torchReady;
    public CanvasGroup bigSave, alert, highScore;

    [SerializeField] private float notificationFadeInDuration = 0.4f;
    [SerializeField] private float notificationStayDuration = 3.5f;
    [SerializeField] private float notificationFadeOutDuration = 0.4f;

    [Header("Teleport")]
    [SerializeField] private Button teleportButton;
    [SerializeField] private Image teleportFill;

    public float teleportRefillDuration = 10f;
    [SerializeField] private float teleportCost = 0.2f;

    private float teleportCharge = 0f;
    private bool wasTeleportFull = true;

    [Header("Torch")]
    [SerializeField] private Button torchButton;
    [SerializeField] private Image torchFill;
    [SerializeField] private GameObject torchObject;

    [SerializeField] private CanvasGroup torchSliderCanvas, torchOnCanvas, torchOffCanvas;
    [SerializeField] private Slider torchSlider;
    [SerializeField] private float torchSliderFadeDuration = 0.4f;

    [SerializeField] private float torchDrainDuration = 20f;
    [SerializeField] private float torchRefillDuration = 10f;
    [SerializeField] private float torchDuration = 6f;

    private float torchCharge = 1f;
    private bool isTorchOn, isCoolingDown;
    private bool torchLocked;
    private Coroutine torchCoroutine;
    private Coroutine gameOverRoutine;

    private FPP_PlayerController playerController;

    private void Start()
    {
        isUIMode = true;

        // Set the initial screen states.
        SetCanvas(splashCanvas, 1f, true);
        SetCanvas(loadingCanvas, 0f, false);
        SetCanvas(mainMenuCanvas, 0f, false);
        SetCanvas(settingsMenuCanvas, 0f, false);
        SetCanvas(storeCanvas, 0f, false);

        if (settingsButton != null)
        {
            settingsButton.alpha = 0f;
            settingsButton.interactable = false;
            settingsButton.blocksRaycasts = false;
        }

        // Hide menu buttons until their entrance animation.
        if (menuButtons != null)
        {
            foreach (RectTransform button in menuButtons)
            {
                if (button != null)
                    button.gameObject.SetActive(false);
            }
        }

        // Hide notifications initially.
        InitializeNotification(critical);
        InitializeNotification(doubleKill);
        InitializeNotification(incoming);
        InitializeNotification(tapToTeleport);
        InitializeNotification(teleportFull);
        InitializeNotification(bigSave);
        InitializeNotification(alert);
        InitializeNotification(highScore);
        InitializeNotification(torchSliderCanvas);
        InitializeNotification(torchOnCanvas);
        InitializeNotification(torchReady);

        // Find player reference.
        if (player != null)
            playerController = player.GetComponent<FPP_PlayerController>();

        // TELEPORT BUTTON
        if (teleportButton != null)
            teleportButton.onClick.AddListener(UseTeleport);

        if (torchObject != null)
            torchObject.SetActive(false);

        if (torchSlider != null)
            torchSlider.value = 1f;

        if (torchFill != null)
            torchFill.fillAmount = 1f;
            
        // Torch button
        if (torchButton != null)
            torchButton.onClick.AddListener(ToggleTorch);

        // Audio settings
        LoadAudioSettings();

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(musicVolume);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (fxSlider != null)
        {
            fxSlider.SetValueWithoutNotify(fxVolume);
            fxSlider.onValueChanged.AddListener(SetFXVolume);
        }

        ApplyAudioSettings();

        UpdateTeleportUI();

        StartCoroutine(PlayIntroSequence());
    }

    private void Update()
    {
        if (isUIMode || Time.timeScale <= 0f)
            return;
    }

    // Notifications
    public void InitializeNotification(CanvasGroup canvas)
    {
        if (canvas == null)
            return;

        canvas.DOKill();

        canvas.alpha = 0f;
        canvas.interactable = false;
        canvas.blocksRaycasts = false;
        canvas.gameObject.SetActive(false);
    }

    public void ShowNotification(CanvasGroup canvas, float stayTime)
    {
        if (canvas == null)
            return;

        canvas.DOKill();

        canvas.gameObject.SetActive(true);
        canvas.alpha = 0f;
        canvas.interactable = false;
        canvas.blocksRaycasts = false;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            canvas.DOFade(1f, notificationFadeInDuration)
                .SetEase(Ease.OutSine)
        );

        sequence.AppendInterval(stayTime);

        sequence.Append(
            canvas.DOFade(0f, notificationFadeOutDuration)
                .SetEase(Ease.InSine)
        );

        sequence.OnComplete(() =>
        {
            canvas.gameObject.SetActive(false);
        });

        sequence.SetUpdate(true);
    }

    #region Notifications

    public void ShowCritical()
    {
        ShowNotification(critical, notificationStayDuration * 1.5f);
    }

    public void ShowDoubleKill()
    {
        ShowNotification(doubleKill, notificationStayDuration);
    }

    public void ShowIncoming()
    {
        ShowNotification(incoming, notificationStayDuration);
    }

    public void ShowTapToTeleport()
    {
        ShowNotification(tapToTeleport, 2.5f);
    }

    public void ShowTeleportFull()
    {
        ShowNotification(teleportFull, notificationStayDuration);
    }

    public void ShowBigSave()
    {
        ShowNotification(bigSave, notificationStayDuration);
    }

    public void ShowAlert()
    {
        ShowNotification(alert, notificationStayDuration);
    }

    public void ShowNewHighScore()
    {
        ShowNotification(highScore, notificationStayDuration);
    }
    #endregion

   // TELEPORT

    public bool CanUseTeleport()
    {
        return teleportCharge >= 1f;
    }

    // Checks if another teleport charge can be added
    public bool CanReceiveTeleportCharge()
    {
        return teleportCharge < 1f;
    }

    // Adds a teleport charge
    public void AddTeleportCharge()
    {
        teleportCharge = 6f;
        UpdateTeleportUI();
        ShowTapToTeleport();
    }

    // Uses the teleport button
    private void UseTeleport()
    {
        if (playerController == null)
            return;

        if (!CanUseTeleport())
            return;

        playerController.ActivateTeleport();
    }

    // Consumes teleport charge
    public void ConsumeTeleportCharge()
    {
        if (!CanUseTeleport())
            return;

        teleportCharge--;

        wasTeleportFull = false;

        UpdateTeleportUI();
    }

    // Updates teleport UI
    private void UpdateTeleportUI()
    {
        if (teleportFill != null)
            teleportFill.fillAmount = teleportCharge;

        if (teleportButton != null)
            teleportButton.interactable = CanUseTeleport();
    }

    public float GetTeleportCharge()
    {
        return teleportCharge;
    }


    // TORCH
    public void ToggleTorch()
    {
        if(isCoolingDown) return;

        if (isTorchOn)
        {
            StopCoroutine(torchCoroutine);
            torchCoroutine = null;
            torchObject.SetActive(false);
            isTorchOn = false;
            ShowNotification(torchOffCanvas, notificationStayDuration);

            StartCoroutine(RefillRoutine());
        }
        else
        {
            torchCoroutine = StartCoroutine(TorchRoutine());
        }
    }

    IEnumerator TorchRoutine()
    {
        isTorchOn = true;
        torchObject.SetActive(true);
        ShowNotification(torchOnCanvas, notificationStayDuration * 1.5f);
        FadeTorchSlider(true);
        float timer = torchDuration;

        while(timer > 0f)
        {
            timer -= Time.deltaTime;
            float progress = Mathf.Clamp01(timer/torchDuration);
            torchSlider.value = progress;
            torchFill.fillAmount = progress;

            yield return null;
        }

        torchObject.SetActive(false);
        isTorchOn = false;
        torchCoroutine = null;
        StartCoroutine(RefillRoutine());
    }

    IEnumerator RefillRoutine()
    {
        isCoolingDown = true;
        torchButton.interactable = false;
        FadeTorchSlider(false);

        float timer = 0f;
        while (timer < torchRefillDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer/torchRefillDuration);
            torchSlider.value = progress;
            torchFill.fillAmount = progress;

            yield return null;
        }

        ShowNotification(torchReady, notificationStayDuration * 1.5f);
        torchSlider.value = 1f;
        torchFill.fillAmount = 1f;
        torchButton.interactable = true;
        isCoolingDown = false;
    }

    private void FadeTorchSlider(bool show)
    {
        if (torchSliderCanvas == null)
            return;

        torchSliderCanvas.DOKill();
        torchOnCanvas.DOKill();

        if (show)
        {
            torchSliderCanvas.gameObject.SetActive(true);
            torchOnCanvas.gameObject.SetActive(true);

            torchSliderCanvas.DOFade(1f, torchSliderFadeDuration)
                .SetEase(Ease.OutSine).SetUpdate(true);

            torchOnCanvas.DOFade(1f, torchSliderFadeDuration)
                .SetEase(Ease.OutSine).SetUpdate(true);
        }
        else
        {
            torchOnCanvas.DOFade(0f, torchSliderFadeDuration)
                .SetEase(Ease.InSine).SetUpdate(true);

            torchSliderCanvas.DOFade(0f, torchSliderFadeDuration)
                .SetEase(Ease.InSine).SetUpdate(true)
                .OnComplete(() =>
                {
                    torchSliderCanvas.gameObject.SetActive(false);
                    torchOnCanvas.gameObject.SetActive(false);
                });
        }
    }


    #region Button functions

    // SETTINGS MENU
    public void OpenSettings()
    {
        // Restore the time scale
        Time.timeScale = 1f;

        if (isTransitioning || settingsMenuCanvas == null || mainMenuCanvas == null)
            return;

        StartCoroutine(FadeToSettings());
    }

    private IEnumerator FadeToSettings()
    {
        isTransitioning = true;

        settingsMenuCanvas.gameObject.SetActive(true);
        settingsMenuCanvas.alpha = 0f;
        settingsMenuCanvas.interactable = false;
        settingsMenuCanvas.blocksRaycasts = false;

        yield return settingsMenuCanvas.DOFade(1f, screenFadeDuration)
            .SetEase(Ease.InOutSine)
            .WaitForCompletion();

        settingsMenuCanvas.interactable = true;
        settingsMenuCanvas.blocksRaycasts = true;
        isTransitioning = false;
    }

    public void BackFromSettings()
    {
        if (isTransitioning || settingsMenuCanvas == null || mainMenuCanvas == null)
            return;

        SaveAudioSettings();
        StartCoroutine(FadeFromSettings());
    }

    private IEnumerator FadeFromSettings()
    {
        isTransitioning = true;

        settingsMenuCanvas.interactable = false;
        settingsMenuCanvas.blocksRaycasts = false;

        yield return settingsMenuCanvas.DOFade(0f, screenFadeDuration)
            .SetEase(Ease.InOutSine)
            .WaitForCompletion();

        settingsMenuCanvas.gameObject.SetActive(false);
        isTransitioning = false;
    }

    // STORE

    public void ShowStore()
    {
        if (isTransitioning || storeCanvas == null || mainMenuCanvas == null)
            return;

        StartCoroutine(FadeToStore());
    }

    private IEnumerator FadeToStore()
    {
        StoreManager.Instance.LoadStoreButtonStates();

        isTransitioning = true;

        storeCanvas.gameObject.SetActive(true);
        storeCanvas.alpha = 0f;
        storeCanvas.interactable = false;
        storeCanvas.blocksRaycasts = false;

        mainMenuCanvas.interactable = false;
        mainMenuCanvas.blocksRaycasts = false;

        yield return mainMenuCanvas.DOFade(0f, screenFadeDuration)
            .SetEase(Ease.InOutSine)
            .WaitForCompletion();

        mainMenuCanvas.gameObject.SetActive(false);

        yield return storeCanvas.DOFade(1f, screenFadeDuration)
            .SetEase(Ease.InOutSine)
            .WaitForCompletion();

        storeCanvas.interactable = true;
        storeCanvas.blocksRaycasts = true;

        isTransitioning = false;
    }

    public void ReturnToMainMenu()
    {
        // Restore the time scale
        Time.timeScale = 1f;

        if (isTransitioning || storeCanvas == null || mainMenuCanvas == null)
            return;

        StartCoroutine(FadeStoreToMainMenu());
    }

    private IEnumerator FadeStoreToMainMenu()
    {
        isTransitioning = true;

        mainMenuCanvas.gameObject.SetActive(true);
        mainMenuCanvas.alpha = 0f;
        mainMenuCanvas.interactable = false;
        mainMenuCanvas.blocksRaycasts = false;

        storeCanvas.interactable = false;
        storeCanvas.blocksRaycasts = false;

        yield return storeCanvas.DOFade(0f, screenFadeDuration)
            .SetEase(Ease.InOutSine)
            .WaitForCompletion();

        storeCanvas.gameObject.SetActive(false);

        yield return mainMenuCanvas.DOFade(1f, screenFadeDuration)
            .SetEase(Ease.InOutSine)
            .WaitForCompletion();

        mainMenuCanvas.interactable = true;
        mainMenuCanvas.blocksRaycasts = true;

        isTransitioning = false;
    }

    // AUDIO SETTINGS

    public void SetMusicVolume(float value)
    {
        musicVolume = value;

        if (musicSource != null)
            musicSource.volume = musicVolume;

        musicSliderPerc.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    public void SetFXVolume(float value)
    {
        fxVolume = value;

        if (fxSources == null)
            return;

        foreach (AudioSource source in fxSources)
        {
            if (source != null)
                source.volume = fxVolume;
        }

        fxSliderPerc.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void ApplyAudioSettings()
    {
        SetMusicVolume(musicVolume);
        SetFXVolume(fxVolume);
    }

    private void SaveAudioSettings()
    {
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolume);
        PlayerPrefs.SetFloat(FX_VOLUME_KEY, fxVolume);

        PlayerPrefs.Save();
    }

    private void LoadAudioSettings()
    {
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
        fxVolume = PlayerPrefs.GetFloat(FX_VOLUME_KEY, 1f);
    }

    // INTRO SEQUENCE

    private IEnumerator PlayIntroSequence()
    {
        if (sequenceStarted)
            yield break;

        sequenceStarted = true;

        // Show splash.
        yield return new WaitForSeconds(splashDuration);

        // Fade out splash.
        if (splashCanvas != null)
        {
            yield return splashCanvas.DOFade(0f, splashFadeDuration)
                .SetEase(Ease.InOutSine)
                .WaitForCompletion();

            splashCanvas.gameObject.SetActive(false);
        }

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
            yield return loadingCanvas.DOFade(0f, loadingFadeDuration)
                .SetEase(Ease.InOutSine)
                .WaitForCompletion();

            loadingCanvas.gameObject.SetActive(false);
        }

        // Show main menu.
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.gameObject.SetActive(true);
            mainMenuCanvas.alpha = 1f;
            mainMenuCanvas.interactable = true;
            mainMenuCanvas.blocksRaycasts = true;
        }

        // Animate menu buttons sequentially.
        yield return StartCoroutine(AnimateMenuButtons());

        // Fade in settings group after all buttons finish animating in.
        if (settingsButton != null)
        {
            settingsButton.gameObject.SetActive(true);

            yield return settingsButton.DOFade(1f, settingsFadeDuration)
                .SetEase(Ease.OutSine)
                .WaitForCompletion();

            settingsButton.interactable = true;
            settingsButton.blocksRaycasts = true;
        }
    }

    private IEnumerator AnimateLoadingSlider()
    {
        if (progressBar == null || progressText == null)
            yield break;

        float displayedProgress = 0f;

        while (displayedProgress < 1f)
        {
            float pauseDuration = Random.Range(minimumPause, maximumPause);
            yield return new WaitForSeconds(pauseDuration);

            float segmentDuration = Random.Range(0.35f, 0.8f);
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

    public void LoadMainMenu()
    {
        // Restore the time scale
        Time.timeScale = 1f;

        StartCoroutine(LoadingToMainMenu());
    }

    private IEnumerator LoadingToMainMenu()
    {
        yield return StartCoroutine(AnimateLoadingSlider());

        if (loadingCanvas != null)
        {
            yield return loadingCanvas.DOFade(0f, loadingFadeDuration)
                .SetEase(Ease.InOutSine)
                .WaitForCompletion();

            loadingCanvas.gameObject.SetActive(false);
        }

        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.gameObject.SetActive(true);
            mainMenuCanvas.alpha = 1f;
            mainMenuCanvas.interactable = true;
            mainMenuCanvas.blocksRaycasts = true;
        }

        yield return StartCoroutine(AnimateMenuButtons());

        if (settingsButton != null)
        {
            settingsButton.gameObject.SetActive(true);

            yield return settingsButton.DOFade(1f, settingsFadeDuration)
                .SetEase(Ease.OutSine)
                .WaitForCompletion();

            settingsButton.interactable = true;
            settingsButton.blocksRaycasts = true;
        }
    }

    private IEnumerator AnimateMenuButtons()
    {
        if (menuButtons == null)
            yield break;

        foreach (RectTransform button in menuButtons)
        {
            if (button == null)
                continue;

            button.gameObject.SetActive(true);

            Vector2 originalPosition = button.anchoredPosition;

            Vector2 startPosition = originalPosition +
                new Vector2(button.rect.width + buttonOvershoot, 0f);

            button.anchoredPosition = startPosition;

            if (audioSource != null && swooshSound != null)
                audioSource.PlayOneShot(swooshSound);

            yield return button
                .DOAnchorPos(originalPosition, buttonSlideDuration)
                .SetEase(Ease.OutCubic)
                .WaitForCompletion();

            yield return new WaitForSeconds(buttonDelay);
        }
    }

    private void SetCanvas(CanvasGroup canvas, float alpha, bool active)
    {
        if (canvas == null)
            return;

        canvas.gameObject.SetActive(active);
        canvas.alpha = alpha;
        canvas.interactable = active;
        canvas.blocksRaycasts = active;
    }

    // START GAME

    public void StartGame()
    {
        StartCoroutine(FadeOutMainMenu());

        if (player != null)
            player.SetActive(true);

        if (spawner != null)
            spawner.SetActive(true);

        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
    }

    private IEnumerator FadeOutMainMenu()
    {
        if (blackBg != null)
            blackBg.SetActive(false);

        if (mainMenuCanvas == null)
            yield break;

        yield return mainMenuCanvas.DOFade(0f, fadeDuration)
            .SetEase(Ease.InOutSine)
            .WaitForCompletion();

        mainMenuCanvas.gameObject.SetActive(false);
        mainMenuCanvas.alpha = 0f;
        mainMenuCanvas.interactable = false;
        mainMenuCanvas.blocksRaycasts = false;
    }

    // Pause Game
    public void PauseGame()
    {
        Time.timeScale = 0f;
    }

    // Resume Game
    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }

    // Restart Game
    public void RestartGame()
    {
        Time.timeScale = 1f;
        isTransitioning = false;

        // Stop game over sequence
        if (gameOverRoutine != null)
        {
            StopCoroutine(gameOverRoutine);
            gameOverRoutine = null;
        }

        // Stop game over animations
        if (GameOverCanvasI != null)
            GameOverCanvasI.DOKill();

        if (GameOverCanvasII != null)
            GameOverCanvasII.DOKill();

        // Reset game oer screens
        ResetCanvasGroup(GameOverCanvasI);
        ResetCanvasGroup(GameOverCanvasII);

        // Reset notifications
        InitializeNotification(critical);
        InitializeNotification(doubleKill);
        InitializeNotification(incoming);
        InitializeNotification(tapToTeleport);
        InitializeNotification(teleportFull);
        InitializeNotification(bigSave);
        InitializeNotification(alert);
        InitializeNotification(highScore);

        // Reset teleport charge
        teleportCharge = 1f;
        wasTeleportFull = true;

        UpdateTeleportUI();

        // Reset torch
        isTorchOn = false;
        torchLocked = false;
        torchCharge = 1f;

        if (torchObject != null)
            torchObject.SetActive(false);

        if (torchSliderCanvas != null)
        {
            torchSliderCanvas.DOKill();
            torchSliderCanvas.alpha = 0f;
            torchSliderCanvas.interactable = false;
            torchSliderCanvas.blocksRaycasts = false;
            torchSliderCanvas.gameObject.SetActive(false);
        }

        // Restore player
        if (player != null)
        {
            player.SetActive(true);

            playerController =
                player.GetComponent<FPP_PlayerController>();

            if (playerController != null)
                playerController.ResetVariables();
        }

        // Reset spawner and return objects to pool
        if (spawner != null)
        {
            spawner.SetActive(true);

            InfinitySpawner infinitySpawner =
                spawner.GetComponent<InfinitySpawner>();

            if (infinitySpawner != null)
                infinitySpawner.RestartSpawner();
        }

        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();

        if (blackBg != null)
            blackBg.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Game Restarted!");
    }

    // -Reset canvas groups
    private void ResetCanvasGroup(CanvasGroup canvas)
    {
        if (canvas == null)
            return;

        canvas.DOKill();

        canvas.alpha = 0f;
        canvas.interactable = false;
        canvas.blocksRaycasts = false;
        canvas.gameObject.SetActive(false);
    }

    // Game over
    public void PlayGameOverSequence()
    {
        if (gameOverRoutine != null)
            StopCoroutine(gameOverRoutine);

        gameOverRoutine = StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        if (GameOverCanvasI != null)
        {
            GameOverCanvasI.gameObject.SetActive(true);
            GameOverCanvasI.alpha = 0f;

            yield return GameOverCanvasI.DOFade(1f, fadeDuration)
                .SetEase(Ease.InOutSine)
                .WaitForCompletion();
        }

        yield return new WaitForSecondsRealtime(2f);

        if (GameOverCanvasII != null)
        {
            GameOverCanvasII.gameObject.SetActive(true);
            GameOverCanvasII.alpha = 0f;

            yield return GameOverCanvasII.DOFade(1f, fadeDuration)
                .SetEase(Ease.InOutSine)
                .WaitForCompletion();
        }

        if (GameOverCanvasI != null)
        {
            GameOverCanvasI.gameObject.SetActive(false);
            GameOverCanvasI.alpha = 0f;
            GameOverCanvasI.interactable = false;
            GameOverCanvasI.blocksRaycasts = false;
        }

        gameOverRoutine = null;
    }

    private void OnDestroy()
    {
        DOTween.Kill(splashCanvas);
        DOTween.Kill(loadingCanvas);
        DOTween.Kill(mainMenuCanvas);
        DOTween.Kill(settingsButton);
        DOTween.Kill(settingsMenuCanvas);
        DOTween.Kill(storeCanvas);

        if (menuButtons != null)
        {
            foreach (RectTransform button in menuButtons)
            {
                if (button != null)
                    button.DOKill();
            }
        }

        DOTween.Kill(critical);
        DOTween.Kill(doubleKill);
        DOTween.Kill(incoming);
        DOTween.Kill(tapToTeleport);
        DOTween.Kill(teleportFull);
        DOTween.Kill(bigSave);
        DOTween.Kill(alert);
        DOTween.Kill(highScore);
        DOTween.Kill(torchSliderCanvas);
        DOTween.Kill(GameOverCanvasI);
        DOTween.Kill(GameOverCanvasII);
    }
    #endregion
}