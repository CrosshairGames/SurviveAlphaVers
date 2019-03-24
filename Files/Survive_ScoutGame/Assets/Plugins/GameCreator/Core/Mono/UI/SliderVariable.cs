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

    [AddComponentMenu("Game Creator/UI/Slider", 10)]
    public class SliderVariable : Slider
    {
        [VariableFilter(Variable.DataType.Number)]
        public VariableProperty variable = new VariableProperty();

        // INITIALIZERS: --------------------------------------------------------------------------

        private new void Start()
        {
            base.Start();
            if (!Application.isPlaying) return;

            object current = this.variable.Get(gameObject);

            if (current != null)
            {
                this.value = (float)current;
                this.onValueChanged.AddListener(this.SyncVariable);
            }
        }

        // PRIVATE METHODS: -----------------------------------------------------------------------

        private void SyncVariable(float value)
        {
            this.variable.Set(value, gameObject);
        }
    }
}