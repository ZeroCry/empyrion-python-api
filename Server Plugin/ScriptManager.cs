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
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static Empyrion.EmpyrionAPI;
using static IronPython.SQLite.PythonSQLite;

namespace Empyrion
{
    public class ScriptManagerConfiguration
    {
        public string databaseFile { get; set; }
        public string loaderScript { get; set; }
    }
    public class ScriptManager
    {
        private readonly Dictionary<string, TriggeredCallback> _triggeredCallbacks;
        private readonly Dictionary<int, FactionInfo> _factionCache;
        private readonly Dictionary<int, PlayerInfo> _playerCache;
        private readonly ScriptManagerConfiguration _config;
        private readonly ServerPlugin _serverPlugin;
        private readonly Connection _sqlConnection;
        private readonly EmpyrionAPI _pythonApi;
        private readonly ModGameAPI _modGameApi;
        private readonly ScriptEngine _engine;
        private Dictionary<ushort, Handle> _handles;
        private Dictionary<string, string> settings;
        private ScriptScope _scope;
        private ushort _sequenceNumber;
        private HeartbeatCallback _heartbeatCallback;
        private GlobalChatMessageCallback _globalChatMessageCallback;
        private FactionChatMessageCallback _factionChatMessageCallback;
        private PrivateChatMessageCallback _privateChatMessageCallback;
        private PlayerConnectedCallback _playerConnectedCallback;
        private PlayerDisconnectedCallback _playerDisconnectedCallback;
        private PlayfieldLoadedCallback _playfieldLoadedCallback;
        private PlayfieldUnloadedCallback _playfieldUnloadedCallback;

        public ScriptManager(ScriptManagerConfiguration config, ServerPlugin serverPlugin, ModGameAPI modGameApi)
        {
            _config = config;
            _serverPlugin = serverPlugin;
            _modGameApi = modGameApi;
            _pythonApi = new EmpyrionAPI(this);
            _sqlConnection = new Connection(_config.databaseFile);
            _engine = Python.CreateEngine();
            _engine.SetSearchPaths(new string[]{
                    "Content/Python",
                    "Content/Scripts"
                });
            _handles = new Dictionary<ushort, Handle>();
            _scope = _engine.CreateScope();
            _scope.ImportModule("clr");
            _scope.SetVariable("settings", settings = new Dictionary<string, string>());
            _scope.SetVariable("empyrion", _pythonApi = new EmpyrionAPI(this));
            _scope.SetVariable("sql", _sqlConnection = new Connection(_config.databaseFile));
            _triggeredCallbacks = new Dictionary<string, TriggeredCallback>();
            _factionCache = new Dictionary<int, FactionInfo>();
            _playerCache = new Dictionary<int, PlayerInfo>();
            LogMessage("PythonAPI Version " + typeof(ServerPlugin).Assembly.GetName().Version);
            LogMessage("IronPython Version " + typeof(Python).Assembly.GetName().Version);
        }

        public void LogMessage(string message)
        {
            _serverPlugin.LogMessage("Script", message);
        }

        public void ProcessResponseData(CmdId eventId, ushort seqId, object responseData)
        {
            if (_handles.ContainsKey(seqId))
            {
                Handle handle = _handles[seqId];
                lock (handle)
                {
                    handle._resCmdId = eventId;
                    handle._responseData = responseData;
                    Monitor.PulseAll(handle);
                }
            }
            else
            {
                LogMessage("Error: Received Response Data for missing Handle");
            }
        }

        private T SyncRequest<T>(CmdId reqCmdId, CmdId resCmdId, object data)
        {
            Handle handle = new Handle();
            lock (_handles)
            {
                bool passedZero = false;
                do
                {
                    if (_sequenceNumber == 0)
                    {
                        // Sequence Number 0 is reserved for Game Events
                        _sequenceNumber = 1;
                        // This avoids the rare scenario that we have somehow 
                        // simultainiously used up all 65534 available sequence
                        // numbers. If we pass Zero twice insize a lock.. fsck it.
                        if (passedZero)
                        {
                            throw new Exception("Ran out of Available Sequence Numbers");
                        }
                        else
                        {
                            passedZero = true;
                        }
                    }
                    else
                    {
                        // Increment until we find an unused SequenceNumber
                        _sequenceNumber += 1;
                    }
                } while (_handles.ContainsKey(_sequenceNumber));
                handle._sequenceNumber = _sequenceNumber;
                _handles.Add(_sequenceNumber, handle);
            }

            lock (handle)
            {
                _modGameApi.Game_Request(reqCmdId, handle._sequenceNumber, data);
                Monitor.Wait(handle);
            }

            lock (_handles)
            {
                _handles.Remove(handle._sequenceNumber);
            }

            if (handle._resCmdId != resCmdId)
            {
                throw new Exception("Expected CmdId: " + Enum.GetName(typeof(CmdId), resCmdId)
                    + " Found CmdId: " + Enum.GetName(typeof(CmdId), handle._resCmdId));
            }
            return (T)handle._responseData;
        }

