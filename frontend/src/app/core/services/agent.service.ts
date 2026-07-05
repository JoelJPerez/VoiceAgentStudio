import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Agent, AgentSummary,
  CreateAgentRequest, UpdateAgentRequest
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class AgentService {
  private readonly baseUrl = `${environment.apiUrl}/agents`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<AgentSummary[]> {
    return this.http.get<AgentSummary[]>(this.baseUrl);
  }

  getById(id: string): Observable<Agent> {
    return this.http.get<Agent>(`${this.baseUrl}/${id}`);
  }

  create(dto: CreateAgentRequest): Observable<Agent> {
    return this.http.post<Agent>(this.baseUrl, dto);
  }

  update(id: string, dto: UpdateAgentRequest): Observable<Agent> {
    return this.http.put<Agent>(`${this.baseUrl}/${id}`, dto);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  toggleStatus(id: string): Observable<Agent> {
    return this.http.patch<Agent>(`${this.baseUrl}/${id}/toggle-status`, {});
  }
}
