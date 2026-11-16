
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Timer")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float elapsedTime;
    private bool isGameRunning;

    [Header("Time High Score")]
    [SerializeField] private TextMeshProUGUI currentTimeText;
    [SerializeField] private TextMeshProUGUI bestTimeText;

    private const string HIGH_TIME_KEY = "PlayerHighTime";
    private float highTime;

    [Header("Enemy Kills")]
    [SerializeField] private TextMeshProUGUI enemiesKilledText;
    [SerializeField] private TextMeshProUGUI bestEnemiesKilledText;

    private const string BEST_KILLS_KEY = "PlayerBestKills";
    private int enemiesKilled;
    private int bestEnemiesKilled;

    [Header("Score")]
    public int pointsPerKill = 7;

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    private const string HIGH_SCORE_KEY = "PlayerHighScore";
    private int score;
    private int highScore;

    [Header("Coins")]
    [SerializeField] private TextMeshProUGUI coinsGainedText;
    [SerializeField] private TextMeshProUGUI totalCoinsText;

    private const string TOTAL_COINS_KEY = "PlayerTotalCoins";

    private int coinsGained;
    private int totalCoins;

    [Header("References")]
    [SerializeField] private UI_Manager uiManager;

    [Header("Double Kill")]
    public float doubleKillWindow = 3f;

    private float lastKillTime;
    private bool hasRecentKill;

    private bool isNewHighScore;

    public float ElapsedTime => elapsedTime;
    public float HighTime => highTime;
    public int EnemiesKilled => enemiesKilled;
    public int BestEnemiesKilled => bestEnemiesKilled;
    public int Score => score;
    public int HighScore => highScore;
    public int CoinsGained => coinsGained;
    public int TotalCoins => totalCoins;
    public bool IsGameRunning => isGameRunning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        LoadGameData();

        UpdateTimerUI();
        UpdateHighScoreUI();
        UpdateTotalCoinsUI();
    }

    private void Start()
    {
        if (uiManager == null)
        {
            GameObject uiObject = GameObject.Find("UI Manager");

            if (uiObject != null)
                uiManager = uiObject.GetComponent<UI_Manager>();
        }
    }

    private void Update()
    {
        if (!isGameRunning)
            return;

        elapsedTime += Time.deltaTime;

        UpdateTimerUI();
    }

    // Start game
    public void StartGame()
    {
        ResetGameData();

        isNewHighScore = false;
        isGameRunning = true;
    }

    public void RegisterEnemyKill()
    {
        if (!isGameRunning)
            return;

        enemiesKilled++;

        score += pointsPerKill;

        UpdateKillUI();
        UpdateScoreUI();

        // Double kill check
        float currentTime = Time.unscaledTime;

        if (hasRecentKill &&
            currentTime - lastKillTime <= doubleKillWindow)
        {
            if (uiManager != null)
                uiManager.ShowDoubleKill();
        }

        lastKillTime = currentTime;
        hasRecentKill = true;
    }

    // Reset kill tracking
    public void ResetKillTracking()
    {
        lastKillTime = 0f;
        hasRecentKill = false;
    }

    // Add coins
    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        coinsGained += amount;
        totalCoins += amount;

        UpdateCoinsUI();

        SaveCoins();
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
            timerText.text = FormatTime(elapsedTime);
    }

    private void UpdateKillUI()
    {
        if (enemiesKilledText != null)
            enemiesKilledText.text = enemiesKilled.ToString();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }

    private void UpdateCoinsUI()
    {
        if (coinsGainedText != null)
            coinsGainedText.text = coinsGained.ToString();

        UpdateTotalCoinsUI();
    }

    private void UpdateTotalCoinsUI()
    {
        if (totalCoinsText != null)
            totalCoinsText.text = totalCoins.ToString();
    }

    // Format time in minutes and seconds
    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);

        return $"{minutes:00}:{seconds:00}";
    }

    public void GameOver()
    {
        if (!isGameRunning)
            return;

        isGameRunning = false;

        CheckHighScores();

        UpdateGameOverUI();

        SaveGameData();
    }

    private void CheckHighScores()
    {
        bool achievedNewRecord = false;

        // Best survival time
        if (elapsedTime > highTime)
        {
            highTime = elapsedTime;
            achievedNewRecord = true;
        }

        // Best enemy kills
        if (enemiesKilled > bestEnemiesKilled)
        {
            bestEnemiesKilled = enemiesKilled;
            achievedNewRecord = true;
        }

        // Best score
        if (score > highScore)
        {
            highScore = score;
            achievedNewRecord = true;
        }

        if (achievedNewRecord)
        {
            isNewHighScore = true;

            if (uiManager != null)
                uiManager.ShowNewHighScore();
        }

        SaveGameData();
    }

    // -- Uupdate game over display
    private void UpdateGameOverUI()
    {
        if (currentTimeText != null)
            currentTimeText.text = FormatTime(elapsedTime);

        if (bestTimeText != null)
            bestTimeText.text = "BEST: " + FormatTime(highTime);

        if (enemiesKilledText != null)
            enemiesKilledText.text = enemiesKilled.ToString();

        if (bestEnemiesKilledText != null)
            bestEnemiesKilledText.text = "BEST: " + bestEnemiesKilled.ToString();

        if (gameOverScoreText != null)
            gameOverScoreText.text = score.ToString();

        if (highScoreText != null)
            highScoreText.text = "BEST: " + highScore.ToString();

        if (coinsGainedText != null)
            coinsGainedText.text = coinsGained.ToString();

        UpdateTotalCoinsUI();
    }

    // Save game data
    public void SaveGameData()
    {
        PlayerPrefs.SetFloat(HIGH_TIME_KEY, highTime);
        PlayerPrefs.SetInt(BEST_KILLS_KEY, bestEnemiesKilled);
        PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
        PlayerPrefs.SetInt(TOTAL_COINS_KEY, totalCoins);

        PlayerPrefs.Save();
    }
    private void SaveCoins()
    {
        PlayerPrefs.SetInt(TOTAL_COINS_KEY, totalCoins);
        PlayerPrefs.Save();
    }

    private void LoadGameData()
    {
        highTime = PlayerPrefs.GetFloat(HIGH_TIME_KEY, 0f);
        bestEnemiesKilled = PlayerPrefs.GetInt(BEST_KILLS_KEY, 0);
        highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        totalCoins = PlayerPrefs.GetInt(TOTAL_COINS_KEY, 0);
    }

    // -- Reset timer
    public void ResetTimer()
    {
        elapsedTime = 0f;

        UpdateTimerUI();
    }

    // -- General Reset
    public void ResetGameData()
    {
        isGameRunning = false;
        isNewHighScore = false;

        ResetTimer();

        enemiesKilled = 0;
        score = 0;
        coinsGained = 0;

        ResetKillTracking();

        UpdateKillUI();
        UpdateScoreUI();
        UpdateCoinsUI();

        if (currentTimeText != null)
            currentTimeText.text = "00:00";

        if (bestTimeText != null)
            bestTimeText.text = FormatTime(highTime);

        if (gameOverScoreText != null)
            gameOverScoreText.text = "0";

        if (highScoreText != null)
            highScoreText.text = highScore.ToString();

        if (bestEnemiesKilledText != null)
            bestEnemiesKilledText.text = bestEnemiesKilled.ToString();
    }

    public void ResetHighScores()
    {
        highTime = 0f;
        bestEnemiesKilled = 0;
        highScore = 0;

        PlayerPrefs.DeleteKey(HIGH_TIME_KEY);
        PlayerPrefs.DeleteKey(BEST_KILLS_KEY);
        PlayerPrefs.DeleteKey(HIGH_SCORE_KEY);

        PlayerPrefs.Save();

        UpdateHighScoreUI();
    }

    public void ResetTotalCoins()
    {
        totalCoins = 0;

        PlayerPrefs.DeleteKey(TOTAL_COINS_KEY);
        PlayerPrefs.Save();

        UpdateTotalCoinsUI();
    }
    
    private void UpdateHighScoreUI()
    {
        if (bestTimeText != null)
            bestTimeText.text = FormatTime(highTime);

        if (bestEnemiesKilledText != null)
            bestEnemiesKilledText.text = bestEnemiesKilled.ToString();

        if (highScoreText != null)
            highScoreText.text = highScore.ToString();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}