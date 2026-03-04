using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenu : MonoBehaviour
{
    private const int MainMenuSceneIndex = 0;
    private const string WinSceneName = "Win";
    private const string LostSceneName = "Lost";

    private void Update()
    {
        if (!IsEndScene())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Quit();
            return;
        }

        bool wantsMainMenu =
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space);

        if (wantsMainMenu)
        {
            LoadMainMenu();
        }
    }

    private bool IsEndScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName == WinSceneName || sceneName == LostSceneName;
    }

    public void Play()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        Time.timeScale = 1f;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(MainMenuSceneIndex);
    }

    public void Quit()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif

        Debug.Log("Player Quit");
    }
}
