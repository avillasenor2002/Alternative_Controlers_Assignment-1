using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using UnityEngine.Animations;

[System.Serializable]
public class ToothState
{
    [Header("Fill ONE of them (Clip has priority)")]
    public string name;
    public Sprite sprite;                  // 静态图
    public AnimationClip clip;             // 单个动画（用 Playables 播放；无需 Legacy/Controller）
    [Tooltip("仅当 clip 有效：是否循环")]
    public bool clipLoops = true;
    [Tooltip("仅当 clip 有效：播放速度")]
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
    [SerializeField] private SpriteRenderer spriteRenderer; // 如不指定，将自动获取

    // ========= 单 Clip 播放（不需要 Animator Controller）=========
    [Header("Animation Runtime (auto)")]
    [SerializeField] private Animator animForOutput; // 仅作为 Playables 输出目标；自动补
    private PlayableGraph graph;
    private AnimationClipPlayable playable;
    private bool graphValid = false;

    // ========= Audio（出现/清理 + 操作音，可选） =========
    [Header("Audio (optional)")]
    [SerializeField] private AudioSource sfxSource;      // 建议 2D AudioSource
    [SerializeField] private AudioClip appearClip;       // 0 -> 非0：出现不良状态
    [SerializeField] private AudioClip cleanClip;        // 非0 -> 0：清理成功
    [SerializeField] private AudioClip drillStartClip;   // 钻头开始长按
    [SerializeField] private AudioClip drillEndClip;     // 钻头长按中断/完成
    [SerializeField] private AudioClip brushTapClip;     // 牙刷每次有效连点
    [SerializeField] private AudioClip hammerHitClip;    // 锤子命中
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;
    [Tooltip("为避免听感机械，给音高轻微随机")]
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

    // Wobble（锤子状态时轻微摆动）
    [SerializeField] private float wobbleSpeed = 10f;
    [SerializeField] private float wobbleAmount = 0.05f;

    // ===================== Unity Lifecycle =====================
    private void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        EnsureAnimOutput();
        EnsureAudioSource();

        ApplySelectedState(true);

        // 工具全部激活但移出屏幕
        if (drillTool) { drillTool.SetActive(true); MoveToolOffscreen(drillTool); }
        if (brushTool) { brushTool.SetActive(true); MoveToolOffscreen(brushTool); }
        if (hammerTool) { hammerTool.SetActive(true); MoveToolOffscreen(hammerTool); }

        // 清零 UI
        UpdateToolFill(drillFillUI, 0f);
        UpdateToolFill(brushFillUI, 0f);
        UpdateToolFill(hammerFillUI, 0f);
    }

    private void OnEnable()
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        EnsureAnimOutput();
        EnsureAudioSource();
        ApplySelectedState(); // 以防编辑器刷新后不显示
    }

    private void OnDisable() { DestroyGraph(); }
    private void OnDestroy() { DestroyGraph(); }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null)
            {
                if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
                EnsureAnimOutput();
                EnsureAudioSource();
                ApplySelectedState();
            }
        };
    }
#endif

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
            case 1: HandleDrillInput(); break;  // Drill: hold
            case 2: HandleBrushInput(); break;  // Brush: mash
            case 3: HandleHammerInput(); break; // Hammer: instant
        }
    }

    // ---------------- DRILL ----------------
    private void HandleDrillInput()
    {
        bool holding = Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(associatedKey);

        if (holding)
        {
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
            // 释放/中断
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
        bool pressing = Input.GetKey(KeyCode.DownArrow) && Input.GetKeyDown(associatedKey);

        if (pressing)
        {
            SetActiveTool(brushTool);
            brushActive = true;
            mashCount++;
            mashTimer = mashResetTime;

            // 每次有效点击给反馈音
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
        if (Input.GetKey(KeyCode.RightArrow) && Input.GetKeyDown(associatedKey))
        {
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

        // 如果上一颗牙处于激活状态，先把其工具移出
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
        // 仅在 Hammer 状态（index==3）做轻微摆动
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
            // 已经是干净：仍然给一个清理音/粒子手感（可选）
            PlayOneShot(cleanClip);
            PlayCleanParticles();
            return;
        }

        selectedStateIndex = 0;
        ApplySelectedState();

        // 清理完成音效 + 特效 + 加时
        PlayOneShot(cleanClip);
        PlayCleanParticles();
        AddBonusTime();

        // 工具收尾
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
        if (!animForOutput) animForOutput = gameObject.AddComponent<Animator>(); // 仅做 Playables 输出目标
    }

    private void EnsureAudioSource()
    {
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D 音效
            sfxSource.volume = sfxVolume;
        }
    }

    private void ApplySelectedState(bool isInit = false)
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        EnsureAnimOutput();

        var st = GetStateOrNull(selectedStateIndex);

        // 停旧动画
        StopClip();

        if (st != null && st.clip != null)
        {
            // 优先动画：清空静态图，播新 Clip
            if (spriteRenderer) spriteRenderer.sprite = null;
            PlayClip(st.clip, st.clipLoops, Mathf.Approximately(st.clipSpeed, 0f) ? 1f : st.clipSpeed);
        }
        else
        {
            // 静态 Sprite
            if (spriteRenderer)
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

    // =================== Playables (single clip) ===================
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

        // 编辑器非运行时采样首帧，立刻可见
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

    // =================== ACCESSORS（含音频触发） ===================
    public void SetState(int newState)
    {
        if (newState < 0 || newState >= (states?.Length ?? 0)) return;

        int old = selectedStateIndex;
        selectedStateIndex = newState;

        // 0 -> 非0：出现不良状态音效
        if (old == 0 && newState > 0)
            PlayOneShot(appearClip);

        // 非0 -> 0：清理成功音效（如果你调用 SetState(0) 走这条）
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

    // =================== Audio Helper ===================
    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        EnsureAudioSource();
        sfxSource.volume = sfxVolume;
        sfxSource.pitch = Random.Range(pitchMin, pitchMax);
        sfxSource.PlayOneShot(clip);
    }
}
