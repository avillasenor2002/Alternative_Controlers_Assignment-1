using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Timer : MonoBehaviour
{
    [Header("Countdown Timer Settings")]
    [SerializeField] private float maxTime = 30f;
    [SerializeField] private Image fillImage;          // UI Image with Fill Method = Radial or Horizontal
    [SerializeField] private TMP_Text countdownText;   // TextMeshProUGUI for countdown display

    [Header("Game Time Display")]
    [SerializeField] private TMP_Text totalTimeText;   // TextMeshProUGUI showing elapsed time (00:00 format)
    [SerializeField] private TMP_Text bestTimeText;    // TextMeshProUGUI for displaying best time

    [Header("Teeth References")]
    [Tooltip("Assign all IndividualTooth objects in the scene.")]
    [SerializeField] private IndividualTooth[] teeth;

    [Header("Decay Settings")]
    [Tooltip("Base speed multiplier when one tooth is dirty.")]
    [SerializeField] private float baseDecayRate = 1f;
    [Tooltip("Multiplier applied per dirty tooth (more dirty teeth = faster decay).")]
    [SerializeField] private float perDirtyMultiplier = 0.5f;
    [Tooltip("Minimum speed multiplier when all teeth are clean.")]
    [SerializeField] private float cleanDecayMultiplier = 0.1f;

    private float countdownTime;
    private float totalElapsedTime;
    private bool countdownActive = true;
    private bool bestTimeChecked = false;

    private const string BEST_TIME_KEY = "BestTime";

    void Start()
    {
        countdownTime = maxTime;
        totalElapsedTime = 0f;

        if (PlayerPrefs.HasKey(BEST_TIME_KEY))
            bestTimeText.text = FormatTime(PlayerPrefs.GetFloat(BEST_TIME_KEY));
        else
            bestTimeText.text = "00:00";

        UpdateCountdownUI();
        UpdateTotalTimeUI();
    }

    void Update()
    {
        // --- Countdown Timer ---
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
            }
        }

        // --- Total Game Time ---
        totalElapsedTime += Time.deltaTime;
        UpdateTotalTimeUI();
    }

    private float CalculateDecayRate()
    {
        if (teeth == null || teeth.Length == 0)
            return baseDecayRate; // fallback if not set

        int dirtyCount = 0;
        foreach (var tooth in teeth)
        {
            if (tooth != null && tooth.GetCurrentStateIndex() != 0)
                dirtyCount++;
        }

        // If all teeth are clean, timer slows down drastically
        if (dirtyCount == 0)
            return baseDecayRate * cleanDecayMultiplier;

        // Otherwise, increase decay rate based on how many are dirty
        return baseDecayRate + (dirtyCount * perDirtyMultiplier);
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
        if (!totalTimeText) return;
        totalTimeText.text = FormatTime(totalElapsedTime);
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return $"{minutes:00}:{seconds:00}";
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

    private void RecordBestTime()
    {
        if (bestTimeChecked) return;
        bestTimeChecked = true;

        float bestTime = PlayerPrefs.GetFloat(BEST_TIME_KEY, 0f);
        if (totalElapsedTime > bestTime)
        {
            PlayerPrefs.SetFloat(BEST_TIME_KEY, totalElapsedTime);
            PlayerPrefs.Save();
            if (bestTimeText)
                bestTimeText.text = FormatTime(totalElapsedTime);
        }
    }
}
