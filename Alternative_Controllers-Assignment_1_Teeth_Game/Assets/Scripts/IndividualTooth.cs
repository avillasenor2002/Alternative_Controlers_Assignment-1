using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using UnityEngine.Animations;

[System.Serializable]
public class ToothState
{
    [Header("Fill ONE of them (Clip has priority)")]
    public string name;
    public Sprite sprite;
    public AnimationClip clip;
    [Tooltip("Only when clip is set: should it loop?")]
    public bool clipLoops = true;
    [Tooltip("Only when clip is set: playback speed")]
    public float clipSpeed = 1f;
}

[ExecuteAlways]
public class IndividualTooth : MonoBehaviour
{
    [Header("Tooth States (0=Clean, 1=Drill, 2=Brush, 3=Hammer)")]
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

    [Header("Sprite/Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Animation Runtime (auto)")]
    [SerializeField] private Animator animForOutput;
    private PlayableGraph graph;
    private AnimationClipPlayable playable;
    private bool graphValid = false;

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip appearClip;
    [SerializeField] private AudioClip cleanClip;
    [SerializeField] private AudioClip drillStartClip;
    [SerializeField] private AudioClip drillEndClip;
    [SerializeField] private AudioClip brushTapClip;
    [SerializeField] private AudioClip hammerHitClip;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;
    [Range(0.5f, 1.5f)][SerializeField] private float pitchMin = 0.95f;
    [Range(0.5f, 1.5f)][SerializeField] private float pitchMax = 1.05f;

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

    // Shared tool control
    private Vector3 offscreenPosition = new Vector3(0, -1000, 0);
    private static GameObject activeTool = null;
    private static IndividualTooth activeTooth = null;

    // 🔹 NEW: Input lock to ensure only one arrow key action at a time
    private static KeyCode? activeArrowKey = null;

    // Wobble
    [SerializeField] private float wobbleSpeed = 10f;
    [SerializeField] private float wobbleAmount = 0.05f;

    private void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        EnsureAnimOutput();
        EnsureAudioSource();

        ApplySelectedState(true);

        if (drillTool) { drillTool.SetActive(true); MoveToolOffscreen(drillTool); }
        if (brushTool) { brushTool.SetActive(true); MoveToolOffscreen(brushTool); }
        if (hammerTool) { hammerTool.SetActive(true); MoveToolOffscreen(hammerTool); }

