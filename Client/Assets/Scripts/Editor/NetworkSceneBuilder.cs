using ProjectER.Network;
using ProjectER.Scene;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectER.Editor
{
    /// <summary>
    /// ConnectScene / LobbyScene UI를 자동으로 생성하는 에디터 유틸.
    /// 메뉴: ProjectER / Build Network Scenes
    /// </summary>
    public static class NetworkSceneBuilder
    {
        [MenuItem("ProjectER/Build Network Scenes")]
        public static void BuildAll()
        {
            BuildConnectScene();
            BuildLobbyScene();
            EditorUtility.DisplayDialog("완료", "ConnectScene, LobbyScene 생성 완료.\nBuild Settings에서 씬 목록에 추가하세요.", "확인");
        }

        // ── ConnectScene ──────────────────────────────────────────
        private static void BuildConnectScene()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // NetworkClient (DontDestroyOnLoad 오브젝트)
            GameObject networkObj = new("NetworkClient");
            networkObj.AddComponent<NetworkClient>();

            // Canvas
            GameObject canvasObj = CreateCanvas("Canvas");
            Canvas canvas = canvasObj.GetComponent<Canvas>();

            // 배경 패널
            GameObject panel = CreatePanel(canvasObj.transform, "Panel", new Vector2(400, 260));

            // 제목
            CreateText(panel.transform, "Title", "서버 접속", 24, new Vector2(0, 90), new Vector2(360, 40));

            // Host 입력
            CreateText(panel.transform, "LabelHost", "서버 주소", 16, new Vector2(-60, 40), new Vector2(100, 30));
            GameObject hostField = CreateInputField(panel.transform, "HostInput", "127.0.0.1", new Vector2(60, 40), new Vector2(200, 36));

            // Port 입력
            CreateText(panel.transform, "LabelPort", "포트", 16, new Vector2(-60, -5), new Vector2(100, 30));
            GameObject portField = CreateInputField(panel.transform, "PortInput", "7777", new Vector2(60, -5), new Vector2(200, 36));

            // 접속 버튼
            GameObject connectBtn = CreateButton(panel.transform, "ConnectButton", "접속", new Vector2(0, -55), new Vector2(200, 44));

            // 상태 텍스트
            GameObject statusText = CreateText(panel.transform, "StatusText", string.Empty, 14, new Vector2(0, -100), new Vector2(360, 30));

            // ConnectSceneController 연결
            GameObject controllerObj = new("ConnectSceneController");
            ConnectSceneController controller = controllerObj.AddComponent<ConnectSceneController>();
            SerializedObject so = new(controller);
            so.FindProperty("_hostInput").objectReferenceValue    = hostField.GetComponent<TMP_InputField>();
            so.FindProperty("_portInput").objectReferenceValue    = portField.GetComponent<TMP_InputField>();
            so.FindProperty("_connectButton").objectReferenceValue = connectBtn.GetComponent<Button>();
            so.FindProperty("_statusText").objectReferenceValue   = statusText.GetComponent<TMP_Text>();
            so.ApplyModifiedProperties();

            SaveScene(scene, "Assets/Scenes/ConnectScene.unity");
        }

        // ── LobbyScene ────────────────────────────────────────────
        private static void BuildLobbyScene()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Canvas
            GameObject canvasObj = CreateCanvas("Canvas");

            // 패널
            GameObject panel = CreatePanel(canvasObj.transform, "Panel", new Vector2(400, 200));

            // 상태 텍스트
            GameObject statusText = CreateText(panel.transform, "StatusText", "서버 접속됨", 20, new Vector2(0, 40), new Vector2(360, 40));

            // 연결 해제 버튼
            GameObject disconnectBtn = CreateButton(panel.transform, "DisconnectButton", "연결 해제", new Vector2(0, -40), new Vector2(200, 44));

            // LobbySceneController 연결
            GameObject controllerObj = new("LobbySceneController");
            LobbySceneController controller = controllerObj.AddComponent<LobbySceneController>();
            SerializedObject so = new(controller);
            so.FindProperty("_statusText").objectReferenceValue       = statusText.GetComponent<TMP_Text>();
            so.FindProperty("_disconnectButton").objectReferenceValue = disconnectBtn.GetComponent<Button>();
            so.ApplyModifiedProperties();

            SaveScene(scene, "Assets/Scenes/LobbyScene.unity");
        }

        // ── UI 생성 유틸 ──────────────────────────────────────────
        private static GameObject CreateCanvas(string name)
        {
            GameObject obj = new(name);
            Canvas canvas = obj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            obj.AddComponent<CanvasScaler>();
            obj.AddComponent<GraphicRaycaster>();
            return obj;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
        {
            GameObject obj = new(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            Image img = obj.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            return obj;
        }

        private static GameObject CreateText(Transform parent, string name, string content, int fontSize, Vector2 pos, Vector2 size)
        {
            GameObject obj = new(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            TMP_Text text = obj.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return obj;
        }

        private static GameObject CreateInputField(Transform parent, string name, string placeholder, Vector2 pos, Vector2 size)
        {
            GameObject obj = new(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            Image bg = obj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            TMP_InputField field = obj.AddComponent<TMP_InputField>();

            // 텍스트 영역
            GameObject textArea = new("Text Area");
            textArea.transform.SetParent(obj.transform, false);
            RectTransform taRt = textArea.AddComponent<RectTransform>();
            taRt.anchorMin = Vector2.zero;
            taRt.anchorMax = Vector2.one;
            taRt.offsetMin = new Vector2(5, 0);
            taRt.offsetMax = new Vector2(-5, 0);

            GameObject textObj = new("Text");
            textObj.transform.SetParent(textArea.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            TMP_Text text = textObj.AddComponent<TextMeshProUGUI>();
            text.color = Color.white;
            text.fontSize = 16;

            field.textComponent = text;
            field.text = placeholder;
            return obj;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            GameObject obj = new(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            Image img = obj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.5f, 0.9f, 1f);
            Button btn = obj.AddComponent<Button>();

            GameObject labelObj = new("Label");
            labelObj.transform.SetParent(obj.transform, false);
            RectTransform labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;
            TMP_Text text = labelObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return obj;
        }

        private static void SaveScene(UnityEngine.SceneManagement.Scene scene, string path)
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[NetworkSceneBuilder] 저장 완료: {path}");
        }
    }
}
