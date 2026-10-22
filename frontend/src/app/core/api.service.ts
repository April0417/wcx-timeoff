import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../environment';
import { AgentOverview, CreateTimeOffRequest, TimeOffRequest } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  listAgents() {
    return this.http.get<AgentOverview[]>(`${this.base}/api/admin/agents`);
  }

  getAgentCredits(agentId: string) {
    return this.http.get<AgentOverview>(`${this.base}/api/admin/agents/${agentId}/credits`);
  }

  updateEarnedCredits(agentId: string, earnedCredits: number) {
    return this.http.put<AgentOverview>(`${this.base}/api/admin/agents/${agentId}/credits`, { earnedCredits });
  }

  myCredits() {
    return this.http.get<AgentOverview>(`${this.base}/api/me/credits`);
  }

  createRequest(request: CreateTimeOffRequest) {
    return this.http.post<TimeOffRequest>(`${this.base}/api/timeoff-requests`, request);
  }

  myRequests() {
    return this.http.get<TimeOffRequest[]>(`${this.base}/api/timeoff-requests/my`);
  }

  getRequest(id: string) {
    return this.http.get<TimeOffRequest>(`${this.base}/api/timeoff-requests/${id}`);
  }

  pendingRequests() {
    return this.http.get<TimeOffRequest[]>(`${this.base}/api/admin/timeoff-requests`, {
      params: { status: 'Pending' }
    });
  }

  approve(id: string) {
    return this.http.post<TimeOffRequest>(`${this.base}/api/admin/timeoff-requests/${id}/approve`, {});
  }

  deny(id: string) {
    return this.http.post<TimeOffRequest>(`${this.base}/api/admin/timeoff-requests/${id}/deny`, {});
  }
}
