using UnityEngine;
using TMPro;

public class EndGameManager : MonoBehaviour
{
    // Reference to the TextMeshPro components
    public TextMeshProUGUI roundScoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI restartPromptText;
    public ScoreSequenceManager scoreSequenceManager;

    // Reference to the score manager or the score variable
    public int playerScore;

    // To store the highest score (using PlayerPrefs)
    private int highScore;

    // Flag to track if the game is over
    private bool isGameOver = false;

    void Start()
    {
        // Load the high score from PlayerPrefs
        highScore = PlayerPrefs.GetInt("HighScore", 0);

        // Hide the end-game UI elements at the start
        roundScoreText.gameObject.SetActive(false);
        highScoreText.gameObject.SetActive(false);
        restartPromptText.gameObject.SetActive(false);
    }

    void Update()
    {
        // Check if the game is over
        if (isGameOver && Input.anyKeyDown)
        {
            RestartGame();
        }

        playerScore = scoreSequenceManager.score;
    }

    public void EndGame()
    {
        // Show the end-game UI
        roundScoreText.gameObject.SetActive(true);
        highScoreText.gameObject.SetActive(true);
        restartPromptText.gameObject.SetActive(true);

        // Update the player's score
        roundScoreText.text = "Score: " + playerScore.ToString();

        // Check if it's a new high score
        if (playerScore > highScore)
        {
            highScore = playerScore;
            PlayerPrefs.SetInt("HighScore", highScore); // Save the new high score
        }

        // Display the high score
        highScoreText.text = "High Score: " + highScore.ToString();

        // Display the restart prompt
        restartPromptText.text = "Press any button to restart";

        // Hide all objects tagged with "Gameplay"
        HideGameplayObjects();

        // Mark the game as over
        isGameOver = true;
    }

    void HideGameplayObjects()
    {
        GameObject[] gameplayObjects = GameObject.FindGameObjectsWithTag("Gameplay");
        foreach (GameObject obj in gameplayObjects)
        {
            obj.SetActive(false); // Hide each gameplay object
        }
    }

    void RestartGame()
    {
        // Reload the current scene or restart the game in some other way
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
