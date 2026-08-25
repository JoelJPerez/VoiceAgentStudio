import { Routes } from '@angular/router';
import { authGuard, publicGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'agents',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    canActivate: [publicGuard],
    children: [
      {
        path: 'login',
        loadComponent: () =>
          import('./features/auth/login/login.component').then(m => m.LoginComponent)
      },
      {
        path: '',
        redirectTo: 'login',
        pathMatch: 'full'
      }
    ]
  },
  {
    path: 'agents',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/agents/agent-list/agent-list.component').then(m => m.AgentListComponent)
      },
      {
        path: 'new',
        loadComponent: () =>
          import('./features/agents/agent-form/agent-form.component').then(m => m.AgentFormComponent)
      },
      {
        path: ':id/edit',
        loadComponent: () =>
          import('./features/agents/agent-form/agent-form.component').then(m => m.AgentFormComponent)
      },
      {
        path: ':agentId/chat',
        loadComponent: () =>
          import('./features/chat/chat-simulator/chat-simulator.component').then(m => m.ChatSimulatorComponent)
      }
    ]
  },
  {
    path: 'campaigns',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/campaigns/campaign-list/campaign-list.component').then(m => m.CampaignListComponent)
      },
      {
        path: ':id/monitor',
        loadComponent: () =>
          import('./features/campaigns/campaign-monitor/campaign-monitor.component').then(m => m.CampaignMonitorComponent)
      }
    ]
  },
  {
    path: 'analytics',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/analytics/analytics-dashboard/analytics-dashboard.component')
        .then(m => m.AnalyticsDashboardComponent)
  },
  {
    path: '**',
    redirectTo: 'agents'
  }
];
