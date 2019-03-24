﻿namespace GameCreator.Camera
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;
	using GameCreator.Core;
	using GameCreator.Core.Hooks;

	#if UNITY_EDITOR
	using UnityEditor;
	#endif

	[AddComponentMenu("")]
    public class ActionCameraShakeSustain : IAction 
	{
        public enum Type
        {
            StartShake,
            StopShake
        }

        public Type type = Type.StartShake;

        public float fadeTime = 3.0f;
        [Range(0.0f, 30.0f)] public float roughness = 5f;
        [Range(0.0f, 10.0f)] public float magnitude = 0.2f;

        public bool shakePosition = true;
        public bool shakeRotation = true;

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            if (HookCamera.Instance != null)
            {
                CameraController cameraController = HookCamera.Instance.Get<CameraController>();
                if (cameraController != null)
                {
                    switch (this.type)
                    {
                        case Type.StartShake:
                            cameraController.SetSustainShake(new CameraShake(
                                0.0f,
                                this.roughness,
                                this.magnitude,
                                this.shakePosition,
                                this.shakeRotation
                            ));
                            break;

                        case Type.StopShake:
                            cameraController.StopSustainShake(this.fadeTime);
                            break;
                    }
                }
            }

            return true;
        }

		// +--------------------------------------------------------------------------------------+
		// | EDITOR                                                                               |
		// +--------------------------------------------------------------------------------------+

		#if UNITY_EDITOR

		public static new string NAME = "Camera/Camera Shake Sustain";
        private const string NODE_TITLE = "{0} Camera Sustained";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spType;
        private SerializedProperty spFadeTime;
		private SerializedProperty spRoughness;
		private SerializedProperty spMagnitude;
        private SerializedProperty spShakePosition;
        private SerializedProperty spShakeRotation;

		// INSPECTOR METHODS: ---------------------------------------------------------------------

		public override string GetNodeTitle()
		{
            return string.Format(NODE_TITLE, this.type.ToString());
		}

		protected override void OnEnableEditorChild ()
		{
            this.spType = this.serializedObject.FindProperty("type");
            this.spFadeTime = this.serializedObject.FindProperty("fadeTime");
            this.spRoughness = this.serializedObject.FindProperty("roughness");
            this.spMagnitude = this.serializedObject.FindProperty("magnitude");
            this.spShakePosition = this.serializedObject.FindProperty("shakePosition");
            this.spShakeRotation = this.serializedObject.FindProperty("shakeRotation");
		}

		protected override void OnDisableEditorChild ()
		{
            this.spType = null;
            this.spFadeTime = null;
            this.spRoughness = null;
            this.spMagnitude = null;
            this.spShakePosition = null;
            this.spShakeRotation = null;
		}

		public override void OnInspectorGUI()
		{
			this.serializedObject.Update();

            EditorGUILayout.PropertyField(this.spType);
            if (this.spType.intValue == (int)Type.StartShake)
            {
                EditorGUILayout.PropertyField(this.spRoughness);
                EditorGUILayout.PropertyField(this.spMagnitude);

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(this.spShakePosition);
                EditorGUILayout.PropertyField(this.spShakeRotation);
            }
            else
            {
                EditorGUILayout.PropertyField(this.spFadeTime);
            }

			this.serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}