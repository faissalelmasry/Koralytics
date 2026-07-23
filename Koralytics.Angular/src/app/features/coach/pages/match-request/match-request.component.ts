import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatchRequestService } from '../../../../../core/services/match/match-request.service';
import { CreateMatchRequestDto, MatchRequestResponseDto } from '../../../../../core/interfaces/match-request.interfaces';

@Component({
  selector: 'app-match-request',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './match-request.component.html',
  styleUrls: ['./match-request.component.css']
})
export class MatchRequestComponent implements OnInit {
  private matchService = inject(MatchRequestService);

  teamId = 1; // Assuming team 1 for the logged-in coach
  
  activeTab = signal<'incoming' | 'outgoing' | 'new'>('incoming');
  
  incomingRequests = signal<MatchRequestResponseDto[]>([]);
  outgoingRequests = signal<MatchRequestResponseDto[]>([]);
  
  loading = signal(false);
  error = signal('');

  // New Request Form
  newRequest: CreateMatchRequestDto = {
    requesterTeamId: this.teamId,
    targetTeamId: 0,
    format: '11v11',
    proposedDate: new Date(Date.now() + 86400000 * 7).toISOString().slice(0, 16), // Next week default
    location: ''
  };
  submitting = signal(false);
  submitError = signal('');
  successMsg = signal('');

  ngOnInit(): void {
    this.loadIncoming();
  }

  setTab(tab: 'incoming' | 'outgoing' | 'new'): void {
    this.activeTab.set(tab);
    if (tab === 'incoming') this.loadIncoming();
    if (tab === 'outgoing') this.loadOutgoing();
  }

  loadIncoming(): void {
    this.loading.set(true);
    this.matchService.getIncomingRequests(this.teamId).subscribe({
      next: (data) => {
        this.incomingRequests.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load incoming requests.');
        this.loading.set(false);
      }
    });
  }

  loadOutgoing(): void {
    this.loading.set(true);
    this.matchService.getOutgoingRequests(this.teamId).subscribe({
      next: (data) => {
        this.outgoingRequests.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load outgoing requests.');
        this.loading.set(false);
      }
    });
  }

  sendRequest(): void {
    if (!this.newRequest.targetTeamId || !this.newRequest.proposedDate) return;
    
    this.submitting.set(true);
    this.submitError.set('');
    
    // Ensure properly formatted date string
    const dto = { ...this.newRequest };
    dto.proposedDate = new Date(dto.proposedDate).toISOString();

    this.matchService.requestFriendlyMatch(dto).subscribe({
      next: () => {
        this.successMsg.set('Match request sent successfully!');
        this.submitting.set(false);
        this.newRequest.targetTeamId = 0;
        this.newRequest.location = '';
        setTimeout(() => {
          this.successMsg.set('');
          this.setTab('outgoing');
        }, 2000);
      },
      error: (err) => {
        this.submitError.set(err?.error?.message || 'Failed to send request.');
        this.submitting.set(false);
      }
    });
  }

  acceptRequest(id: number): void {
    this.matchService.acceptMatchRequest(id).subscribe({
      next: () => this.loadIncoming(),
      error: (err) => alert(err?.error?.message || 'Failed to accept match.')
    });
  }

  declineRequest(id: number): void {
    if (!confirm('Decline this match request?')) return;
    this.matchService.declineMatchRequest(id).subscribe({
      next: () => this.loadIncoming(),
      error: (err) => alert(err?.error?.message || 'Failed to decline match.')
    });
  }

  cancelRequest(id: number): void {
    if (!confirm('Cancel this outgoing match request?')) return;
    this.matchService.cancelMatchRequest(id).subscribe({
      next: () => this.loadOutgoing(),
      error: (err) => alert(err?.error?.message || 'Failed to cancel request.')
    });
  }

  getStatusClass(status: string): string {
    switch(status.toLowerCase()) {
      case 'pending': return 'status-pending';
      case 'accepted': return 'status-accepted';
      case 'declined': return 'status-declined';
      case 'cancelled': return 'status-cancelled';
      default: return 'status-default';
    }
  }
}
