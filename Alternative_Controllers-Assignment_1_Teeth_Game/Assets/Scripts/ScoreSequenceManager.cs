using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreSequenceManager : MonoBehaviour
{
    // Reference to the IconSelectorScript
    public IconSelectorScript iconSelectorScript;

    // Float to track the player's score
    public int score = 0;

    public bool event1 = false;
    public bool event2 = false;
    public bool event3 = false;
    public TextMeshProUGUI textMeshPro;

    // Method to increase the score
    public void IncreaseScore()
    {
        score += 1;
        Debug.Log("Score: " + score);
    }

    void Start()
    {
        // Optionally set the initial number of letters
        iconSelectorScript.SetNumberOfLetters(3); // Set to select 3 random letters initially
    }

    private void Update()
    {
        if (score > 8 && event1 == false)
        {
            iconSelectorScript.SetNumberOfLetters(4);
            event1 = true;
        }
        
        if (score > 20 && event2 == false)
        {
            iconSelectorScript.SetNumberOfLetters(5);
            event2 = true;
        }
        
        if (score > 32 && event3 == false)
        {
            iconSelectorScript.SetNumberOfLetters(6);
            event3 = true;
        }

        string ScoreString = score.ToString();
        textMeshPro.text = ScoreString;
    }
}