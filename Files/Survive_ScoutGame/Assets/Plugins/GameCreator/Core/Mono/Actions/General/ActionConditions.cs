namespace GameCreator.Core
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	#if UNITY_EDITOR
	using UnityEditor;
	#endif

	[AddComponentMenu("")]
	public class ActionConditions : IAction
	{
        public Conditions conditions;
        public bool waitToFinish = true;

		// EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            if (!this.waitToFinish)
            {
                if (this.conditions != null) this.conditions.Interact(target);
                return true;
            }

            return false;
        }

        public override IEnumerator Execute(GameObject target, IAction[] actions, int index)
		{
            if (this.conditions != null)
            {
                yield return this.conditions.InteractCoroutine(target);
            }

			yield return 0;
		}

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

        #if UNITY_EDITOR

		public static new string NAME = "General/Call Conditions";
		private const string NODE_TITLE = "Call conditions {0}";

		// PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spConditions;
        private SerializedProperty spWaitToFinish;

		// INSPECTOR METHODS: ---------------------------------------------------------------------

		public override string GetNodeTitle()
		{
            return string.Format(
                NODE_TITLE, 
                (this.conditions == null ? "none" : this.conditions.gameObject.name)
            );
		}

		protected override void OnEnableEditorChild ()
		{
            this.spConditions = this.serializedObject.FindProperty("conditions");
            this.spWaitToFinish = this.serializedObject.FindProperty("waitToFinish");
		}

		protected override void OnDisableEditorChild ()
		{
			this.spConditions = null;
            this.spWaitToFinish = null;
		}

		public override void OnInspectorGUI()
		{
			this.serializedObject.Update();

            EditorGUILayout.PropertyField(this.spConditions);
            EditorGUILayout.PropertyField(this.spWaitToFinish);

			this.serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
