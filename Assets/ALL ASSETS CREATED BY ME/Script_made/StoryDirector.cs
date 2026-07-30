using UnityEngine;
using System;

public class StoryDirector : MonoBehaviour
{
    public static StoryDirector Instance;

    public enum StoryEvent
    {
        GameStart,
        MorningRoundComplete,
        TrekkersMissing,
        SiddharthNameHeard,
        FirstBodyFound,
        SecondBodyFound,
        ThirdBodyFound,
        SiddharthCalls,
        RiyaFound,
        ConfrontationBegin,
        ConfrontationEnd,
        WalkBack,
        Ending
    }

    public static event Action<StoryEvent> OnStoryEvent;

    void Awake()
    {
        // singleton
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    public void TriggerEvent(StoryEvent storyEvent)
    {
        Debug.Log($"[StoryDirector] Event: {storyEvent}");
        OnStoryEvent?.Invoke(storyEvent);
    }
}