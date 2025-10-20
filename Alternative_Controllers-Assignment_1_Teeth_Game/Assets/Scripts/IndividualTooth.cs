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

    [Header("Timer Connection")]
    [Tooltip("Reference to your UI Timer script in the scene")]
    [SerializeField] private Timer gameTimer;
    [SerializeField] private float timeBonus = 2f; // how much time to add when cleaned

    [Header("Wobble Animation")]
    [SerializeField] private float wobbleAmplitude = 0.1f; // how far it moves left/right
    [SerializeField] private float wobbleSpeed = 10f;      // speed of wobble oscillation

    private SpriteRenderer spriteRenderer;

    // Drill (hold) logic for state 1
    private float holdTimer = 0f;
    private const float holdDuration = 1.5f; // seconds required to drill clean

    // Mash logic for state 2
    private float mashTimer = 0f;
    private int mashCount = 0;
    private const int mashGoal = 5;
    private const float mashResetTime = 0.7f;

    // Wobble logic
    private Vector3 basePosition;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on this GameObject!");
            enabled = false;
            return;
        }

        basePosition = transform.localPosition;
        UpdateSprite();
    }

    void OnValidate()
    {
        if (states != null && states.Length > 0)
        {
            selectedStateIndex = Mathf.Clamp(selectedStateIndex, 0, states.Length - 1);
            ApplySelectedState();
        }

        // Keep basePosition in sync when editing in the inspector
        basePosition = transform.localPosition;
    }

    void Update()
    {
        HandleInputs();
        UpdateMashTimer();
        UpdateWobble();
    }

    private void HandleInputs()
    {
        if (associatedKey == KeyCode.None)
            return;

        switch (selectedStateIndex)
        {
            case 1: // HOLD: Right Arrow + Tooth Key (Drill behavior)
                if (Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(associatedKey))
                {
                    holdTimer += Time.deltaTime;
                    if (holdTimer >= holdDuration)
                    {
                        ReturnToClean();
                        holdTimer = 0f;
                    }
                }
                else
                {
                    holdTimer = 0f; // reset if released early
                }
                break;

            case 2: // MASH: Down Arrow + Tooth Key
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

            case 3: // TAP: Right Arrow + Tooth Key (Wobble state)
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

    private void UpdateWobble()
    {
        // Only wobble in the designated "wobble" state (state 3)
        if (selectedStateIndex == 3)
        {
            float offset = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmplitude;
            transform.localPosition = basePosition + new Vector3(offset, 0f, 0f);
        }
        else
        {
            // Ensure it returns to its original position when not wobbling
            transform.localPosition = basePosition;
        }
    }

    private void ReturnToClean()
    {
        if (selectedStateIndex == 0)
            return;

        selectedStateIndex = 0;
        ApplySelectedState();
        PlayCleanParticles();
        AddBonusTime();
        Debug.Log($"{gameObject.name} returned to CLEAN state (0)");
    }

    private void ApplySelectedState()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (states == null || states.Length == 0)
            return;

        if (selectedStateIndex < 0 || selectedStateIndex >= states.Length)
            return;

        if (states[selectedStateIndex].sprite != null)
        {
            spriteRenderer.sprite = states[selectedStateIndex].sprite;
        }
        else
        {
            Debug.LogWarning($"No sprite assigned for state {selectedStateIndex} ({states[selectedStateIndex].name})");
        }
    }

    // Fix: provide the missing UpdateSprite method
    private void UpdateSprite() => ApplySelectedState();

    private void PlayCleanParticles()
    {
        if (cleanParticleEffect != null)
        {
            cleanParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            cleanParticleEffect.Play();
        }
    }

    private void AddBonusTime()
    {
        if (gameTimer != null)
        {
            gameTimer.AddTime(timeBonus);
        }
        else
        {
            Debug.LogWarning($"No Timer assigned to {gameObject.name} — bonus time not applied!");
        }
    }

    public void SetState(int newState)
    {
        if (newState < 0 || states == null || newState >= states.Length)
        {
            Debug.LogWarning("Invalid state index!");
            return;
        }

        selectedStateIndex = newState;
        ApplySelectedState();
        Debug.Log($"State changed to {selectedStateIndex} ({states[selectedStateIndex].name})");

        // reset base position so wobble continues from current position
        basePosition = transform.localPosition;
    }

    public int GetCurrentStateIndex() => selectedStateIndex;
    public string GetCurrentStateName() => (states != null && states.Length > selectedStateIndex) ? states[selectedStateIndex].name : "";
    public KeyCode GetAssociatedKey() => associatedKey;
}
