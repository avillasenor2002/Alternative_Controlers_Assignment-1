using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    [Header("Scene Name to Load")]
    [SerializeField] private string sceneName;

    [Header("Key to Press")]
    [SerializeField] private KeyCode activationKey = KeyCode.Space;

    private void Update()
    {
        if (Input.anyKeyDown)
        {
            LoadScene();
        }
    }

    private void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Scene name is empty! Please assign a scene to load.");
        }
    }
}
