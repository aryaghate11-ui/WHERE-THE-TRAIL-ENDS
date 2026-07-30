using UnityEngine;

public enum TriggerType
{
    CutsceneObject,    // body, bag, cairn etc
    PhoneCall,         // incoming call
    StoryEvent,        // fires story director event
    ZoneTransition,    // audio/lighting zone change
    Dialogue,          // face to face or internal
    JournalEntry       // unlocks journal page
}

public class TriggerManager : MonoBehaviour
{
    public static TriggerManager Instance;

    [Header("References")]
    public CutsceneSystem cutsceneSystem;
    public PhoneCallSystem phoneCallSystem;
    public ConversationSystem conversationSystem;
    public AudioDirector audioDirector;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void FireTrigger(
        TriggerType type,
        string dataID,
        Transform targetTransform = null)
    {
        Debug.Log($"[TriggerManager] {type} : {dataID}");

        switch (type)
        {
            case TriggerType.CutsceneObject:
                cutsceneSystem.PlayCutscene(
                    dataID, targetTransform);
                break;

            case TriggerType.PhoneCall:
                phoneCallSystem.TriggerCall(dataID);
                break;

            case TriggerType.StoryEvent:
                StoryDirector.Instance.TriggerEvent(
                    (StoryDirector.StoryEvent)
                    System.Enum.Parse(
                        typeof(StoryDirector.StoryEvent),
                        dataID));
                break;

            case TriggerType.ZoneTransition:
                audioDirector.TransitionToZone(dataID);
                break;

            case TriggerType.Dialogue:
               // conversationSystem.TriggerConversation(
                  //  dataID);
                break;
        }
    }
}