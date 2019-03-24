namespace GameCreator.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    [ExecuteInEditMode, Serializable]
    public abstract class GlobalID : MonoBehaviour, ISerializationCallbackReceiver
    {
        private const int GUID_LENGTH = 16;

        // PROPERTIES: ----------------------------------------------------------------------------

        [SerializeField] private string gid;
        protected bool exitingApplication = false;

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public string GetID()
        {
            return this.gid;
        }

        // PROTECTED METHODS: ---------------------------------------------------------------------

        protected void OnDestroyGID()
        {
            if (this.exitingApplication) return;
        }

        protected virtual void OnApplicationQuit()
        {
            this.exitingApplication = true;
        }

        // PRIVATE METHODS: -----------------------------------------------------------------------

        private void CreateGuid()
        {
            if (string.IsNullOrEmpty(this.gid))
            {
                this.gid = Guid.NewGuid().ToString("D");

                #if UNITY_EDITOR
                PrefabType prefabType = PrefabUtility.GetPrefabType(this);
                if (prefabType == PrefabType.PrefabInstance)
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(this);
                }
                #endif
            }
        }

        // SERIALIZATON METHODS: ------------------------------------------------------------------

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            #if UNITY_EDITOR
            PrefabType prefabType = PrefabUtility.GetPrefabType(this);
            if (prefabType == PrefabType.Prefab || prefabType == PrefabType.ModelPrefab)
            {
                this.gid = string.Empty;
                return;
            }
            #endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        { }

        protected virtual void Awake()
        {
            this.CreateGuid();
        }

        protected virtual void OnValidate()
        {
            #if UNITY_EDITOR
            PrefabType prefabType = PrefabUtility.GetPrefabType(this);
            if (prefabType == PrefabType.Prefab || prefabType == PrefabType.ModelPrefab)
            {
                this.gid = string.Empty;
                return;
            }
            #endif

            this.CreateGuid();
        }
    }
}