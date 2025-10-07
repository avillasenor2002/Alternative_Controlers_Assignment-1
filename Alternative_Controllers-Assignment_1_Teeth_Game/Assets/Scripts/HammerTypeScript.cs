using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HammerTypeScript : MonoBehaviour
{
    // Reference to the TextMeshPro component
    public TextMeshProUGUI textMeshPro;

    // Reference to the IconSelectorScript to access the selected letters
    public IconSelectorScript iconSelectorScript;

    // Reference to the ScoreSequenceManager to update the score
    public ScoreSequenceManager scoreSequenceManager;

    // Pool of letters the player is allowed to type
    public string[] allowedLetterPool = { "A", "Z", "X", "D", "F", "V", "B", "H" };

    // String to store the player's typed characters
    private string typedLetters = "";

    // Other variables and references
    public TextMeshProUGUI textMeshPro2;

    // Add a boolean to control if inputs are allowed
    private bool isInputAllowed = true;

    void Update()
    {
        // Check for player input, using GetKeyDown to only register first keypress
        foreach (string allowedLetter in allowedLetterPool)
        {
            if (isInputAllowed && Input.GetKeyDown(allowedLetter.ToLower()))
            {
                char c = allowedLetter[0]; // Convert the string to a character

                // Add the letter to typed letters
                typedLetters += c;

                // Check if the typed letters match the randomly selected letters
                if (IsMatchingSequence())
                {
                    UpdateText(); // Update the TextMeshPro with the typed letters

                    // If the sequence is fully matched, increase the score
                    if (typedLetters.Length == iconSelectorScript.GetSelectedLetters().Length)
                    {
                        // Increase the score in ScoreSequenceManager
                        scoreSequenceManager.IncreaseScore();

                        // Clear the typed letters for the next round
                        typedLetters = "";

                        // Regenerate the letter sequence in IconSelectorScript
                        iconSelectorScript.UpdateTextWithRandomLetters();

                        // Update the text display with the new sequence
                        UpdateText();
                    }
                }
                else
                {
                    // If there's a mismatch, clear typed letters
                    typedLetters = "";
                    UpdateText(); // Clear the TextMeshPro
                }
            }
        }
    }

    // Function to check if the letter is in the allowed pool
    bool IsAllowedLetter(char letter)
    {
        foreach (string allowedLetter in allowedLetterPool)
        {
            if (allowedLetter == letter.ToString())
            {
                return true;
            }
        }
        return false;
    }

    // Function to check if the typed letters match the selected letters
    bool IsMatchingSequence()
    {
        string selectedLetters = iconSelectorScript.GetSelectedLetters();

        // If typed letters exceed the selected sequence, return false
        if (typedLetters.Length > selectedLetters.Length)
            return false;

        // Compare typed letters with the corresponding portion of the selected letters
        for (int i = 0; i < typedLetters.Length; i++)
        {
            if (typedLetters[i] != selectedLetters[i])
            {
                return false;
            }
        }

        return true;
    }

    // Update the TextMeshPro text to reflect the typed letters
    void UpdateText()
    {
        string displayText = "";

        // Format typed letters with the special letter's color if it matches the sequence
        for (int i = 0; i < typedLetters.Length; i++)
        {
            if (i == iconSelectorScript.GetSpecialLetterIndex())
            {
                displayText += $"<color={iconSelectorScript.specialLetterColorHex}>{typedLetters[i]}</color>";
            }
            else
            {
                displayText += typedLetters[i];
            }
        }

        textMeshPro.text = displayText;
    }

    public void StopInput()
    {
        isInputAllowed = false; // Disable input when called
    }
}
