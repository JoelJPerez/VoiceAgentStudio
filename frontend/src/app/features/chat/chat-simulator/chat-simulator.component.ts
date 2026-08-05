import {
  Component, inject, OnInit, OnDestroy,
  signal, computed, ElementRef, ViewChild
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';
import { ChatService, ChatMessage, AgentInfo, EscalationEvent } from '../../../core/services/chat.service';
import { AgentService } from '../../../core/services/agent.service';
import { Agent } from '../../../core/models/models';

@Component({
  selector: 'app-chat-simulator',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterLink,
    MatCardModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatProgressSpinnerModule,
    MatChipsModule, MatTooltipModule, MatSnackBarModule
  ],
  template: `
    <div class="simulator-layout">

      <!-- Sidebar: agent info -->
      <aside class="agent-sidebar">
        <a mat-icon-button routerLink="/agents" class="back-btn">
          <mat-icon>arrow_back</mat-icon>
        </a>

        @if (agent()) {
          <div class="agent-card">
            <div class="agent-avatar">
              <mat-icon>smart_toy</mat-icon>
            </div>
            <h2 class="agent-name">{{ agent()!.name }}</h2>
            <mat-chip class="tone-chip">{{ agent()!.tone }}</mat-chip>
            <mat-chip class="model-chip">{{ agent()!.modelName }}</mat-chip>
          </div>

          <div class="info-section">
            <p class="info-label">Objetivo</p>
            <p class="info-value">{{ agent()!.objective }}</p>
          </div>

          <div class="info-section">
            <p class="info-label">Estado de conexión</p>
            <div class="connection-status" [class.connected]="chatService.isConnected | async">
              <span class="status-dot"></span>
              {{ (chatService.isConnected | async) ? 'Conectado' : 'Reconectando...' }}
            </div>
          </div>

          <div class="info-section">
            <p class="info-label">Mensajes</p>
            <p class="info-value">{{ messages().length }}</p>
          </div>

          <button mat-stroked-button color="warn" class="clear-btn"
            (click)="clearConversation()" [disabled]="messages().length === 0">
            <mat-icon>delete_sweep</mat-icon> Limpiar chat
          </button>
        }

        @if (!agent() && connecting()) {
          <div class="loading-sidebar">
            <mat-spinner diameter="32" />
            <p>Cargando agente...</p>
          </div>
        }
      </aside>

      <!-- Main chat area -->
      <main class="chat-area">

        <!-- Escalation banner -->
        @if (escalation()) {
          <div class="escalation-banner">
            <mat-icon>transfer_within_a_station</mat-icon>
            <div>
              <strong>Escalada activada</strong>
              <span>Palabra detectada: "{{ escalation()!.matchedKeyword }}" — transferir a agente humano</span>
            </div>
            <button mat-icon-button (click)="escalation.set(null)">
              <mat-icon>close</mat-icon>
            </button>
          </div>
        }

        <!-- Messages -->
        <div class="messages-container" #messagesContainer>

          @if (messages().length === 0 && !connecting()) {
            <div class="empty-chat">
              <mat-icon class="empty-icon">chat_bubble_outline</mat-icon>
              <p>Escribe un mensaje para iniciar la conversación con <strong>{{ agent()?.name }}</strong></p>
            </div>
          }

          @for (msg of messages(); track $index) {
            <div class="message-row" [class.user-row]="msg.role === 'user'"
                                     [class.assistant-row]="msg.role === 'assistant'">
              @if (msg.role === 'assistant') {
                <div class="msg-avatar">
                  <mat-icon>smart_toy</mat-icon>
                </div>
              }

              <div class="bubble" [class.user-bubble]="msg.role === 'user'"
                                  [class.assistant-bubble]="msg.role === 'assistant'"
                                  [class.streaming]="msg.streaming">
                <p class="msg-content">{{ msg.content }}<span class="cursor" *ngIf="msg.streaming">▌</span></p>
                <span class="msg-time">{{ msg.timestamp | date:'HH:mm' }}</span>
              </div>

              @if (msg.role === 'user') {
                <div class="msg-avatar user-avatar">
                  <mat-icon>person</mat-icon>
                </div>
              }
            </div>
          }
        </div>

        <!-- Input area -->
        <div class="input-area">
          <mat-form-field appearance="outline" class="message-input">
            <input matInput
              [(ngModel)]="userInput"
              (keydown.enter)="sendMessage()"
              placeholder="Escribe un mensaje..."
              [disabled]="(chatService.isStreaming | async) === true || connecting()"
              autocomplete="off" />
          </mat-form-field>

          <button mat-fab color="primary"
            (click)="sendMessage()"
            [disabled]="!userInput.trim() || (chatService.isStreaming | async) === true || connecting()"
            matTooltip="Enviar (Enter)">
            @if ((chatService.isStreaming | async)) {
              <mat-spinner diameter="24" color="accent" />
            } @else {
              <mat-icon>send</mat-icon>
            }
          </button>
        </div>
      </main>
    </div>
  `,
  styles: [`
    .simulator-layout {
      display: grid;
      grid-template-columns: 260px 1fr;
      height: calc(100vh - 64px);
      gap: 0;
      margin: -24px;
    }

    /* ── Sidebar ─────────────────────────────── */
    .agent-sidebar {
      background: var(--mat-sys-surface-container-low, #f3f4f8);
      border-right: 1px solid #e0e0e0;
      padding: 16px;
      display: flex;
      flex-direction: column;
      gap: 16px;
      overflow-y: auto;
    }
    .back-btn { align-self: flex-start; }
    .agent-card { text-align: center; padding: 16px 0; }
    .agent-avatar {
      width: 64px; height: 64px; border-radius: 50%;
      background: #e8eaf6; display: flex; align-items: center;
      justify-content: center; margin: 0 auto 12px;
    }
    .agent-avatar mat-icon { font-size: 32px; width: 32px; height: 32px; color: #3f51b5; }
    .agent-name { margin: 0 0 8px; font-size: 16px; font-weight: 600; }
    .tone-chip, .model-chip { font-size: 11px; margin: 2px; }

    .info-section { padding: 0 4px; }
    .info-label { font-size: 11px; color: #999; text-transform: uppercase; letter-spacing: 0.5px; margin: 0 0 4px; }
    .info-value { margin: 0; font-size: 13px; color: #444; line-height: 1.4; }

    .connection-status {
      display: flex; align-items: center; gap: 6px;
      font-size: 13px; color: #f44336;
    }
    .connection-status.connected { color: #4caf50; }
    .status-dot {
      width: 8px; height: 8px; border-radius: 50%;
      background: currentColor;
    }

    .clear-btn { width: 100%; margin-top: auto; }
    .loading-sidebar { display: flex; flex-direction: column; align-items: center; gap: 12px; padding: 32px 0; color: #999; }

    /* ── Chat area ───────────────────────────── */
    .chat-area {
      display: flex;
      flex-direction: column;
      height: 100%;
      background: #fafafa;
    }

    .escalation-banner {
      display: flex; align-items: center; gap: 12px;
      padding: 12px 20px;
      background: #fff3e0; border-bottom: 2px solid #ff9800;
      color: #e65100;
    }
    .escalation-banner mat-icon { color: #ff9800; }
    .escalation-banner div { flex: 1; display: flex; flex-direction: column; gap: 2px; }
    .escalation-banner span { font-size: 13px; }

    .messages-container {
      flex: 1;
      overflow-y: auto;
      padding: 24px 20px;
      display: flex;
      flex-direction: column;
      gap: 16px;
      scroll-behavior: smooth;
    }

    .empty-chat {
      display: flex; flex-direction: column;
      align-items: center; justify-content: center;
      flex: 1; color: #bdbdbd; text-align: center;
      padding: 40px;
    }
    .empty-icon { font-size: 56px; width: 56px; height: 56px; margin-bottom: 16px; }

    /* ── Message rows ────────────────────────── */
    .message-row {
      display: flex;
      align-items: flex-end;
      gap: 8px;
      max-width: 75%;
    }
    .user-row { align-self: flex-end; flex-direction: row-reverse; }
    .assistant-row { align-self: flex-start; }

    .msg-avatar {
      width: 32px; height: 32px; border-radius: 50%;
      background: #e8eaf6; display: flex;
      align-items: center; justify-content: center;
      flex-shrink: 0;
    }
    .msg-avatar mat-icon { font-size: 18px; width: 18px; height: 18px; color: #3f51b5; }
    .user-avatar { background: #e3f2fd; }
    .user-avatar mat-icon { color: #1976d2; }

    .bubble {
      padding: 10px 14px;
      border-radius: 18px;
      position: relative;
      max-width: 100%;
    }
    .user-bubble {
      background: #3f51b5; color: white;
      border-bottom-right-radius: 4px;
    }
    .assistant-bubble {
      background: white; color: #212121;
      border: 1px solid #e0e0e0;
      border-bottom-left-radius: 4px;
    }
    .assistant-bubble.streaming {
      border-color: #3f51b5;
      box-shadow: 0 0 0 2px #e8eaf6;
    }

    .msg-content { margin: 0 0 4px; font-size: 14px; line-height: 1.5; white-space: pre-wrap; }
    .msg-time { font-size: 10px; opacity: 0.6; }
    .cursor { animation: blink 0.7s step-end infinite; }
    @keyframes blink { 50% { opacity: 0; } }

    /* ── Input area ──────────────────────────── */
    .input-area {
      padding: 16px 20px;
      display: flex;
      gap: 12px;
      align-items: center;
      border-top: 1px solid #e0e0e0;
      background: white;
    }
    .message-input { flex: 1; margin-bottom: -22px; }
  `]
})
export class ChatSimulatorComponent implements OnInit, OnDestroy {
  @ViewChild('messagesContainer') private messagesContainer!: ElementRef;

