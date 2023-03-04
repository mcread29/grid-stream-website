mergeInto(LibraryManager.library, {
	EngineIOWSCreateInstance: function (instanceName, targetAddress) {
		var iName = UTF8ToString(instanceName);
		
		if (typeof window.UnityEngineIOWebSockets === 'undefined') {
			console.log('Creating UnityEngineIOWebSockets object');
			window.UnityEngineIOWebSockets = [];
		}
		
		try {
			if (typeof window.UnityEngineIOWebSockets[iName] !== 'undefined' && window.UnityEngineIOWebSockets[iName] != null) {
				console.log("Cleaning up websocket system for " + iName);
				window.UnityEngineIOWebSockets[iName].close();
				delete window.UnityEngineIOWebSockets[iName];
			}
		} catch(e) {
			console.warning('Exception while cleaning up EngineIOWS ' + iName + ': ' + e);
		}
		
		console.log('Connecting Engine.IO WebSocket: ' + UTF8ToString(targetAddress));
		window.UnityEngineIOWebSockets[iName] = new WebSocket(UTF8ToString(targetAddress));
		
		window.UnityEngineIOWebSockets[iName].onopen = function (e) {
			SendMessage(iName, 'EngineIOWebSocketState', 2); 
		};
		
		window.UnityEngineIOWebSockets[iName].onerror = function(e) {
			console.log(iName + " WebSocket Error: " , e);
			SendMessage(iName, 'EngineIOWebSocketError', e);
			SendMessage(iName, 'EngineIOWebSocketState', 6);
		};
		
		window.UnityEngineIOWebSockets[iName].onclose = function(e) {
			console.log(iName + " WebSocket Closed: " , e);
			if (e.wasClean) SendMessage(iName, 'EngineIOWebSocketState', 5);
			else {
				SendMessage(iName, 'EngineIOWebSocketError', e.reason);
				SendMessage(iName, 'EngineIOWebSocketUncleanState', 5);
			}
		};
		
		window.UnityEngineIOWebSockets[iName].onmessage  = function (e) {
			if (typeof e.data == "string") {
				SendMessage(iName, 'EngineIOWebSocketStringMessage', e.data);
			} else {
				SendMessage(iName, 'EngineIOWebSocketBinaryMessage', e.data);
			}
		};
	},
	
	EngineIOWSSendString: function (instanceName, message) {
		var iName = UTF8ToString(instanceName);
		window.UnityEngineIOWebSockets[iName].send(UTF8ToString(message));
	},
	
	EngineIOWSSendBinary: function (instanceName, message) {
		var iName = UTF8ToString(instanceName);
		window.UnityEngineIOWebSockets[iName].send(message);
	},
	
	EngineIOWSClose: function (instanceName) {
		var iName = UTF8ToString(instanceName);
		
		if (typeof window.UnityEngineIOWebSockets[iName] !== 'undefined' && window.UnityEngineIOWebSockets[iName] != null) {
			if (window.UnityEngineIOWebSockets[iName].readyState != 3) window.UnityEngineIOWebSockets[iName].close();
		
			//Cleanup
			console.log("Cleaning up websocket system for " + iName);
			window.UnityEngineIOWebSockets[iName].close();
			delete window.UnityEngineIOWebSockets[iName];
		}
	}
});
