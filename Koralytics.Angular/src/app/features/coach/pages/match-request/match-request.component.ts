import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatchRequestService } from '../../../../../core/services/match/match-request.service';
import { ModalService } from '../../../../../core/services/Modal/modal';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { CreateMatchRequestDto, MatchRequestResponseDto } from '../../../../../core/interfaces/match-request.interfaces';
import { formatToLocalISO } from '../../../../../core/utils/date.util';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-match-request',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './match-request.component.html',
  styleUrls: ['./match-request.component.css']
})
export class MatchRequestComponent implements OnInit {
  private matchService = inject(MatchRequestService);
  private modalService = inject(ModalService);
  private toastService = inject(ToastService);
  private destroyRef = inject(DestroyRef);
  private translate = inject(TranslateService);

  teamId = 0; // TODO: resolve from coach profile API or route params
  
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
    proposedDate: formatToLocalISO(new Date(Date.now() + 86400000 * 7)).slice(0, 16), // Next week default
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
    this.matchService.getIncomingRequests(this.teamId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.incomingRequests.set(data);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(this.translate.instant('COACH.MATCH_REQUEST.FAILED_LOAD_INCOMING'));
          this.loading.set(false);
        }
      });
  }

  loadOutgoing(): void {
    this.loading.set(true);
    this.matchService.getOutgoingRequests(this.teamId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.outgoingRequests.set(data);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(this.translate.instant('COACH.MATCH_REQUEST.FAILED_LOAD_OUTGOING'));
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
    dto.proposedDate = formatToLocalISO(dto.proposedDate);

    this.matchService.requestFriendlyMatch(dto)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.successMsg.set(this.translate.instant('COACH.MATCH_REQUEST.REQUEST_SENT'));
          this.submitting.set(false);
          this.newRequest.targetTeamId = 0;
          this.newRequest.location = '';
          setTimeout(() => {
            this.successMsg.set('');
            this.setTab('outgoing');
          }, 2000);
        },
        error: (err) => {
          this.submitError.set(err?.error?.message || this.translate.instant('COACH.MATCH_REQUEST.FAILED_SEND'));
          this.submitting.set(false);
        }
      });
  }

  acceptRequest(id: number): void {
    this.matchService.acceptMatchRequest(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loadIncoming(),
        error: (err) => this.toastService.show(err?.error?.message || this.translate.instant('COACH.MATCH_REQUEST.FAILED_ACCEPT'), 'error')
      });
  }

  async declineRequest(id: number): Promise<void> {
    const confirmed = await this.modalService.open({
      title: this.translate.instant('COACH.MATCH_REQUEST.MODAL_DECLINE_TITLE'),
      message: this.translate.instant('COACH.MATCH_REQUEST.MODAL_DECLINE_MSG'),
      confirmText: this.translate.instant('COACH.MATCH_REQUEST.MODAL_DECLINE_CONFIRM'),
      cancelText: this.translate.instant('COMMON.CANCEL'),
      variant: 'danger'
    });

    if (!confirmed) return;

    this.matchService.declineMatchRequest(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loadIncoming(),
        error: (err) => this.toastService.show(err?.error?.message || this.translate.instant('COACH.MATCH_REQUEST.FAILED_DECLINE'), 'error')
      });
  }

  async cancelRequest(id: number): Promise<void> {
    const confirmed = await this.modalService.open({
      title: this.translate.instant('COACH.MATCH_REQUEST.MODAL_CANCEL_TITLE'),
      message: this.translate.instant('COACH.MATCH_REQUEST.MODAL_CANCEL_MSG'),
      confirmText: this.translate.instant('COACH.MATCH_REQUEST.MODAL_CANCEL_CONFIRM'),
      cancelText: this.translate.instant('COACH.MATCH_REQUEST.MODAL_CANCEL_KEEP'),
      variant: 'danger'
    });

    if (!confirmed) return;

    this.matchService.cancelMatchRequest(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loadOutgoing(),
        error: (err) => this.toastService.show(err?.error?.message || this.translate.instant('COACH.MATCH_REQUEST.FAILED_CANCEL'), 'error')
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
