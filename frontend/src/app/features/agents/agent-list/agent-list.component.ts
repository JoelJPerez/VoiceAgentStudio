import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AgentService } from '../../../core/services/agent.service';
import { AgentSummary } from '../../../core/models/models';
import { MatDividerModule } from '@angular/material/divider';

@Component({
  selector: 'app-agent-list',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    MatCardModule, MatButtonModule, MatIconModule,
    MatChipsModule, MatMenuModule, MatProgressSpinnerModule,
    MatSnackBarModule, MatDialogModule, MatTooltipModule, MatDividerModule
  ],
  template: `
    <div class="page-header">
      <div>
        <h1 class="page-title">Agentes de IA</h1>
        <p class="page-subtitle">Configura y administra tus agentes conversacionales</p>
      </div>
      <button mat-raised-button color="primary" routerLink="/agents/new">
        <mat-icon>add</mat-icon> Nuevo agente
      </button>
    </div>

    @if (loading()) {
      <div class="loading-center">
        <mat-spinner />
      </div>
    }

    @if (!loading() && agents().length === 0) {
      <div class="empty-state">
        <mat-icon class="empty-icon">smart_toy</mat-icon>
        <h2>No tienes agentes aún</h2>
        <p>Crea tu primer agente de IA para comenzar</p>
        <button mat-raised-button color="primary" routerLink="/agents/new">
          Crear agente
        </button>
      </div>
    }

    <div class="agents-grid">
      @for (agent of agents(); track agent.id) {
        <mat-card class="agent-card">
          <mat-card-header>
            <div mat-card-avatar class="agent-avatar">
              <mat-icon>smart_toy</mat-icon>
            </div>
            <mat-card-title>{{ agent.name }}</mat-card-title>
            <mat-card-subtitle>{{ agent.modelName }}</mat-card-subtitle>

            <button mat-icon-button [matMenuTriggerFor]="agentMenu" class="card-menu-btn">
              <mat-icon>more_vert</mat-icon>
            </button>
            <mat-menu #agentMenu="matMenu">
              <a mat-menu-item [routerLink]="['/agents', agent.id, 'edit']">
                <mat-icon>edit</mat-icon> Editar
              </a>
              <button mat-menu-item (click)="toggleStatus(agent)">
                <mat-icon>{{ agent.status === 'Active' ? 'pause' : 'play_arrow' }}</mat-icon>
                {{ agent.status === 'Active' ? 'Desactivar' : 'Activar' }}
              </button>
              <mat-divider />
              <button mat-menu-item class="danger-item" (click)="confirmDelete(agent)">
                <mat-icon>delete</mat-icon> Eliminar
              </button>
            </mat-menu>
          </mat-card-header>

          <mat-card-content>
            <div class="chip-row">
              <mat-chip [class]="'status-' + agent.status.toLowerCase()">
                <mat-icon matChipAvatar>circle</mat-icon>
                {{ statusLabel(agent.status) }}
              </mat-chip>
              <mat-chip>{{ toneLabel(agent.tone) }}</mat-chip>
            </div>

            <div class="stats-row">
              <div class="stat">
                <span class="stat-value">{{ agent.totalSessions }}</span>
                <span class="stat-label">Sesiones</span>
              </div>
              <div class="stat">
                <span class="stat-value">{{ (agent.avgResolutionRate * 100).toFixed(0) }}%</span>
                <span class="stat-label">Resolución</span>
              </div>
              <div class="stat">
                <span class="stat-value">{{ agent.createdAt | date:'dd/MM/yy' }}</span>
                <span class="stat-label">Creado</span>
              </div>
            </div>
          </mat-card-content>

          <mat-card-actions>
            <a mat-button color="primary" [routerLink]="['/agents', agent.id, 'edit']">
              <mat-icon>edit</mat-icon> Editar
            </a>
            <!-- Sprint 2: Chat Simulator will go here -->
            <button mat-button color="accent" matTooltip="Disponible en Sprint 2" disabled>
              <mat-icon>chat</mat-icon> Simular
            </button>
          </mat-card-actions>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 32px; }
    .page-title { margin: 0; font-size: 28px; font-weight: 600; }
    .page-subtitle { margin: 4px 0 0; color: #666; }
    .loading-center { display: flex; justify-content: center; padding: 80px; }
    .empty-state { text-align: center; padding: 80px 24px; color: #999; }
    .empty-icon { font-size: 64px; width: 64px; height: 64px; margin-bottom: 16px; }

    .agents-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
      gap: 20px;
    }
    .agent-card { position: relative; }
    .agent-avatar {
      background: #e8eaf6; display: flex;
      align-items: center; justify-content: center; border-radius: 50%;
    }
    .agent-avatar mat-icon { color: #3f51b5; }
    .card-menu-btn { position: absolute; top: 8px; right: 8px; }

    .chip-row { display: flex; gap: 8px; flex-wrap: wrap; margin: 12px 0; }
    .status-active mat-icon { color: #4caf50; }
    .status-inactive mat-icon { color: #f44336; }
    .status-draft mat-icon { color: #ff9800; }

    .stats-row { display: flex; gap: 24px; margin-top: 12px; }
    .stat { display: flex; flex-direction: column; align-items: center; }
    .stat-value { font-size: 18px; font-weight: 600; color: #3f51b5; }
    .stat-label { font-size: 11px; color: #999; text-transform: uppercase; letter-spacing: 0.5px; }

    .danger-item { color: #f44336; }
    .danger-item mat-icon { color: #f44336; }
  `]
})
export class AgentListComponent implements OnInit {
  private agentService = inject(AgentService);
  private snackBar     = inject(MatSnackBar);

  agents  = signal<AgentSummary[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.loadAgents();
  }

  private loadAgents(): void {
    this.loading.set(true);
    this.agentService.getAll().subscribe({
      next: (data) => { this.agents.set(data); this.loading.set(false); },
      error: () => { this.loading.set(false); }
    });
  }

  toggleStatus(agent: AgentSummary): void {
    this.agentService.toggleStatus(agent.id).subscribe({
      next: (updated) => {
        this.agents.update(list =>
          list.map(a => a.id === agent.id ? { ...a, status: updated.status } : a));
        this.snackBar.open(
          `Agente ${updated.status === 'Active' ? 'activado' : 'desactivado'}`,
          'OK', { duration: 3000 });
      }
    });
  }

  confirmDelete(agent: AgentSummary): void {
    if (!confirm(`¿Eliminar el agente "${agent.name}"? Esta acción no se puede deshacer.`)) return;

    this.agentService.delete(agent.id).subscribe({
      next: () => {
        this.agents.update(list => list.filter(a => a.id !== agent.id));
        this.snackBar.open('Agente eliminado', 'OK', { duration: 3000 });
      }
    });
  }

  statusLabel(status: string): string {
    return ({ Active: 'Activo', Inactive: 'Inactivo', Draft: 'Borrador' })[status] ?? status;
  }

  toneLabel(tone: string): string {
    return ({
      Professional: 'Profesional', Friendly: 'Amigable',
      Formal: 'Formal', Empathetic: 'Empático'
    })[tone] ?? tone;
  }
}