        private void AsyncRequest(CmdId reqCmdId, CmdId resCmdId, object data)
        {
            Handle handle = new Handle();
            lock (_handles)
            {
                bool passedZero = false;
                do
                {
                    if (_sequenceNumber == 0)
                    {
                        // Sequence Number 0 is reserved for Game Events
                        _sequenceNumber = 1;
                        // This avoids the rare scenario that we have somehow 
                        // simultainiously used up all 65534 available sequence
                        // numbers. If we pass Zero twice insize a lock.. fsck it.
                        if (passedZero)
                        {
                            throw new Exception("Ran out of Available Sequence Numbers");
                        }
                        else
                        {
                            passedZero = true;
                        }
                    }
                    else
                    {
                        // Increment until we find an unused SequenceNumber
                        _sequenceNumber += 1;
                    }
                } while (_handles.ContainsKey(_sequenceNumber));
                handle._sequenceNumber = _sequenceNumber;
                _handles.Add(_sequenceNumber, handle);
            }
            _modGameApi.Game_Request(reqCmdId, handle._sequenceNumber, data);
        }

        public bool ExecuteScript(string script)
        {
            try
            {
                LogMessage("Loading 'Content/Scripts/" + script + "' Script.");
                ScriptSource scriptSource = _engine.CreateScriptSourceFromFile("Content/Scripts/" + script);
                new Thread(() =>
                {
                    try
                    {
                        scriptSource.Execute(_scope);
                        LogMessage("Completed Execution of 'Content/Scripts/" + script + "'.");
                    }
                    catch (Exception ex)
                    {
                        LogMessage("Script Error: " + ex);
                    }
                }).Start();
                return true;
            }
            catch (Exception ex)
            {
                LogMessage("Error Loading 'Content/Scripts/" + script + "' Script: " + ex);
                return false;
            }
        }

