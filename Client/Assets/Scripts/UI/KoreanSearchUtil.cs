namespace ProjectER.UI
{
    /// <summary>
    /// 한글 부분 음절 매칭 검색 유틸리티.
    /// 마지막 음절이 받침 없이 미완성인 경우 초성+중성만 비교하므로
    /// "라" 입력 시 "람", "랄" 등도 매칭된다.
    /// </summary>
    public static class KoreanSearchUtil
    {
        /// <summary>
        /// name이 lowerQuery를 포함하는지 판단한다.
        /// lowerQuery는 호출 전에 소문자 변환된 상태여야 한다.
        /// </summary>
        public static bool IsMatch(string name, string lowerQuery)
        {
            if (string.IsNullOrEmpty(lowerQuery)) return true;
            if (string.IsNullOrEmpty(name))       return false;

            // ⚠️ GC 주의: ToLowerInvariant 할당 — 필터 갱신 시마다 호출됨
            string lowerName = name.ToLowerInvariant();
            int nameLen  = lowerName.Length;
            int queryLen = lowerQuery.Length;

            if (nameLen < queryLen) return false;

            for (int ni = 0; ni <= nameLen - queryLen; ni++)
            {
                if (MatchAt(lowerName, ni, lowerQuery))
                    return true;
            }
            return false;
        }

        // ── 내부 매칭 ────────────────────────────────────────────

        private static bool MatchAt(string name, int nameStart, string query)
        {
            for (int qi = 0; qi < query.Length; qi++)
            {
                char nc = name[nameStart + qi];
                char qc = query[qi];

                if (nc == qc) continue;

                // 마지막 글자가 아니면 무조건 불일치
                if (qi < query.Length - 1) return false;

                // ── 마지막 글자: 한글 부분 음절 매칭 ──────────────────
                if (IsHangulSyllable(qc) && IsHangulSyllable(nc))
                {
                    // 쿼리 음절에 받침이 없으면 → 초성·중성만 비교 (받침 무시)
                    if (GetJongseong(qc) == 0
                        && GetChoseong(nc)  == GetChoseong(qc)
                        && GetJungseong(nc) == GetJungseong(qc))
                        continue;
                }
                else if (IsHangulJamo(qc) && IsHangulSyllable(nc))
                {
                    // 단독 자음 입력(ㄹ 등) → 초성만 비교
                    int choseong = JamoToChoseongIndex(qc);
                    if (choseong >= 0 && GetChoseong(nc) == choseong)
                        continue;
                }

                return false;
            }
            return true;
        }

        // ── 한글 유틸리티 ────────────────────────────────────────

        // 한글 음절: U+AC00(가) ~ U+D7A3(힣)
        // 음절 = (초성 × 21 + 중성) × 28 + 종성  (초성 19종, 중성 21종, 종성 28종)
        private const int HangulBase     = 0xAC00;
        private const int JongseongCount = 28;
        private const int JungseongCount = 21;
        private const int ChoseongStride = JungseongCount * JongseongCount; // 588

        private static bool IsHangulSyllable(char c) => c >= 0xAC00 && c <= 0xD7A3;

        // 단독 자음: U+3131(ㄱ) ~ U+314E(ㅎ) — 모음(U+314F~)은 포함 안 됨
        private static bool IsHangulJamo(char c) => c >= 0x3131 && c <= 0x314E;

        private static int GetChoseong(char c)  => (c - HangulBase) / ChoseongStride;
        private static int GetJungseong(char c) => (c - HangulBase) / JongseongCount % JungseongCount;
        private static int GetJongseong(char c) => (c - HangulBase) % JongseongCount;

        // 단독 자음(ㄱ~ㅎ) → 초성 인덱스 변환 테이블
        // 유니코드 자음 순서가 초성 순서와 다르고, 복합 자음(ㄳ, ㄵ…)은 초성이 아님
        private static readonly int[] JamoToChoseongTable = BuildJamoToChoseongTable();

        private static int[] BuildJamoToChoseongTable()
        {
            // 범위: 0x3131(ㄱ) ~ 0x314E(ㅎ) = 30개 항목
            int[] t = new int[0x314E - 0x3131 + 1];
            for (int i = 0; i < t.Length; i++) t[i] = -1; // 기본값: 유효하지 않음

            // 초성 순서: ㄱ0 ㄲ1 ㄴ2 ㄷ3 ㄸ4 ㄹ5 ㅁ6 ㅂ7 ㅃ8 ㅅ9 ㅆ10 ㅇ11 ㅈ12 ㅉ13 ㅊ14 ㅋ15 ㅌ16 ㅍ17 ㅎ18
            t[0x3131 - 0x3131] = 0;  // ㄱ
            t[0x3132 - 0x3131] = 1;  // ㄲ
            // 0x3133 = ㄳ (복합 종성, 초성 아님)
            t[0x3134 - 0x3131] = 2;  // ㄴ
            // 0x3135 = ㄵ, 0x3136 = ㄶ (복합 종성)
            t[0x3137 - 0x3131] = 3;  // ㄷ
            t[0x3138 - 0x3131] = 4;  // ㄸ
            t[0x3139 - 0x3131] = 5;  // ㄹ
            // 0x313A ~ 0x3140 = ㄺ~ㅀ (복합 종성)
            t[0x3141 - 0x3131] = 6;  // ㅁ
            t[0x3142 - 0x3131] = 7;  // ㅂ
            t[0x3143 - 0x3131] = 8;  // ㅃ
            // 0x3144 = ㅄ (복합 종성)
            t[0x3145 - 0x3131] = 9;  // ㅅ
            t[0x3146 - 0x3131] = 10; // ㅆ
            t[0x3147 - 0x3131] = 11; // ㅇ
            t[0x3148 - 0x3131] = 12; // ㅈ
            t[0x3149 - 0x3131] = 13; // ㅉ
            t[0x314A - 0x3131] = 14; // ㅊ
            t[0x314B - 0x3131] = 15; // ㅋ
            t[0x314C - 0x3131] = 16; // ㅌ
            t[0x314D - 0x3131] = 17; // ㅍ
            t[0x314E - 0x3131] = 18; // ㅎ
            return t;
        }

        private static int JamoToChoseongIndex(char c)
        {
            int idx = c - 0x3131;
            if (idx < 0 || idx >= JamoToChoseongTable.Length) return -1;
            return JamoToChoseongTable[idx];
        }
    }
}
