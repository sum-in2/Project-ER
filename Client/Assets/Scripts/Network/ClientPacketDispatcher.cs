using System;
using System.Collections.Generic;
using ProjectER.Network.Protocol;
using UnityEngine;

namespace ProjectER.Network
{
    /// <summary>
    /// PacketType 별 핸들러를 등록하고 수신 패킷을 라우팅.
    /// NetworkClient.Update() 에서 메인 스레드에서 호출됨.
    /// </summary>
    public class ClientPacketDispatcher
    {
        private readonly Dictionary<PacketType, Action<byte[]>> _handlers = new();

        public void Register(PacketType type, Action<byte[]> handler)
        {
            _handlers[type] = handler;
        }

        public void Dispatch(PacketType type, byte[] body)
        {
            if (_handlers.TryGetValue(type, out Action<byte[]> handler))
            {
                handler.Invoke(body);
            }
            else
            {
                Debug.LogWarning($"[ClientPacketDispatcher] 미등록 패킷 타입: {type}");
            }
        }
    }
}
