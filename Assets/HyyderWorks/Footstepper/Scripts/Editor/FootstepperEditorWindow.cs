using System;

namespace HyyderWorks.Footstepper.Editor
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections.Generic;
    using System.Reflection; // For AudioUtil reflection

    public class FootstepperEditorWindow : EditorWindow
    {
        private FootstepsDatabase footstepData;
        private SerializedObject serializedFootstepData; // The SerializedObject for footstepData

        // SerializedProperties for direct access and modification
        private SerializedProperty propStepDistance;
        private SerializedProperty propDefaultVolume;
        private SerializedProperty propGroundLayerMask;
        private SerializedProperty propTextureFootsteps;
        private SerializedProperty propDefaultFootstepSounds;

        private Vector2 scrollPosition;
        private bool showSettings = true;
        private bool showTextures = true;
        private bool showDefaults = true;
        private Dictionary<int, bool> textureFootstepFoldouts = new Dictionary<int, bool>();

        private const string LAST_SELECTED_DATA_KEY = "FootstepEditor_LastSelectedData";
        private GUISkin footstepperSkin;

        // --- Caching GUIStyles for Performance ---
        private GUIStyle _cachedPreviewBoxStyle;
        
        private GUIStyle _interactiveBoxStyle;

        private GUIStyle InteractiveBoxStyle
        {
            get
            {
                // Create a new style based on the default 'box' which has an outline
                // This will only be created once for performance
                if (_interactiveBoxStyle == null)
                {
                    _interactiveBoxStyle = new GUIStyle(GUI.skin.box)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Italic, // Make helper text stand out
                    };
                    // Slightly darken the text for the "Drop Here" message
                    _interactiveBoxStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 1f);
                }

                return _interactiveBoxStyle;
            }
        }


        [MenuItem("Tools/Hyyderworks/Footstepper")]
        public static void ShowWindow()
        {
            var window = GetWindow<FootstepperEditorWindow>();
            window.titleContent = new GUIContent("Footstepper");
            window.minSize = new Vector2(700, 400);
            window.LoadLastSelectedData();
        }

        void OnEnable()
        {
            LoadLastSelectedData();
            footstepperSkin = Resources.Load<GUISkin>("Footstepper");
            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            // Initialize SerializedObject if footstepData is already loaded
            if (footstepData != null)
            {
                InitializeSerializedProperties();
            }
        }

        void OnDisable()
        {
            
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            // Dispose SerializedObject to prevent memory leaks
            if (serializedFootstepData != null)
            {
                serializedFootstepData.Dispose();
                serializedFootstepData = null;
            }
        }

        void OnUndoRedoPerformed()
        {
            
            if (serializedFootstepData != null)
            {
                serializedFootstepData.Update(); // Re-read all properties from the target object
            }

            Repaint();
            textureFootstepFoldouts.Clear();
        }

        void LoadLastSelectedData()
        {
            string path = EditorPrefs.GetString(LAST_SELECTED_DATA_KEY, "");
            if (!string.IsNullOrEmpty(path))
            {
                FootstepsDatabase loadedData = AssetDatabase.LoadAssetAtPath<FootstepsDatabase>(path);
                if (loadedData != null)
                {
                    footstepData = loadedData;
                    InitializeSerializedProperties();
                }
            }
        }

        void InitializeSerializedProperties()
        {
            if (footstepData == null) return;

            // Dispose of the old SerializedObject if it exists to avoid memory leaks
            if (serializedFootstepData != null)
            {
                serializedFootstepData.Dispose();
            }

            serializedFootstepData = new SerializedObject(footstepData);
            propStepDistance = serializedFootstepData.FindProperty("stepDistance");
            propDefaultVolume = serializedFootstepData.FindProperty("defaultVolume");
            propGroundLayerMask = serializedFootstepData.FindProperty("groundLayerMask");
            propTextureFootsteps = serializedFootstepData.FindProperty("textureFootsteps");
            propDefaultFootstepSounds = serializedFootstepData.FindProperty("defaultFootstepSounds");
        }

        void SaveLastSelectedData()
        {
            if (footstepData != null)
            {
                string path = AssetDatabase.GetAssetPath(footstepData);
                EditorPrefs.SetString(LAST_SELECTED_DATA_KEY, path);
            }
        }

        void OnGUI()
        {
            if (footstepperSkin != null)
                GUI.skin = footstepperSkin;

            // Window background
            EditorGUILayout.BeginVertical(GUI.skin.GetStyle("Window"));

            DrawHeader();
            DrawDataSelection();

            if (footstepData == null)
            {
                DrawNoDataMessage();
                EditorGUILayout.EndVertical();
                return;
            }

            // Apply any changes from the previous GUI frame and update the SerializedObject
            // This is crucial for undo/redo with SerializedProperties
            serializedFootstepData.Update();


            GUILayout.Space(10);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawSectionBox("General Settings", showSettings, DrawGeneralSettings, ref showSettings);
            GUILayout.Space(10);

            DrawSectionBox("Texture Footsteps", showTextures, DrawTextureFootsteps, ref showTextures);
            GUILayout.Space(10);

            DrawSectionBox("Default Sounds", showDefaults, DrawDefaultSounds, ref showDefaults);

            EditorGUILayout.EndScrollView();

            DrawSaveButton();


            GUILayout.FlexibleSpace();
            DrawFooter();

            EditorGUILayout.EndVertical();
            
            if (GUI.changed) 
            {
                serializedFootstepData.ApplyModifiedProperties();
            }
        }

        void DrawHeader()
        {
            GUIStyle titleStyle = GUI.skin.GetStyle("Title");
            GUIStyle subtitleStyle = GUI.skin.GetStyle("Subtitle");
            GUIStyle logoStyle = GUI.skin.GetStyle("Logo");

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("", logoStyle);
            GUILayout.Label("Footstepper", titleStyle);
            GUILayout.Label("A lightweight footstep solution", subtitleStyle);
            EditorGUILayout.EndVertical();
        }

        void DrawFooter()
        {
            GUIStyle subtitleStyle = GUI.skin.GetStyle("Tagline");

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("A solution by Hyyder Works", subtitleStyle);
            GUILayout.Label("Made with <size=20>💖</size> for the Unity community", subtitleStyle);
            EditorGUILayout.EndVertical();
        }

        void DrawDataSelection()
        {
            GUIStyle boxStyle = GUI.skin.box;
            GUIStyle labelStyle = GUI.skin.GetStyle("Label");

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.BeginHorizontal();
            
            var newData = DrawLabeledObjectField<FootstepsDatabase>(
                new GUIContent("Footstep Data", "The ScriptableObject asset containing all footstep configurations."),
                footstepData,
                labelStyle);

            if (newData != footstepData)
            {
                // When changing the database asset, we need to re-initialize SerializedObject
                footstepData = newData;
                SaveLastSelectedData();
                InitializeSerializedProperties(); // Re-initialize for the new asset
                textureFootstepFoldouts.Clear(); // Clear foldouts for new data context
                Repaint(); 
            }
            
            GUIStyle btnStyle = GUI.skin.GetStyle("Add");
            if (GUILayout.Button(new GUIContent("", "Create a new Footstep Database asset."), btnStyle))
            {
                CreateNewFootstepData();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }


        void DrawNoDataMessage()
        {
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical();
            
            GUIStyle logoStyle = GUI.skin.GetStyle("Empty");
            GUILayout.Label("", logoStyle); 
            
            GUIStyle messageStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            GUILayout.Label("Assign or create a footstep data", messageStyle);

            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
        }

        void DrawSectionBox(string titleText, bool isExpanded, Action drawContent, ref bool foldoutState)
        {
            GUIStyle containerStyle = GUI.skin.GetStyle("SectionContainer");
            GUIStyle headerBackground = GUI.skin.GetStyle("SectionHeaderBg");
            GUIStyle headerLabelStyle = GUI.skin.GetStyle("SectionHeaderLabel");
            GUIStyle contentBackground = GUI.skin.GetStyle("SectionContentBg");

            EditorGUILayout.BeginVertical(containerStyle);
            EditorGUILayout.BeginHorizontal(headerBackground);

            string arrow = foldoutState ? "▼" : "►";
            bool newFoldoutState = GUILayout.Toggle(foldoutState, $"{arrow} {titleText}", headerLabelStyle);
            if (newFoldoutState != foldoutState)
            {
                
                foldoutState = newFoldoutState;
            }

            EditorGUILayout.EndHorizontal();

            if (foldoutState)
            {
                EditorGUILayout.BeginVertical(contentBackground);
                drawContent();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        void DrawGeneralSettings()
        {
            GUIStyle labelStyle = GUI.skin.label;


            DrawLabeledSlider(propStepDistance, 0.25f, 5f,
                new GUIContent("Step Distance",
                    "The distance in Unity units after which a footstep sound should play."),
                labelStyle);
            DrawLabeledSlider(propDefaultVolume, 0f, 1f,
                new GUIContent("Default Volume", "The base volume for all footstep sounds."), labelStyle);

            DrawLabeledPropertyField(propGroundLayerMask,
                new GUIContent("Ground Layer Mask", "Which layers the footstep system should consider as ground."),
                labelStyle);
        }

        void DrawTextureFootsteps()
        {
            GUIStyle buttonSecondaryStyle = GUI.skin.GetStyle("ButtonSecondary");
            if (GUILayout.Button(
                    new GUIContent("Add New Footstep", "Add a new texture-specific footstep configuration."),
                    buttonSecondaryStyle))
            {
                propTextureFootsteps.arraySize++;

                SerializedProperty newElement =
                    propTextureFootsteps.GetArrayElementAtIndex(propTextureFootsteps.arraySize - 1);
                newElement.FindPropertyRelative("name").stringValue = ""; // Clear name
                newElement.FindPropertyRelative("texture").objectReferenceValue = null; // Clear texture
                newElement.FindPropertyRelative("terrainLayerIndex").intValue = -1; // Default to -1
                newElement.FindPropertyRelative("volumeMultiplier").floatValue = 1f; // Default
                newElement.FindPropertyRelative("pitchVariation").floatValue = 0.1f; // Default
                newElement.FindPropertyRelative("footstepSounds").arraySize = 0; // Empty sounds array

         
                textureFootstepFoldouts[propTextureFootsteps.arraySize - 1] = true;
                scrollPosition.y = float.MaxValue;
            }


            for (int i = 0; i < propTextureFootsteps.arraySize; i++)
            {
                DrawTexturePair(propTextureFootsteps.GetArrayElementAtIndex(i), i);
                GUILayout.Space(8);
            }
            
            DrawTerrainLayerWarnings();
        }
        
        void DrawTerrainLayerWarnings()
        {
            if (footstepData == null || propTextureFootsteps == null) return;

            HashSet<int> seenTerrainLayers = new HashSet<int>();
            bool hasDuplicates = false;
            for (int i = 0; i < propTextureFootsteps.arraySize; i++)
            {
                SerializedProperty pairProp = propTextureFootsteps.GetArrayElementAtIndex(i);
                SerializedProperty terrainLayerIndexProp = pairProp.FindPropertyRelative("terrainLayerIndex");
                int terrainLayerIndex = terrainLayerIndexProp.intValue;

                if (terrainLayerIndex >= 0) // Only check valid indices
                {
                    if (seenTerrainLayers.Contains(terrainLayerIndex))
                    {
                        hasDuplicates = true;
                        break;
                    }

                    seenTerrainLayers.Add(terrainLayerIndex);
                }
            }

            if (hasDuplicates)
            {
                EditorGUILayout.HelpBox(
                    "Warning: Multiple Texture Footstep entries use the same Terrain Layer Index. This may lead to unexpected behavior.",
                    MessageType.Warning);
            }
        }


        void DrawTexturePair(SerializedProperty pairProp, int index)
        {
            // Get individual properties from the pair's SerializedProperty
            SerializedProperty nameProp = pairProp.FindPropertyRelative("name");
            SerializedProperty textureProp = pairProp.FindPropertyRelative("texture");
            SerializedProperty terrainLayerIndexProp = pairProp.FindPropertyRelative("terrainLayerIndex");
            SerializedProperty volumeMultiplierProp = pairProp.FindPropertyRelative("volumeMultiplier");
            SerializedProperty pitchVariationProp = pairProp.FindPropertyRelative("pitchVariation");
            SerializedProperty footstepSoundsProp = pairProp.FindPropertyRelative("footstepSounds");

            if (!textureFootstepFoldouts.ContainsKey(index))
                textureFootstepFoldouts[index] = true;

            GUIStyle containerStyle = GUI.skin.GetStyle("SectionContainer");
            GUIStyle headerBackground = GUI.skin.GetStyle("SectionHeaderBg");
            GUIStyle headerLabelStyle = GUI.skin.GetStyle("SectionHeaderLabel");
            GUIStyle contentBackground = GUI.skin.GetStyle("SectionContentBg");
            GUIStyle labelStyle = GUI.skin.label;
            GUIStyle soundsSectionStyle = GUI.skin.GetStyle("SoundsSectionBg");
            GUIStyle deleteBtnStyle = GUI.skin.GetStyle("Delete");
            GUIStyle buttonStyle = GUI.skin.GetStyle("ButtonSecondary");

            EditorGUILayout.BeginVertical(containerStyle);

            EditorGUILayout.BeginHorizontal(headerBackground);
            string arrow = textureFootstepFoldouts[index] ? "▼" : "►";
            string title = GetPairName(pairProp, index);
            bool newFoldoutState =
                GUILayout.Toggle(textureFootstepFoldouts[index], $"{arrow} {title}", headerLabelStyle);
            if (newFoldoutState != textureFootstepFoldouts[index])
            {
                // The foldout state is managed by the dictionary, not a SerializedProperty.
                // Direct assignment is fine.
                textureFootstepFoldouts[index] = newFoldoutState;
            }

            if (GUILayout.Button(new GUIContent("", $"Remove {title}"), deleteBtnStyle))
            {
                // Remove element from SerializedProperty array. Undo is automatic with ApplyModifiedProperties().
                propTextureFootsteps.DeleteArrayElementAtIndex(index);
                // UX: Rebuild textureFootstepFoldouts to match new indices after removal
                Dictionary<int, bool> newFoldouts = new Dictionary<int, bool>();
                for (int i = 0; i < propTextureFootsteps.arraySize; i++)
                {
                    // Adjust index for the shift after deletion
                    newFoldouts[i] = textureFootstepFoldouts.ContainsKey(i + 1) ? textureFootstepFoldouts[i + 1] : true;
                }

                textureFootstepFoldouts = newFoldouts;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();

            if (textureFootstepFoldouts[index])
            {
                EditorGUILayout.BeginVertical(contentBackground);
                GUILayout.Space(5);

                DrawLabeledPropertyField(nameProp, new GUIContent("Name", "A descriptive name for this footstep type."),
                    labelStyle);
                DrawTexturePreviewField(
                    new GUIContent("Texture", "The texture associated with this footstep. Used for mesh renderers."),
                    textureProp, labelStyle, index);

                DrawLabeledPropertyField(terrainLayerIndexProp,
                    new GUIContent("Terrain Layer Index",
                        "The index of the terrain layer (0-indexed) associated with this footstep. Set to -1 for any unmatched terrain."),
                    labelStyle);
                DrawLabeledSlider(volumeMultiplierProp, 0f, 2f,
                    new GUIContent("Volume Multiplier", "Adjusts the volume for this specific footstep type."),
                    labelStyle);
                DrawLabeledSlider(pitchVariationProp, 0f, 0.5f,
                    new GUIContent("Pitch Variation",
                        "Adds a random pitch variation to footstep sounds (e.g., 0.1 for +/- 10% pitch)."), labelStyle);

                GUILayout.Space(8);

                
                EditorGUILayout.BeginVertical(soundsSectionStyle);
                GUILayout.Label("Footstep Sounds", labelStyle);

                if (footstepSoundsProp.arraySize > 0)
                {
                    for (int j = 0; j < footstepSoundsProp.arraySize; j++)
                    {
                        SerializedProperty soundElementProp = footstepSoundsProp.GetArrayElementAtIndex(j);
                        DrawLabeledObjectFieldWithDelete<AudioClip>(new GUIContent($"Sound {j + 1}"), soundElementProp,
                            labelStyle, footstepSoundsProp, j);
                    }
                }

         
                GUILayout.Space(5);
                if (GUILayout.Button(new GUIContent("Add New", "Add a new footstep sound for this texture."),
                        buttonStyle))
                {
                    footstepSoundsProp.arraySize++;
                    SerializedProperty newElement =
                        footstepSoundsProp.GetArrayElementAtIndex(footstepSoundsProp.arraySize - 1);
                    newElement.objectReferenceValue = null; // Initialize as null
                }

                EditorGUILayout.EndVertical(); // End sounds section

                GUILayout.Space(5);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        void DrawTexturePreviewField(GUIContent label, SerializedProperty textureProp, GUIStyle labelStyle, int index)
        {
            EditorGUILayout.BeginHorizontal();

           
            GUILayout.Label(label, labelStyle, GUILayout.Width(150));

            
            GUILayout.FlexibleSpace();

            // Get the current texture and prepare the preview content
            Texture2D texture = textureProp.objectReferenceValue as Texture2D;
            GUIContent previewContent = texture != null
                ? new GUIContent(texture)
                : new GUIContent("➕");

            float previewSize = 75f;

            
            GUILayout.Box(previewContent, InteractiveBoxStyle, GUILayout.Width(previewSize),
                GUILayout.Height(previewSize));
            Rect dropArea = GUILayoutUtility.GetLastRect();

            // Get a unique control ID for the object picker
            int controlID = GUIUtility.GetControlID(FocusType.Passive, dropArea);
            Event currentEvent = Event.current;
            EventType eventType = currentEvent.type;

            switch (eventType)
            {
                case EventType.MouseDown:
                    if (dropArea.Contains(currentEvent.mousePosition) && currentEvent.button == 0)
                    {
                        // 4. FIX WARNING: Temporarily use the default skin for the object picker
                        GUISkin originalSkin = GUI.skin; // Store the custom skin
                        GUI.skin = null; // Use the default Editor skin

                        EditorGUIUtility.ShowObjectPicker<Texture2D>(texture, false, "", controlID);

                        GUI.skin = originalSkin; // Restore the custom skin

                        currentEvent.Use(); // Consume the event
                    }

                    break;

                
                case EventType.ExecuteCommand:
                    if (currentEvent.commandName == "ObjectSelectorUpdated")
                    {
                        if (EditorGUIUtility.GetObjectPickerControlID() == controlID)
                        {
                            textureProp.objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
                            GUI.changed = true;
                            currentEvent.Use();
                        }
                    }

                    break;

                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (dropArea.Contains(currentEvent.mousePosition))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (eventType == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            foreach (var draggedObject in DragAndDrop.objectReferences)
                            {
                                if (draggedObject is Texture2D)
                                {
                                    textureProp.objectReferenceValue = draggedObject;
                                    GUI.changed = true;
                                    break;
                                }
                            }
                        }

                        currentEvent.Use();
                    }

                    break;
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        void DrawDefaultSounds()
        {
            GUIStyle labelStyle = GUI.skin.label;
            GUIStyle buttonStyle = GUI.skin.GetStyle("ButtonSecondary");

            if (propDefaultFootstepSounds.arraySize > 0)
            {
                for (int i = 0; i < propDefaultFootstepSounds.arraySize; i++)
                {
                    SerializedProperty soundElementProp = propDefaultFootstepSounds.GetArrayElementAtIndex(i);
                    DrawLabeledObjectFieldWithDelete<AudioClip>(new GUIContent($"Default Sound {i + 1}"),
                        soundElementProp,
                        labelStyle, propDefaultFootstepSounds, i);
                }
            }

            
            GUILayout.Space(5);
            if (GUILayout.Button(new GUIContent("Add New", "Add a new default footstep sound."), buttonStyle))
            {
                propDefaultFootstepSounds.arraySize++;
                SerializedProperty newElement =
                    propDefaultFootstepSounds.GetArrayElementAtIndex(propDefaultFootstepSounds.arraySize - 1);
                newElement.objectReferenceValue = null; // Initialize as null
            }
        }


        
        void DrawLabeledPropertyField(SerializedProperty property, GUIContent label, GUIStyle labelStyle)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(150));
            GUISkin originalSkin = GUI.skin;
            GUI.skin = null; // Use default Editor skin for PropertyField
            EditorGUILayout.PropertyField(property, GUIContent.none);
            GUI.skin = originalSkin; // Revert to custom skin
            EditorGUILayout.EndHorizontal();
        }


        void DrawLabeledSlider(SerializedProperty property, float leftValue, float rightValue, GUIContent label,
            GUIStyle labelStyle)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(150));
            GUISkin originalSkin = GUI.skin;
            GUI.skin = null; // Use default Editor skin for Slider
            EditorGUILayout.Slider(property, leftValue, rightValue, GUIContent.none);
            GUI.skin = originalSkin; // Revert to custom skin
            EditorGUILayout.EndHorizontal();
        }

        void DrawLabeledObjectFieldWithDelete<T>(GUIContent label, SerializedProperty objProp, GUIStyle labelStyle,
            SerializedProperty arrayProp, int arrayIndex) where T : Object
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(150));

            GUISkin originalSkin = GUI.skin;
            GUI.skin = null; // Use default Editor skin for ObjectField
            
            EditorGUILayout.PropertyField(objProp, GUIContent.none);
            GUI.skin = originalSkin; // Revert to custom skin

           
            if (typeof(T) == typeof(AudioClip) && objProp.objectReferenceValue as AudioClip != null)
            {
                AudioClip clip = objProp.objectReferenceValue as AudioClip;
                GUIStyle play = GUI.skin.GetStyle("Play");
                GUIStyle pause = GUI.skin.GetStyle("Pause");
                if (GUILayout.Button(new GUIContent("", $"Play {clip.name}"), play))
                {
                    PlayClip(clip);
                }

                if (GUILayout.Button(new GUIContent("", $"Stop {clip.name}"), pause))
                {
                    StopClip();
                }
            }
            GUIStyle deleteBtnStyle = GUI.skin.GetStyle("Delete");
            if (GUILayout.Button(new GUIContent("", $"Remove {label.text}"), deleteBtnStyle))
            {
                arrayProp.DeleteArrayElementAtIndex(arrayIndex);
            }

            EditorGUILayout.EndHorizontal();
        }


        T DrawLabeledObjectField<T>(GUIContent label, T obj, GUIStyle labelStyle) where T : Object
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(150));

            GUISkin originalSkin = GUI.skin;
            GUI.skin = null; // Use default Editor skin for ObjectField
            T newObj = (T)EditorGUILayout.ObjectField(obj, typeof(T), false);
            GUI.skin = originalSkin; // Revert to custom skin

            EditorGUILayout.EndHorizontal();
            return newObj;
        }

   
        private static void PlayClip(AudioClip clip, int startSample = 0, bool loop = false)
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            if (audioUtilClass == null)
            {
                Debug.LogWarning("Could not find internal UnityEditor.AudioUtil class. Audio preview will not work.");
                return;
            }

           
            MethodInfo playClipMethod = audioUtilClass.GetMethod(
                "PlayPreviewClip", // The method is now named PlayPreviewClip
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null
            );

            if (playClipMethod != null)
            {
                playClipMethod.Invoke(null, new object[] { clip, startSample, loop });
            }
            else
            {
                Debug.LogWarning(
                    "Could not find internal method 'PlayPreviewClip'. Audio preview may be broken in this Unity version.");
            }
        }

        private static void StopClip() 
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            if (audioUtilClass == null)
            {
                Debug.LogWarning("Could not find internal UnityEditor.AudioUtil class. Audio preview will not work.");
                return;
            }
            
            MethodInfo stopAllClipsMethod = audioUtilClass.GetMethod(
                "StopAllPreviewClips", // The method is now named StopAllPreviewClips and takes no parameters
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { }, // No parameters
                null
            );

            if (stopAllClipsMethod != null)
            {
                stopAllClipsMethod.Invoke(null, new object[] { }); // Invoke with empty parameters
            }
            else
            {
                Debug.LogWarning(
                    "Could not find internal method 'StopAllPreviewClips'. Audio preview may be broken in this Unity version.");
            }
        }

        void DrawSaveButton()
        {
            GUIStyle buttonPrimaryStyle = GUI.skin.GetStyle("ButtonPrimary");
            EditorGUI.BeginDisabledGroup(footstepData == null || !serializedFootstepData.targetObject);
            if (GUILayout.Button(new GUIContent("Save Changes", "Saves all modifications to the Footstep Data asset."),
                    buttonPrimaryStyle))
                Save();
            EditorGUI.EndDisabledGroup();
        }
        
        string GetPairName(SerializedProperty pairProp, int index)
        {
            
            SerializedProperty nameProp = pairProp.FindPropertyRelative("name");
            SerializedProperty textureProp = pairProp.FindPropertyRelative("texture");
            SerializedProperty terrainLayerIndexProp = pairProp.FindPropertyRelative("terrainLayerIndex");

            if (!string.IsNullOrEmpty(nameProp.stringValue)) return nameProp.stringValue;
            if (textureProp.objectReferenceValue != null) return textureProp.objectReferenceValue.name;
            if (terrainLayerIndexProp.intValue >= 0) return $"Terrain Layer {terrainLayerIndexProp.intValue}";
            return $"Footstep {index + 1}";
        }

        void CreateNewFootstepData()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Footstep Data", "NewFootstepData", "asset",
                "Save footstep data asset");
            if (string.IsNullOrEmpty(path)) return;

            var newData = CreateInstance<FootstepsDatabase>();
            Undo.RegisterCreatedObjectUndo(newData, "Create Footstep Database");

            AssetDatabase.CreateAsset(newData, path);
            AssetDatabase.SaveAssets();
            footstepData = newData;
            SaveLastSelectedData();
            InitializeSerializedProperties(); 
            textureFootstepFoldouts.Clear(); 
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newData;
        }

        void Save()
        {
            if (footstepData == null) return;
            EditorUtility.SetDirty(footstepData);
            AssetDatabase.SaveAssets();
            Debug.Log("Footstep data saved successfully!");
        }
    }
}