using UnityEngine;

[System.Serializable]
public class ToothState
{
    public string name;   // e.g. "Clean", "Dirty", "Cavity", etc.
    public Sprite sprite; // Sprite for this state
}

[ExecuteAlways]
public class IndividualTooth : MonoBehaviour
{
    [Header("Tooth States (Name + Sprite)")]
    [SerializeField] private ToothState[] states = new ToothState[4];

    [Header("Current State (Select in Inspector)")]
    [SerializeField] private int selectedStateIndex = 0;

    [Header("Key Association")]
    [Tooltip("The key uniquely associated with this tooth (e.g., A, S, D, F)")]
    [SerializeField] private KeyCode associatedKey = KeyCode.None;

    [Header("Particle Effect")]
    [Tooltip("Particle effect to play when tooth returns to clean (state 0)")]
    [SerializeField] private ParticleSystem cleanParticleEffect;

    private SpriteRenderer spriteRenderer;

    // Mash logic for state 2
    private float mashTimer = 0f;
    private int mashCount = 0;
    private const int mashGoal = 5;
    private const float mashResetTime = 0.7f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on this GameObject!");
            enabled = false;
            return;
        }

        UpdateSprite();
    }

    void OnValidate()
    {
        if (states != null && states.Length > 0)
        {
            selectedStateIndex = Mathf.Clamp(selectedStateIndex, 0, states.Length - 1);
            ApplySelectedState();
        }
    }

    void Update()
    {
        HandleInputs();
        UpdateMashTimer();
    }

    private void HandleInputs()
    {
        if (associatedKey == KeyCode.None)
            return;

        switch (selectedStateIndex)
        {
            case 1:
                // HOLD: Right Arrow + Tooth Key
                if (Input.GetKey(KeyCode.RightArrow) && Input.GetKey(associatedKey))
                {
                    ReturnToClean();
                }
                break;

            case 2:
                // MASH: Down Arrow + Tooth Key
                if (Input.GetKey(KeyCode.DownArrow) && Input.GetKeyDown(associatedKey))
                {
                    mashCount++;
                    mashTimer = mashResetTime;

                    if (mashCount >= mashGoal)
                    {
                        ReturnToClean();
                        mashCount = 0;
                        mashTimer = 0;
                    }
                }
                break;

            case 3:
                // TAP: Right Arrow + Tooth Key
                if (Input.GetKey(KeyCode.RightArrow) && Input.GetKeyDown(associatedKey))
                {
                    ReturnToClean();
                }
                break;
        }
    }

    private void UpdateMashTimer()
    {
        if (mashTimer > 0)
        {
            mashTimer -= Time.deltaTime;
            if (mashTimer <= 0)
            {
                mashCount = 0; // too slow, reset
            }
        }
    }

    private void ReturnToClean()
    {
        if (selectedStateIndex == 0)
            return;

        selectedStateIndex = 0;
        ApplySelectedState();
        PlayCleanParticles();
        Debug.Log($"{gameObject.name} returned to CLEAN state (0)");
    }

    private void ApplySelectedState()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (states[selectedStateIndex].sprite != null)
        {
            spriteRenderer.sprite = states[selectedStateIndex].sprite;
        }
        else
        {
            Debug.LogWarning($"No sprite assigned for state {selectedStateIndex} ({states[selectedStateIndex].name})");
        }
    }

    private void UpdateSprite() => ApplySelectedState();

    private void PlayCleanParticles()
    {
        if (cleanParticleEffect != null)
        {
            cleanParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            cleanParticleEffect.Play();
        }
        else
        {
            Debug.LogWarning($"No particle system assigned on {gameObject.name}!");
        }
    }

    public void SetState(int newState)
    {
        if (newState < 0 || newState >= states.Length)
        {
            Debug.LogWarning("Invalid state index!");
            return;
        }

        selectedStateIndex = newState;
        ApplySelectedState();
        Debug.Log($"State changed to {selectedStateIndex} ({states[selectedStateIndex].name})");
    }

    public int GetCurrentStateIndex() => selectedStateIndex;
    public string GetCurrentStateName() => states[selectedStateIndex].name;
    public KeyCode GetAssociatedKey() => associatedKey;
}
