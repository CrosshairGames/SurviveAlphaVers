namespace GameCreator.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    using GameCreator.Variables;

    #if UNITY_EDITOR
    using UnityEditor.Events;
    #endif

    [AddComponentMenu("Game Creator/UI/Toggle", 10)]
    public class ToggleVariable : Toggle
    {
        [VariableFilter(Variable.DataType.Bool)]
        public VariableProperty variable = new VariableProperty();

        // INITIALIZERS: --------------------------------------------------------------------------

        private new void Start()
        {
            base.Start();
            if (!Application.isPlaying) return;

            object current = this.variable.Get(gameObject);

            if (current != null)
            {
                this.isOn = (bool)current;
                this.onValueChanged.AddListener(this.SyncVariable);
            }
        }

        // PRIVATE METHODS: -----------------------------------------------------------------------

        private void SyncVariable(bool state)
        {
            this.variable.Set(state, gameObject);
        }
    }
}