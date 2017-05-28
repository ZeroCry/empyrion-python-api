"""
   Copyright 2017 Hans Uhlig.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
"""
import time

def heartbeat():
    pass

def globalChatMessage(senderInfo, message):
    empyrion.LogMessage("Player " + senderInfo.playerName + " said '" + message + "' on the global channel")
    empyrion.SendGlobalMessage("Global Echo: " + senderInfo.playerName)	
    
def factionChatMessage(senderInfo, factionInfo, message):
    empyrion.LogMessage("Player " + senderInfo.playerName + " said '" + message + "' on a faction channel")
    empyrion.SendFactionMessage(senderInfo.factionId, "Faction(" + str(senderInfo.factionId) + ") Echo: " + senderInfo.playerName)

def privateChatMessage(senderInfo, receiverInfo, message):
    empyrion.LogMessage("Player " + senderInfo.playerName + " said " + message + " to "+ receiverInfo.playerName)
    empyrion.SendPrivateMessage(senderInfo.entityId, "Private Echo: " + senderInfo.playerName)

def playerConnected(playerInfo):
    empyrion.LogMessage("Player Connected to " + playerInfo.playfield + ": (" + str(playerInfo.entityId) + ") " + playerInfo.playerName )
    time.sleep(20) # Long enough for them to actually connect
    empyrion.SendGlobalMessage("Hello " + senderInfo.playerName)

def playerDisconnected(playerInfo):
    empyrion.LogMessage("Player Disconnected from " + playerInfo.playfield + ": (" + str(playerInfo.entityId) + ")" + playerInfo.playerName )

def playfieldLoaded(playfieldLoad):
    empyrion.LogMessage("Playfield Loaded: " + str(playfieldLoad.playfield))

def playfieldUnloaded(playfieldLoad):
    empyrion.LogMessage("Playfield Unloaded: " + str(playfieldLoad.playfield))

def reloadPlayfield(parameters):
    empyrion.LogMessage("Attempting playfield reload")
    empyrion.ExecuteConsoleCommand("playfield " + parameters['playfield'])

empyrion.LogMessage("Hi from IronPython 'loader.py' Script")
empyrion.RegisterCallback(empyrion.HeartbeatCallback(heartbeat))
empyrion.RegisterCallback(empyrion.GlobalChatMessageCallback(globalChatMessage))
empyrion.RegisterCallback(empyrion.FactionChatMessageCallback(factionChatMessage))
empyrion.RegisterCallback(empyrion.PrivateChatMessageCallback(privateChatMessage))
empyrion.RegisterCallback(empyrion.PlayerConnectedCallback(playerConnected))
empyrion.RegisterCallback(empyrion.PlayerDisconnectedCallback(playerDisconnected))
empyrion.RegisterCallback(empyrion.PlayfieldLoadedCallback(playfieldLoaded))
empyrion.RegisterCallback(empyrion.PlayfieldUnloadedCallback(playfieldUnloaded))
empyrion.RegisterCallback("playfield", empyrion.TriggeredCallback(reloadPlayfield))
