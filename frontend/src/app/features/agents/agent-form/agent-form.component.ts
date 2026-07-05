import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSliderModule } from '@angular/material/slider';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatStepperModule } from '@angular/material/stepper';
import { AgentService } from '../../../core/services/agent.service';
import { Agent, AgentStatus } from '../../../core/models/models';

@Component({
  selector: 'app-agent-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatSliderModule, MatSlideToggleModule,
    MatButtonModule, MatIconModule, MatDividerModule,
    MatProgressSpinnerModule, MatSnackBarModule, MatStepperModule
  ],
  template: `
    <div class="form-header">
      <a mat-icon-button routerLink="/agents">
        <mat-icon>arrow_back</mat-icon>
      </a>
      <div>
        <h1 class="page-title">{{ isEditMode() ? 'Editar agente' : 'Nuevo agente' }}</h1>
        <p class="page-subtitle">{{ isEditMode() ? 'Modifica la configuración del agente' : 'Configura tu nuevo agente de IA' }}</p>
      </div>
    </div>

    @if (loadingAgent()) {
      <div class="loading-center"><mat-spinner /></div>
    }

    @if (!loadingAgent()) {
      <form [formGroup]="form" (ngSubmit)="onSubmit()">
        <mat-stepper linear #stepper orientation="vertical">

          <!-- Step 1: Basic info -->
          <mat-step label="Información básica" [stepControl]="basicInfoGroup">
            <div class="step-content" [formGroup]="basicInfoGroup">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Nombre del agente</mat-label>
                <input matInput formControlName="name" placeholder="Ej: Agente de Ventas - B2B" />
                <mat-hint>Un nombre claro y descriptivo</mat-hint>
                @if (basicInfoGroup.get('name')?.hasError('required') && basicInfoGroup.get('name')?.touched) {
                  <mat-error>El nombre es requerido</mat-error>
                }
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Descripción</mat-label>
                <textarea matInput formControlName="description" rows="2"
                  placeholder="Describe brevemente el propósito de este agente..."></textarea>
              </mat-form-field>

              <div class="two-cols">
                <mat-form-field appearance="outline">
                  <mat-label>Tono de comunicación</mat-label>
                  <mat-select formControlName="tone">
                    <mat-option value="Professional">Profesional</mat-option>
                    <mat-option value="Friendly">Amigable</mat-option>
                    <mat-option value="Formal">Formal</mat-option>
                    <mat-option value="Empathetic">Empático</mat-option>
                  </mat-select>
                </mat-form-field>

                @if (isEditMode()) {
                  <mat-form-field appearance="outline">
                    <mat-label>Estado</mat-label>
                    <mat-select formControlName="status">
                      <mat-option value="Draft">Borrador</mat-option>
                      <mat-option value="Active">Activo</mat-option>
                      <mat-option value="Inactive">Inactivo</mat-option>
                    </mat-select>
                  </mat-form-field>
                }
              </div>

              <div class="step-actions">
                <button mat-raised-button color="primary" matStepperNext
                  [disabled]="basicInfoGroup.invalid">
                  Siguiente <mat-icon>arrow_forward</mat-icon>
                </button>
              </div>
            </div>
          </mat-step>

          <!-- Step 2: LLM Config -->
          <mat-step label="Configuración del modelo IA" [stepControl]="llmConfigGroup">
            <div class="step-content" [formGroup]="llmConfigGroup">
              <div class="two-cols">
                <mat-form-field appearance="outline">
                  <mat-label>Proveedor de IA</mat-label>
                  <mat-select formControlName="llmProvider">
                    <mat-option value="OpenAI">OpenAI</mat-option>
                    <mat-option value="Anthropic">Anthropic (Claude)</mat-option>
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Modelo</mat-label>
                  <mat-select formControlName="modelName">
                    @if (llmConfigGroup.get('llmProvider')?.value === 'OpenAI') {
                      <mat-option value="gpt-4o">GPT-4o</mat-option>
                      <mat-option value="gpt-4o-mini">GPT-4o Mini</mat-option>
                      <mat-option value="gpt-4-turbo">GPT-4 Turbo</mat-option>
                    } @else {
                      <mat-option value="claude-sonnet-4-6">Claude Sonnet 4.6</mat-option>
                      <mat-option value="claude-haiku-4-5">Claude Haiku 4.5</mat-option>
                    }
                  </mat-select>
                </mat-form-field>
              </div>

              <div class="slider-field">
                <label class="slider-label">
                  Temperatura: <strong>{{ llmConfigGroup.get('temperature')?.value }}</strong>
                </label>
                <input type="range" formControlName="temperature"
                  min="0" max="2" step="0.1" class="range-input" />
                <div class="slider-hints">
                  <span>Preciso (0)</span>
                  <span>Creativo (2)</span>
                </div>
              </div>

              <div class="slider-field">
                <label class="slider-label">
                  Máximo de tokens: <strong>{{ llmConfigGroup.get('maxTokens')?.value }}</strong>
                </label>
                <input type="range" formControlName="maxTokens"
                  min="100" max="2000" step="50" class="range-input" />
                <div class="slider-hints">
                  <span>Conciso (100)</span>
                  <span>Detallado (2000)</span>
                </div>
              </div>

              <div class="step-actions">
                <button mat-button matStepperPrevious>Anterior</button>
                <button mat-raised-button color="primary" matStepperNext>
                  Siguiente <mat-icon>arrow_forward</mat-icon>
                </button>
              </div>
            </div>
          </mat-step>

          <!-- Step 3: Prompt Engineering -->
          <mat-step label="Prompt y comportamiento" [stepControl]="promptGroup">
            <div class="step-content" [formGroup]="promptGroup">

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Prompt del sistema (System Prompt)</mat-label>
                <textarea matInput formControlName="systemPrompt" rows="6"
                  placeholder="Eres [nombre], asistente de [empresa]. Tu objetivo es..."></textarea>
                <mat-hint>
                  Define la personalidad, rol y comportamiento base del agente.
                  {{ promptGroup.get('systemPrompt')?.value?.length ?? 0 }} caracteres
                </mat-hint>
                @if (promptGroup.get('systemPrompt')?.hasError('required') && promptGroup.get('systemPrompt')?.touched) {
                  <mat-error>El system prompt es requerido (mínimo 20 caracteres)</mat-error>
                }
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Objetivo de la conversación</mat-label>
                <input matInput formControlName="objective"
                  placeholder="Ej: Agendar una demo o cerrar venta del plan básico" />
                <mat-hint>Qué debe lograr el agente al finalizar la conversación</mat-hint>
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Contexto de la empresa</mat-label>
                <textarea matInput formControlName="companyContext" rows="3"
                  placeholder="Nombre de la empresa, servicios, precios, horarios..."></textarea>
                <mat-hint>Información que el agente puede mencionar durante la conversación</mat-hint>
              </mat-form-field>

              <mat-divider />

              <div class="escalation-section">
                <h3 class="section-title">
                  <mat-icon>transfer_within_a_station</mat-icon>
                  Escalada automática a humano
                </h3>

                <mat-slide-toggle formControlName="autoEscalate" color="primary">
                  Activar escalada automática
                </mat-slide-toggle>

                <mat-form-field appearance="outline" class="full-width" style="margin-top: 16px;">
                  <mat-label>Palabras clave de escalada</mat-label>
                  <input matInput formControlName="escalationKeywords"
                    placeholder="angry,cancelar,fraude,supervisor,gerente" />
                  <mat-hint>Separadas por comas. Si el cliente usa estas palabras, el agente transfiere la llamada</mat-hint>
                </mat-form-field>
              </div>

              <div class="step-actions">
                <button mat-button matStepperPrevious>Anterior</button>
                <button mat-raised-button color="primary" matStepperNext
                  [disabled]="promptGroup.invalid">
                  Revisar y guardar <mat-icon>arrow_forward</mat-icon>
                </button>
              </div>
            </div>
          </mat-step>

          <!-- Step 4: Review -->
          <mat-step label="Revisar y guardar">
            <div class="step-content review-section">
              <h3>Resumen del agente</h3>
              <div class="review-grid">
                <div class="review-item">
                  <span class="review-label">Nombre</span>
                  <span class="review-value">{{ form.get('name')?.value }}</span>
                </div>
                <div class="review-item">
                  <span class="review-label">Tono</span>
                  <span class="review-value">{{ form.get('tone')?.value }}</span>
                </div>
                <div class="review-item">
                  <span class="review-label">Modelo</span>
                  <span class="review-value">{{ form.get('llmProvider')?.value }} — {{ form.get('modelName')?.value }}</span>
                </div>
                <div class="review-item">
                  <span class="review-label">Temperatura</span>
                  <span class="review-value">{{ form.get('temperature')?.value }}</span>
                </div>
                <div class="review-item">
                  <span class="review-label">Escalada automática</span>
                  <span class="review-value">{{ form.get('autoEscalate')?.value ? 'Sí' : 'No' }}</span>
                </div>
              </div>

              <div class="step-actions">
                <button mat-button matStepperPrevious>Anterior</button>
                <button mat-raised-button color="primary" type="submit"
                  [disabled]="saving() || form.invalid">
                  @if (saving()) {
                    <mat-spinner diameter="20" />
                  } @else {
                    <mat-icon>save</mat-icon>
                    {{ isEditMode() ? 'Guardar cambios' : 'Crear agente' }}
                  }
                </button>
              </div>
            </div>
          </mat-step>

        </mat-stepper>
      </form>
    }
  `,
  styles: [`
    .form-header { display: flex; align-items: center; gap: 12px; margin-bottom: 32px; }
    .page-title { margin: 0; font-size: 24px; font-weight: 600; }
    .page-subtitle { margin: 4px 0 0; color: #666; }
    .loading-center { display: flex; justify-content: center; padding: 80px; }
    .step-content { padding: 16px 0 8px; display: flex; flex-direction: column; gap: 16px; }
    .full-width { width: 100%; }
    .two-cols { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
    .step-actions { display: flex; gap: 8px; justify-content: flex-end; padding-top: 8px; }
    .slider-field { display: flex; flex-direction: column; gap: 8px; }
    .slider-label { font-size: 14px; color: #555; }
    .range-input { width: 100%; accent-color: #3f51b5; }
    .slider-hints { display: flex; justify-content: space-between; font-size: 12px; color: #999; }
    .section-title { display: flex; align-items: center; gap: 8px; font-size: 16px; margin: 0; }
    .escalation-section { display: flex; flex-direction: column; gap: 12px; padding-top: 16px; }
    .review-section h3 { margin-top: 0; }
    .review-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .review-item { display: flex; flex-direction: column; gap: 2px; }
    .review-label { font-size: 12px; color: #999; text-transform: uppercase; letter-spacing: 0.5px; }
    .review-value { font-size: 14px; font-weight: 500; }
    mat-spinner { margin: 0 auto; }
  `]
})
export class AgentFormComponent implements OnInit {
  private fb           = inject(FormBuilder);
  private agentService = inject(AgentService);
  private router       = inject(Router);
  private route        = inject(ActivatedRoute);
  private snackBar     = inject(MatSnackBar);

