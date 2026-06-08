using System;
using System.Collections.Generic;
using ProjectER.Core.Packets;

namespace ProjectER.Server.Network
{
    /// <summary>
    /// PacketType 별 핸들러를 등록하고, 수신 패킷을 해당 핸들러로 라우팅.
    /// </summary>
    public class PacketDispatcher
    {
        private readonly Dictionary<PacketType, Action<ClientSession, byte[]>> _handlers = new();

        /// <summary>패킷 타입에 핸들러 등록</summary>
        public void Register(PacketType type, Action<ClientSession, byte[]> handler)
        {
            _handlers[type] = handler;
        }

        /// <summary>수신된 패킷을 등록된 핸들러로 전달</summary>
        public void Dispatch(ClientSession session, PacketType type, byte[] body)
        {
            if (_handlers.TryGetValue(type, out Action<ClientSession, byte[]>? handler))
            {
                handler.Invoke(session, body);
            }
            else
            {
                Console.WriteLine($"[Dispatcher] 미등록 패킷 타입: {type}");
            }
        }
    }
}
