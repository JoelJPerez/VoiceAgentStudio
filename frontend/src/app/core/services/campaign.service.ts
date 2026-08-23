import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CampaignSummary {
  id: string;
  name: string;
  status: CampaignStatus;
  agentName: string;
  totalContacts: number;
  completedSessions: number;
  createdAt: string;
}

export interface Campaign {
  id: string;
  name: string;
  description: string;
  status: CampaignStatus;
  agentId: string;
  agentName: string;
  scheduledAt?: string;
  startedAt?: string;
  completedAt?: string;
  totalContacts: number;
  pendingSessions: number;
  activeSessions: number;
  completedSessions: number;
  transferredSessions: number;
  failedSessions: number;
  createdAt: string;
}

export interface SessionMonitor {
  id: string;
  contactName: string;
  phoneNumber: string;
  status: SessionStatus;
  detectedIntention: string;
  messageCount: number;
  wasEscalated: boolean;
  escalationReason: string;
  startedAt?: string;
  endedAt?: string;
  messages?: SessionMessage[];
}

export interface SessionMessage {
  role: 'user' | 'assistant';
  content: string;
  createdAt: string;
}

export interface CreateCampaignRequest {
  name: string;
  description: string;
  agentId: string;
  scheduledAt?: string;
}

export type CampaignStatus = 'Draft' | 'Scheduled' | 'Running' | 'Paused' | 'Completed' | 'Cancelled';
export type SessionStatus  = 'Pending' | 'Active' | 'Completed' | 'Transferred' | 'Failed';

@Injectable({ providedIn: 'root' })
export class CampaignService {
  private readonly baseUrl = `${environment.apiUrl}/campaigns`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<CampaignSummary[]> {
    return this.http.get<CampaignSummary[]>(this.baseUrl);
  }

  getById(id: string): Observable<Campaign> {
    return this.http.get<Campaign>(`${this.baseUrl}/${id}`);
  }

  create(dto: CreateCampaignRequest): Observable<Campaign> {
    return this.http.post<Campaign>(this.baseUrl, dto);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  importContacts(campaignId: string, file: File): Observable<{ imported: number; message: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ imported: number; message: string }>(
      `${this.baseUrl}/${campaignId}/contacts/import`, formData);
  }

  start(id: string): Observable<Campaign> {
    return this.http.post<Campaign>(`${this.baseUrl}/${id}/start`, {});
  }

  pause(id: string): Observable<Campaign> {
    return this.http.post<Campaign>(`${this.baseUrl}/${id}/pause`, {});
  }

  getSessions(campaignId: string): Observable<SessionMonitor[]> {
    return this.http.get<SessionMonitor[]>(`${this.baseUrl}/${campaignId}/sessions`);
  }

  getSessionDetail(sessionId: string): Observable<SessionMonitor> {
    return this.http.get<SessionMonitor>(`${this.baseUrl}/sessions/${sessionId}`);
  }
}
