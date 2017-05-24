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
def heartbeat():
	pass

def globalChatMessage(senderInfo, message):
	empyrion.LogMessage("Player " + senderInfo.playerName + " said '" + message + "' on the global channel")
	
def factionChatMessage(senderInfo, factionInfo, message):
	empyrion.LogMessage("Player " + senderInfo.playerName + " said '" + message + "' on a faction channel")

def privateChatMessage(senderInfo, receiverInfo, message):
	empyrion.LogMessage("Player " + senderInfo.playerName + " said " + message + " to "+ receiverInfo.playerName)
	
def playerConnected(playerInfo):
    empyrion.LogMessage("Player Connected to " + playerInfo.playfield + ": (" + str(playerInfo.entityId) + ") " + playerInfo.playerName )
    empyrion.SendGlobalMessage("Hi " + playerInfo.playerName)

def playerDisconnected(playerInfo):
    empyrion.LogMessage("Player Disconnected from " + playerInfo.playfield + ": (" + str(playerInfo.entityId) + ")" + playerInfo.playerName )

def playfieldLoaded(playfieldLoad):
	empyrion.LogMessage("Playfield Loaded: " + str(playfieldLoad.playfield))	

def playfieldUnloaded(playfieldLoad):
	empyrion.LogMessage("Playfield Unloaded: " + str(playfieldLoad.playfield))	
	
empyrion.LogMessage("Hi from IronPython 'loader.py' Script")
empyrion.RegisterCallback(empyrion.HeartbeatCallback(heartbeat))
empyrion.RegisterCallback(empyrion.GlobalChatMessageCallback(globalChatMessage))
empyrion.RegisterCallback(empyrion.FactionChatMessageCallback(factionChatMessage))
empyrion.RegisterCallback(empyrion.PrivateChatMessageCallback(privateChatMessage))
empyrion.RegisterCallback(empyrion.PlayerConnectedCallback(playerConnected))
empyrion.RegisterCallback(empyrion.PlayerDisconnectedCallback(playerDisconnected))
empyrion.RegisterCallback(empyrion.PlayfieldLoadedCallback(playfieldLoaded))
empyrion.RegisterCallback(empyrion.PlayfieldUnloadedCallback(playfieldUnloaded))