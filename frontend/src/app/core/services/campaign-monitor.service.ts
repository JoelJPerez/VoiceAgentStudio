import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject, BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { SessionMonitor } from './campaign.service';

export interface CampaignCompletedEvent {
  campaignId: string;
  completedAt: string;
}

@Injectable({ providedIn: 'root' })
export class CampaignMonitorService {
  private hub: signalR.HubConnection | null = null;
  private auth = inject(AuthService);

  sessionUpdated$    = new Subject<SessionMonitor>();
  campaignCompleted$ = new Subject<CampaignCompletedEvent>();
  isConnected        = new BehaviorSubject<boolean>(false);

  async connect(): Promise<void> {
    if (this.hub?.state === signalR.HubConnectionState.Connected) return;

    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.signalRUrl}/campaign-monitor`, {
        accessTokenFactory: () => this.auth.getToken() ?? ''
      })
      .withAutomaticReconnect([0, 2000, 5000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.hub.on('SessionUpdated', (data: SessionMonitor) => {
      this.sessionUpdated$.next(data);
    });

    this.hub.on('CampaignCompleted', (data: CampaignCompletedEvent) => {
      this.campaignCompleted$.next(data);
    });

    this.hub.onreconnected(() => this.isConnected.next(true));
    this.hub.onclose(() => this.isConnected.next(false));

    await this.hub.start();
    this.isConnected.next(true);
  }

  async joinCampaign(campaignId: string): Promise<void> {
    await this.ensureConnected();
    await this.hub!.invoke('JoinCampaign', campaignId);
  }

  async leaveCampaign(campaignId: string): Promise<void> {
    if (this.hub?.state === signalR.HubConnectionState.Connected) {
      await this.hub.invoke('LeaveCampaign', campaignId);
    }
  }

  async disconnect(): Promise<void> {
    if (this.hub) {
      await this.hub.stop();
      this.isConnected.next(false);
    }
  }

  private async ensureConnected(): Promise<void> {
    if (this.hub?.state !== signalR.HubConnectionState.Connected) {
      await this.connect();
    }
  }
}
