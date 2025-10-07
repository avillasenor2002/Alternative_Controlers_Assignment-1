using UnityEngine;
using TMPro;

public class IconSelectorScript : MonoBehaviour
{
    // Reference to the TextMeshPro component
    public TextMeshProUGUI textMeshPro;

    // Pool of letters to randomly pick from
    public string[] letterPool = { "A", "Z", "X", "D", "F", "V", "B", "H" };

    // Number of letters to select
    public int numberOfLetters = 5;

    // String to store the selected random letters
    private string selectedLetters = "";

    // Hex color for special letters (can be set via inspector)
    public string specialLetterColorHex = "#FF0000"; // Example: Red

    // Index of the special letter in the sequence
    private int specialLetterIndex;

    void Start()
    {
        // Initialize the text with random letters
        UpdateTextWithRandomLetters();
    }

    // Method to update the selected random letters
    public void UpdateTextWithRandomLetters()
    {
        selectedLetters = "";

        // Select a random index for the special letter
        specialLetterIndex = Random.Range(0, numberOfLetters);

        // Loop to select random letters based on the current numberOfLetters
        for (int i = 0; i < numberOfLetters; i++)
        {
            int randomIndex = Random.Range(0, letterPool.Length); // Pick a random index
            selectedLetters += letterPool[randomIndex]; // Add the randomly selected letter to the string
        }

        // Update the TextMeshPro text with color applied to the special letter
        UpdateTextWithSpecialLetterColor();
    }

    // Update the TextMeshPro text to apply color to the special letter
    private void UpdateTextWithSpecialLetterColor()
    {
        string formattedText = "";

        // Loop through the selected letters and format the special letter with the color
        for (int i = 0; i < selectedLetters.Length; i++)
        {
            if (i == specialLetterIndex)
            {
                // Apply color to the special letter
                formattedText += $"<color={specialLetterColorHex}>{selectedLetters[i]}</color>";
            }
            else
            {
                formattedText += selectedLetters[i];
            }
        }

        textMeshPro.text = formattedText;
    }

    // Public method to set the number of letters
    public void SetNumberOfLetters(int newNumberOfLetters)
    {
        // Update the number of letters
        numberOfLetters = newNumberOfLetters;

        // Call UpdateTextWithRandomLetters to refresh the selected letters
        UpdateTextWithRandomLetters();
    }

    // Public method to get the selected letters
    public string GetSelectedLetters()
    {
        return selectedLetters;
    }

    // Public method to get the index of the special letter
    public int GetSpecialLetterIndex()
    {
        return specialLetterIndex;
    }
}
