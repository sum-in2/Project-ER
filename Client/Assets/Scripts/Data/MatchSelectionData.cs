using UnityEngine;

namespace ProjectER.Data
{
    /// <summary>
    /// 픽 씬에서 인게임 씬으로 출전 정보를 전달하는 캐리어 ScriptableObject.
    /// (static 전역 상태 대신 SO 채널 사용 — 코딩 컨벤션 권장)
    /// PickScene이 선택 실험체/루트를 기록하고, InGameScene이 읽는다.
    /// 런타임 전용 값이며 플레이 세션 동안만 유지된다(에셋에 영구 저장 의도 아님).
    /// 멀티/서버 GameStart 연동 시 서버 권위 데이터로 대체될 자리(현재는 단일 클라 캐리).
    /// </summary>
    [CreateAssetMenu(fileName = "MatchSelectionData", menuName = "ProjectER/Data/MatchSelectionData")]
    public sealed class MatchSelectionData : ScriptableObject
    {
        public CharacterData SelectedCharacter { get; private set; }
        public SavedRoute    SelectedRoute     { get; private set; }

        public void SetCharacter(CharacterData character) => SelectedCharacter = character;
        public void SetRoute(SavedRoute route)            => SelectedRoute = route;

        public void Clear()
        {
            SelectedCharacter = null;
            SelectedRoute = null;
        }
    }
}
