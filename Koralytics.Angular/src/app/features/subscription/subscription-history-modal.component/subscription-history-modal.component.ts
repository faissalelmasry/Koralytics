import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SubscriptionService } from '@core/services/subscription/subscription.service';
import { PlayerSubscriptionDto } from '@core/models/subscription/subscription.model';
import { SubscriptionStatus } from '@core/enums/koralytics.enums';

@Component({
  selector: 'app-subscription-history-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './subscription-history-modal.component.html',
  styleUrls: ['./subscription-history-modal.component.css']
})
export class SubscriptionHistoryModalComponent implements OnInit {
  @Input() playerId!: number;
  @Input() playerName: string = '';
  @Output() close = new EventEmitter<void>();

  history: PlayerSubscriptionDto[] = [];
  loading: boolean = true;

  constructor(private subscriptionService: SubscriptionService) { }

  ngOnInit(): void {
    if (this.playerId) {
      this.subscriptionService.getPlayerSubscriptionHistory(this.playerId).subscribe({
        next: (data) => {
          this.history = data || [];
          this.loading = false;
        },
        error: (err) => {
          console.error('Failed to load subscription history:', err);
          this.loading = false;
        }
      });
    }
  }

  getStatusBadgeClass(status: SubscriptionStatus | string | number): string {
    const s = String(status || '').toLowerCase();
    if (s === 'paid' || s === '0' || s === '1') {
      return 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20';
    }
    if (s === 'grace' || s === '3') {
      return 'bg-amber-500/10 text-amber-400 border border-amber-500/20';
    }
    if (s === 'unpaid' || s === '2') {
      return 'bg-rose-500/10 text-rose-400 border border-rose-500/20';
    }
    return 'bg-slate-500/10 text-slate-400 border border-slate-500/20';
  }

  getStatusText(status: SubscriptionStatus | string | number): string {
    const s = String(status || '').toLowerCase();
    if (s === 'paid' || s === '0' || s === '1') return 'PAID';
    if (s === 'grace' || s === '3') return 'GRACE PERIOD';
    if (s === 'unpaid' || s === '2') return 'UNPAID';
    return String(status || 'UNKNOWN').toUpperCase();
  }

  isValidDate(dateVal?: string | Date): boolean {
    if (!dateVal) return false;
    const strVal = String(dateVal);
    return !strVal.startsWith('0001-01-01') && strVal !== 'null' && strVal !== 'undefined';
  }
}