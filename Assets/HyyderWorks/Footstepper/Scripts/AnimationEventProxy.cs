namespace HyyderWorks.Footstepper
{
    using UnityEngine;
    using UnityEngine.Events;
    using System;
    using System.Collections.Generic;

    [Serializable]
    public struct NamedUnityEvent
    {
        [Tooltip("The name used in the Animation Event's string parameter. Must be an exact match.")]
        public string eventName;

        [Tooltip("The functions to call when this event is triggered by the animation.")]
        public UnityEvent onEventTriggered;
    }

    public class AnimationEventProxy : MonoBehaviour
    {
        [Header("Event Mappings")] [Tooltip("A list of all named events this proxy can respond to.")]
        public List<NamedUnityEvent> events;

        private Dictionary<string, UnityEvent> eventDictionary;

        private void Awake()
        {
            eventDictionary = new Dictionary<string, UnityEvent>();
            foreach (var namedEvent in events)
            {
                if (!eventDictionary.ContainsKey(namedEvent.eventName))
                    eventDictionary.Add(namedEvent.eventName, namedEvent.onEventTriggered);
                else
                    Debug.LogWarning($"Duplicate event name '{namedEvent.eventName}' on {name}.", this);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="animEvent">The AnimationEvent fired by Mecanim.</param>
        public void TriggerEvent(AnimationEvent animEvent)
        {
            
            string eventName = animEvent.stringParameter;

            
            if (animEvent.animatorClipInfo.weight <= 0.5f)
                return;

            Debug.Log(
                $"[AnimationEventProxy] Firing '{eventName}' from clip '{animEvent.animatorClipInfo.clip.name}' (weight={animEvent.animatorClipInfo.weight:F2})");

           
            if (eventDictionary.TryGetValue(eventName, out var thisEvent))
                thisEvent.Invoke();
            else
                Debug.LogWarning($"No mapping for '{eventName}' on {name}.", this);
        }
    }

}