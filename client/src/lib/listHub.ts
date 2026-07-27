import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { CSRF_HEADER_NAME } from "./apiClient";
import { getCsrfToken } from "./csrf";

const HUB_PATH = "/hubs/shopping-list";

export type ConnectionStatus =
  | "connecting"
  | "connected"
  | "reconnecting"
  | "disconnected";

export function createListHubConnection(): HubConnection {
  const base = import.meta.env.VITE_API_URL ?? "";
  const headers: Record<string, string> = {};
  const csrf = getCsrfToken();
  if (csrf) {
    headers[CSRF_HEADER_NAME] = csrf;
  }

  return new HubConnectionBuilder()
    .withUrl(`${base}${HUB_PATH}`, {
      withCredentials: true,
      headers,
    })
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