        UpdateToolFill(drillFillUI, 0f);
        UpdateToolFill(brushFillUI, 0f);
        UpdateToolFill(hammerFillUI, 0f);
    }

    private void OnEnable()
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        EnsureAnimOutput();
        EnsureAudioSource();
        ApplySelectedState();
    }

    private void OnDisable() { DestroyGraph(); }
    private void OnDestroy() { DestroyGraph(); }

    private void Update()
    {
        HandleInputs();
        UpdateMashTimer();
        UpdateWobbleAnimation();
        KeepActiveToolInPlace();

        // 🔹 Clear arrow lock when no arrow is being held
        if (activeArrowKey != null &&
            !Input.GetKey(KeyCode.LeftArrow) &&
            !Input.GetKey(KeyCode.RightArrow) &&
            !Input.GetKey(KeyCode.DownArrow))
        {
            activeArrowKey = null;
        }
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
        // 🔹 Only accept input if no other arrow is locked or the same arrow is held
        if (activeArrowKey != null && activeArrowKey != KeyCode.LeftArrow)
            return;

        bool holding = Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(associatedKey);

        if (holding)
        {
            activeArrowKey ??= KeyCode.LeftArrow;

            if (!drillActive)
            {
                drillActive = true;
                PlayOneShot(drillStartClip);
            }

            SetActiveTool(drillTool);
            holdTimer += Time.deltaTime;

            float progress = Mathf.Clamp01(holdTimer / holdDuration);
            UpdateToolFill(drillFillUI, progress);

            if (progress >= 1f)
            {
                PlayOneShot(drillEndClip);
                ReturnToClean();
                holdTimer = 0f;
                drillActive = false;
            }
        }
        else if (drillActive && !holding)
        {
            PlayOneShot(drillEndClip);
            holdTimer = 0f;
            drillActive = false;
            UpdateToolFill(drillFillUI, 0f);
            MoveToolOffscreen(drillTool);
        }
    }

    // ---------------- BRUSH ----------------
    private void HandleBrushInput()
    {
        if (activeArrowKey != null && activeArrowKey != KeyCode.DownArrow)
            return;

        bool pressing = Input.GetKey(KeyCode.DownArrow) && Input.GetKeyDown(associatedKey);

        if (pressing)
        {
            activeArrowKey ??= KeyCode.DownArrow;

            SetActiveTool(brushTool);
            brushActive = true;
            mashCount++;
            mashTimer = mashResetTime;

            PlayOneShot(brushTapClip);

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
        if (activeArrowKey != null && activeArrowKey != KeyCode.RightArrow)
            return;

        if (Input.GetKey(KeyCode.RightArrow) && Input.GetKeyDown(associatedKey))
        {
            activeArrowKey ??= KeyCode.RightArrow;

            SetActiveTool(hammerTool);
            UpdateToolFill(hammerFillUI, 1f);
            PlayOneShot(hammerHitClip);
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
        if (selectedStateIndex == 0)
        {
            PlayOneShot(cleanClip);
            PlayCleanParticles();
            return;
        }

        selectedStateIndex = 0;
        ApplySelectedState();

        PlayOneShot(cleanClip);
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
    private void EnsureAnimOutput()
    {
        animForOutput = GetComponent<Animator>();
        if (!animForOutput) animForOutput = gameObject.AddComponent<Animator>();
    }

    private void EnsureAudioSource()
    {
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = sfxVolume;
        }
    }

    private void ApplySelectedState(bool isInit = false)
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        EnsureAnimOutput();

        var st = GetStateOrNull(selectedStateIndex);

        StopClip();

        if (st != null && st.clip != null)
        {
            if (spriteRenderer) spriteRenderer.sprite = null;
            PlayClip(st.clip, st.clipLoops, Mathf.Approximately(st.clipSpeed, 0f) ? 1f : st.clipSpeed);
        }
        else if (spriteRenderer)
        {
            spriteRenderer.sprite = st != null ? st.sprite : null;
        }
    }

    private ToothState GetStateOrNull(int idx)
    {
        if (states == null || idx < 0 || idx >= states.Length) return null;
        return states[idx];
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

    // =================== Playables ===================
    private void PlayClip(AnimationClip clip, bool loop, float speed)
    {
        DestroyGraph();

        graph = PlayableGraph.Create("IndividualToothGraph");
        graph.SetTimeUpdateMode(Application.isPlaying ? DirectorUpdateMode.GameTime : DirectorUpdateMode.Manual);

        playable = AnimationClipPlayable.Create(graph, clip);
        playable.SetApplyFootIK(false);
        playable.SetApplyPlayableIK(false);
        playable.SetSpeed(speed);
        playable.SetDuration(loop ? double.PositiveInfinity : clip.length);

        var output = AnimationPlayableOutput.Create(graph, "ToothOutput", animForOutput);
        output.SetSourcePlayable(playable);

        graph.Play();
        graphValid = true;

        if (!Application.isPlaying) graph.Evaluate(0);
    }

    private void StopClip()
    {
        if (graphValid && graph.IsValid())
        {
            graph.Stop();
            graph.Destroy();
        }
        graphValid = false;
    }

    private void DestroyGraph()
    {
        if (graphValid && graph.IsValid())
        {
            graph.Stop();
            graph.Destroy();
        }
        graphValid = false;
    }

    // =================== Accessors ===================
    public void SetState(int newState)
    {
        if (newState < 0 || newState >= (states?.Length ?? 0)) return;

        int old = selectedStateIndex;
        selectedStateIndex = newState;

        if (old == 0 && newState > 0)
            PlayOneShot(appearClip);
        if (old > 0 && newState == 0)
            PlayOneShot(cleanClip);

        ApplySelectedState();
    }

    public int GetCurrentStateIndex() => selectedStateIndex;

    public string GetCurrentStateName()
    {
        var st = GetStateOrNull(selectedStateIndex);
        return st != null ? st.name : "Unknown";
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        EnsureAudioSource();
        sfxSource.volume = sfxVolume;
        sfxSource.pitch = Random.Range(pitchMin, pitchMax);
        sfxSource.PlayOneShot(clip);
    }
}
