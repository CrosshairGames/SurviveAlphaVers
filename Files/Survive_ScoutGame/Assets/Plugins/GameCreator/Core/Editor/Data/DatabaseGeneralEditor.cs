namespace GameCreator.Localization
{
	using System;
	using System.IO;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;
	using UnityEditor.AnimatedValues;
	using UnityEditor.SceneManagement;
	using UnityEditorInternal;
	using System.Linq;
	using System.Reflection;
	using GameCreator.Core;

	[CustomEditor(typeof(DatabaseGeneral))]
	public class DatabaseGeneralEditor : IDatabaseEditor
	{
        private const string MSG_DP = "The default PlayerPrefs will be used if no Data Provider is selected";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spGeneralRenderMode;
        private SerializedProperty spPrefabMessage;
        private SerializedProperty spPrefabTouchstick;
        private SerializedProperty spProvider;
        private SerializedProperty spToolbarPositionX;
        private SerializedProperty spToolbarPositionY;

        // INITIALIZE: ----------------------------------------------------------------------------

        private void OnEnable()
		{
            if (target == null || serializedObject == null) return;
            this.spGeneralRenderMode = serializedObject.FindProperty("generalRenderMode");
            this.spPrefabMessage = serializedObject.FindProperty("prefabMessage");
            this.spPrefabTouchstick = serializedObject.FindProperty("prefabTouchstick");
            this.spProvider = serializedObject.FindProperty("provider");
            this.spToolbarPositionX = serializedObject.FindProperty("toolbarPositionX");
            this.spToolbarPositionY = serializedObject.FindProperty("toolbarPositionY");
        }

		// OVERRIDE METHODS: ----------------------------------------------------------------------

		public override string GetDocumentationURL ()
		{
			return "https://docs.gamecreator.io/";
		}

		public override string GetName ()
		{
			return "General";
		}

        public override int GetPanelWeight()
        {
            return 98;
        }

        public override bool CanBeDecoupled()
        {
            return true;
        }

        // GUI METHODS: ---------------------------------------------------------------------------

        public override void OnInspectorGUI()
		{
			this.serializedObject.Update();

            EditorGUILayout.PropertyField(this.spGeneralRenderMode);
            EditorGUILayout.PropertyField(this.spPrefabMessage);
            EditorGUILayout.PropertyField(this.spPrefabTouchstick);

            this.PaintProvider();

            EditorGUILayout.LabelField("Toolbar", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(this.spToolbarPositionX);
            EditorGUILayout.PropertyField(this.spToolbarPositionY);
            EditorGUI.indentLevel--;

            this.serializedObject.ApplyModifiedProperties();
		}

        private void PaintProvider()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Save/Load System:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(this.spProvider, GUIContent.none);

            if (this.spProvider.objectReferenceValue != null)
            {
                IDataProvider provider = this.spProvider.objectReferenceValue as IDataProvider;
                if (provider == null) return;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(provider.title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(provider.description);
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox(MSG_DP, MessageType.Info);
            }
        }
	}
}