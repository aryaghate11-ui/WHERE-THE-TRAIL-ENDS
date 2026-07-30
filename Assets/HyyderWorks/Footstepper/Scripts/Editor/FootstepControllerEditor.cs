using System;

namespace HyyderWorks.Footstepper.Editor
{
    using UnityEngine;
    using UnityEditor;
    using System.Reflection;

    [CustomEditor(typeof(FootstepController))]
    public class FootstepControllerEditor : Editor
    {
        private FootstepController footstepController;
        private GUISkin footstepperSkin;
        private Vector2 scrollPosition;

        // Foldout states
        private bool showConfiguration = true;
        private bool showDetection = true;
        private bool showTriggerMode = true;
        private bool showDebug = false;
        private bool showRuntimeInfo = true;

        void OnEnable()
        {
            footstepController = (FootstepController)target;
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

            DrawSectionBox("Configuration", showConfiguration, DrawConfigurationSection, ref showConfiguration);
            DrawSectionBox("Detection", showDetection, DrawDetectionSection, ref showDetection);
            DrawSectionBox("Trigger Mode", showTriggerMode, DrawTriggerModeSection, ref showTriggerMode);
            DrawSectionBox("Debug", showDebug, DrawDebugSection, ref showDebug);

            if (Application.isPlaying)
            {
                DrawSectionBox("Runtime Info", showRuntimeInfo, DrawRuntimeInfoSection, ref showRuntimeInfo);
            }

            EditorGUILayout.EndScrollView();

            DrawActionButtons();

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawHeaderArea()
        {
            
            GUIStyle titleStyle = new GUIStyle(GUI.skin.GetStyle("Title"));
            titleStyle.fontSize = Mathf.RoundToInt(EditorGUIUtility.currentViewWidth * 0.1f);

            GUIStyle subtitleStyle = new GUIStyle(GUI.skin.GetStyle("Subtitle"));


            GUIStyle logoStyle = new GUIStyle(GUI.skin.GetStyle("LogoSmall"));
            logoStyle.margin = new RectOffset(Mathf.RoundToInt(EditorGUIUtility.currentViewWidth * 0.3f), 0, 0, 0);
            logoStyle.fixedHeight = Mathf.RoundToInt(EditorGUIUtility.currentViewWidth * 0.3f);
            logoStyle.fixedWidth = Mathf.RoundToInt(EditorGUIUtility.currentViewWidth * 0.3f);


            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("", logoStyle);
            GUILayout.Label("Footstep Controller", titleStyle);

            string subtitle = Application.isPlaying
                ? (footstepController.footstepData == null ? "⚠️ Missing data" : "✅ Active")
                : "☑️ Ready ️";

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
            headerLabelStyle.fontSize = 14;

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

        void DrawConfigurationSection()
        {
            // Footstep Data
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Footstep Data", GUILayout.Width(120));

            GUISkin originalSkin = GUI.skin;
            GUI.skin = null;
            SerializedProperty footstepDataProp = serializedObject.FindProperty("footstepData");
            EditorGUILayout.PropertyField(footstepDataProp, GUIContent.none);
            GUI.skin = originalSkin;

            if (footstepController.footstepData != null)
            {
                GUIStyle viewbtnStyle = GUI.skin.GetStyle("View");
                if (GUILayout.Button("", viewbtnStyle))
                {
                    var window = EditorWindow.GetWindow<FootstepperEditorWindow>();
                    window.Show();
                }
            }

            EditorGUILayout.EndHorizontal();

            // Audio Source
            DrawPropertyField("audioSource", "Audio Source");
            GUIStyle autoSetupBtnStyle = GUI.skin.GetStyle("ButtonSecondary");
            // Auto-setup button if missing
            if (footstepController.audioSource == null)
            {
                if (GUILayout.Button("Auto-Setup Audio Source", autoSetupBtnStyle))
                {
                    SetupAudioSource();
                }
            }
        }

        void DrawDetectionSection()
        {
            DrawPropertyField("raycastOrigin", "Raycast Origin");
            DrawSliderField("raycastDistance", 0.1f, 10f, "Distance");
            DrawSliderField("raycastOffset", 0f, 1f, "Offset");
        }

        void DrawTriggerModeSection()
        {
            DrawPropertyField("triggerMode", "Trigger Mode");

            // Show distance progress in play mode for distance trigger
            if (Application.isPlaying &&
                footstepController.triggerMode == FootstepController.FootstepTriggerMode.Distance)
            {
                DrawDistanceProgress();
            }

        }

        void DrawDistanceProgress()
        {
            if (footstepController.footstepData == null) return;

            var type = typeof(FootstepController);
            var distanceField = type.GetField("distanceTraveled", BindingFlags.NonPublic | BindingFlags.Instance);

            if (distanceField != null)
            {
                float distanceTraveled = (float)distanceField.GetValue(footstepController);
                float stepDistance = footstepController.footstepData.stepDistance;
                float progress = distanceTraveled / stepDistance;

                GUILayout.Label($"Progress: {distanceTraveled:F2}m / {stepDistance:F2}m");
                Rect progressRect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(progressRect, progress, $"{(progress * 100):F0}%");
            }
        }

        void DrawDebugSection()
        {
            DrawPropertyField("enableDebug", "Enable Debug");
        }

        void DrawRuntimeInfoSection()
        {
            if (!Application.isPlaying) return;

            // Ground detection status
            var type = typeof(FootstepController);
            var isGroundedField = type.GetField("isGrounded", BindingFlags.NonPublic | BindingFlags.Instance);

            if (isGroundedField != null)
            {
                bool isGrounded = (bool)isGroundedField.GetValue(footstepController);
                string status = isGrounded ? "✅ Grounded" : "❌ Not Grounded";
                GUILayout.Label(status);
            }

            // Audio status
            if (footstepController.audioSource != null)
            {
                bool isPlaying = footstepController.audioSource.isPlaying;
                string audioStatus = isPlaying ? "🔊 Playing" : "🔇 Silent";
                GUILayout.Label(audioStatus);
            }
        }

        void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();


            GUIStyle secondaryButtonStyle = GUI.skin.GetStyle("ButtonSecondary");
            if (GUILayout.Button("Reset", secondaryButtonStyle))
            {
                if (EditorUtility.DisplayDialog("Reset Settings", "Reset all settings to defaults?", "Yes", "Cancel"))
                {
                    ResetToDefaults();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // Helper methods
        void DrawPropertyField(string propertyName, string label)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(120));

            GUISkin originalSkin = GUI.skin;
            GUI.skin = null;
            SerializedProperty prop = serializedObject.FindProperty(propertyName);
            EditorGUILayout.PropertyField(prop, GUIContent.none);
            GUI.skin = originalSkin;

            EditorGUILayout.EndHorizontal();
        }

        void DrawSliderField(string propertyName, float min, float max, string label)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(120));

            GUISkin originalSkin = GUI.skin;
            GUI.skin = null;
            SerializedProperty prop = serializedObject.FindProperty(propertyName);
            EditorGUILayout.Slider(prop, min, max, GUIContent.none);
            GUI.skin = originalSkin;

            EditorGUILayout.EndHorizontal();
        }

        void SetupAudioSource()
        {
            Undo.RecordObject(footstepController, "Setup Audio Source");

            AudioSource audioSource = footstepController.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = footstepController.gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.volume = 0.7f;

            footstepController.audioSource = audioSource;
            EditorUtility.SetDirty(footstepController);
        }

        void ResetToDefaults()
        {
            Undo.RecordObject(footstepController, "Reset Footstep Controller");

            footstepController.raycastDistance = 2f;
            footstepController.raycastOffset = 0.1f;
            footstepController.triggerMode = FootstepController.FootstepTriggerMode.Distance;
            footstepController.enableDebug = false;

            if (footstepController.raycastOrigin == null)
            {
                footstepController.raycastOrigin = footstepController.transform;
            }

            EditorUtility.SetDirty(footstepController);
        }
    }
}