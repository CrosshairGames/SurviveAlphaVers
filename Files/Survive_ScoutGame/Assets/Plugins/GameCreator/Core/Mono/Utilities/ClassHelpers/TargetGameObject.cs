namespace GameCreator.Core
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using GameCreator.Core.Hooks;
    using GameCreator.Variables;

	[System.Serializable]
	public class TargetGameObject
	{
		public enum Target
		{
			Player,
            Camera,
			Invoker,
			GameObject
		}

		// PROPERTIES: ----------------------------------------------------------------------------

        public Target target = Target.GameObject;
        public GameObjectProperty gameObject;


        // INITIALIZERS: --------------------------------------------------------------------------

        public TargetGameObject() { }

        public TargetGameObject(TargetGameObject.Target target) 
        {
            this.target = target;
        }

		// PUBLIC METHODS: ------------------------------------------------------------------------

        public GameObject GetGameObject(GameObject invoker)
		{
            GameObject result = null;

			switch (this.target)
			{
			case Target.Player :
                if (HookPlayer.Instance != null) result = HookPlayer.Instance.gameObject;
				break;

            case Target.Camera:
                if (HookCamera.Instance != null) result = HookCamera.Instance.gameObject;
                break;

                case Target.Invoker:
				result = invoker;
				break;
                
            case Target.GameObject:
                    result = this.gameObject.GetValue(invoker);
				break;
			}

			return result;
		}

		// UTILITIES: -----------------------------------------------------------------------------

		public override string ToString ()
		{
			string result = "(unknown)";
			switch (this.target)
			{
			case Target.Player : result = "Player"; break;
			case Target.Invoker: result = "Invoker"; break;
            case Target.Camera: result = "Camera"; break;
            case Target.GameObject: result = this.gameObject.ToString(); break;
			}

			return result;
		}
	}
}