  chatService  = inject(ChatService);
  agentService = inject(AgentService);
  snackBar     = inject(MatSnackBar);
  route        = inject(ActivatedRoute);

  agent      = signal<Agent | null>(null);
  messages   = signal<ChatMessage[]>([]);
  escalation = signal<EscalationEvent | null>(null);
  connecting = signal(true);
  userInput  = '';

  private subs: Subscription[] = [];
  private agentId!: string;

  async ngOnInit(): Promise<void> {
    this.agentId = this.route.snapshot.paramMap.get('agentId')!;

    // Load agent details
    this.agentService.getById(this.agentId).subscribe({
      next: (a) => { this.agent.set(a); this.connecting.set(false); },
      error: () => this.connecting.set(false)
    });

    // Connect to SignalR hub
    try {
      await this.chatService.connect();
      await this.chatService.joinAgentSession(this.agentId);
    } catch (err) {
      this.snackBar.open('Error al conectar con el agente', 'Cerrar', { duration: 5000 });
    }

    // Subscribe to hub events
    this.subs.push(
      this.chatService.token$.subscribe(token => this.appendToken(token)),
      this.chatService.complete$.subscribe(() => this.finalizeStreaming()),
      this.chatService.escalation$.subscribe(e => this.escalation.set(e)),
      this.chatService.error$.subscribe(msg => {
        this.snackBar.open(`Error: ${msg}`, 'Cerrar', { duration: 5000 });
        this.finalizeStreaming();
      })
    );
  }

