using UnityEngine;

public class ToothSelector : MonoBehaviour
{
    [Header("Sprite Renderers to Update")]
    [SerializeField] private SpriteRenderer[] spriteRenderers; // Assign all possible SpriteRenderers

    [Header("Sprites for Numbers 1–5")]
    [SerializeField] private Sprite[] numberSprites; // 5 sprites (index 0 = 1, index 4 = 5)

    void Start()
    {
        UpdateRandomSprite();
    }

    public void UpdateRandomSprite()
    {
        // Generate random number (1–5)
        int randomNumber = Random.Range(1, 4);

        // Pick a random SpriteRenderer
        int randomRendererIndex = Random.Range(0, spriteRenderers.Length);

        // Update the sprite
        spriteRenderers[randomRendererIndex].sprite = numberSprites[randomNumber - 1];

        Debug.Log($"Set SpriteRenderer {randomRendererIndex} to sprite for number {randomNumber}");
    }

    void Update() 
    { 
        if (Input.GetMouseButtonDown(0)) 
        {
            UpdateRandomSprite();
        }
    }
}