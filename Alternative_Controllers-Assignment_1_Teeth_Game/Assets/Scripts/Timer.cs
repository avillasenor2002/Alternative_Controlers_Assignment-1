using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class Timer : MonoBehaviour
{
    [Header("Countdown Timer Settings")]
    [SerializeField] private float maxTime = 30f;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text countdownText;

    [Header("Game Time Display")]
    [SerializeField] private TMP_Text totalTimeText;
    [SerializeField] private TMP_Text bestTimeText;

    [Header("Teeth References")]
    [SerializeField] private IndividualTooth[] teeth;

    [Header("Decay Settings")]
    [SerializeField] private float baseDecayRate = 1f;
    [SerializeField] private float perDirtyMultiplier = 0.5f;
    [SerializeField] private float cleanDecayMultiplier = 0.1f;

    [Header("Scene Settings")]
    [SerializeField] private string nextSceneName; // Scene to load when timer ends

    [Header("Hotkeys")]
    [SerializeField] private KeyCode resetBestTimeKey = KeyCode.R;

    private float countdownTime;
    private float totalElapsedTime;
    private bool countdownActive = true;
    private bool bestTimeChecked = false;

    private const string BEST_TIME_KEY = "BestTime";
    private const string FINAL_TIME_KEY = "FinalTime";

    void Start()
    {
        countdownTime = maxTime;
        totalElapsedTime = 0f;

        UpdateBestTimeUI();
        UpdateCountdownUI();
        UpdateTotalTimeUI();
    }

    void Update()
    {
        // Reset best time hotkey
        if (Input.GetKeyDown(resetBestTimeKey))
            ResetBestTime();

        // Countdown timer
        if (countdownActive)
        {
            float decayRate = CalculateDecayRate();
            countdownTime -= Time.deltaTime * decayRate;
            countdownTime = Mathf.Clamp(countdownTime, 0f, maxTime);
            UpdateCountdownUI();

            if (countdownTime <= 0)
            {
                countdownActive = false;
                RecordBestTime();
                SaveFinalTimesAndLoadScene();
            }
        }

        // Total elapsed time
        totalElapsedTime += Time.deltaTime;
        UpdateTotalTimeUI();
    }

    private float CalculateDecayRate()
    {
        int dirtyCount = 0;
        foreach (var tooth in teeth)
            if (tooth != null && tooth.GetCurrentStateIndex() != 0)
                dirtyCount++;

        return dirtyCount == 0 ? baseDecayRate * cleanDecayMultiplier : baseDecayRate + (dirtyCount * perDirtyMultiplier);
    }

    private void UpdateCountdownUI()
    {
        if (fillImage)
            fillImage.fillAmount = countdownTime / maxTime;

        if (countdownText)
            countdownText.text = Mathf.CeilToInt(countdownTime).ToString();
    }

    private void UpdateTotalTimeUI()
    {
        if (totalTimeText)
            totalTimeText.text = FormatTime(totalElapsedTime);
    }

    private void UpdateBestTimeUI()
    {
        if (bestTimeText)
        {
            float bestTime = PlayerPrefs.GetFloat(BEST_TIME_KEY, 0f);
            bestTimeText.text = bestTime > 0 ? FormatTime(bestTime) : "00:00";
        }
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    private void RecordBestTime()
    {
        if (bestTimeChecked) return;
        bestTimeChecked = true;

        float bestTime = PlayerPrefs.GetFloat(BEST_TIME_KEY, 0f);
        if (totalElapsedTime > bestTime)
        {
            PlayerPrefs.SetFloat(BEST_TIME_KEY, totalElapsedTime);
            PlayerPrefs.Save();
            UpdateBestTimeUI();
        }
    }

    private void SaveFinalTimesAndLoadScene()
    {
        PlayerPrefs.SetFloat(FINAL_TIME_KEY, totalElapsedTime);
        PlayerPrefs.SetFloat(BEST_TIME_KEY, PlayerPrefs.GetFloat(BEST_TIME_KEY, 0f));
        PlayerPrefs.Save();

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    private void ResetBestTime()
    {
        PlayerPrefs.DeleteKey(BEST_TIME_KEY);
        PlayerPrefs.Save();
        UpdateBestTimeUI();
        Debug.Log("Best time reset!");
    }

    public void RestoreCountdown()
    {
        countdownTime = maxTime;
        countdownActive = true;
        bestTimeChecked = false;
        UpdateCountdownUI();
    }

    public void AddTime(float amount)
    {
        countdownTime = Mathf.Clamp(countdownTime + amount, 0f, maxTime);
        UpdateCountdownUI();
    }

    public float GetFinalTime() => totalElapsedTime;
    public float GetBestTime() => PlayerPrefs.GetFloat(BEST_TIME_KEY, 0f);
}
