import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  template: `
    <div class="login-container">
      <mat-card class="login-card">
        <mat-card-header>
          <div class="login-logo">
            <mat-icon class="logo-icon">smart_toy</mat-icon>
            <div>
              <h1 class="logo-title">VoiceAgent Studio</h1>
              <p class="logo-subtitle">Plataforma de Agentes de IA</p>
            </div>
          </div>
        </mat-card-header>

        <mat-card-content>
          <form [formGroup]="form" (ngSubmit)="onSubmit()" class="login-form">

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Correo electrónico</mat-label>
              <input matInput type="email" formControlName="email" autocomplete="email" />
              <mat-icon matSuffix>email</mat-icon>
              @if (form.get('email')?.hasError('required') && form.get('email')?.touched) {
                <mat-error>El correo es requerido</mat-error>
              }
              @if (form.get('email')?.hasError('email')) {
                <mat-error>Ingresa un correo válido</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Contraseña</mat-label>
              <input matInput
                [type]="showPassword ? 'text' : 'password'"
                formControlName="password"
                autocomplete="current-password" />
              <button mat-icon-button matSuffix type="button"
                (click)="showPassword = !showPassword">
                <mat-icon>{{ showPassword ? 'visibility_off' : 'visibility' }}</mat-icon>
              </button>
              @if (form.get('password')?.hasError('required') && form.get('password')?.touched) {
                <mat-error>La contraseña es requerida</mat-error>
              }
            </mat-form-field>

            @if (errorMessage) {
              <div class="error-banner">
                <mat-icon>error_outline</mat-icon>
                {{ errorMessage }}
              </div>
            }

            <button mat-raised-button color="primary" type="submit"
              class="submit-btn" [disabled]="loading || form.invalid">
              @if (loading) {
                <mat-spinner diameter="20" />
              } @else {
                Ingresar
              }
            </button>
          </form>
        </mat-card-content>

        <mat-card-footer>
          <p class="demo-hint">
            Demo: <strong>admin&#64;voiceagent.dev</strong> / <strong>Admin1234!</strong>
          </p>
        </mat-card-footer>
      </mat-card>
    </div>
  `,
  styles: [`
    .login-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: #f5f5f5;
    }
    .login-card { width: 400px; padding: 24px; }
    .login-logo { display: flex; align-items: center; gap: 16px; margin-bottom: 24px; width: 100%; }
    .logo-icon { font-size: 48px; width: 48px; height: 48px; color: #3f51b5; }
    .logo-title { margin: 0; font-size: 20px; font-weight: 600; }
    .logo-subtitle { margin: 0; font-size: 13px; color: #666; }
    .login-form { display: flex; flex-direction: column; gap: 8px; margin-top: 8px; }
    .full-width { width: 100%; }
    .submit-btn { width: 100%; height: 48px; margin-top: 8px; font-size: 16px; }
    .error-banner {
      display: flex; align-items: center; gap: 8px;
      padding: 12px; border-radius: 4px;
      background: #fdecea; color: #c62828; font-size: 14px;
    }
    .demo-hint {
      text-align: center; font-size: 12px; color: #999;
      padding: 12px 0 0; margin: 0;
    }
    mat-spinner { margin: 0 auto; }
  `]
})
export class LoginComponent {
  private fb     = inject(FormBuilder);
  private auth   = inject(AuthService);
  private router = inject(Router);

  showPassword = false;
  loading      = false;
  errorMessage = '';

  form = this.fb.group({
    email:    ['admin@voiceagent.dev', [Validators.required, Validators.email]],
    password: ['Admin1234!', [Validators.required, Validators.minLength(6)]]
  });

  onSubmit(): void {
    if (this.form.invalid) return;

    this.loading      = true;
    this.errorMessage = '';

    this.auth.login({
      email:    this.form.value.email!,
      password: this.form.value.password!
    }).subscribe({
      next: () => this.router.navigate(['/agents']),
      error: (err) => {
        this.errorMessage = err.error?.message ?? 'Error al iniciar sesión';
        this.loading = false;
      }
    });
  }
}
