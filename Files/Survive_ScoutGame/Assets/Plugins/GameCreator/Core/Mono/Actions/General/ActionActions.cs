namespace GameCreator.Core
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;

	#if UNITY_EDITOR
	using UnityEditor;
	#endif

	[AddComponentMenu("")]
	public class ActionActions : IAction 
	{
		public Actions actions;
		public bool waitToFinish = false;

        private bool actionsComplete = false;
        private bool forceStop = false;

		// EXECUTABLE: ----------------------------------------------------------------------------
		
        public override IEnumerator Execute(GameObject target, IAction[] actions, int index)
		{
			if (this.actions != null)
			{
                this.actionsComplete = false;
                this.actions.actionsList.Execute(target, this.OnCompleteActions);

                if (this.waitToFinish)
                {
                    WaitUntil wait = new WaitUntil(() =>
                    {
                        if (this.actions == null) return true;
                        if (this.forceStop)
                        {
                            this.actions.actionsList.Stop();
                            return true;
                        }

                        return this.actionsComplete;
                    });

                    yield return wait;
                }
			}

			yield return 0;
		}

        private void OnCompleteActions()
        {
            this.actionsComplete = true;
        }

        public override void Stop()
        {
            this.forceStop = true;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

        #if UNITY_EDITOR

        public static new string NAME = "General/Execute Actions";
		private const string NODE_TITLE = "Execute actions {0} {1}";

		// PROPERTIES: ----------------------------------------------------------------------------

		private SerializedProperty spActions;
		private SerializedProperty spWaitToFinish;

		// INSPECTOR METHODS: ---------------------------------------------------------------------

		public override string GetNodeTitle()
		{
			return string.Format(
				NODE_TITLE, 
				(this.actions == null ? "none" : this.actions.name),
				(this.waitToFinish ? "and wait" : "")
			);
		}

		protected override void OnEnableEditorChild ()
		{
			this.spActions = this.serializedObject.FindProperty("actions");
			this.spWaitToFinish = this.serializedObject.FindProperty("waitToFinish");
		}

		protected override void OnDisableEditorChild ()
		{
			this.spActions = null;
			this.spWaitToFinish = null;
		}

		public override void OnInspectorGUI()
		{
			this.serializedObject.Update();

			EditorGUILayout.PropertyField(this.spActions);
			EditorGUILayout.PropertyField(this.spWaitToFinish);

			this.serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}