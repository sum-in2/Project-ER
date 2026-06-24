using System.Collections.Generic;

namespace ProjectER.Data
{
    /// <summary>
    /// 계정에 저장된 루트 목록 제공자. 픽 씬 루트 선택 UI가 이 인터페이스로 루트를 읽는다.
    /// 현재 구현은 더미 스텁(StubSavedRouteSource). 추후 계정 DB 조회(서버 패킷) 구현으로 교체.
    /// (D 원칙: 구체 DB 구현 대신 인터페이스에 의존)
    /// </summary>
    public interface ISavedRouteSource
    {
        IReadOnlyList<SavedRoute> GetRoutes();
    }
}
