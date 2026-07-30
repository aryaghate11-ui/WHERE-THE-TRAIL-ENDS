using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene Names")]
    public string gameSceneName = "Level1";

    [Header("Options Panel")]
    public GameObject optionsPanel;

    [Header("Content Panels")]
    public GameObject controlsContent;
    public GameObject settingsContent;
    public GameObject musicContent;
    public GameObject graphicsContent;

    void Start()
    {
        // Make sure options is hidden at start
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        // Show controls by default when options opens
        HideAllContent();
    }

    // ── Main Menu Buttons ──────────────────────

    public void StartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void OpenOptions()
    {
        optionsPanel.SetActive(true);
        HideAllContent();
        ShowControls(); // default tab when opening
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    // ── Tab Buttons ────────────────────────────

    public void ShowControls()
    {
        HideAllContent();
        controlsContent.SetActive(true);
    }

    public void ShowSettings()
    {
        HideAllContent();
        settingsContent.SetActive(true);
    }

    public void ShowMusic()
    {
        HideAllContent();
        musicContent.SetActive(true);
    }

    public void ShowGraphics()
    {
        HideAllContent();
        graphicsContent.SetActive(true);
    }

    // ── Helper ─────────────────────────────────

    void HideAllContent()
    {
        controlsContent.SetActive(false);
        settingsContent.SetActive(false);
        musicContent.SetActive(false);
        graphicsContent.SetActive(false);
    }
}