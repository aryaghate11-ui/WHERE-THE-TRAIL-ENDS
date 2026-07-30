using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Prompt")]
    public GameObject promptPanel;
    public TMP_Text promptText;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ShowPrompt(string text)
    {
        promptText.text = text;
        promptPanel.SetActive(true);
    }

    public void HidePrompt()
    {
        promptPanel.SetActive(false);
    }
}