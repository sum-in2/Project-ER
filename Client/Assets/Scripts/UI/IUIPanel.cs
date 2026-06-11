namespace ProjectER.UI
{
    /// <summary>
    /// LobbyUIManager가 스택으로 관리하는 패널이 구현해야 하는 인터페이스.
    /// </summary>
    public interface IUIPanel
    {
        /// <summary>
        /// 패널 종류 (LobbyUIManager의 패널-프리팹 매핑 키)
        /// </summary>
        LobbyPanelType PanelType { get; }

        /// <summary>
        /// 닫힐 때 SetActive(false)로 캐시할지(true), Destroy해서 메모리를 회수할지(false).
        /// 도감처럼 무거운 패널은 false로 설정한다.
        /// </summary>
        bool CacheOnClose { get; }

        /// <summary>
        /// 패널이 화면에 표시될 때 호출 (최초 생성 직후 또는 캐시에서 재활성화 시)
        /// </summary>
        void OnOpen();

        /// <summary>
        /// 패널이 화면에서 사라지기 직전에 호출 (캐시 또는 파괴 직전)
        /// </summary>
        void OnClose();
    }
}