  isEditMode   = signal(false);
  loadingAgent = signal(false);
  saving       = signal(false);
  agentId: string | null = null;

  // Form groups per stepper step
  basicInfoGroup = this.fb.group({
    name:        ['', [Validators.required, Validators.minLength(3)]],
    description: [''],
    tone:        ['Professional', Validators.required],
    status:      ['Draft' as AgentStatus]
  });

  llmConfigGroup = this.fb.group({
    llmProvider: ['OpenAI', Validators.required],
    modelName:   ['gpt-4o', Validators.required],
    temperature: [0.7],
    maxTokens:   [500]
  });

  promptGroup = this.fb.group({
    systemPrompt:        ['', [Validators.required, Validators.minLength(20)]],
    objective:           ['', Validators.required],
    companyContext:      [''],
    escalationKeywords:  ['angry,cancelar,fraude,supervisor,gerente'],
    autoEscalate:        [true]
  });

  // Merged form for submission
  form = this.fb.group({
    ...this.basicInfoGroup.controls,
    ...this.llmConfigGroup.controls,
    ...this.promptGroup.controls
  });

  ngOnInit(): void {
    this.agentId = this.route.snapshot.paramMap.get('id');
    if (this.agentId) {
      this.isEditMode.set(true);
      this.loadAgent(this.agentId);
    }
  }