  async sendMessage(): Promise<void> {
    const text = this.userInput.trim();
    if (!text) return;

    this.userInput = '';
    this.escalation.set(null);

    // Add user message to UI
    this.messages.update(msgs => [
      ...msgs,
      { role: 'user', content: text, timestamp: new Date() }
    ]);

    // Add empty assistant bubble (will be filled by streaming)
    this.messages.update(msgs => [
      ...msgs,
      { role: 'assistant', content: '', streaming: true, timestamp: new Date() }
    ]);

    this.scrollToBottom();

    // Build history (excluding the empty assistant bubble just added)
    const history = this.messages()
      .slice(0, -1)
      .map(m => ({ role: m.role, content: m.content }));

    try {
      await this.chatService.sendMessage(this.agentId, text, history);
    } catch (err) {
      this.snackBar.open('Error al enviar el mensaje', 'Cerrar', { duration: 3000 });
      this.finalizeStreaming();
    }
  }

  clearConversation(): void {
    this.messages.set([]);
    this.escalation.set(null);
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
    this.chatService.disconnect();
  }

  // ── Private ────────────────────────────────────────────────────────

  private appendToken(token: string): void {
    this.messages.update(msgs => {
      const updated = [...msgs];
      const last = updated[updated.length - 1];
      if (last?.role === 'assistant') {
        updated[updated.length - 1] = {
          ...last,
          content: last.content + token,
          streaming: true
        };
      }
      return updated;
    });
    this.scrollToBottom();
  }

  private finalizeStreaming(): void {
    this.messages.update(msgs => {
      const updated = [...msgs];
      const last = updated[updated.length - 1];
      if (last?.role === 'assistant') {
        updated[updated.length - 1] = { ...last, streaming: false };
      }
      return updated;
    });
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      const el = this.messagesContainer?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    }, 0);
  }
}
