using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace ProjectER.Server.Database
{
    /// <summary>
    /// accounts 테이블 CRUD.
    /// 비즈니스 로직 없음 - 순수 DB 접근 레이어.
    /// </summary>
    public class AccountDb : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly object _lock = new();

        public AccountDb(string dbPath)
        {
            string? dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            _connection = new SqliteConnection($"Data Source={dbPath}");
            _connection.Open();
            InitializeSchema();

            Console.WriteLine($"[AccountDb] 연결됨: {dbPath}");
        }

        // ── 스키마 초기화 ─────────────────────────────────────────
        private void InitializeSchema()
        {
            using SqliteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS accounts (
                    account_id  INTEGER PRIMARY KEY AUTOINCREMENT,
                    username    TEXT    UNIQUE NOT NULL,
                    password    TEXT    NOT NULL,
                    created_at  TEXT    NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        // ── 삽입 ─────────────────────────────────────────────────
        /// <summary>
        /// 새 계정 삽입.
        /// 아이디 중복 시 false 반환 (SqliteErrorCode 19: UNIQUE constraint).
        /// </summary>
        public bool TryInsert(string username, string passwordHash, out int accountId)
        {
            lock (_lock)
            {
                try
                {
                    using SqliteCommand cmd = _connection.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO accounts (username, password, created_at)
                        VALUES ($username, $password, $createdAt);
                        SELECT last_insert_rowid();";
                    cmd.Parameters.AddWithValue("$username",   username);
                    cmd.Parameters.AddWithValue("$password",   passwordHash);
                    cmd.Parameters.AddWithValue("$createdAt",  DateTime.UtcNow.ToString("o"));

                    object? result = cmd.ExecuteScalar();
                    accountId = result != null ? Convert.ToInt32(result) : 0;
                    return true;
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // UNIQUE constraint failed
                {
                    accountId = 0;
                    return false;
                }
            }
        }

        // ── 조회 ─────────────────────────────────────────────────
        /// <summary>
        /// 아이디로 계정 검색.
        /// 존재하지 않으면 false 반환.
        /// </summary>
        public bool TryFind(string username, out int accountId, out string passwordHash)
        {
            lock (_lock)
            {
                using SqliteCommand cmd = _connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT account_id, password
                    FROM accounts
                    WHERE username = $username
                    LIMIT 1;";
                cmd.Parameters.AddWithValue("$username", username);

                using SqliteDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    accountId    = reader.GetInt32(0);
                    passwordHash = reader.GetString(1);
                    return true;
                }

                accountId    = 0;
                passwordHash = string.Empty;
                return false;
            }
        }

        public void Dispose() => _connection.Dispose();
    }
}
