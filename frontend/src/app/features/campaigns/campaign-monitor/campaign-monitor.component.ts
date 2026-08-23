import {
  Component, inject, OnInit, OnDestroy, signal
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';
import { CampaignService, Campaign, SessionMonitor, SessionStatus } from '../../../core/services/campaign.service';
import { CampaignMonitorService } from '../../../core/services/campaign-monitor.service';

@Component({
  selector: 'app-campaign-monitor',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    MatCardModule, MatButtonModule, MatIconModule, MatChipsModule,
    MatProgressBarModule, MatProgressSpinnerModule, MatExpansionModule,
    MatDividerModule, MatTooltipModule, MatSnackBarModule
  ],
  template: `
    <div class="monitor-header">
      <a mat-icon-button routerLink="/campaigns">
        <mat-icon>arrow_back</mat-icon>
      </a>
      <div class="header-info">
        <h1 class="page-title">{{ campaign()?.name ?? 'Monitor de campaña' }}</h1>
        <div class="header-meta">
          <span>Agente: <strong>{{ campaign()?.agentName }}</strong></span>
          <mat-chip [class]="'status-' + campaign()?.status?.toLowerCase()">
            {{ statusLabel(campaign()?.status) }}
          </mat-chip>
          <div class="live-indicator" [class.live]="campaign()?.status === 'Running'">
            <span class="live-dot"></span>
            {{ campaign()?.status === 'Running' ? 'EN VIVO' : 'Inactivo' }}
          </div>
        </div>
      </div>

      <div class="header-actions">
        @if (campaign()?.status === 'Running') {
          <button mat-stroked-button color="warn" (click)="pause()">
            <mat-icon>pause</mat-icon> Pausar
          </button>
        }
      </div>
    </div>

    <!-- Stats bar -->
    @if (campaign()) {
      <div class="stats-bar">
        <div class="stat-box">
          <span class="stat-number total">{{ campaign()!.totalContacts }}</span>
          <span class="stat-label">Total</span>
        </div>
        <div class="stat-box">
          <span class="stat-number pending">{{ campaign()!.pendingSessions }}</span>
          <span class="stat-label">Pendientes</span>
        </div>
        <div class="stat-box">
          <span class="stat-number active">{{ campaign()!.activeSessions }}</span>
          <span class="stat-label">Activas</span>
        </div>
        <div class="stat-box">
          <span class="stat-number completed">{{ campaign()!.completedSessions }}</span>
          <span class="stat-label">Completadas</span>
        </div>
        <div class="stat-box">
          <span class="stat-number transferred">{{ campaign()!.transferredSessions }}</span>
          <span class="stat-label">Transferidas</span>
        </div>
        <div class="stat-box">
          <span class="stat-number failed">{{ campaign()!.failedSessions }}</span>
          <span class="stat-label">Fallidas</span>
        </div>
      </div>

      <!-- Overall progress bar -->
      <div class="progress-section">
        <div class="progress-label">
          Progreso: {{ completionPercent() }}%
        </div>
        <mat-progress-bar
          [mode]="campaign()!.status === 'Running' ? 'buffer' : 'determinate'"
          [value]="completionPercent()"
          color="primary" />
      </div>
    }

    <!-- Session grid -->
    @if (loading()) {
      <div class="loading-center"><mat-spinner /></div>
    }

    @if (!loading() && sessions().length === 0) {
      <div class="empty-state">
        <mat-icon>group</mat-icon>
        <p>No hay sesiones aún. Inicia la campaña para ver los contactos aquí.</p>
      </div>
    }

    <div class="sessions-grid">
      @for (session of sessions(); track session.id) {
        <mat-card class="session-card" [class]="'session-' + session.status.toLowerCase()">
          <mat-card-content>
            <div class="session-header">
              <div class="session-contact">
                <mat-icon class="contact-icon">person</mat-icon>
                <div>
                  <p class="contact-name">{{ session.contactName }}</p>
                  <p class="contact-phone">{{ session.phoneNumber }}</p>
                </div>
              </div>
              <mat-chip [class]="'chip-' + session.status.toLowerCase()">
                {{ sessionStatusLabel(session.status) }}
              </mat-chip>
            </div>

            @if (session.wasEscalated) {
              <div class="escalation-badge">
                <mat-icon>transfer_within_a_station</mat-icon>
                Escalada
              </div>
            }

            <div class="session-meta">
              <span class="meta-item">
                <mat-icon>chat</mat-icon> {{ session.messageCount }} mensajes
              </span>
              @if (session.detectedIntention !== 'Unknown') {
                <span class="meta-item">
                  <mat-icon>psychology</mat-icon> {{ intentionLabel(session.detectedIntention) }}
                </span>
              }
              @if (session.endedAt) {
                <span class="meta-item">
                  <mat-icon>schedule</mat-icon>
                  {{ getDuration(session.startedAt, session.endedAt) }}s
                </span>
              }
            </div>

            @if (session.status === 'Active') {
              <mat-progress-bar mode="indeterminate" color="accent" class="session-progress" />
            }
          </mat-card-content>

          @if (session.status === 'Completed' || session.status === 'Transferred') {
            <mat-card-actions>
              <button mat-button (click)="viewTranscript(session)">
                <mat-icon>article</mat-icon> Ver transcripción
              </button>
            </mat-card-actions>
          }
        </mat-card>
      }
    </div>

    <!-- Transcript Dialog -->
    @if (selectedSession()) {
      <div class="dialog-backdrop" (click)="selectedSession.set(null)">
        <mat-card class="transcript-dialog" (click)="$event.stopPropagation()">
          <mat-card-header>
            <mat-card-title>Transcripción — {{ selectedSession()!.contactName }}</mat-card-title>
            <button mat-icon-button (click)="selectedSession.set(null)" class="close-btn">
              <mat-icon>close</mat-icon>
            </button>
          </mat-card-header>
          <mat-card-content class="transcript-content">
            @if (loadingTranscript()) {
              <div class="loading-center"><mat-spinner diameter="32" /></div>
            }
            @for (msg of selectedSession()!.messages ?? []; track $index) {
              <div class="transcript-msg" [class.user-msg]="msg.role === 'user'"
                                          [class.assistant-msg]="msg.role === 'assistant'">
                <span class="msg-role">{{ msg.role === 'user' ? '👤 Cliente' : '🤖 Agente' }}</span>
                <p class="msg-text">{{ msg.content }}</p>
                <span class="msg-time">{{ msg.createdAt | date:'HH:mm:ss' }}</span>
              </div>
            }
          </mat-card-content>
        </mat-card>
      </div>
    }
  `,
  styles: [`
    .monitor-header { display: flex; align-items: flex-start; gap: 12px; margin-bottom: 24px; }
    .header-info { flex: 1; }
    .page-title { margin: 0; font-size: 24px; font-weight: 600; }
    .header-meta { display: flex; align-items: center; gap: 12px; margin-top: 6px; flex-wrap: wrap; }
    .header-actions { display: flex; gap: 8px; }

    .live-indicator { display: flex; align-items: center; gap: 6px; font-size: 12px; font-weight: 500; color: #999; }
    .live-indicator.live { color: #f44336; }
    .live-dot { width: 8px; height: 8px; border-radius: 50%; background: currentColor; }
    .live-indicator.live .live-dot { animation: pulse 1s infinite; }
    @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.3; } }

    /* Stats bar */
    .stats-bar { display: flex; gap: 16px; margin-bottom: 20px; flex-wrap: wrap; }
    .stat-box { background: white; border-radius: 12px; padding: 16px 20px; text-align: center; border: 1px solid #e0e0e0; min-width: 90px; }
    .stat-number { display: block; font-size: 28px; font-weight: 700; }
    .stat-label { font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px; color: #999; }
    .total { color: #5c6bc0; }
    .pending { color: #78909c; }
    .active { color: #29b6f6; }
    .completed { color: #66bb6a; }
    .transferred { color: #ffa726; }
    .failed { color: #ef5350; }

    .progress-section { margin-bottom: 24px; }
    .progress-label { font-size: 13px; color: #666; margin-bottom: 8px; }

    /* Sessions grid */
    .loading-center { display: flex; justify-content: center; padding: 60px; }
    .empty-state { text-align: center; padding: 60px; color: #999; display: flex; flex-direction: column; align-items: center; gap: 8px; }

    .sessions-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 16px; }
    .session-card { border-left: 4px solid #e0e0e0; transition: border-color 0.3s; }
    .session-pending { border-left-color: #bdbdbd; }
    .session-active { border-left-color: #29b6f6; }
    .session-completed { border-left-color: #66bb6a; }
    .session-transferred { border-left-color: #ffa726; }
    .session-failed { border-left-color: #ef5350; }

    .session-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 8px; }
    .session-contact { display: flex; align-items: center; gap: 8px; }
    .contact-icon { color: #9e9e9e; }
    .contact-name { margin: 0; font-weight: 500; font-size: 14px; }
    .contact-phone { margin: 0; font-size: 12px; color: #999; }

    .escalation-badge { display: inline-flex; align-items: center; gap: 4px; padding: 2px 8px; background: #fff3e0; color: #e65100; border-radius: 12px; font-size: 12px; margin-bottom: 8px; }
    .escalation-badge mat-icon { font-size: 14px; width: 14px; height: 14px; }

    .session-meta { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 8px; }
    .meta-item { display: flex; align-items: center; gap: 4px; font-size: 12px; color: #666; }
    .meta-item mat-icon { font-size: 14px; width: 14px; height: 14px; }

    .session-progress { margin-top: 8px; border-radius: 4px; }

    .chip-pending { background: #f5f5f5 !important; color: #616161 !important; }
    .chip-active { background: #e3f2fd !important; color: #1565c0 !important; }
    .chip-completed { background: #e8f5e9 !important; color: #2e7d32 !important; }
    .chip-transferred { background: #fff3e0 !important; color: #e65100 !important; }
    .chip-failed { background: #fce4ec !important; color: #b71c1c !important; }

    .status-running { background: #e3f2fd !important; color: #1565c0 !important; }
    .status-completed { background: #e8f5e9 !important; color: #2e7d32 !important; }
    .status-draft { background: #f5f5f5 !important; color: #616161 !important; }
    .status-paused { background: #fff3e0 !important; color: #e65100 !important; }

    /* Transcript dialog */
    .dialog-backdrop { position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .transcript-dialog { width: 600px; max-width: 95vw; max-height: 80vh; display: flex; flex-direction: column; }
    .transcript-content { flex: 1; overflow-y: auto; max-height: 60vh; display: flex; flex-direction: column; gap: 12px; padding: 8px 0; }
    .close-btn { position: absolute; right: 8px; top: 8px; }

    .transcript-msg { padding: 12px 16px; border-radius: 12px; max-width: 85%; }
    .user-msg { background: #e3f2fd; align-self: flex-end; }
    .assistant-msg { background: #f5f5f5; align-self: flex-start; }
    .msg-role { font-size: 11px; font-weight: 500; color: #666; display: block; margin-bottom: 4px; }
    .msg-text { margin: 0 0 4px; font-size: 14px; line-height: 1.5; }
    .msg-time { font-size: 10px; color: #999; }
  `]
})
export class CampaignMonitorComponent implements OnInit, OnDestroy {
  private campaignService = inject(CampaignService);
  private monitorService  = inject(CampaignMonitorService);
  private snackBar        = inject(MatSnackBar);
  private route           = inject(ActivatedRoute);

