namespace GameCreator.Core
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    public class DatabaseGeneral : IDatabase
    {
        public enum GENERAL_SCREEN_SPACE
        {
            ScreenSpaceOverlay,
            ScreenSpaceCamera,
            WorldSpaceCamera
        }

        private const string PROVIDER_PATH = "GameCreator/provider.gamecreator.default";

        // PROPERTIES: ----------------------------------------------------------------------------

        public GENERAL_SCREEN_SPACE generalRenderMode = GENERAL_SCREEN_SPACE.ScreenSpaceOverlay;
        public GameObject prefabMessage;
        public GameObject prefabTouchstick;

        [SerializeField] private IDataProvider provider;

        public float toolbarPositionX = 10f;
        public float toolbarPositionY = 10f;

        // PUBLIC STATIC METHODS: -----------------------------------------------------------------

        public static DatabaseGeneral Load()
        {
            return IDatabase.LoadDatabase<DatabaseGeneral>();
        }

        public IDataProvider GetDataProvider()
        {
            if (this.provider == null)
            {
                this.provider = Resources.Load<IDataProvider>(
                    PROVIDER_PATH
                );
            }

            return this.provider;
        }

        public void ChangeDataProvider(IDataProvider provider)
        {
            if (provider == null) return;
            this.provider = provider;
        }

        // OVERRIDE METHODS: ----------------------------------------------------------------------

        #if UNITY_EDITOR

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            IDatabase.Setup<DatabaseGeneral>();
        }

        #endif
	}
}