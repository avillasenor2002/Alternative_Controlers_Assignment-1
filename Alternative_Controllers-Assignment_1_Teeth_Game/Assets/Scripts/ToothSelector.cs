using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ToothSelector : MonoBehaviour
{
    [Header("Timing Settings")]
    [Tooltip("How many seconds between selections at the start of the game.")]
    [SerializeField] private float startingInterval = 3f;

    [Tooltip("How much faster the interval decreases per second (e.g. 0.05 = 5% faster per second).")]
    [SerializeField] private float accelerationRate = 0.05f;

    [Tooltip("Minimum possible interval between selections.")]
    [SerializeField] private float minimumInterval = 0.5f;

    [Header("Debug Info (Read-Only)")]
    [SerializeField] private float currentInterval;
    [SerializeField] private float elapsedTime;

    private List<IndividualTooth> allTeeth = new List<IndividualTooth>();
    private bool running = true;

    void Start()
    {
        // Find all teeth in the scene
        allTeeth.Clear();
        allTeeth.AddRange(FindObjectsOfType<IndividualTooth>());

        if (allTeeth.Count == 0)
        {
            Debug.LogWarning("No IndividualTooth objects found in the scene!");
            enabled = false;
            return;
        }

        currentInterval = startingInterval;
        StartCoroutine(ToothSelectionRoutine());
    }

    private IEnumerator ToothSelectionRoutine()
    {
        while (running)
        {
            PickRandomToothAndChangeState();

            yield return new WaitForSeconds(currentInterval);

            // Time-based difficulty scaling
            elapsedTime += currentInterval;
            currentInterval = Mathf.Max(minimumInterval, currentInterval - (accelerationRate * elapsedTime));
        }
    }

    private void PickRandomToothAndChangeState()
    {
        // Get all teeth in state 0
        List<IndividualTooth> availableTeeth = new List<IndividualTooth>();
        foreach (var tooth in allTeeth)
        {
            if (tooth.GetCurrentStateIndex() == 0)
                availableTeeth.Add(tooth);
        }

        if (availableTeeth.Count == 0)
        {
            Debug.Log("No teeth in state 0 — skipping selection this round.");
            return;
        }

        // Pick one random tooth from available list
        IndividualTooth chosenTooth = availableTeeth[Random.Range(0, availableTeeth.Count)];

        // Pick a random new state between 1–3
        int randomState = Random.Range(1, 4);

        // Apply new state
        chosenTooth.SetState(randomState);

        Debug.Log($"Selected tooth '{chosenTooth.gameObject.name}' changed to state {randomState} ({chosenTooth.GetCurrentStateName()}).");
    }

    public void StopSelection() => running = false;
}
