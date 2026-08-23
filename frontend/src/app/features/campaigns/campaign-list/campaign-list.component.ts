import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CampaignService, CampaignSummary } from '../../../core/services/campaign.service';
import { AgentService } from '../../../core/services/agent.service';
import { AgentSummary } from '../../../core/models/models';

@Component({
  selector: 'app-campaign-list',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule, MatChipsModule,
    MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatProgressSpinnerModule, MatProgressBarModule, MatSnackBarModule, MatTooltipModule
  ],
  template: `
    <div class="page-header">
      <div>
        <h1 class="page-title">Campañas</h1>
        <p class="page-subtitle">Gestiona y ejecuta campañas de llamadas masivas</p>
      </div>
      <button mat-raised-button color="primary" (click)="showCreateDialog = true">
        <mat-icon>add</mat-icon> Nueva campaña
      </button>
    </div>

    @if (loading()) {
      <div class="loading-center"><mat-spinner /></div>
    }

    @if (!loading() && campaigns().length === 0) {
      <div class="empty-state">
        <mat-icon class="empty-icon">campaign</mat-icon>
        <h2>No tienes campañas aún</h2>
        <p>Crea tu primera campaña para ejecutar llamadas masivas con tus agentes de IA</p>
        <button mat-raised-button color="primary" (click)="showCreateDialog = true">
          Crear campaña
        </button>
      </div>
    }

    <div class="campaigns-grid">
      @for (campaign of campaigns(); track campaign.id) {
        <mat-card class="campaign-card">
          <mat-card-header>
            <div mat-card-avatar class="campaign-avatar">
              <mat-icon>campaign</mat-icon>
            </div>
            <mat-card-title>{{ campaign.name }}</mat-card-title>
            <mat-card-subtitle>{{ campaign.agentName }}</mat-card-subtitle>
          </mat-card-header>

          <mat-card-content>
            <div class="chip-row">
              <mat-chip [class]="'status-' + campaign.status.toLowerCase()">
                {{ statusLabel(campaign.status) }}
              </mat-chip>
            </div>

            <div class="stats-row">
              <div class="stat">
                <span class="stat-value">{{ campaign.totalContacts }}</span>
                <span class="stat-label">Contactos</span>
              </div>
              <div class="stat">
                <span class="stat-value">{{ campaign.completedSessions }}</span>
                <span class="stat-label">Completadas</span>
              </div>
              <div class="stat">
                <span class="stat-value">{{ campaign.createdAt | date:'dd/MM/yy' }}</span>
                <span class="stat-label">Creada</span>
              </div>
            </div>

            @if (campaign.status === 'Running') {
              <mat-progress-bar mode="indeterminate" color="accent" class="progress-bar" />
            }
          </mat-card-content>

          <mat-card-actions>
            <a mat-button color="primary"
              [routerLink]="['/campaigns', campaign.id, 'monitor']">
              <mat-icon>monitor</mat-icon> Monitor
            </a>

            <!-- Import CSV -->
            <label mat-button class="import-btn"
              [matTooltip]="campaign.status === 'Running' ? 'Pausa la campaña para importar' : 'Importar contactos CSV'">
              <mat-icon>upload_file</mat-icon> CSV
              <input type="file" accept=".csv" hidden
                (change)="onCsvSelected($event, campaign.id)"
                [disabled]="campaign.status === 'Running'" />
            </label>

            @if (campaign.status === 'Running') {
              <button mat-button color="warn" (click)="pause(campaign)">
                <mat-icon>pause</mat-icon> Pausar
              </button>
            } @else if (campaign.status !== 'Completed') {
              <button mat-button color="accent" (click)="start(campaign)"
                [disabled]="campaign.totalContacts === 0"
                [matTooltip]="campaign.totalContacts === 0 ? 'Importa contactos primero' : 'Iniciar campaña'">
                <mat-icon>play_arrow</mat-icon> Iniciar
              </button>
            }

            <button mat-icon-button color="warn" (click)="delete(campaign)"
              [matTooltip]="'Eliminar campaña'"
              [disabled]="campaign.status === 'Running'">
              <mat-icon>delete</mat-icon>
            </button>
          </mat-card-actions>
        </mat-card>
      }
    </div>

    <!-- Create Campaign Dialog -->
    @if (showCreateDialog) {
      <div class="dialog-backdrop" (click)="showCreateDialog = false">
        <mat-card class="create-dialog" (click)="$event.stopPropagation()">
          <mat-card-header>
            <mat-card-title>Nueva campaña</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <form [formGroup]="createForm" class="dialog-form">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Nombre de la campaña</mat-label>
                <input matInput formControlName="name" />
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Descripción</mat-label>
                <textarea matInput formControlName="description" rows="2"></textarea>
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Agente de IA</mat-label>
                <mat-select formControlName="agentId">
                  @for (agent of agents(); track agent.id) {
                    <mat-option [value]="agent.id">
                      {{ agent.name }} — {{ agent.modelName }}
                    </mat-option>
                  }
                </mat-select>
              </mat-form-field>
            </form>
          </mat-card-content>
          <mat-card-actions align="end">
            <button mat-button (click)="showCreateDialog = false">Cancelar</button>
            <button mat-raised-button color="primary"
              (click)="createCampaign()"
              [disabled]="createForm.invalid || creating()">
              @if (creating()) { <mat-spinner diameter="20" /> }
              @else { Crear campaña }
            </button>
          </mat-card-actions>
        </mat-card>
      </div>
    }
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 32px; }
    .page-title { margin: 0; font-size: 28px; font-weight: 600; }
    .page-subtitle { margin: 4px 0 0; color: #666; }
    .loading-center { display: flex; justify-content: center; padding: 80px; }
    .empty-state { text-align: center; padding: 80px 24px; color: #999; }
    .empty-icon { font-size: 64px; width: 64px; height: 64px; margin-bottom: 16px; }

    .campaigns-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(340px, 1fr)); gap: 20px; }
    .campaign-avatar { background: #e8f5e9; display: flex; align-items: center; justify-content: center; border-radius: 50%; }
    .campaign-avatar mat-icon { color: #388e3c; }

    .chip-row { display: flex; gap: 8px; margin: 12px 0; }
    .status-running { background: #e3f2fd !important; color: #1565c0 !important; }
    .status-completed { background: #e8f5e9 !important; color: #2e7d32 !important; }
    .status-draft { background: #f5f5f5 !important; color: #616161 !important; }
    .status-paused { background: #fff3e0 !important; color: #e65100 !important; }

    .stats-row { display: flex; gap: 24px; margin-top: 8px; }
    .stat { display: flex; flex-direction: column; align-items: center; }
    .stat-value { font-size: 18px; font-weight: 600; color: #388e3c; }
    .stat-label { font-size: 11px; color: #999; text-transform: uppercase; letter-spacing: 0.5px; }

    .progress-bar { margin-top: 12px; border-radius: 4px; }
    .import-btn { cursor: pointer; display: inline-flex; align-items: center; gap: 4px; padding: 0 8px; font-size: 14px; }

    .dialog-backdrop {
      position: fixed; inset: 0; background: rgba(0,0,0,0.5);
      display: flex; align-items: center; justify-content: center; z-index: 1000;
    }
    .create-dialog { width: 480px; max-width: 95vw; }
    .dialog-form { display: flex; flex-direction: column; gap: 8px; padding-top: 8px; }
    .full-width { width: 100%; }
    mat-spinner { margin: 0 auto; }
  `]
})
export class CampaignListComponent implements OnInit {
  private campaignService = inject(CampaignService);
  private agentService    = inject(AgentService);
  private snackBar        = inject(MatSnackBar);
  private fb              = inject(FormBuilder);