  campaign         = signal<Campaign | null>(null);
  sessions         = signal<SessionMonitor[]>([]);
  selectedSession  = signal<SessionMonitor | null>(null);
  loading          = signal(true);
  loadingTranscript = signal(false);

  private campaignId!: string;
  private subs: Subscription[] = [];

  completionPercent = () => {
    const c = this.campaign();
    if (!c || c.totalContacts === 0) return 0;
    return Math.round(((c.completedSessions + c.transferredSessions + c.failedSessions) / c.totalContacts) * 100);
  };

  async ngOnInit(): Promise<void> {
    this.campaignId = this.route.snapshot.paramMap.get('id')!;

    // Load initial data
    this.campaignService.getById(this.campaignId).subscribe(c => {
      this.campaign.set(c);
    });

    this.campaignService.getSessions(this.campaignId).subscribe(s => {
      this.sessions.set(s);
      this.loading.set(false);
    });

    // Connect to SignalR monitor
    try {
      await this.monitorService.connect();
      await this.monitorService.joinCampaign(this.campaignId);
    } catch { /* will retry with auto-reconnect */ }

    // Handle real-time session updates
    this.subs.push(
      this.monitorService.sessionUpdated$.subscribe(updated => {
        this.sessions.update(list => {
          const idx = list.findIndex(s => s.id === updated.id);
          if (idx >= 0) {
            const next = [...list];
            next[idx] = updated;
            return next;
          }
          return [...list, updated];
        });
        // Update campaign stats
        this.campaignService.getById(this.campaignId).subscribe(c => this.campaign.set(c));
      }),

      this.monitorService.campaignCompleted$.subscribe(() => {
        this.snackBar.open('¡Campaña completada!', 'OK', { duration: 5000 });
        this.campaignService.getById(this.campaignId).subscribe(c => this.campaign.set(c));
      })
    );
  }

