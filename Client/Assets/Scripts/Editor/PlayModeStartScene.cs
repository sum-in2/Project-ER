using UnityEditor;
using UnityEditor.SceneManagement;

namespace ProjectER.Editor
{
    /// <summary>
    /// Play 버튼을 누르면 항상 00_ConnectScene 부터 시작하도록 강제한다.
    /// 다른 씬을 편집 중이어도 적용된다.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartScene
    {
        private const string StartScenePath = "Assets/Scenes/00_ConnectScene.unity";

        static PlayModeStartScene()
        {
            SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScenePath);
            EditorSceneManager.playModeStartScene = scene;
        }
    }
}
