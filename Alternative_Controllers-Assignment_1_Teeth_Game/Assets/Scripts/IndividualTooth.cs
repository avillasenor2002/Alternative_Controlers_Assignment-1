using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private Timer gameTimer;
    [SerializeField] private float timeBonus = 2f;

    [Header("Tool System")]
    [Tooltip("Point where the tool will appear when interacting.")]
    [SerializeField] private Transform toolSpawnPoint;
    [Tooltip("Offscreen position where tools will rest when unused.")]
    [SerializeField] private Vector3 offscreenPosition = new Vector3(0, -1000, 0);

    private SpriteRenderer spriteRenderer;

    // Tool progress UI
    private Image toolFillUI;
    private GameObject activeTool;

    // Drill (hold)
    private float holdTimer = 0f;
    private const float holdDuration = 1.5f;

    // Mash (brush)
    private float mashTimer = 0f;
    private int mashCount = 0;
    private const int mashGoal = 5;
    private const float mashResetTime = 0.7f;

    // Wobble animation
    [SerializeField] private float wobbleSpeed = 10f;
    [SerializeField] private float wobbleAmount = 0.05f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on this GameObject!");
            enabled = false;
            return;
        }

        ApplySelectedState();
    }

    void Update()
    {
        HandleInputs();
        UpdateMashTimer();
        UpdateWobbleAnimation();
        UpdateToolProgressUI();
    }

    private void HandleInputs()
    {
        if (associatedKey == KeyCode.None)
            return;

        switch (selectedStateIndex)
        {
            case 1: // Drill (Hold)
                if (Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(associatedKey))
                {
                    ActivateTool<Drill>();
                    holdTimer += Time.deltaTime;

                    if (holdTimer >= holdDuration)
                    {
                        ReturnToClean();
                        holdTimer = 0f;
                    }
                }
                else
                {
                    DeactivateTool();
                    holdTimer = 0f;
                }
                break;

            case 2: // Brush (Mash)
                if (Input.GetKey(KeyCode.DownArrow) && Input.GetKeyDown(associatedKey))
                {
                    ActivateTool<Brush>();
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

            case 3: // Hammer (Tap)
                if (Input.GetKey(KeyCode.RightArrow) && Input.GetKeyDown(associatedKey))
                {
                    ActivateTool<Hammer>();
                    ReturnToClean();
                }
                break;

            default:
                DeactivateTool();
                break;
        }
    }

    private void UpdateMashTimer()
    {
        if (mashTimer > 0)
        {
            mashTimer -= Time.deltaTime;
            if (mashTimer <= 0)
                mashCount = 0;
        }
    }

    private void UpdateWobbleAnimation()
    {
        if (selectedStateIndex == 3 && spriteRenderer != null) // Wobble state
        {
            float angle = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount * 30f; // ±30 degrees
            spriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.transform.localRotation = Quaternion.identity;
        }
    }

    // Generic tool activator
    private void ActivateTool<T>() where T : MonoBehaviour
    {
        if (activeTool != null && activeTool.GetComponent<T>() != null)
            return;

        DeactivateTool();

        T toolScript = FindObjectOfType<T>();
        if (toolScript != null)
        {
            activeTool = toolScript.gameObject;

            if (toolSpawnPoint != null)
                activeTool.transform.position = toolSpawnPoint.position;

            // Find the child named "Fill" (or first Image)
            toolFillUI = activeTool.transform.Find("Fill")?.GetComponent<Image>();
            if (toolFillUI == null)
                toolFillUI = activeTool.GetComponentInChildren<Image>();
        }
    }

    private void DeactivateTool()
    {
        if (activeTool != null)
        {
            activeTool.transform.position = offscreenPosition;
            activeTool = null;
            toolFillUI = null;
        }
    }

    private void UpdateToolProgressUI()
    {
        if (toolFillUI == null)
            return;

        float progress = 0f;

        switch (selectedStateIndex)
        {
            case 1: // Drill
                progress = holdTimer / holdDuration;
                break;
            case 2: // Brush
                progress = (float)mashCount / mashGoal;
                break;
            case 3: // Hammer
                progress = 1f;
                break;
        }

        toolFillUI.fillAmount = Mathf.Clamp01(progress);
    }

    private void ReturnToClean()
    {
        if (selectedStateIndex == 0)
            return;

        selectedStateIndex = 0;
        ApplySelectedState();
        PlayCleanParticles();
        AddBonusTime();
        DeactivateTool();
    }

    private void ApplySelectedState()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (states != null && selectedStateIndex >= 0 && selectedStateIndex < states.Length)
        {
            if (states[selectedStateIndex].sprite != null)
                spriteRenderer.sprite = states[selectedStateIndex].sprite;
        }
    }

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
            gameTimer.AddTime(timeBonus);
    }

    public void SetState(int newState)
    {
        if (newState < 0 || newState >= states.Length)
            return;

        selectedStateIndex = newState;
        ApplySelectedState();
    }

    public int GetCurrentStateIndex() => selectedStateIndex;

    public string GetCurrentStateName()
    {
        if (states != null && selectedStateIndex >= 0 && selectedStateIndex < states.Length)
            return states[selectedStateIndex].name;
        return "Unknown";
    }
}