  viewTranscript(session: SessionMonitor): void {
    this.loadingTranscript.set(true);
    this.selectedSession.set({ ...session, messages: [] });

    this.campaignService.getSessionDetail(session.id).subscribe({
      next: (detail) => {
        this.selectedSession.set(detail);
        this.loadingTranscript.set(false);
      },
      error: () => this.loadingTranscript.set(false)
    });
  }

  pause(): void {
    this.campaignService.pause(this.campaignId).subscribe({
      next: (c) => {
        this.campaign.set(c);
        this.snackBar.open('Campaña pausada', 'OK', { duration: 3000 });
      }
    });
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
    this.monitorService.leaveCampaign(this.campaignId);
    this.monitorService.disconnect();
  }

  statusLabel = (s?: string) => ({
    Draft: 'Borrador', Running: 'Ejecutando', Paused: 'Pausada',
    Completed: 'Completada', Scheduled: 'Programada'
  })[s ?? ''] ?? s ?? '';

  sessionStatusLabel = (s: string) => ({
    Pending: 'Pendiente', Active: 'En curso', Completed: 'Completada',
    Transferred: 'Transferida', Failed: 'Fallida'
  })[s] ?? s;

  intentionLabel = (i: string) => ({
    Satisfied: '😊 Satisfecho', NeedsHuman: '🙋 Necesita agente', Objection: '❌ Objeción',
    Interested: '👍 Interesado', Closed: '✅ Cerrado', Unknown: ''
  })[i] ?? i;

  getDuration(start?: string, end?: string): number {
    if (!start || !end) return 0;
    return Math.round((new Date(end).getTime() - new Date(start).getTime()) / 1000);
  }
}