        public bool ExecuteTrigger(string trigger, IDictionary<string, string> parameters)
        {
            TriggeredCallback callback;
            if (_triggeredCallbacks.ContainsKey(trigger) && (callback = _triggeredCallbacks[trigger]) != null)
            {
                callback.Invoke(parameters);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void HandleServerStartup()
        {
            ExecuteScript(_config.loaderScript);
        }

        public void HandleServerShutdown()
        {
            _engine.Runtime.Shutdown();
        }

        public void HandleHeartbeat()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    _heartbeatCallback?.Invoke();
                }
                catch (Exception ex)
                {
                    LogMessage("Error Calling HeartbeatCallback" + ex);
                }
            });
        }

        public void HandlePlayerConnected(Id entityId)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    _playerConnectedCallback?.Invoke(GetUpdatedPlayerInfo(entityId.id));
                }
                catch (Exception ex)
                {
                    LogMessage("Error Calling PlayerConnectedCallback: " + ex);
                }
            });
        }

        public void HandlePlayerDisconnected(Id entityId)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    _playerDisconnectedCallback?.Invoke(GetPlayerInfo(entityId.id));
                }
                catch (Exception ex)
                {
                    LogMessage("Error Calling PlayerDisconnectedCallback: " + ex);
                }
            });
        }

        public void HandlePlayfieldLoaded(PlayfieldLoad playfieldLoad)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    _playfieldLoadedCallback?.Invoke(playfieldLoad);
                }
                catch (Exception ex)
                {
                    LogMessage("Error Calling PlayfieldLoadedCallback: " + ex);
                }
            });
        }

        public void HandlePlayfieldUnloaded(PlayfieldLoad playfieldLoad)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    _playfieldUnloadedCallback?.Invoke(playfieldLoad);
                }
                catch (Exception ex)
                {
                    LogMessage("Error Calling PlayfieldUnloadedCallback: " + ex);
                }
            });
        }


        public void HandleFactionChanged(FactionChangeInfo factionChangeInfo)
        {
            LogMessage("FactionChangeInfo: " + JsonUtility.ToJson(factionChangeInfo));
            GetUpdatedFactionInfoList(new Id(factionChangeInfo.id)); // TODO Fix this. No idea what the ID it wants is.
        }

        public void HandleGlobalChatMessage(int senderEntityId, string message)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    PlayerInfo senderInfo = GetUpdatedPlayerInfo(senderEntityId);
                    LogMessage("{Global} " + senderInfo.playerName + ": " + message);
                    _globalChatMessageCallback?.Invoke(senderInfo, message);
                }
                catch (Exception ex)
                {
                    LogMessage("Error Calling GlobalMessageCallback: " + ex);
                }
            });

        }

        public void HandleFactionChatMessage(int senderEntityId, int factionId, string message)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    PlayerInfo senderInfo = GetUpdatedPlayerInfo(senderEntityId);
                    FactionInfo factionInfo = new FactionInfo(); // GetFactionInfo(factionId); // TODO: Fix Faction fetching
                    LogMessage("{Faction} " + senderInfo.playerName + " -> " + factionId + ": " + message);
                    _factionChatMessageCallback?.Invoke(senderInfo, factionInfo, message);
                }
                catch (Exception ex)
                {
                    LogMessage("Error Calling GlobalMessageCallback: " + ex);
                }
            });

        }

        public void HandlePrivateChatMessage(int senderEntityId, int receiverEntityId, string message)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    PlayerInfo senderInfo = GetUpdatedPlayerInfo(senderEntityId);
                    PlayerInfo receiverInfo = GetUpdatedPlayerInfo(receiverEntityId);
                    LogMessage("{Private} " + senderInfo.playerName + " -> " + receiverInfo.playerName + ": " + message);
                    _privateChatMessageCallback?.Invoke(senderInfo, receiverInfo, message);
                }
                catch (Exception ex)
                {
                    LogMessage("Error Calling GlobalMessageCallback: " + ex);
                }
            });
        }

        private PlayerInfo GetUpdatedPlayerInfo(int entityId)
        {
            PlayerInfo playerInfo = SyncRequest<PlayerInfo>(CmdId.Request_Player_Info, CmdId.Event_Player_Info, new Id(entityId));
            _playerCache[playerInfo.entityId] = playerInfo;
            return playerInfo;
        }

        public PlayerInfo GetPlayerInfo(int entityId)
        {
            return _playerCache[entityId];
        }

        private FactionInfoList GetUpdatedFactionInfoList(Id id) // TODO: Identify what ID this is
        {
            FactionInfoList factionInfoList = SyncRequest<FactionInfoList>(CmdId.Request_Get_Factions, CmdId.Event_Get_Factions, new Id(0));
            foreach (FactionInfo factionInfo in factionInfoList.factions)
            {
                _factionCache[factionInfo.factionId] = factionInfo;
            }
            return factionInfoList;
        }

        public FactionInfo GetFactionInfo(int factionId)
        {
            return _factionCache[factionId];
        }

        public void SendGlobalMessage(string message, byte prio = 2, float time = 10)
        {
            LogMessage("Sent Global Message: " + message);
            AsyncRequest(CmdId.Request_InGameMessage_AllPlayers, CmdId.Event_Ok, new IdMsgPrio(0, message, prio, time));
        }

        public void SendFactionMessage(int factionId, string message, byte prio = 2, float time = 10)
        {
            LogMessage("Sent Faction Message to (" + factionId + "): " + message);
            AsyncRequest(CmdId.Request_InGameMessage_Faction, CmdId.Event_Ok, new IdMsgPrio(factionId, message, prio, time));
        }

        public void SendPrivateMessage(int entityId, string message, byte prio = 2, float time = 10)
        {
            LogMessage("Sent Private Message to (" + entityId + "): " + message);
            AsyncRequest(CmdId.Request_InGameMessage_SinglePlayer, CmdId.Event_Ok, new IdMsgPrio(entityId, message, prio, time));
        }

        public void ExecuteConsoleCommand(string command)
        {
            LogMessage("Executing Console Command: " + command);
            AsyncRequest(CmdId.Request_ConsoleCommand, CmdId.Event_Ok, command);
        }

        public void RegisterCallback(string name, TriggeredCallback callback)
        {
            _triggeredCallbacks[name.ToLower()] = callback;
        }

        public void RegisterCallback(HeartbeatCallback callback)
        {
            _heartbeatCallback = callback;
        }

        public void RegisterCallback(GlobalChatMessageCallback callback)
        {
            _globalChatMessageCallback = callback;
        }

        public void RegisterCallback(FactionChatMessageCallback callback)
        {
            _factionChatMessageCallback = callback;
        }

        public void RegisterCallback(PrivateChatMessageCallback callback)
        {
            _privateChatMessageCallback = callback;
        }

        public void RegisterCallback(PlayerConnectedCallback callback)
        {
            _playerConnectedCallback = callback;
        }

        public void RegisterCallback(PlayerDisconnectedCallback callback)
        {
            _playerDisconnectedCallback = callback;
        }

        public void RegisterCallback(PlayfieldLoadedCallback callback)
        {
            _playfieldLoadedCallback = callback;
        }

        public void RegisterCallback(PlayfieldUnloadedCallback callback)
        {
            _playfieldUnloadedCallback = callback;
        }

    }
    internal class Handle
    {
        public CmdId _resCmdId;
        public ushort _sequenceNumber;
        public object _responseData;
    }
}