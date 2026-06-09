using System;
using MessagePack;
using ProjectER.Core.Packets;
using ProjectER.Core.Packets.C2S;
using ProjectER.Core.Packets.S2C;
using ProjectER.Server.Database;
using ProjectER.Server.Network;

namespace ProjectER.Server.Handlers
{
    /// <summary>
    /// C2S_Register / C2S_Login 패킷 처리.
    /// </summary>
    public class LoginHandler
    {
        private readonly AccountRepository _repository;

        public LoginHandler(AccountRepository repository)
        {
            _repository = repository;
        }

        public void Register(PacketDispatcher dispatcher)
        {
            dispatcher.Register(PacketType.C2S_Register, HandleRegister);
            dispatcher.Register(PacketType.C2S_Login,    HandleLogin);
        }

        // ── 회원가입 ──────────────────────────────────────────────
        private void HandleRegister(ClientSession session, byte[] body)
        {
            C2SRegisterPacket request = MessagePackSerializer.Deserialize<C2SRegisterPacket>(body);
            (bool success, _, string reason) = _repository.Register(request.Username, request.Password);

            S2CRegisterResultPacket response = new()
            {
                Success      = success,
                RejectReason = reason,
            };
            byte[] data = PacketBuilder.Build(PacketType.S2C_RegisterResult, response);
            _ = session.SendAsync(data);

            Console.WriteLine(success
                ? $"[LoginHandler] 회원가입 성공: {request.Username}"
                : $"[LoginHandler] 회원가입 실패: {request.Username} - {reason}");
        }

        // ── 로그인 ────────────────────────────────────────────────
        private void HandleLogin(ClientSession session, byte[] body)
        {
            C2SLoginPacket request = MessagePackSerializer.Deserialize<C2SLoginPacket>(body);
            (bool success, int accountId, string reason) = _repository.Login(request.Username, request.Password);

            if (success)
                session.AccountId = accountId;

            S2CLoginResultPacket response = new()
            {
                Success      = success,
                AccountId    = accountId,
                RejectReason = reason,
            };
            byte[] data = PacketBuilder.Build(PacketType.S2C_LoginResult, response);
            _ = session.SendAsync(data);

            Console.WriteLine(success
                ? $"[LoginHandler] 로그인 성공: {request.Username} (AccountId: {accountId})"
                : $"[LoginHandler] 로그인 실패: {request.Username} - {reason}");
        }
    }
}
