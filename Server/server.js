// requirements
const WebSocketServer = require('ws').Server;
const ip = require("ip");

// network info
const addr = ip.address();
const port = process.env.PORT || "8080";

// the server itself
const wss = new WebSocketServer({ port: port });

// error codes
const ServerFullCode = 4000;
const ServerFullError = 'ServerFullError';

// used for logging
let nextConnectionId = 0;

// handler for a new connection to the server
wss.on('connection', (ws) => {

    // mark this connection uniquely
    let connectionId = nextConnectionId;
    nextConnectionId++;

    // rejects connections after the first 2
    if (wss.clients.size > 2) {
        console.log(`Rejected connection ${connectionId}`);
        ws.close(ServerFullCode, ServerFullError);
    }
    else {
        console.log(`Accepted connection ${connectionId}`);
    }
    
    // handler for receiving a message on the socket
    ws.on('message', (message) => {        
        wss.clients.forEach((client) => {
            if (client !== ws && client.readyState === ws.OPEN) {
                client.send(message);
            }
        });
    });

    // handler for the socket connection closing
    ws.on('close', () => {
        console.log(`Closed connection ${connectionId}`);
    });
});

console.log(`Running on port ${port} at address ${addr}`);