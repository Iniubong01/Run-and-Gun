
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using PlayerControllers;
using UnityEngine.SceneManagement;

public class UI_Manager : MonoBehaviour
{
    public static UI_Manager Instance;

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
    private bool hasStarted;

    private Coroutine introRoutine;
    public bool isUIMode;
    public GameObject blackBg;

    public GameObject player, spawner;

    [Header("Notifications")]
    public CanvasGroup critical, doubleKill, incoming;
    public CanvasGroup tapToTeleport, teleportFull, torchReady;
    public CanvasGroup bigSave, alert, highScore;

    [SerializeField] private float notificationFadeInDuration = 0.4f;
    public float notificationStayDuration = 3.5f;
    public float notificationFadeOutDuration = 0.4f;

    [Header("Teleport")]
    [SerializeField] private Button teleportButton;
    [SerializeField] private Image teleportFill;

    public float teleportRefillDuration = 10f;
    [SerializeField] private float teleportCost = 0.2f;

    private float teleportCharge = 0f;
    private bool wasTeleportFull = true;

    public CanvasGroup torchSliderCanvas, torchOnCanvas, torchOffCanvas;
    public Slider torchSlider;
    public float torchSliderFadeDuration = 0.4f;

    public float torchDrainDuration = 20f;
    public float torchRefillDuration = 10f;
    public float torchDuration = 6f;

    private float torchCharge = 1f;
    private bool isTorchOn, isCoolingDown;
    private bool torchLocked;
    private Coroutine torchCoroutine;
    private Coroutine gameOverRoutine;

    private FPP_PlayerController playerController;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Handles the UI scene being loaded again
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!hasStarted)
            return;

        if (scene.name == "UI Scene")
        {
            RestartIntroSequence();
        }
    }

    // Restarts the UI intro sequence
    private void RestartIntroSequence()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        // Stop existing canvas animations
        if (splashCanvas != null) splashCanvas.DOKill();
        if (loadingCanvas != null) loadingCanvas.DOKill();
        if (mainMenuCanvas != null) mainMenuCanvas.DOKill();
        if (settingsMenuCanvas != null) settingsMenuCanvas.DOKill();
        if (storeCanvas != null) storeCanvas.DOKill();
        if (GameOverCanvasI != null) GameOverCanvasI.DOKill();
        if (GameOverCanvasII != null) GameOverCanvasII.DOKill();
        if (settingsButton != null) settingsButton.DOKill();

        sequenceStarted = false;
        isTransitioning = false;
        isUIMode = true;

        Time.timeScale = 1f;

        // Stop the game over sequence if it is running
        if (gameOverRoutine != null)
        {
            StopCoroutine(gameOverRoutine);
            gameOverRoutine = null;
        }

        // Reset all screen canvases
        SetCanvas(splashCanvas, 1f, true);
        SetCanvas(loadingCanvas, 0f, false);
        SetCanvas(mainMenuCanvas, 0f, false);
        SetCanvas(settingsMenuCanvas, 0f, false);
        SetCanvas(storeCanvas, 0f, false);
        SetCanvas(GameOverCanvasI, 0f, false);
        SetCanvas(GameOverCanvasII, 0f, false);

        // Reset the settings button
        if (settingsButton != null)
        {
            settingsButton.alpha = 0f;
            settingsButton.interactable = false;
            settingsButton.blocksRaycasts = false;
            settingsButton.gameObject.SetActive(false);
        }

        // Reset the menu buttons
        if (menuButtons != null)
        {
            foreach (RectTransform button in menuButtons)
            {
                if (button == null)
                    continue;

                button.DOKill();
                button.gameObject.SetActive(false);
            }
        }

        // Reset the loading progress
        if (progressBar != null)
            progressBar.value = 0f;

        if (progressText != null)
            progressText.text = "0%";

        // Reset the background
        if (blackBg != null)
            blackBg.SetActive(true);

        // Reset notifications
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
        InitializeNotification(torchOffCanvas);
        InitializeNotification(torchReady);

        // Start the intro sequence again
        introRoutine = StartCoroutine(PlayIntroSequence());
    }

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
        InitializeNotification(torchOffCanvas);
        InitializeNotification(torchReady);

        // Find player reference.
        if (player != null)
            playerController = player.GetComponent<FPP_PlayerController>();

        // TELEPORT BUTTON
        if (teleportButton != null)
            teleportButton.onClick.AddListener(UseTeleport);

        if (torchSlider != null)
            torchSlider.value = 1f;

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

        hasStarted = true;
        RestartIntroSequence();
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

    public void FadeTorchSlider(bool show)
    {
        if (torchSliderCanvas == null)
            return;

        if (torchOnCanvas != null)
            torchOnCanvas.DOKill();

        torchSliderCanvas.DOKill();

        if (show)
        {
            torchSliderCanvas.gameObject.SetActive(true);

            if (torchOnCanvas != null)
                torchOnCanvas.gameObject.SetActive(true);

            torchSliderCanvas.DOFade(1f, torchSliderFadeDuration)
                .SetEase(Ease.OutSine).SetUpdate(true);

            if (torchOnCanvas != null)
            {
                torchOnCanvas.DOFade(1f, torchSliderFadeDuration)
                    .SetEase(Ease.OutSine).SetUpdate(true);
            }
        }
        else
        {
            if (torchOnCanvas != null)
            {
                torchOnCanvas.DOFade(0f, torchSliderFadeDuration)
                    .SetEase(Ease.InSine).SetUpdate(true);
            }

            torchSliderCanvas.DOFade(0f, torchSliderFadeDuration)
                .SetEase(Ease.InSine).SetUpdate(true)
                .OnComplete(() =>
                {
                    torchSliderCanvas.gameObject.SetActive(false);

                    if (torchOnCanvas != null)
                        torchOnCanvas.gameObject.SetActive(false);
                });
        }
    }

    #region Button functions

    // SETTINGS MENU
    public void OpenSettings()
    {
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
            .SetEase(Ease.InOutSine).SetUpdate(true).WaitForCompletion();

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
            .SetEase(Ease.InOutSine).SetUpdate(true).WaitForCompletion();

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
        if (StoreManager.Instance != null)
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

        if (musicSliderPerc != null)
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

        if (fxSliderPerc != null)
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

    // Intro sequence
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

        introRoutine = null;
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

        SceneManager.LoadScene("Game Scene");
    }

    // --
    public void LoadUIScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("UI Scene");
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene("Game Scene");
    }

    // Reset canvas groups
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

            GameOverCanvasII.interactable = true;
            GameOverCanvasII.blocksRaycasts = true;
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
        SceneManager.sceneLoaded -= OnSceneLoaded;

        DOTween.Kill(splashCanvas);
        DOTween.Kill(loadingCanvas);
        DOTween.Kill(mainMenuCanvas);
        DOTween.Kill(settingsButton);
        DOTween.Kill(settingsMenuCanvas);
        DOTween.Kill(storeCanvas);
        DOTween.Kill(GameOverCanvasI);
        DOTween.Kill(GameOverCanvasII);

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
        DOTween.Kill(torchOnCanvas);
        DOTween.Kill(torchOffCanvas);
        DOTween.Kill(torchReady);

        if (Instance == this)
            Instance = null;
    }

    #endregion
}
