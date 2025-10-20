using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ToothState
{
    public string name;
    public Sprite sprite;
}

[ExecuteAlways]
public class IndividualTooth : MonoBehaviour
{
    [Header("Tooth States")]
    [SerializeField] private ToothState[] states = new ToothState[4];

    [Header("Current State")]
    [SerializeField] private int selectedStateIndex = 0;

    [Header("Key Association")]
    [SerializeField] private KeyCode associatedKey = KeyCode.None;

    [Header("Particle Effect")]
    [SerializeField] private ParticleSystem cleanParticleEffect;

    [Header("Timer Connection")]
    [SerializeField] private Timer gameTimer;
    [SerializeField] private float timeBonus = 2f;

    [Header("Tool System")]
    [SerializeField] private Transform toolSpawnPoint;

    [Header("Tools & Fill UI")]
    public GameObject drillTool;
    public Image drillFillUI;
    public GameObject brushTool;
    public Image brushFillUI;
    public GameObject hammerTool;
    public Image hammerFillUI;

    private SpriteRenderer spriteRenderer;

    // Drill Hold
    private float holdTimer = 0f;
    private const float holdDuration = 1.5f;
    private bool drillActive = false;

    // Brush Mash
    private float mashTimer = 0f;
    private int mashCount = 0;
    private const int mashGoal = 5;
    private const float mashResetTime = 0.7f;
    private bool brushActive = false;

    // Shared
    private Vector3 offscreenPosition = new Vector3(0, -1000, 0);
    private static GameObject activeTool = null;
    private static IndividualTooth activeTooth = null;

    // Wobble
    [SerializeField] private float wobbleSpeed = 10f;
    [SerializeField] private float wobbleAmount = 0.05f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplySelectedState();

        // Keep all tools active but start offscreen
        if (drillTool) { drillTool.SetActive(true); MoveToolOffscreen(drillTool); }
        if (brushTool) { brushTool.SetActive(true); MoveToolOffscreen(brushTool); }
        if (hammerTool) { hammerTool.SetActive(true); MoveToolOffscreen(hammerTool); }
    }

    private void Update()
    {
        HandleInputs();
        UpdateMashTimer();
        UpdateWobbleAnimation();
        KeepActiveToolInPlace();
    }

    // =================== INPUT HANDLING ===================
    private void HandleInputs()
    {
        if (associatedKey == KeyCode.None || selectedStateIndex == 0)
            return;

        switch (selectedStateIndex)
        {
            case 1: HandleDrillInput(); break;
            case 2: HandleBrushInput(); break;
            case 3: HandleHammerInput(); break;
        }
    }

    // ---------------- DRILL ----------------
    private void HandleDrillInput()
    {
        bool holding = Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(associatedKey);

        if (holding)
        {
            SetActiveTool(drillTool);
            drillActive = true;
            holdTimer += Time.deltaTime;

            float progress = Mathf.Clamp01(holdTimer / holdDuration);
            UpdateToolFill(drillFillUI, progress);

            if (progress >= 1f)
            {
                ReturnToClean();
                holdTimer = 0f;
                drillActive = false;
            }
        }
        else if (drillActive && !holding)
        {
            holdTimer = 0f;
            drillActive = false;
            UpdateToolFill(drillFillUI, 0f);
            MoveToolOffscreen(drillTool);
        }
    }

    // ---------------- BRUSH ----------------
    private void HandleBrushInput()
    {
        bool pressing = Input.GetKey(KeyCode.DownArrow) && Input.GetKeyDown(associatedKey);

        if (pressing)
        {
            SetActiveTool(brushTool);
            brushActive = true;
            mashCount++;
            mashTimer = mashResetTime;

            float progress = Mathf.Clamp01((float)mashCount / mashGoal);
            UpdateToolFill(brushFillUI, progress);

            if (progress >= 1f)
            {
                ReturnToClean();
                mashCount = 0;
                brushActive = false;
            }
        }
    }

    private void UpdateMashTimer()
    {
        if (mashTimer > 0)
        {
            mashTimer -= Time.deltaTime;
            if (mashTimer <= 0)
            {
                mashCount = 0;
                UpdateToolFill(brushFillUI, 0f);
                MoveToolOffscreen(brushTool);
                brushActive = false;
            }
        }
    }

    // ---------------- HAMMER ----------------
    private void HandleHammerInput()
    {
        if (Input.GetKey(KeyCode.RightArrow) && Input.GetKeyDown(associatedKey))
        {
            SetActiveTool(hammerTool);
            UpdateToolFill(hammerFillUI, 1f);
            ReturnToClean();
            Invoke(nameof(ClearHammer), 0.5f);
        }
    }

    private void ClearHammer()
    {
        MoveToolOffscreen(hammerTool);
        UpdateToolFill(hammerFillUI, 0f);
    }

    // =================== TOOL MOVEMENT ===================
    private void SetActiveTool(GameObject tool)
    {
        if (tool == null) return;

        // If a different tooth was active, clear its tool
        if (activeTooth != null && activeTooth != this)
            activeTooth.MoveToolOffscreen(activeTool);

        activeTool = tool;
        activeTooth = this;
        MoveToolToSpawn(tool);
    }

    private void MoveToolToSpawn(GameObject tool)
    {
        if (tool != null && toolSpawnPoint != null)
            tool.transform.position = toolSpawnPoint.position;
    }

    private void MoveToolOffscreen(GameObject tool)
    {
        if (tool != null)
            tool.transform.position = offscreenPosition;
    }

    private void KeepActiveToolInPlace()
    {
        if (activeTool != null && activeTooth == this && toolSpawnPoint != null)
        {
            activeTool.transform.position = toolSpawnPoint.position;
        }
    }

    private void UpdateToolFill(Image fillUI, float progress)
    {
        if (fillUI != null)
            fillUI.fillAmount = Mathf.Clamp01(progress);
    }

    // =================== VISUALS ===================
    private void UpdateWobbleAnimation()
    {
        if (selectedStateIndex == 3 && spriteRenderer != null)
        {
            float angle = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount * 30f;
            spriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.transform.localRotation = Quaternion.identity;
        }
    }

    // =================== CLEAN STATE ===================
    private void ReturnToClean()
    {
        if (selectedStateIndex == 0) return;

        selectedStateIndex = 0;
        ApplySelectedState();
        PlayCleanParticles();
        AddBonusTime();

        MoveToolOffscreen(drillTool);
        MoveToolOffscreen(brushTool);
        MoveToolOffscreen(hammerTool);

        UpdateToolFill(drillFillUI, 0f);
        UpdateToolFill(brushFillUI, 0f);
        UpdateToolFill(hammerFillUI, 0f);

        activeTool = null;
        activeTooth = null;
    }

    // =================== HELPERS ===================
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

    // =================== ACCESSORS ===================
    public void SetState(int newState)
    {
        if (newState < 0 || newState >= states.Length) return;
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
