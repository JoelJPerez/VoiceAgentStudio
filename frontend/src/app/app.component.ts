import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { CommonModule } from '@angular/common';
import { AuthService } from './core/services/auth.service';
import { MatDividerModule } from '@angular/material/divider';


@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive,
    CommonModule, MatToolbarModule, MatButtonModule,
    MatIconModule, MatMenuModule, MatDividerModule  
  ],
  template: `
    @if (auth.currentUser()) {
      <mat-toolbar color="primary" class="app-toolbar">
        <span class="brand">
          <mat-icon>smart_toy</mat-icon>
          VoiceAgent Studio
        </span>

        <nav class="nav-links">
          <a mat-button routerLink="/agents" routerLinkActive="active-link">
            <mat-icon>groups</mat-icon> Agentes
          </a>
        </nav>

        <span class="spacer"></span>

        <button mat-icon-button [matMenuTriggerFor]="userMenu">
          <mat-icon>account_circle</mat-icon>
        </button>
        <mat-menu #userMenu="matMenu">
          <div class="user-info">
            <p class="user-name">{{ auth.currentUser()?.fullName }}</p>
            <p class="user-email">{{ auth.currentUser()?.email }}</p>
          </div>
          <mat-divider />
          <button mat-menu-item (click)="auth.logout()">
            <mat-icon>logout</mat-icon> Cerrar sesión
          </button>
        </mat-menu>
      </mat-toolbar>
    }

    <main class="main-content">
      <router-outlet />
    </main>
  `,
  styles: [`
    .app-toolbar { gap: 16px; }
    .brand { display: flex; align-items: center; gap: 8px; font-weight: 500; font-size: 18px; }
    .nav-links { display: flex; gap: 4px; margin-left: 24px; }
    .spacer { flex: 1; }
    .active-link { background: rgba(255,255,255,0.15); border-radius: 4px; }
    .main-content { padding: 24px; max-width: 1200px; margin: 0 auto; }
    .user-info { padding: 12px 16px; }
    .user-name { margin: 0; font-weight: 500; font-size: 14px; }
    .user-email { margin: 0; font-size: 12px; color: #666; }
  `]
})
export class AppComponent {
  auth = inject(AuthService);
}

