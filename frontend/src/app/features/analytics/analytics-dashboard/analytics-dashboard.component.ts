import {
  Component, inject, OnInit, signal,
  AfterViewInit, ViewChild, ElementRef, OnDestroy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { Chart, registerables } from 'chart.js';
import {
  AnalyticsService, FullAnalytics
} from '../../../core/services/analytics.service';

Chart.register(...registerables);

@Component({
  selector: 'app-analytics-dashboard',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    MatCardModule, MatIconModule, MatButtonModule,
    MatProgressSpinnerModule, MatDividerModule,
    MatTableModule, MatChipsModule
  ],
  template: `
    <div class="page-header">
      <div>
        <h1 class="page-title">Analytics</h1>
        <p class="page-subtitle">Métricas de rendimiento de agentes y campañas</p>
      </div>
      <button mat-stroked-button (click)="load()">
        <mat-icon>refresh</mat-icon> Actualizar
      </button>
    </div>

    @if (loading()) {
      <div class="loading-center"><mat-spinner /></div>
    }

    @if (!loading() && data()) {

      <!-- KPI Cards -->
      <div class="kpi-grid">
        <mat-card class="kpi-card">
          <div class="kpi-icon blue"><mat-icon>smart_toy</mat-icon></div>
          <div class="kpi-content">
            <span class="kpi-value">{{ data()!.stats.totalAgents }}</span>
            <span class="kpi-label">Agentes totales</span>
            <span class="kpi-sub">{{ data()!.stats.activeAgents }} activos</span>
          </div>
        </mat-card>

        <mat-card class="kpi-card">
          <div class="kpi-icon green"><mat-icon>campaign</mat-icon></div>
          <div class="kpi-content">
            <span class="kpi-value">{{ data()!.stats.totalCampaigns }}</span>
            <span class="kpi-label">Campañas</span>
          </div>
        </mat-card>

        <mat-card class="kpi-card">
          <div class="kpi-icon purple"><mat-icon>chat</mat-icon></div>
          <div class="kpi-content">
            <span class="kpi-value">{{ data()!.stats.totalSessions }}</span>
            <span class="kpi-label">Sesiones totales</span>
            <span class="kpi-sub">{{ data()!.stats.completedSessions }} completadas</span>
          </div>
        </mat-card>

        <mat-card class="kpi-card">
          <div class="kpi-icon orange"><mat-icon>check_circle</mat-icon></div>
          <div class="kpi-content">
            <span class="kpi-value">{{ data()!.stats.avgResolutionRate }}%</span>
            <span class="kpi-label">Tasa de resolución</span>
          </div>
        </mat-card>

        <mat-card class="kpi-card">
          <div class="kpi-icon red"><mat-icon>transfer_within_a_station</mat-icon></div>
          <div class="kpi-content">
            <span class="kpi-value">{{ data()!.stats.escalatedSessions }}</span>
            <span class="kpi-label">Escaladas a humano</span>
          </div>
        </mat-card>

        <mat-card class="kpi-card">
          <div class="kpi-icon teal"><mat-icon>timer</mat-icon></div>
          <div class="kpi-content">
            <span class="kpi-value">{{ data()!.stats.avgSessionDurationSeconds }}s</span>
            <span class="kpi-label">Duración promedio</span>
          </div>
        </mat-card>
      </div>

      <!-- Charts row -->
      <div class="charts-grid">

        <!-- Line chart: sessions per day -->
        <mat-card class="chart-card wide">
          <mat-card-header>
            <mat-card-title>Sesiones últimos 7 días</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <canvas #lineChart></canvas>
          </mat-card-content>
        </mat-card>

        <!-- Doughnut: by status -->
        <mat-card class="chart-card">
          <mat-card-header>
            <mat-card-title>Sesiones por estado</mat-card-title>
          </mat-card-header>
          <mat-card-content class="chart-center">
            <canvas #statusChart></canvas>
          </mat-card-content>
        </mat-card>

        <!-- Doughnut: by intention -->
        <mat-card class="chart-card">
          <mat-card-header>
            <mat-card-title>Intención detectada</mat-card-title>
          </mat-card-header>
          <mat-card-content class="chart-center">
            <canvas #intentionChart></canvas>
          </mat-card-content>
        </mat-card>
      </div>

      <!-- Agent performance table -->
      @if (data()!.agentPerformance.length > 0) {
        <mat-card class="table-card">
          <mat-card-header>
            <mat-card-title>Rendimiento por agente</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <table mat-table [dataSource]="data()!.agentPerformance" class="perf-table">
              <ng-container matColumnDef="name">
                <th mat-header-cell *matHeaderCellDef>Agente</th>
                <td mat-cell *matCellDef="let row">
                  <div class="agent-cell">
                    <mat-icon class="agent-icon">smart_toy</mat-icon>
                    <div>
                      <p class="agent-name">{{ row.name }}</p>
                      <p class="agent-model">{{ row.modelName }}</p>
                    </div>
                  </div>
                </td>
              </ng-container>
              <ng-container matColumnDef="sessions">
                <th mat-header-cell *matHeaderCellDef>Sesiones</th>
                <td mat-cell *matCellDef="let row">{{ row.totalSessions }}</td>
              </ng-container>
              <ng-container matColumnDef="completed">
                <th mat-header-cell *matHeaderCellDef>Completadas</th>
                <td mat-cell *matCellDef="let row">{{ row.completedSessions }}</td>
              </ng-container>
              <ng-container matColumnDef="escalated">
                <th mat-header-cell *matHeaderCellDef>Escaladas</th>
                <td mat-cell *matCellDef="let row">{{ row.escalatedSessions }}</td>
              </ng-container>
              <ng-container matColumnDef="rate">
                <th mat-header-cell *matHeaderCellDef>Resolución</th>
                <td mat-cell *matCellDef="let row">
                  <div class="rate-cell">
                    <span [class]="rateClass(row.resolutionRate)">{{ row.resolutionRate }}%</span>
                  </div>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="agentColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: agentColumns;"></tr>
            </table>
          </mat-card-content>
        </mat-card>
      }

      <!-- Campaign stats table -->
      @if (data()!.campaignStats.length > 0) {
        <mat-card class="table-card">
          <mat-card-header>
            <mat-card-title>Estadísticas por campaña</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <table mat-table [dataSource]="data()!.campaignStats" class="perf-table">
              <ng-container matColumnDef="name">
                <th mat-header-cell *matHeaderCellDef>Campaña</th>
                <td mat-cell *matCellDef="let row">{{ row.name }}</td>
              </ng-container>
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>Estado</th>
                <td mat-cell *matCellDef="let row">
                  <mat-chip [class]="'status-' + row.status.toLowerCase()">{{ row.status }}</mat-chip>
                </td>
              </ng-container>
              <ng-container matColumnDef="contacts">
                <th mat-header-cell *matHeaderCellDef>Contactos</th>
                <td mat-cell *matCellDef="let row">{{ row.totalContacts }}</td>
              </ng-container>
              <ng-container matColumnDef="completed">
                <th mat-header-cell *matHeaderCellDef>Completadas</th>
                <td mat-cell *matCellDef="let row">{{ row.completedSessions }}</td>
              </ng-container>
              <ng-container matColumnDef="rate">
                <th mat-header-cell *matHeaderCellDef>Tasa</th>
                <td mat-cell *matCellDef="let row">
                  <span [class]="rateClass(row.completionRate)">{{ row.completionRate }}%</span>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="campaignColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: campaignColumns;"></tr>
            </table>
          </mat-card-content>
        </mat-card>
      }

      @if (data()!.stats.totalSessions === 0) {
        <mat-card class="empty-analytics">
          <mat-icon>insights</mat-icon>
          <h3>Sin datos aún</h3>
          <p>Ejecuta campañas para ver métricas aquí.</p>
          <a mat-raised-button color="primary" routerLink="/campaigns">Ir a campañas</a>
        </mat-card>
      }
    }
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 28px; }
    .page-title { margin: 0; font-size: 28px; font-weight: 600; }
    .page-subtitle { margin: 4px 0 0; color: #666; }
    .loading-center { display: flex; justify-content: center; padding: 80px; }

    /* KPI Cards */
    .kpi-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); gap: 16px; margin-bottom: 24px; }
    .kpi-card { display: flex; align-items: center; gap: 16px; padding: 16px !important; }
    .kpi-icon { width: 48px; height: 48px; border-radius: 12px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
    .kpi-icon mat-icon { color: white; }
    .blue { background: #5c6bc0; }
    .green { background: #43a047; }
    .purple { background: #8e24aa; }
    .orange { background: #fb8c00; }
    .red { background: #e53935; }
    .teal { background: #00897b; }
    .kpi-content { display: flex; flex-direction: column; }
    .kpi-value { font-size: 24px; font-weight: 700; line-height: 1; }
    .kpi-label { font-size: 12px; color: #666; margin-top: 4px; }
    .kpi-sub { font-size: 11px; color: #999; margin-top: 2px; }

    /* Charts */
    .charts-grid { display: grid; grid-template-columns: 2fr 1fr 1fr; gap: 16px; margin-bottom: 24px; }
    .chart-card canvas { max-height: 280px; }
    .chart-center { display: flex; justify-content: center; }
    .wide { grid-column: span 1; }

    /* Tables */
    .table-card { margin-bottom: 24px; }
    .perf-table { width: 100%; }
    .agent-cell { display: flex; align-items: center; gap: 8px; }
    .agent-icon { color: #5c6bc0; }
    .agent-name { margin: 0; font-weight: 500; font-size: 14px; }
    .agent-model { margin: 0; font-size: 12px; color: #999; }
    .rate-good { color: #43a047; font-weight: 600; }
    .rate-mid { color: #fb8c00; font-weight: 600; }
    .rate-bad { color: #e53935; font-weight: 600; }
    .status-running { background: #e3f2fd !important; color: #1565c0 !important; }
    .status-completed { background: #e8f5e9 !important; color: #2e7d32 !important; }
    .status-draft { background: #f5f5f5 !important; color: #616161 !important; }

    .empty-analytics { text-align: center; padding: 48px; display: flex; flex-direction: column; align-items: center; gap: 12px; color: #999; }
    .empty-analytics mat-icon { font-size: 56px; width: 56px; height: 56px; }

    @media (max-width: 900px) {
      .charts-grid { grid-template-columns: 1fr; }
    }
  `]
})
export class AnalyticsDashboardComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('lineChart') lineChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('statusChart') statusChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('intentionChart') intentionChartRef!: ElementRef<HTMLCanvasElement>;

  private analyticsService = inject(AnalyticsService);

  data    = signal<FullAnalytics | null>(null);
  loading = signal(true);

  agentColumns   = ['name', 'sessions', 'completed', 'escalated', 'rate'];
  campaignColumns = ['name', 'status', 'contacts', 'completed', 'rate'];

  private charts: Chart[] = [];

  ngOnInit(): void { this.load(); }

  ngAfterViewInit(): void {
    // Charts are built after data loads — see load()
  }

  load(): void {
    this.loading.set(true);
    this.destroyCharts();

    this.analyticsService.getDashboard().subscribe({
      next: (d) => {
        this.data.set(d);
        this.loading.set(false);
        setTimeout(() => this.buildCharts(d), 100);
      },
      error: () => this.loading.set(false)
    });
  }

  ngOnDestroy(): void { this.destroyCharts(); }

  rateClass(rate: number): string {
    if (rate >= 70) return 'rate-good';
    if (rate >= 40) return 'rate-mid';
    return 'rate-bad';
  }

  private buildCharts(d: FullAnalytics): void {
    this.buildLineChart(d);
    this.buildStatusChart(d);
    this.buildIntentionChart(d);
  }

  private buildLineChart(d: FullAnalytics): void {
    if (!this.lineChartRef) return;
    const ctx = this.lineChartRef.nativeElement.getContext('2d')!;
    this.charts.push(new Chart(ctx, {
      type: 'line',
      data: {
        labels: d.byDay.map(x => x.date),
        datasets: [
          {
            label: 'Total', data: d.byDay.map(x => x.total),
            borderColor: '#5c6bc0', backgroundColor: 'rgba(92,107,192,0.1)',
            fill: true, tension: 0.4
          },
          {
            label: 'Completadas', data: d.byDay.map(x => x.completed),
            borderColor: '#43a047', backgroundColor: 'rgba(67,160,71,0.1)',
            fill: true, tension: 0.4
          },
          {
            label: 'Escaladas', data: d.byDay.map(x => x.escalated),
            borderColor: '#e53935', backgroundColor: 'rgba(229,57,53,0.1)',
            fill: true, tension: 0.4
          }
        ]
      },
      options: {
        responsive: true,
        plugins: { legend: { position: 'bottom' } },
        scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } }
      }
    }));
  }

  private buildStatusChart(d: FullAnalytics): void {
    if (!this.statusChartRef || d.byStatus.length === 0) return;
    const colors = ['#5c6bc0','#43a047','#fb8c00','#e53935','#9e9e9e'];
    const ctx = this.statusChartRef.nativeElement.getContext('2d')!;
    this.charts.push(new Chart(ctx, {
      type: 'doughnut',
      data: {
        labels: d.byStatus.map(x => x.status),
        datasets: [{ data: d.byStatus.map(x => x.count), backgroundColor: colors }]
      },
      options: {
        responsive: true,
        plugins: { legend: { position: 'bottom' } }
      }
    }));
  }

  private buildIntentionChart(d: FullAnalytics): void {
    if (!this.intentionChartRef || d.byIntention.length === 0) return;
    const colors = ['#5c6bc0','#43a047','#fb8c00','#e53935','#00897b','#8e24aa'];
    const ctx = this.intentionChartRef.nativeElement.getContext('2d')!;
    this.charts.push(new Chart(ctx, {
      type: 'doughnut',
      data: {
        labels: d.byIntention.map(x => x.intention),
        datasets: [{ data: d.byIntention.map(x => x.count), backgroundColor: colors }]
      },
      options: {
        responsive: true,
        plugins: { legend: { position: 'bottom' } }
      }
    }));
  }

  private destroyCharts(): void {
    this.charts.forEach(c => c.destroy());
    this.charts = [];
  }
}
