namespace ProjectER.Server.Database
{
    /// <summary>
    /// 회원가입 / 로그인 비즈니스 로직.
    /// BCrypt 해시 처리 및 유효성 검사 담당.
    /// </summary>
    public class AccountRepository
    {
        // ── 유효성 제한 ───────────────────────────────────────────
        private const int UsernameMinLength = 3;
        private const int UsernameMaxLength = 20;
        private const int PasswordMinLength = 4;

        private readonly AccountDb _db;

        public AccountRepository(AccountDb db)
        {
            _db = db;
        }

        // ── 회원가입 ──────────────────────────────────────────────
        /// <summary>아이디/비밀번호 유효성 검사 → BCrypt 해시 → DB 삽입.</summary>
        public (bool Success, int AccountId, string RejectReason) Register(string username, string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                username.Length < UsernameMinLength ||
                username.Length > UsernameMaxLength)
                return (false, 0, $"아이디는 {UsernameMinLength}~{UsernameMaxLength}자여야 합니다.");

            if (string.IsNullOrWhiteSpace(plainPassword) ||
                plainPassword.Length < PasswordMinLength)
                return (false, 0, $"비밀번호는 {PasswordMinLength}자 이상이어야 합니다.");

            string hash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

            if (!_db.TryInsert(username, hash, out int accountId))
                return (false, 0, "이미 사용 중인 아이디입니다.");

            return (true, accountId, string.Empty);
        }

        // ── 로그인 ────────────────────────────────────────────────
        /// <summary>DB 조회 → BCrypt 검증. 실패 원인은 보안상 동일 메시지로 반환.</summary>
        public (bool Success, int AccountId, string RejectReason) Login(string username, string plainPassword)
        {
            if (!_db.TryFind(username, out int accountId, out string storedHash))
                return (false, 0, "아이디 또는 비밀번호가 올바르지 않습니다.");

            if (!BCrypt.Net.BCrypt.Verify(plainPassword, storedHash))
                return (false, 0, "아이디 또는 비밀번호가 올바르지 않습니다.");

            return (true, accountId, string.Empty);
        }
    }
}
