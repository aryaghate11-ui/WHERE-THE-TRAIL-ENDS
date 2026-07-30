using UnityEngine;

public class InspectableObject : MonoBehaviour
{
    [Header("Inspection")]
    [TextArea]
    public string description;
    // this feeds into your existing descriptionText UI
    // nothing changes here

    [Header("Evidence — optional")]
    [Tooltip("Leave false if this object " +
             "is just decorative inspection")]
    public bool isEvidence = false;
    public string evidenceID;
    public string evidenceName;

    [Header("Story Event — optional")]
    public bool triggersStoryEvent = false;
    public StoryDirector.StoryEvent storyEvent;

    [Header("Journal — optional")]
    public bool addsJournalEntry = false;
    [TextArea(2, 5)]
    public string journalEntry;

    // ─── State ───────────────────────────────────────
    public bool hasBeenInspected = false;

    // called by Object_interact when player inspects
    public void OnInspected()
    {
        if (hasBeenInspected) return;
        hasBeenInspected = true;

        Debug.Log($"[Inspect] {evidenceName}");

        if (triggersStoryEvent)
            StoryDirector.Instance?.TriggerEvent(
                storyEvent);
    }
}