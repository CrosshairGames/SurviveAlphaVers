namespace GameCreator.Update
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.SceneManagement;

    public static class GameCreatorInstall
    {
        public const string ASSETS_PATH = "Assets/";
        public const string PACKAGE_PATH = "Plugins/GameCreatorUpdate/Data/Package.unitypackage";
        public const string CONFIG_PATH = "Plugins/GameCreatorUpdate/Data/Config.asset";

        private const string MSG_INSTALL1 = "Installing Game Creator {0}";
        private const string MSG_INSTALL2 = "This process should take less than a few minutes...";

        private const string MSG_COMPLETE1 = "Update Complete!";
        private const string MSG_COMPLETE2 = "Game Creator has been updated to version {0}";

        // INITIALIZE METHODS: --------------------------------------------------------------------

        [InitializeOnLoadMethod]
        static void OnInitializeInstall()
        {
            if (EditorApplication.isPlaying) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            EditorApplication.update += InstallUpdate;
        }

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public static void InstallUpdate()
        {
            EditorApplication.update -= InstallUpdate;

            Version updateVersion = Config.GetUpdate().version;
            Version currentVersion = Config.GetCurrent().version;

            if (updateVersion.Equals(Version.NONE)) return;
            if (!updateVersion.HigherThan(currentVersion)) return;

            if (!File.Exists(Path.Combine(Application.dataPath, PACKAGE_PATH)))
            {
                string path = Path.Combine(Application.dataPath, PACKAGE_PATH);
                Debug.LogError("Unable to locate Package file at: " + path);
                return;
            }

            if (!File.Exists(Path.Combine(Application.dataPath, CONFIG_PATH)))
            {
                string path = Path.Combine(Application.dataPath, CONFIG_PATH);
                Debug.LogError("Unable to locate Config file at: " + path);
                return;
            }

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            SceneSetup[] scenesSetup = EditorSceneManager.GetSceneManagerSetup();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            string[] removeFolders = Config.GetUpdate().removeDirectories;
            for (int i = 0; i < removeFolders.Length; ++i)
            {
                string assetPath = Path.Combine(Application.dataPath, removeFolders[i]);
                if (File.Exists(assetPath) || Directory.Exists(assetPath))
                {
                    FileUtil.DeleteFileOrDirectory(assetPath);
                }
            }

            Debug.Log("Installing <b>Game Creator</b>...");
            AssetDatabase.ImportPackage(
                Path.Combine(ASSETS_PATH, PACKAGE_PATH),
                Config.GetUpdate().interactiveInstall
            );

            if (scenesSetup != null && scenesSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(scenesSetup);
            }

            EditorUtility.CopySerialized(Config.GetUpdate(), Config.GetCurrent());

            EditorUtility.DisplayDialog(
                MSG_COMPLETE1,
                string.Format(MSG_COMPLETE2, updateVersion),
                "Ok"
            );
        }
    }
}