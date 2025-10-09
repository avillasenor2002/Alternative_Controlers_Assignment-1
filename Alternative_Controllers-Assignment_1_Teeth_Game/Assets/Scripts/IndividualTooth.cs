using UnityEngine;

public class IndividualTooth: MonoBehaviour
{
    [Header("Sprites for Each State (Order Matters)")]
    [SerializeField] private Sprite[] stateSprites = new Sprite[4]; // 4 states total

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int currentState = 0; // Index of the current state (0–3)

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

    void Update()
    {
        // Example input: switch state with number keys 1–4
        if (Input.GetKeyDown(KeyCode.Q)) SetState(0);
        if (Input.GetKeyDown(KeyCode.W)) SetState(1);
        if (Input.GetKeyDown(KeyCode.E)) SetState(2);
        if (Input.GetKeyDown(KeyCode.R)) SetState(3);
    }

    public void SetState(int newState)
    {
        if (newState < 0 || newState >= stateSprites.Length)
        {
            Debug.LogWarning("Invalid state index!");
            return;
        }

        currentState = newState;
        UpdateSprite();
        Debug.Log($"State changed to {currentState}");
    }

    private void UpdateSprite()
    {
        if (stateSprites[currentState] != null)
        {
            spriteRenderer.sprite = stateSprites[currentState];
        }
        else
        {
            Debug.LogWarning($"No sprite assigned for state {currentState}");
        }
    }

    public void NextState()
    {
        currentState = (currentState + 1) % stateSprites.Length;
        UpdateSprite();
    }

    public int GetCurrentState() => currentState;
}
