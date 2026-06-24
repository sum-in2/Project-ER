using System.Collections.Generic;
using ProjectER.Inventory;

namespace ProjectER.Data
{
    /// <summary>
    /// 더미 저장 루트 제공자 — 계정 루트 생성/저장 UI가 구현되기 전까지 픽 선택 흐름을 검증하기 위한 스텁.
    /// ItemDatabase의 완성 장비 아이템을 부위별로 골라 몇 개의 루트를 합성한다.
    /// (추후 계정 DB 조회 구현으로 교체 — ISavedRouteSource)
    /// </summary>
    public sealed class StubSavedRouteSource : ISavedRouteSource
    {
        // EquipmentSlotType 순서와 동일 (Weapon→Chest→Helmet→Arms→Shoes)
        private static readonly ItemType[] EquipmentTypes =
        {
            ItemType.Weapon, ItemType.Chest, ItemType.Helmet, ItemType.Arms, ItemType.Shoes,
        };

        private readonly ItemDatabase _itemDatabase;
        private readonly int _routeCount;

        public StubSavedRouteSource(ItemDatabase itemDatabase, int routeCount = 3)
        {
            _itemDatabase = itemDatabase;
            _routeCount = routeCount;
        }

        public IReadOnlyList<SavedRoute> GetRoutes()
        {
            List<SavedRoute> routes = new();
            if (_itemDatabase == null) return routes;

            // 부위별 완성 장비 후보를 미리 수집
            List<ItemData>[] candidates = new List<ItemData>[EquipmentTypes.Length];
            for (int t = 0; t < EquipmentTypes.Length; t++)
            {
                candidates[t] = new List<ItemData>();
                foreach (ItemData item in _itemDatabase.GetByType(EquipmentTypes[t]))
                {
                    if (item != null && item.IsCompletedItem)
                        candidates[t].Add(item);
                }
            }

            for (int r = 0; r < _routeCount; r++)
            {
                ItemData[] items = new ItemData[SavedRoute.EquipmentSlotCount];
                for (int t = 0; t < EquipmentTypes.Length; t++)
                {
                    List<ItemData> list = candidates[t];
                    if (list.Count == 0) continue;
                    items[t] = list[r % list.Count]; // 루트마다 다른 후보를 순환 선택
                }
                routes.Add(new SavedRoute($"루트 {r + 1}", items));
            }

            return routes;
        }
    }
}
