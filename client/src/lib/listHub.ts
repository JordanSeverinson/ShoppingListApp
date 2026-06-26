import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";

const HUB_PATH = "/hubs/shopping-list";

export type ConnectionStatus =
  | "connecting"
  | "connected"
  | "reconnecting"
  | "disconnected";

export function hubUrl(): string {
  const base = import.meta.env.VITE_API_URL ?? "";
  return `${base}${HUB_PATH}`;
}

export function createListHubConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(hubUrl())
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(
      import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning,
    )
    .build();
}

export function mapHubState(state: HubConnectionState): ConnectionStatus {
  switch (state) {
    case HubConnectionState.Connected:
      return "connected";
    case HubConnectionState.Reconnecting:
      return "reconnecting";
    case HubConnectionState.Connecting:
      return "connecting";
    default:
      return "disconnected";
  }
}
