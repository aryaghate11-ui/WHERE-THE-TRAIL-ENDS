using UnityEngine;

public class EvidenceObject : MonoBehaviour
{
    [Header("Identity")]
    public string evidenceID;
    public string evidenceName;

    [TextArea(2, 4)]
    public string description;
    // shown in journal when photographed

    [Header("State")]
    public bool photographed = false;
    public bool canBeInspected = true;

    [Header("Story Event")]
    public bool triggersStoryEvent = false;
    public StoryDirector.StoryEvent storyEvent;

    [Header("Journal")]
    public bool addsJournalEntry = false;
    [TextArea(2, 6)]
    public string journalEntry;

    // called by PhoneController when photographed
    public void OnPhotographed()
    {
        if (photographed) return;

        photographed = true;

        Debug.Log($"[Evidence] Photographed: {evidenceName}");

        if (triggersStoryEvent)
            StoryDirector.Instance.TriggerEvent(storyEvent);
    }

    // called by inspection system when player inspects
    public void OnInspected()
    {
        if (!canBeInspected) return;

        Debug.Log($"[Evidence] Inspected: {evidenceName}");

        // trigger story event on inspect
        // even if not photographed
        if (triggersStoryEvent && !photographed)
            StoryDirector.Instance.TriggerEvent(storyEvent);
    }
}