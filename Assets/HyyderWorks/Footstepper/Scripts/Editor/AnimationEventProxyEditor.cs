using System;

namespace HyyderWorks.Footstepper.Editor
{
    using UnityEngine;
    using UnityEditor;
    using System.Linq;

    [CustomEditor(typeof(AnimationEventProxy))]
    public class AnimationEventProxyEditor : Editor
    {
        private AnimationEventProxy eventProxy;
        private GUISkin footstepperSkin;
        private Vector2 scrollPosition;

        // Foldout states
        private bool showEvents = true;
        private bool showHelp = false;
        private bool showRuntimeInfo = true;

        void OnEnable()
        {
            eventProxy = (AnimationEventProxy)target;
            footstepperSkin = Resources.Load<GUISkin>("Footstepper");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (footstepperSkin != null)
                GUI.skin = footstepperSkin;

            EditorGUILayout.BeginVertical(GUI.skin.GetStyle("Window"));

            DrawHeaderArea();
            GUILayout.Space(5);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawSectionBox("Event Mappings", showEvents, DrawEventsSection, ref showEvents);
            DrawSectionBox("Help & Setup", showHelp, DrawHelpSection, ref showHelp);

            if (Application.isPlaying)
            {
                DrawSectionBox("Runtime Info", showRuntimeInfo, DrawRuntimeInfoSection, ref showRuntimeInfo);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawHeaderArea()
        {
           
            GUIStyle titleStyle = new GUIStyle(GUI.skin.GetStyle("Title"));
            titleStyle.fontSize = Mathf.RoundToInt(EditorGUIUtility.currentViewWidth * 0.04f);
            titleStyle.fixedHeight = 0;

            GUIStyle subtitleStyle = new GUIStyle(GUI.skin.GetStyle("Subtitle"));
            subtitleStyle.fontSize = Mathf.RoundToInt(EditorGUIUtility.currentViewWidth * 0.025f);
            subtitleStyle.fixedHeight = 0;

            GUIStyle logoStyle = new GUIStyle(GUI.skin.GetStyle("LogoSmall"));
            logoStyle.fixedHeight = Mathf.RoundToInt(EditorGUIUtility.currentViewWidth * 0.2f);
            logoStyle.fixedWidth = Mathf.RoundToInt(EditorGUIUtility.currentViewWidth * 0.2f);
            logoStyle.margin = new RectOffset(Mathf.RoundToInt(EditorGUIUtility.currentViewWidth * 0.34f), 0, 0, 0);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("", logoStyle);
            GUILayout.Label("Animation Event Proxy", titleStyle);
        
            string subtitle = Application.isPlaying
                ? (eventProxy.events.Count == 0
                    ? "⚠️ No events configured"
                    : $"✅ {eventProxy.events.Count} events active")
                : $"☑️ {eventProxy.events?.Count} events configured";

            GUILayout.Label(subtitle, subtitleStyle);
            EditorGUILayout.EndVertical();
        }

        void DrawSectionBox(string titleText, bool isExpanded, Action drawContent, ref bool foldoutState)
        {
            
            GUIStyle containerStyle = new GUIStyle(GUI.skin.GetStyle("SectionContainer"));
            containerStyle.padding = new RectOffset(5, 5, 3, 3);

            GUIStyle headerStyle = new GUIStyle(GUI.skin.GetStyle("SectionHeaderBg"));
            headerStyle.fixedHeight = 35;

            GUIStyle headerLabelStyle = new GUIStyle(GUI.skin.GetStyle("SectionHeaderLabel"));
            headerLabelStyle.fontSize = 13;

            EditorGUILayout.BeginVertical(containerStyle);
            EditorGUILayout.BeginHorizontal(headerStyle);

            string arrow = foldoutState ? "▼" : "►";
            foldoutState = GUILayout.Toggle(foldoutState, $"{arrow} {titleText}", headerLabelStyle);

            EditorGUILayout.EndHorizontal();

            if (foldoutState)
            {
                EditorGUILayout.BeginVertical(GUI.skin.GetStyle("SectionContentBg"));
                drawContent();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(3);
        }

        void DrawEventsSection()
        {
            SerializedProperty eventsProperty = serializedObject.FindProperty("events");

            GUIStyle addBtnStyle = GUI.skin.GetStyle("ButtonSecondary");
            GUIStyle clearAllBtnStyle = GUI.skin.GetStyle("ButtonDanger");
           
            GUILayout.Label($"Configured Events: {eventsProperty.arraySize}");

            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Event", addBtnStyle))
            {
                eventsProperty.InsertArrayElementAtIndex(eventsProperty.arraySize);
                var newElement = eventsProperty.GetArrayElementAtIndex(eventsProperty.arraySize - 1);
                newElement.FindPropertyRelative("eventName").stringValue = "NewEvent";
            }

            if (GUILayout.Button("Clear All", clearAllBtnStyle))
            {
                if (EditorUtility.DisplayDialog("Clear All Events", "Remove all configured events?", "Yes", "Cancel"))
                {
                    eventsProperty.ClearArray();
                }
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Draw each event
            for (int i = 0; i < eventsProperty.arraySize; i++)
            {
                DrawEventElement(eventsProperty, i);
            }

            
            ShowDuplicateWarnings();
        }

        void DrawEventElement(SerializedProperty eventsProperty, int index)
        {
            SerializedProperty eventElement = eventsProperty.GetArrayElementAtIndex(index);
            SerializedProperty eventName = eventElement.FindPropertyRelative("eventName");
            SerializedProperty unityEvent = eventElement.FindPropertyRelative("onEventTriggered");

            // Event container
            EditorGUILayout.BeginVertical(GUI.skin.GetStyle("SoundsSectionBg"));

          
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Event {index + 1}", GUILayout.Width(60));

            GUISkin originalSkin = GUI.skin;
            GUI.skin = null;
            EditorGUILayout.PropertyField(eventName, GUIContent.none, GUILayout.Height(22));
            GUI.skin = originalSkin;
            GUIStyle deleteBtnStyle = GUI.skin.GetStyle("Delete");
            if (GUILayout.Button("", deleteBtnStyle))
            {
                eventsProperty.DeleteArrayElementAtIndex(index);
                return;
            }

            EditorGUILayout.EndHorizontal();

            // Unity Event
            GUILayout.Space(3);
            GUI.skin = null;
            EditorGUILayout.PropertyField(unityEvent, new GUIContent("Actions"));
            GUI.skin = originalSkin;

            EditorGUILayout.EndVertical();
            GUILayout.Space(3);
        }

        void ShowDuplicateWarnings()
        {
            var duplicates = eventProxy.events
                .GroupBy(e => e.eventName)
                .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
                .Select(g => g.Key);

            foreach (var duplicate in duplicates)
            {
                EditorGUILayout.HelpBox($"Duplicate event name: '{duplicate}'", MessageType.Warning);
            }
        }

        void DrawHelpSection()
        {
            DrawEmojiLabel("🎞️", "Animation Event Setup", 20);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            DrawEmojiLabel("✅", "In your Animation/Animation Import window, add Animation Events", 15);
            DrawEmojiLabel("✅", "Set the Function to: TriggerEvent", 15);
            DrawEmojiLabel("✅", "Set the String parameter to match your event names", 15);
            DrawEmojiLabel("✅", "Configure the UnityEvents below to respond", 15);

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            DrawEmojiLabel("💡", "Tips", 20);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            DrawEmojiLabel("⭕", "Event names are case-sensitive", 15);
            DrawEmojiLabel("⭕", "Events only trigger when clip weight > 0.5", 15);
            DrawEmojiLabel("⭕", "Use descriptive names like 'FootstepLeft', 'Attack'", 15);
            DrawEmojiLabel("⭕", "Check Console for event firing logs", 15);

            EditorGUILayout.EndVertical();
        }


        private void DrawEmojiLabel(string emoji, string message, int emojisize)
        {
            GUILayout.Label($"<size={emojisize}>{emoji}</size>{message}", GUI.skin.label);
        }

        void DrawRuntimeInfoSection()
        {
            if (!Application.isPlaying) return;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (eventProxy.events.Count == 0)
            {
                DrawEmojiLabel("❌", "No events configured", 20);
            }
            else
            {
                DrawEmojiLabel("☑️", $"{eventProxy.events.Count} events ready", 20);

                
                DrawEmojiLabel("", "Event Names:", 0);
                foreach (var eventMapping in eventProxy.events)
                {
                    if (!string.IsNullOrEmpty(eventMapping.eventName))
                    {
                        int listenerCount = eventMapping.onEventTriggered.GetPersistentEventCount();
                        string listenerInfo = listenerCount > 0 ? $"({listenerCount} listeners)" : "(no listeners)";
                        DrawEmojiLabel("⭕", $"{eventMapping.eventName} {listenerInfo}", 20);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

    }
}