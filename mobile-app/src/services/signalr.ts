import * as signalR from '@microsoft/signalr';
import * as SecureStore from 'expo-secure-store';

const HUB_URL = `${process.env.EXPO_PUBLIC_API_URL ?? 'http://10.0.2.2:5000'}/hubs/status`;

class StatusHubService {
  private connection: signalR.HubConnection | null = null;
  private listeners: Map<string, Set<(...args: any[]) => void>> = new Map();

  async connect() {
    if (this.connection?.state === signalR.HubConnectionState.Connected) return;

    const token = await SecureStore.getItemAsync('auth_token');

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => token ?? '',
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Wire up any pre-registered listeners
    this.listeners.forEach((callbacks, event) => {
      callbacks.forEach((cb) => this.connection!.on(event, cb));
    });

    this.connection.onreconnecting(() =>
      console.log('[SignalR] Reconnecting...')
    );
    this.connection.onreconnected(() =>
      console.log('[SignalR] Reconnected')
    );

    try {
      await this.connection.start();
      console.log('[SignalR] Connected');
    } catch (err) {
      console.warn('[SignalR] Failed to connect:', err);
    }
  }

  async disconnect() {
    await this.connection?.stop();
    this.connection = null;
  }

  on<T>(event: string, callback: (data: T) => void) {
    if (!this.listeners.has(event)) this.listeners.set(event, new Set());
    this.listeners.get(event)!.add(callback as any);
    this.connection?.on(event, callback as any);
  }

  off<T>(event: string, callback: (data: T) => void) {
    this.listeners.get(event)?.delete(callback as any);
    this.connection?.off(event, callback as any);
  }

  async subscribeToEquipment(equipmentId: number) {
    await this.connection?.invoke('SubscribeToEquipment', equipmentId);
  }
}

// Singleton — one connection shared across the app
export const statusHub = new StatusHubService();

// Event payload types (must match server-side records)
export interface EquipmentStatusEvent {
  equipmentId: number;
  name: string;
  oldStatus: string;
  newStatus: string;
  timestamp: string;
}

export interface InspectionSubmittedEvent {
  inspectionId: number;
  equipmentId: number;
  equipmentName: string;
  result: string;
  inspectorName: string;
  timestamp: string;
}
