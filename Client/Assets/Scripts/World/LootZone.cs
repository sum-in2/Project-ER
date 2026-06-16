using System.Collections.Generic;
using System.Linq;
using ProjectER.Data;
using UnityEngine;

namespace ProjectER.World
{
    /// <summary>
    /// ZoneSpawnData 1개를 BoxCount개의 LootBox에 분배해 영역 내에 배치한다.
    ///
    /// 분배 기준:
    ///   BSER 서브존 데이터는 동일한 아이템 풀의 반복 정의이므로 1개만 사용한다.
    ///   dropCount는 해당 아이템의 상대 가중치를 의미한다.
    ///   가중치 비율을 유지하면서 합계를 BoxCount * TargetItemsPerBox 로 스케일링한 뒤
    ///   기존 회전 분배 알고리즘으로 박스당 TargetItemsPerBox 종류가 들어가도록 한다.
    /// </summary>
    public class LootZone : MonoBehaviour
    {
        private const int BoxCount          = 5;
        private const int TargetItemsPerBox = 10;

        [SerializeField] private ZoneSpawnData _spawnData;
        [SerializeField] private LootBox       _boxPrefab;
        [SerializeField] private Color         _zoneColor       = Color.white;
        [SerializeField] private Vector2       _areaSize        = new(20f, 20f);
        [SerializeField] private float         _boxHeightOffset = 0.5f;

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

            // ⚠️ GC 주의: Start()에서만 호출되므로 허용
            int[]                scaledCounts = ScaleDropCounts(_spawnData.Entries);
            List<SpawnEntry>[]   boxContents  = Distribute(_spawnData.Entries, scaledCounts);

            for (int i = 0; i < BoxCount; i++)
            {
                if (boxContents[i].Count == 0) continue;
                Vector3 position = GetRandomPositionInArea();
                LootBox box = Instantiate(_boxPrefab, position, Quaternion.identity, transform);
                box.Initialize(boxContents[i]);

                // ⚠️ GC 주의: renderer.material은 인스턴스를 새로 생성함 (Start() 1회이므로 허용)
                if (box.TryGetComponent(out Renderer boxRenderer))
                    boxRenderer.material.color = _zoneColor;
            }
        }

        /// <summary>
        /// dropCount 비율을 유지하면서 합계를 BoxCount * TargetItemsPerBox 로 스케일링한다.
        /// BoxCount 초과분은 스케일 단계에서 미리 클램프하고,
        /// Hamilton method로 나머지 슬롯을 재분배해 합계를 정확히 맞춘다.
        /// </summary>
        private static int[] ScaleDropCounts(IReadOnlyList<SpawnEntry> entries)
        {
            int rawSum = 0;
            for (int i = 0; i < entries.Count; i++)
                rawSum += entries[i].DropCount;

            int     target    = BoxCount * TargetItemsPerBox;
            float   scale     = (float)target / rawSum;
            int[]   result    = new int[entries.Count];
            float[] fractions = new float[entries.Count];
            int     allocated = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                // BoxCount 초과분을 미리 클램프해서 낭비된 슬롯이 재분배에 포함되도록 한다
                float exact   = Mathf.Min(entries[i].DropCount * scale, BoxCount);
                result[i]     = Mathf.Max(1, (int)exact);
                fractions[i]  = exact - Mathf.Floor(exact);
                allocated    += result[i];
            }

            // Hamilton method: 남은 슬롯을 소수점이 큰 순서로 BoxCount 미만 항목에만 배분
            int leftover = target - allocated;
            // ⚠️ GC 주의: Start() 1회 실행이므로 허용
            int[] sortedIdx = Enumerable.Range(0, entries.Count)
                .OrderByDescending(i => fractions[i])
                .ToArray();

            for (int k = 0; k < sortedIdx.Length && leftover > 0; k++)
            {
                int idx = sortedIdx[k];
                if (result[idx] >= BoxCount) continue;
                result[idx]++;
                leftover--;
            }

            return result;
        }

        /// <summary>
        /// 항목별 scaledCount 만큼의 박스에 1개씩 배분.
        /// 나머지는 항목 인덱스를 오프셋으로 회전시켜 편향을 방지한다.
        /// </summary>
        private static List<SpawnEntry>[] Distribute(IReadOnlyList<SpawnEntry> entries, int[] scaledCounts)
        {
            List<SpawnEntry>[] result = new List<SpawnEntry>[BoxCount];
            for (int i = 0; i < BoxCount; i++)
                result[i] = new List<SpawnEntry>();

            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                int count     = scaledCounts[entryIndex];
                int baseAmt   = count / BoxCount;
                int remainder = count % BoxCount;

                for (int box = 0; box < BoxCount; box++)
                {
                    int amount  = baseAmt;
                    int rotated = ((box - entryIndex) % BoxCount + BoxCount) % BoxCount;
                    if (rotated < remainder)
                        amount++;

                    if (amount > 0)
                        result[box].Add(new SpawnEntry(entries[entryIndex].Item, amount));
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
