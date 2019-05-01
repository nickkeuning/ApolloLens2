const WebSocketServer = require('ws').Server;

const port = process.env.PORT || "8080";
const wss = new WebSocketServer({ port: port });

const ServerFullCode = 4000;
const ServerFullError = 'ServerFullError';

let nextConnectionId = 0;

// handle a new connection
wss.on('connection', function (ws) {

    let connectionId = nextConnectionId;
    nextConnectionId++;

    if (wss.clients.size > 2) {
        console.log(`Rejected connection ${connectionId}`);
        ws.close(ServerFullCode, ServerFullError);
    }
    else {
        console.log(`Accepted connection ${connectionId}`);
    }
        
    ws.on('message', function (message) {        
        wss.clients.forEach(function (client) {
            if (client !== ws && client.readyState === ws.OPEN) {
                client.send(message);
            }
        });
    });

    ws.on('close', function () {
        console.log(`Closed connection ${connectionId}`);
    });
});

console.log('Running on port %s', port);