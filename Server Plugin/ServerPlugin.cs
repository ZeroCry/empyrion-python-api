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
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Empyrion
{
    public class ServerPluginConfiguration
    {
        public ScriptManagerConfiguration scriptManager { get; set; }
        public HttpManagerConfiguration httpManager { get; set; }
    }
    public class ServerPlugin : ModInterface
    {
        private ServerPluginConfiguration _config;
        private ScriptManager _scriptManager;
        private HttpManager _httpManager;
        private ModGameAPI _modGameApi;

        public void Game_Start(ModGameAPI modGameApi)
        {
            using (var reader = File.OpenText("epaconfig.yaml"))
            {
                var deserializer = new Deserializer();
                _config = deserializer.Deserialize<ServerPluginConfiguration>(reader);
            }
            _modGameApi = modGameApi;
            _scriptManager = new ScriptManager(_config.scriptManager, this, _modGameApi);
            _httpManager = new HttpManager(_config.httpManager, _scriptManager, this);
            _scriptManager.HandleServerStartup();
            _httpManager.StartServer();
        }

        public void LogMessage(string component, string message)
        {
            _modGameApi.Console_Write("[PythonApi:" + component + "] " + message);
        }

        public void Game_Event(CmdId eventId, ushort seqNr, object data)
        {
            switch (eventId)
            {
                case CmdId.Event_Player_Connected:
                    _scriptManager.HandlePlayerConnected((Id)data);
                    break;
                case CmdId.Event_Player_Disconnected:
                    _scriptManager.HandlePlayerDisconnected((Id)data);
                    break;
                case CmdId.Event_ChatMessage:
                    ChatInfo chatInfo = (ChatInfo)data;
                    switch (chatInfo.type)
                    {
                        case 3: // Global?
                            _scriptManager.HandleGlobalChatMessage(chatInfo.playerId, chatInfo.msg);
                            break;
                        case 5: // Faction?
                            _scriptManager.HandleFactionChatMessage(chatInfo.playerId, chatInfo.recipientFactionId, chatInfo.msg);
                            break;
                        default:
                            LogMessage("Plugin", "Unknown Chat message Type: " + JsonUtility.ToJson(chatInfo));
                            break;
                    }
                    break;
                case CmdId.Event_Playfield_Loaded:
                    _scriptManager.HandlePlayfieldLoaded((PlayfieldLoad)data);
                    break;
                case CmdId.Event_Playfield_Unloaded:
                    _scriptManager.HandlePlayfieldUnloaded((PlayfieldLoad)data);
                    break;
                case CmdId.Event_Faction_Changed:
                    _scriptManager.HandleFactionChanged((FactionChangeInfo)data);
                    break;
                case CmdId.Event_Statistics:
                    StatisticsParam statisticsParam = (StatisticsParam)data;
                    switch (statisticsParam.type)
                    {
                        // TODO: Break these into Seperate functions
                        case StatisticsType.CoreAdded:
                            LogMessage("Plugin", "Core Added: " + JsonUtility.ToJson(statisticsParam));
                            break;
                        case StatisticsType.CoreRemoved:
                            LogMessage("Plugin", "Core Removed: " + JsonUtility.ToJson(statisticsParam));
                            break;
                        case StatisticsType.PlayerDied:
                            LogMessage("Plugin", "Player Died: " + JsonUtility.ToJson(statisticsParam));
                            break;
                        case StatisticsType.StructOnOff:
                            LogMessage("Plugin", "Struct On/Off: " + JsonUtility.ToJson(statisticsParam));
                            break;
                    }
                    break;
                case CmdId.Event_Player_Info:
                    _scriptManager.ProcessResponseData(eventId, seqNr, data);
                    break;
                case CmdId.Event_Error:
                    _scriptManager.ProcessResponseData(eventId, seqNr, (ErrorInfo)data);
                    break;
                default:
                    LogMessage("Plugin",
                         "Unhandled Event - CmdId '" + Enum.GetName(typeof(CmdId), eventId) + "' " +
                         "seqNr: '" + seqNr + "' " +
                         "data: (" + data.GetType() + ") '" + JsonUtility.ToJson(data) + "'");
                    break;
            }
        }

        public void Game_Update()
        {
            _scriptManager.HandleHeartbeat();
        }

        public void Game_Exit()
        {
            _httpManager.StopServer();
            _scriptManager.HandleServerShutdown();
        }
    }
}
