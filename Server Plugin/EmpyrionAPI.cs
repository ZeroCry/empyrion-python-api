/*
 * Copyright 2017 Hans Uhlig.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using Eleon.Modding;
using System;
using System.Collections.Generic;

namespace Empyrion
{
    public class EmpyrionAPI
    {
        public delegate void HeartbeatCallback();
        public delegate void TriggeredCallback(IDictionary<string, string> parameters);
        public delegate void GlobalChatMessageCallback(PlayerInfo sender, string message);
        public delegate void FactionChatMessageCallback(PlayerInfo sender, FactionInfo faction, string message);
        public delegate void PrivateChatMessageCallback(PlayerInfo sender, PlayerInfo receiver, string message);
        public delegate void PlayerConnectedCallback(PlayerInfo playerInfo);
        public delegate void PlayerDisconnectedCallback(PlayerInfo playerInfo);
        public delegate void PlayfieldLoadedCallback(PlayfieldLoad playfieldLoad);
        public delegate void PlayfieldUnloadedCallback(PlayfieldLoad playfieldLoad);

        private readonly ScriptManager _scriptManager;

        public EmpyrionAPI(ScriptManager scriptManager)
        {
            _scriptManager = scriptManager;
        }

        public void LogMessage(string message)
        {
            _scriptManager.LogMessage(message);
        }

        public void RegisterCallback(string name, TriggeredCallback callback)
        {
            _scriptManager.RegisterCallback(name, callback);
        }

        public void RegisterCallback(HeartbeatCallback callback)
        {
            _scriptManager.RegisterCallback(callback);
        }

        public void RegisterCallback(GlobalChatMessageCallback callback)
        {
            _scriptManager.RegisterCallback(callback);
        }

        public void RegisterCallback(FactionChatMessageCallback callback)
        {
            _scriptManager.RegisterCallback(callback);
        }

        public void RegisterCallback(PrivateChatMessageCallback callback)
        {
            _scriptManager.RegisterCallback(callback);
        }

        public void RegisterCallback(PlayerConnectedCallback callback)
        {
            _scriptManager.RegisterCallback(callback);
        }

        public void RegisterCallback(PlayerDisconnectedCallback callback)
        {
            _scriptManager.RegisterCallback(callback);
        }

        public void RegisterCallback(PlayfieldLoadedCallback callback)
        {
            _scriptManager.RegisterCallback(callback);
        }

        public void RegisterCallback(PlayfieldUnloadedCallback callback)
        {
            _scriptManager.RegisterCallback(callback);
        }

        public PlayerInfo GetPlayerInfo(int entityId)
        {
            return _scriptManager.GetPlayerInfo(entityId);
        }

        public FactionInfo GetFactionInfo(int factionId)
        {
            return _scriptManager.GetFactionInfo(factionId);
        }

        public void SendGlobalMessage(string message)
        {
            _scriptManager.SendGlobalMessage(message);
        }

        public void SendFactionMessage(int factionId, string message, byte prio = 2, float time = 10)
        {
            _scriptManager.SendFactionMessage(factionId, message);
        }

        public void SendPrivateMessage(int entityId, string message, byte prio = 2, float time = 10)
        {
            _scriptManager.SendPrivateMessage(entityId, message);
        }
    }

    [Serializable]
    public class Player
    {
        private int entityId { get; }
        private int clientId { get; }
        private string playerName { get; }
        private byte factionGroup { get; }
        private byte factionRole { get; }
        private int factionId { get; }
        private string playfield { get; }
        private string steamId { get; }
        private string steamOwnerId { get; }


        public Player(PlayerInfo playerInfo)
        {
            entityId = playerInfo.entityId;
            clientId = playerInfo.clientId;
            playerName = playerInfo.playerName;
            factionId = playerInfo.factionId;
            factionGroup = playerInfo.factionGroup;
            factionRole = playerInfo.factionRole;
            playfield = playerInfo.playfield;
            steamId = playerInfo.steamId;
            steamOwnerId = playerInfo.steamOwnerId;

        }
    }
}