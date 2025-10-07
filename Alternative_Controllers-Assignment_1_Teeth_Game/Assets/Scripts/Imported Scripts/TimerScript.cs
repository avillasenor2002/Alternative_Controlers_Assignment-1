using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimerScript : MonoBehaviour
{
    // Reference to the TextMeshPro component for displaying the timer
    public TextMeshProUGUI timerText;

    // Countdown time in seconds
    public float countdownTime = 10f;

    // Reference to the HammerTypeScript to disable input
    public HammerTypeScript hammerTypeScript;

    // Reference to the EndGameManager to end the game
    public EndGameManager endGameManager;

    // Flag to check if the timer is running
    private bool isTimerRunning = false;

    void Start()
    {
        // Start the countdown when the script starts
        StartTimer(countdownTime);
    }

    void Update()
    {
        // If the timer is running, update the countdown
        if (isTimerRunning)
        {
            if (countdownTime > 0)
            {
                // Decrease the timer by the time passed since last frame
                countdownTime -= Time.deltaTime;

                // Update the timer text
                UpdateTimerText(countdownTime);
            }
            else
            {
                // Timer has reached zero, stop the timer
                countdownTime = 0;
                isTimerRunning = false;

                // Stop accepting inputs in HammerTypeScript
                hammerTypeScript.StopInput();

                // Update the timer text to show "Time's Up!"
                timerText.text = "Time's Up!";

                // End the game by calling the EndGameManager's EndGame() method
                endGameManager.EndGame();
            }
        }
    }

    // Method to start the timer
    public void StartTimer(float time)
    {
        countdownTime = time;
        isTimerRunning = true;
    }

    // Update the TextMeshPro to show the remaining time
    void UpdateTimerText(float time)
    {
        timerText.text = Mathf.Ceil(time).ToString(); // Show whole seconds
    }
}
