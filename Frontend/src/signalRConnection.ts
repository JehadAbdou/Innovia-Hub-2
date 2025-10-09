// signalRConnection.ts
import * as signalR from "@microsoft/signalr";

const token = localStorage.getItem("token") ?? "";

// Booking Hub (for confirming/canceling)
export const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5022/bookingHub", {
    accessTokenFactory: () => token,
  })
  .withAutomaticReconnect()
  .build();

// TTS Hub (for streaming audio + syncing text)
export const ttsConnection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5022/ttshub", {
    accessTokenFactory: () => token,
  })
  .withAutomaticReconnect()
  .build();

// Start both safely
async function startConnection(conn: signalR.HubConnection, name: string) {
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    try {
      await conn.start();
      console.log(`${name} connected ✅`);
    } catch (err) {
      console.error(`${name} connection error:`, err);
    }
  }
}

startConnection(connection, "BookingHub");
startConnection(ttsConnection, "TtsHub");
