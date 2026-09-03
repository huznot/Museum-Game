using UnityEditor;
using UnityEngine;

namespace MicrophoneInput.EditorTools
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MicrophoneInputSystem))]
    public class MicrophoneInputSystemEditor : Editor
    {
        private SerializedProperty voiceTargetMode;
        private SerializedProperty blendShapeName;
        private SerializedProperty skinnedMesh;

        private GUIStyle headerStyle;
        private GUIStyle foldoutStyle;
        private GUIStyle infoStyle;
        private bool angleFoldout = true;
        private bool blendShapeFoldout = true;
        private bool intensityFoldout = true;
        private bool positionFoldout = true;
        private bool spriteSwapFoldout = true;

        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel);
                headerStyle.fontSize = 14;
                headerStyle.normal.textColor = new Color(1f, 0.85f, 0f);
            }

            if (foldoutStyle == null)
            {
                foldoutStyle = new GUIStyle(EditorStyles.foldout);
                foldoutStyle.fontStyle = FontStyle.Bold;
                foldoutStyle.fontSize = 12;
            }

            if (infoStyle == null)
            {
                infoStyle = new GUIStyle(EditorStyles.label);
                infoStyle.fontSize = 10;
                infoStyle.normal.textColor = Color.gray;
                infoStyle.wordWrap = true;
            }
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();
            var animator = (MicrophoneInputSystem)target;

            EditorGUILayout.Space(5);
            GUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("General Voice Animation Settings", headerStyle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("voiceTargetMode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sensitivity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("smoothSpeed"));
            GUILayout.EndVertical();
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            voiceTargetMode = serializedObject.FindProperty("voiceTargetMode");

            switch ((MicrophoneInputSystem.VoiceTargetMode)voiceTargetMode.enumValueIndex)
            {
                case MicrophoneInputSystem.VoiceTargetMode.Angle:
                    angleFoldout = EditorGUILayout.Foldout(angleFoldout, "Angle Mode Settings", true, foldoutStyle);
                    if (angleFoldout)
                    {
                        GUILayout.BeginVertical("box");
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("targetObject"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("angleAxis"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("minAngle"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAngle"));

                        if (animator.targetObject == null)
                            EditorGUILayout.HelpBox("Target Object is not assigned!", MessageType.Warning);
                        GUILayout.EndVertical();
                    }
                    break;

                case MicrophoneInputSystem.VoiceTargetMode.BlendShape:
                    blendShapeFoldout = EditorGUILayout.Foldout(blendShapeFoldout, "BlendShape Mode Settings", true, foldoutStyle);
                    if (blendShapeFoldout)
                    {
                        GUILayout.BeginVertical("box");
                        blendShapeName = serializedObject.FindProperty("blendShapeName");
                        skinnedMesh = serializedObject.FindProperty("skinnedMesh");

                        EditorGUILayout.PropertyField(blendShapeName);

                        // Küçük sarı info alanı
                        Rect lastRect = GUILayoutUtility.GetLastRect();
                        Rect backgroundRect = new Rect(lastRect.x, lastRect.yMax + 2, lastRect.width, 20);
                        EditorGUI.DrawRect(backgroundRect, new Color(1f, 0.92f, 0.016f, 0.4f));
                        var smallStyle = new GUIStyle(EditorStyles.label)
                        {
                            fontSize = 10,
                            normal = { textColor = Color.black },
                            alignment = TextAnchor.MiddleLeft
                        };
                        EditorGUI.LabelField(backgroundRect, "Please verify the shape key name.", smallStyle);
                        GUILayout.Space(24);

                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(skinnedMesh);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (animator.skinnedMesh != null && animator.skinnedMesh.sharedMesh != null)
                            {
                                if (animator.skinnedMesh.sharedMesh.blendShapeCount > 0)
                                {
                                    string firstBlendShapeName = animator.skinnedMesh.sharedMesh.GetBlendShapeName(0);
                                    blendShapeName.stringValue = firstBlendShapeName;
                                }
                            }
                        }

                        if (animator.skinnedMesh == null)
                            EditorGUILayout.HelpBox("Skinned Mesh Renderer is not assigned!", MessageType.Warning);
                        else if (string.IsNullOrEmpty(animator.blendShapeName))
                            EditorGUILayout.HelpBox("BlendShape Name is empty!", MessageType.Warning);

                        GUILayout.EndVertical();
                    }
                    break;

                case MicrophoneInputSystem.VoiceTargetMode.Intensity:
                    intensityFoldout = EditorGUILayout.Foldout(intensityFoldout, "Intensity Mode Settings", true, foldoutStyle);
                    if (intensityFoldout)
                    {
                        GUILayout.BeginVertical("box");
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("textTarget"));

                        if (animator.textTarget == null)
                            EditorGUILayout.HelpBox("Text Target (TMP Text) is not assigned!", MessageType.Warning);
                        GUILayout.EndVertical();
                    }
                    break;

                case MicrophoneInputSystem.VoiceTargetMode.Position:
                    positionFoldout = EditorGUILayout.Foldout(positionFoldout, "Position Mode Settings", true, foldoutStyle);
                    if (positionFoldout)
                    {
                        GUILayout.BeginVertical("box");
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("targetObject"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("positionAxis"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("minPosition"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxPosition"));

                        if (animator.targetObject == null)
                            EditorGUILayout.HelpBox("Target Object is not assigned!", MessageType.Warning);
                        GUILayout.EndVertical();
                    }
                    break;

                case MicrophoneInputSystem.VoiceTargetMode.SpriteSwap:
                    spriteSwapFoldout = EditorGUILayout.Foldout(spriteSwapFoldout, "SpriteSwap Mode Settings", true, foldoutStyle);
                    if (spriteSwapFoldout)
                    {
                        GUILayout.BeginVertical("box");
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("spriteRenderer"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("mouthSprites"), true);

                        if (animator.spriteRenderer == null)
                            EditorGUILayout.HelpBox("Sprite Renderer is not assigned!", MessageType.Warning);
                        GUILayout.EndVertical();
                    }
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
