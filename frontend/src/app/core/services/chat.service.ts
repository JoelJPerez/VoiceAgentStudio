import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject, BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export interface ChatToken {
  token: string;
}

export interface AgentInfo {
  id: string;
  name: string;
  tone: string;
  objective: string;
  modelName: string;
}

export interface EscalationEvent {
  reason: string;
  matchedKeyword: string;
  agentName: string;
}

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  streaming?: boolean;
  timestamp: Date;
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private hub: signalR.HubConnection | null = null;
  private auth = inject(AuthService);

  // Streams the frontend subscribes to
  token$      = new Subject<string>();
  complete$   = new Subject<string>();
  escalation$ = new Subject<EscalationEvent>();
  error$      = new Subject<string>();
  agentInfo$  = new Subject<AgentInfo>();

  isConnected = new BehaviorSubject<boolean>(false);
  isStreaming = new BehaviorSubject<boolean>(false);

  async connect(): Promise<void> {
    if (this.hub?.state === signalR.HubConnectionState.Connected) return;

    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.signalRUrl}/chat`, {
        // JWT token passed as query param for SignalR WebSocket connections
        accessTokenFactory: () => this.auth.getToken() ?? ''
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // ── Event handlers ─────────────────────────────────────────────
    this.hub.on('ReceiveToken', (token: string) => {
      this.token$.next(token);
    });

    this.hub.on('StreamComplete', (fullText: string) => {
      this.isStreaming.next(false);
      this.complete$.next(fullText);
    });

    this.hub.on('EscalationTriggered', (event: EscalationEvent) => {
      this.isStreaming.next(false);
      this.escalation$.next(event);
    });

    this.hub.on('StreamError', (message: string) => {
      this.isStreaming.next(false);
      this.error$.next(message);
    });

    this.hub.on('AgentInfo', (info: AgentInfo) => {
      this.agentInfo$.next(info);
    });

    this.hub.onreconnected(() => this.isConnected.next(true));
    this.hub.onclose(() => this.isConnected.next(false));

    await this.hub.start();
    this.isConnected.next(true);
  }

  async joinAgentSession(agentId: string): Promise<void> {
    await this.ensureConnected();
    await this.hub!.invoke('JoinAgentSession', agentId);
  }

  async sendMessage(
    agentId: string,
    userMessage: string,
    history: { role: string; content: string }[]
  ): Promise<void> {
    await this.ensureConnected();
    this.isStreaming.next(true);

    await this.hub!.invoke('SendMessage', {
      agentId,
      userMessage,
      history
    });
  }

  async disconnect(): Promise<void> {
    if (this.hub) {
      await this.hub.stop();
      this.isConnected.next(false);
    }
  }

  private async ensureConnected(): Promise<void> {
    if (this.hub?.state !== signalR.HubConnectionState.Connected) {
      await this.connect();
    }
  }
}
