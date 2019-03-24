namespace GameCreator.Variables
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using GameCreator.Core;

    [Serializable]
    public abstract class BaseProperty<T>
    {
        public enum OPTION
        {
            Value,
            UseGlobalVariable,
            UseLocalVariable
        }

        // PROPERTIES: ----------------------------------------------------------------------------

        public OPTION optionIndex = OPTION.Value;
        public T value;

        public HelperGlobalVariable global = new HelperGlobalVariable();
        public HelperLocalVariable local = new HelperLocalVariable();

        // INITIALIZERS: --------------------------------------------------------------------------

        protected BaseProperty()
        {
            this.value = default(T);
            this.SetupVariables();
        }

        protected BaseProperty(T value)
        {
            this.value = value;
            this.SetupVariables();
        }

        private void SetupVariables()
        {
            this.global = this.global ?? new HelperGlobalVariable();
            this.local = this.local ?? new HelperLocalVariable();
        }

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public T GetValue()
        {
            Debug.LogWarning(
                "This should not be called but it's here for legacy purposes. " +
                "Please contact us at: marti@catsoft-studios.com. Thanks!"
            );

            return this.GetValue(null);
        }

        public T GetValue(GameObject invoker)
        {
            switch (this.optionIndex)
            {
                case OPTION.Value: return this.value;
                case OPTION.UseGlobalVariable : return (T)this.global.Get();
                case OPTION.UseLocalVariable: return (T)this.local.Get(invoker);
            }

            return default(T);
        }

		// OVERRIDERS: ----------------------------------------------------------------------------

		public override string ToString()
		{
            switch (this.optionIndex)
            {
                case OPTION.Value : return (this.value == null ? "(none)" : this.value.ToString());
                case OPTION.UseGlobalVariable: return "(Global Variable)";
                case OPTION.UseLocalVariable: return "(Local Variable)";
            }

            return "unknown";
		}
	}
}