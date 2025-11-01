import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
const API_BASE_URL = import.meta.env.VITE_API_URL;
const token = localStorage.getItem("token") ?? "";

// Booking Hub
export const connection = new HubConnectionBuilder()
  .withUrl(`${API_BASE_URL}/bookingHub`, {
    accessTokenFactory: () => token,
  })
  .withAutomaticReconnect()
  .build();

// TTS Hub
export const ttsConnection = new HubConnectionBuilder()
  .withUrl(`${API_BASE_URL}/ttshub`, {
    accessTokenFactory: () => token,
  })
  .withAutomaticReconnect()
  .build();

// IoT Hub
export const IOTconnection = new HubConnectionBuilder()
  .withUrl(`http://localhost:5103/hub/telemetry`)
  .withAutomaticReconnect()
  .build();

// Register IoT handlers before starting
IOTconnection.on("measurementReceived", (data: any) => console.log("Realtime:", data));

// Safe start function
async function startConnection(conn: HubConnection, name: string, onConnected?: () => Promise<void>) {
  if (conn.state === HubConnectionState.Disconnected) {
    try {
      await conn.start();
      console.log(`${name} connected ✅`);
      if (onConnected) await onConnected();
    } catch (err) {
      console.error(`${name} connection error:`, err);
    }
  } else {
    console.log(`${name} already in state`, conn.state);
  }
}

// Start connections
startConnection(connection, "BookingHub");
startConnection(ttsConnection, "TtsHub");
startConnection(IOTconnection, "IOTHub", async () => {
  await IOTconnection.invoke("JoinTenant", "innovia");
});
