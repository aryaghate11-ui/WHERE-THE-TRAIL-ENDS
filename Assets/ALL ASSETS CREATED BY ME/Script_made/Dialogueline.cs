using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Game/Dialogue Line")]
public class DialogueLine : ScriptableObject
{
    [TextArea(3, 6)]
    public string[] lines;          // Each string = one subtitle line shown in sequence
    public float displayTime = 3f;  // Seconds each line stays on screen
    public AudioClip voiceClip;     // Optional: narration audio
}