  private loadAgent(id: string): void {
    this.loadingAgent.set(true);
    this.agentService.getById(id).subscribe({
      next: (agent: Agent) => {
        this.form.patchValue({
          name: agent.name, description: agent.description,
          tone: agent.tone, status: agent.status,
          llmProvider: agent.llmProvider, modelName: agent.modelName,
          temperature: agent.temperature, maxTokens: agent.maxTokens,
          systemPrompt: agent.systemPrompt, objective: agent.objective,
          companyContext: agent.companyContext,
          escalationKeywords: agent.escalationKeywords,
          autoEscalate: agent.autoEscalate
        });
        this.loadingAgent.set(false);
      },
      error: () => {
        this.router.navigate(['/agents']);
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);

    const payload = this.form.value as any;

    const request$ = this.isEditMode()
      ? this.agentService.update(this.agentId!, payload)
      : this.agentService.create(payload);

    request$.subscribe({
      next: () => {
        this.snackBar.open(
          this.isEditMode() ? 'Agente actualizado' : 'Agente creado exitosamente',
          'OK', { duration: 3000 });
        this.router.navigate(['/agents']);
      },
      error: (err) => {
        this.snackBar.open(
          err.error?.message ?? 'Error al guardar el agente',
          'Cerrar', { duration: 5000 });
        this.saving.set(false);
      }
    });
  }
}
