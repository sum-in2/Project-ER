using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectER.Network
{
    /// <summary>
    /// 서버 MessagePack 라이브러리와 호환되는 최소 구현.
    /// [MessagePackObject] fixarray 형식만 지원.
    /// Unity에 MessagePack 패키지 설치 없이 동작.
    /// </summary>
    public static class MiniMsgPack
    {
        // ── 쓰기 ──────────────────────────────────────────────────
        /// <summary>fixarray 헤더 (요소 수 0~15 지원)</summary>
        public static void WriteArrayHeader(List<byte> buf, int count)
        {
            if (count > 15)
                throw new NotSupportedException("fixarray 최대 15개");
            buf.Add((byte)(0x90 | count));
        }

        /// <summary>문자열 (fixstr: 0~31바이트, str8: 32~255바이트)</summary>
        public static void WriteString(List<byte> buf, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            if (bytes.Length <= 31)
            {
                buf.Add((byte)(0xa0 | bytes.Length)); // fixstr
            }
            else if (bytes.Length <= 255)
            {
                buf.Add(0xd9);                       // str8
                buf.Add((byte)bytes.Length);
            }
            else
            {
                throw new NotSupportedException("문자열 255바이트 초과");
            }
            buf.AddRange(bytes); // ⚠️ GC 주의
        }

        /// <summary>int32 (고정 4바이트, 빅엔디안 - MessagePack 표준)</summary>
        public static void WriteInt32(List<byte> buf, int value)
        {
            buf.Add(0xd2); // int32 포맷 코드
            buf.Add((byte)(value >> 24));
            buf.Add((byte)(value >> 16));
            buf.Add((byte)(value >> 8));
            buf.Add((byte)value);
        }

        /// <summary>bool</summary>
        public static void WriteBool(List<byte> buf, bool value)
        {
            buf.Add(value ? (byte)0xc3 : (byte)0xc2);
        }

        // ── 읽기 ──────────────────────────────────────────────────
        /// <summary>fixarray 헤더 읽기. 반환값: 요소 수</summary>
        public static int ReadArrayHeader(byte[] data, ref int offset)
        {
            byte b = data[offset++];
            if ((b & 0xf0) == 0x90)
                return b & 0x0f; // fixarray
            throw new Exception($"배열 헤더 예상, 실제: 0x{b:X2}");
        }

        /// <summary>문자열 읽기 (fixstr / str8)</summary>
        public static string ReadString(byte[] data, ref int offset)
        {
            byte b = data[offset++];
            int length;
            if ((b & 0xe0) == 0xa0)
            {
                length = b & 0x1f; // fixstr
            }
            else if (b == 0xd9)
            {
                length = data[offset++]; // str8
            }
            else
            {
                throw new Exception($"문자열 헤더 예상, 실제: 0x{b:X2}");
            }

            string result = Encoding.UTF8.GetString(data, offset, length); // ⚠️ GC 주의
            offset += length;
            return result;
        }

        /// <summary>int32 읽기 (int32 / positive fixint 모두 처리)</summary>
        public static int ReadInt32(byte[] data, ref int offset)
        {
            byte b = data[offset++];
            if (b == 0xd2) // int32
            {
                int value = (data[offset] << 24)
                          | (data[offset + 1] << 16)
                          | (data[offset + 2] << 8)
                          |  data[offset + 3];
                offset += 4;
                return value;
            }
            if ((b & 0x80) == 0) // positive fixint (0~127)
                return b;

            throw new Exception($"int 헤더 예상, 실제: 0x{b:X2}");
        }

        /// <summary>bool 읽기</summary>
        public static bool ReadBool(byte[] data, ref int offset)
        {
            byte b = data[offset++];
            if (b == 0xc3) return true;
            if (b == 0xc2) return false;
            throw new Exception($"bool 헤더 예상, 실제: 0x{b:X2}");
        }
    }
}
