import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DashboardStats {
  totalAgents: number;
  activeAgents: number;
  totalCampaigns: number;
  totalSessions: number;
  completedSessions: number;
  escalatedSessions: number;
  avgResolutionRate: number;
  avgSessionDurationSeconds: number;
}

export interface SessionsByStatus { status: string; count: number; }
export interface SessionsByIntention { intention: string; count: number; }
export interface SessionsByDay { date: string; total: number; completed: number; escalated: number; }

export interface AgentPerformance {
  id: string;
  name: string;
  modelName: string;
  totalSessions: number;
  completedSessions: number;
  escalatedSessions: number;
  resolutionRate: number;
}

export interface CampaignSummaryStats {
  id: string;
  name: string;
  status: string;
  totalContacts: number;
  completedSessions: number;
  escalatedSessions: number;
  completionRate: number;
}

export interface FullAnalytics {
  stats: DashboardStats;
  byStatus: SessionsByStatus[];
  byIntention: SessionsByIntention[];
  byDay: SessionsByDay[];
  agentPerformance: AgentPerformance[];
  campaignStats: CampaignSummaryStats[];
}

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly baseUrl = `${environment.apiUrl}/analytics`;

  constructor(private http: HttpClient) {}

  getDashboard(): Observable<FullAnalytics> {
    return this.http.get<FullAnalytics>(`${this.baseUrl}/dashboard`);
  }
}
