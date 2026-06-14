using System.Collections.Generic;
using ProjectER.Data;
using UnityEngine;

namespace ProjectER.World
{
    /// <summary>
    /// ZoneSpawnData 1개(스폰 그룹)를 BoxCount개의 LootBox에 분배해 영역 내에 배치한다.
    ///
    /// 분배 기준: 항목별 dropCount를 BoxCount로 균등 분배하고, 나머지는 항목 인덱스를 오프셋으로
    /// 회전시켜가며 1개씩 추가 배분한다 (특정 박스에 나머지가 몰리는 편향 방지).
    /// ItemSpawn.json에는 박스 단위 분배 정보가 없으므로 이 분배 로직은 자체 설계임.
    /// </summary>
    public class LootZone : MonoBehaviour
    {
        private const int BoxCount = 5;

        [SerializeField] private ZoneSpawnData _spawnData;
        [SerializeField] private LootBox _boxPrefab;
        [SerializeField] private Vector2 _areaSize = new(15f, 15f);
        [SerializeField] private float _boxHeightOffset = 0.5f;

        private void Awake()
        {
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        private void OnDestroy()
        {
        }

        private void Start()
        {
            SpawnBoxes();
        }

        private void SpawnBoxes()
        {
            if (_spawnData == null || _boxPrefab == null) return;

            List<SpawnEntry>[] boxContents = Distribute(_spawnData.Entries);

            for (int i = 0; i < BoxCount; i++)
            {
                if (boxContents[i].Count == 0) continue;

                Vector3 position = GetRandomPositionInArea();
                LootBox box = Instantiate(_boxPrefab, position, Quaternion.identity, transform);
                box.Initialize(boxContents[i]);
            }
        }

        /// <summary>
        /// 항목별 dropCount를 BoxCount개에 균등 분배. 나머지는 항목 인덱스를 시작 오프셋으로
        /// 회전시켜 (box - entryIndex) % BoxCount &lt; remainder 인 박스에 1개씩 추가한다.
        /// </summary>
        private static List<SpawnEntry>[] Distribute(IReadOnlyList<SpawnEntry> entries)
        {
            List<SpawnEntry>[] result = new List<SpawnEntry>[BoxCount];
            for (int i = 0; i < BoxCount; i++)
                result[i] = new List<SpawnEntry>();

            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                SpawnEntry entry = entries[entryIndex];
                int baseAmount = entry.DropCount / BoxCount;
                int remainder  = entry.DropCount % BoxCount;

                for (int box = 0; box < BoxCount; box++)
                {
                    int amount = baseAmount;
                    int rotated = ((box - entryIndex) % BoxCount + BoxCount) % BoxCount;
                    if (rotated < remainder)
                        amount++;

                    if (amount > 0)
                        result[box].Add(new SpawnEntry(entry.Item, amount));
                }
            }

            return result;
        }

        private Vector3 GetRandomPositionInArea()
        {
            Vector3 origin = transform.position;
            float x = Random.Range(-_areaSize.x * 0.5f, _areaSize.x * 0.5f);
            float z = Random.Range(-_areaSize.y * 0.5f, _areaSize.y * 0.5f);
            return new Vector3(origin.x + x, origin.y + _boxHeightOffset, origin.z + z);
        }
    }
}
