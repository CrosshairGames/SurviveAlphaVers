namespace GameCreator.Core
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using GameCreator.Characters;
    using GameCreator.Core.Hooks;

    [AddComponentMenu("")]
    public class TouchStickManager : Singleton<TouchStickManager>
    {
        private const string RESOURCE_PATH = "GameCreator/Input/TouchStick";

        // PROPERTIES: ----------------------------------------------------------------------------

        private TouchStick touchStick;

        // INITIALIZERS: --------------------------------------------------------------------------

        protected override void OnCreate()
        {
            DontDestroyOnLoad(gameObject);

            GameObject prefab = DatabaseGeneral.Load().prefabTouchstick;
            if (prefab == null) prefab = Resources.Load<GameObject>(RESOURCE_PATH);

            GameObject instance = Instantiate<GameObject>(prefab, transform);
            this.touchStick = instance.GetComponentInChildren<TouchStick>();

            SceneManager.sceneUnloaded += this.OnSceneUnload;
        }

        // PRIVATE METHODS: -----------------------------------------------------------------------

        private void OnSceneUnload(Scene scene)
        {
            if (HookPlayer.Instance == null)
            {
                this.touchStick.gameObject.SetActive(false);
            }
        }

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public Vector2 GetDirection(PlayerCharacter player)
        {
            if (!player.IsControllable())
            {
                this.touchStick.gameObject.SetActive(false);
                return Vector2.zero;
            }

            this.touchStick.gameObject.SetActive(true);
            return this.touchStick.GetDirection();
        }
    }
}