  campaigns = signal<CampaignSummary[]>([]);
  agents    = signal<AgentSummary[]>([]);
  loading   = signal(true);
  creating  = signal(false);
  showCreateDialog = false;

  createForm = this.fb.group({
    name:        ['', [Validators.required, Validators.minLength(3)]],
    description: [''],
    agentId:     ['', Validators.required]
  });

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.campaignService.getAll().subscribe({
      next: d => { this.campaigns.set(d); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
    this.agentService.getAll().subscribe(a => this.agents.set(a));
  }

  createCampaign(): void {
    if (this.createForm.invalid) return;
    this.creating.set(true);
    this.campaignService.create(this.createForm.value as any).subscribe({
      next: (c) => {
        this.campaigns.update(list => [
          { id: c.id, name: c.name, status: c.status, agentName: c.agentName,
            totalContacts: 0, completedSessions: 0, createdAt: c.createdAt },
          ...list
        ]);
        this.showCreateDialog = false;
        this.createForm.reset();
        this.creating.set(false);
        this.snackBar.open('Campaña creada', 'OK', { duration: 3000 });
      },
      error: (err) => {
        this.snackBar.open(err.error?.message ?? 'Error al crear la campaña', 'Cerrar', { duration: 4000 });
        this.creating.set(false);
      }
    });
  }

  onCsvSelected(event: Event, campaignId: string): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    this.campaignService.importContacts(campaignId, file).subscribe({
      next: (res) => {
        this.campaigns.update(list =>
          list.map(c => c.id === campaignId
            ? { ...c, totalContacts: c.totalContacts + res.imported }
            : c));
        this.snackBar.open(res.message, 'OK', { duration: 4000 });
      },
      error: (err) =>
        this.snackBar.open(err.error?.message ?? 'Error importando CSV', 'Cerrar', { duration: 4000 })
    });
  }

  start(campaign: CampaignSummary): void {
    this.campaignService.start(campaign.id).subscribe({
      next: () => {
        this.campaigns.update(list =>
          list.map(c => c.id === campaign.id ? { ...c, status: 'Running' } : c));
        this.snackBar.open('Campaña iniciada', 'OK', { duration: 3000 });
      },
      error: (err) =>
        this.snackBar.open(err.error?.message ?? 'Error al iniciar', 'Cerrar', { duration: 4000 })
    });
  }

  pause(campaign: CampaignSummary): void {
    this.campaignService.pause(campaign.id).subscribe({
      next: () => {
        this.campaigns.update(list =>
          list.map(c => c.id === campaign.id ? { ...c, status: 'Paused' } : c));
        this.snackBar.open('Campaña pausada', 'OK', { duration: 3000 });
      }
    });
  }

  delete(campaign: CampaignSummary): void {
    if (!confirm(`¿Eliminar la campaña "${campaign.name}"?`)) return;
    this.campaignService.delete(campaign.id).subscribe({
      next: () => {
        this.campaigns.update(list => list.filter(c => c.id !== campaign.id));
        this.snackBar.open('Campaña eliminada', 'OK', { duration: 3000 });
      }
    });
  }

  statusLabel(status: string): string {
    return ({
      Draft: 'Borrador', Running: 'Ejecutando', Paused: 'Pausada',
      Completed: 'Completada', Scheduled: 'Programada', Cancelled: 'Cancelada'
    })[status] ?? status;
  }
}
