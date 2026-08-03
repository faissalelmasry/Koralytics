import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { loadStripe, Stripe, StripeElements, StripeCardElement } from '@stripe/stripe-js';
import { SubscriptionService } from '@core/services/subscription/subscription.service';
import { PlayerSubscriptionDto } from '@core/models/subscription/subscription.model';
import { SubscriptionStatus, SubscriptionDuration } from '@core/enums/koralytics.enums';
import { LoadingSpinnerComponent } from '@shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '@shared/components/empty-state/empty-state';
import { CustomButtonComponent } from '@shared/components/custom-button/custom-button';
import { StatusChipComponent } from '@shared/components/status-chip/status-chip';
import { Pagination } from '@shared/components/pagination/pagination';
import { NotificationService } from '@core/services/SignalR/notificationservice';

@Component({
  selector: 'app-parent-subscriptions',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    CustomButtonComponent,
    StatusChipComponent,
    Pagination
  ],
  templateUrl: './parent-subscriptions.component.html',
  styleUrls: ['./parent-subscriptions.component.css']
})
export class ParentSubscriptionsComponent implements OnInit {
  subscriptions: PlayerSubscriptionDto[] = [];
  paginatedSubscriptions: PlayerSubscriptionDto[] = [];
  currentPage = 1;
  pageSize = 6;
  isLoading = true;
  isProcessingId: number | null = null;
  errorMessage = '';
  successMessage = '';

  // 💳 Payment Modal State
  selectedSubForPayment: PlayerSubscriptionDto | null = null;

  // 💳 Stripe SDK References
  stripe: Stripe | null = null;
  elements: StripeElements | null = null;
  cardElement: StripeCardElement | null = null;
  clientSecret = '';
  isStripeLoading = false;

  // 📜 Subscription History State
  selectedHistoryPlayer: { id: number; name: string } | null = null;
  historySubscriptions: PlayerSubscriptionDto[] = [];
  isLoadingHistory = false;

  readonly Status = SubscriptionStatus;
  readonly Duration = SubscriptionDuration;

  constructor(private subscriptionService: SubscriptionService, private notificationService: NotificationService) { }

  ngOnInit(): void {
    this.loadSubscriptions();
  }

  loadSubscriptions(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.subscriptionService.getMyChildrenSubscriptions().subscribe({
      next: (data: PlayerSubscriptionDto[]) => {
        this.subscriptions = data || [];
        this.currentPage = 1;
        this.updatePaginatedSubscriptions();
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load subscriptions:', err);
        this.errorMessage = err?.error?.message || 'Failed to load subscription details. Please try again.';
        this.isLoading = false;
      }
    });
  }

  updatePaginatedSubscriptions(): void {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    this.paginatedSubscriptions = this.subscriptions.slice(startIndex, startIndex + this.pageSize);
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.updatePaginatedSubscriptions();
  }

  // Stats Counters
  get totalCount(): number {
    return this.subscriptions.length;
  }

  get paidCount(): number {
    return this.subscriptions.filter(s => this.isPaid(s.status)).length;
  }

  get unpaidCount(): number {
    return this.subscriptions.filter(s => String(s.status).toUpperCase() === 'UNPAID' || String(s.status) === '2').length;
  }

  get graceCount(): number {
    return this.subscriptions.filter(s => String(s.status).toUpperCase() === 'GRACE' || String(s.status) === '3').length;
  }

  // 📜 Step 2: Open History Modal & Load History
  openHistoryModal(playerId: number, playerName: string): void {
    this.selectedHistoryPlayer = { id: playerId, name: playerName };
    this.isLoadingHistory = true;
    this.historySubscriptions = [];

    this.subscriptionService.getPlayerSubscriptionHistory(playerId).subscribe({
      next: (data) => {
        this.historySubscriptions = data || [];
        this.isLoadingHistory = false;
      },
      error: (err) => {
        console.error('Failed to load history:', err);
        this.errorMessage = 'Failed to load subscription history.';
        this.isLoadingHistory = false;
      }
    });
  }

  closeHistoryModal(): void {
    this.selectedHistoryPlayer = null;
    this.historySubscriptions = [];
  }

  // 🟢 Step A: Open Payment Modal & Fetch Payment Intent
  openPaymentModal(sub: PlayerSubscriptionDto): void {
    this.selectedSubForPayment = sub;
    this.errorMessage = '';

    this.initStripePayment(sub.id);
  }

  // 🟢 Step B: Clean up and Close Payment Modal
  closePaymentModal(): void {
    if (this.cardElement) {
      this.cardElement.destroy();
      this.cardElement = null;
    }
    this.selectedSubForPayment = null;
  }

  // 🟢 Step C: Contact .NET and Mount Stripe Input
  async initStripePayment(subscriptionId: number): Promise<void> {
    this.isStripeLoading = true;

    this.subscriptionService.createPaymentIntent(subscriptionId).subscribe({
      next: async (res) => {
        this.clientSecret = res.clientSecret;

        // Load Stripe SDK with Publishable Key returned by .NET
        this.stripe = await loadStripe(res.publishableKey);

        if (this.stripe) {
          this.elements = this.stripe.elements();

          // Style Stripe Card Input to match Koralytics Dark Theme
          this.cardElement = this.elements.create('card', {
            style: {
              base: {
                color: '#f8fafc',
                fontFamily: 'Inter, sans-serif',
                fontSmoothing: 'antialiased',
                fontSize: '16px',
                '::placeholder': { color: '#94a3b8' }
              },
              invalid: {
                color: '#ef4444',
                iconColor: '#ef4444'
              }
            }
          });

          // Mount into <div id="card-element">
          setTimeout(() => {
            this.cardElement?.mount('#card-element');
            this.isStripeLoading = false;
          }, 100);
        }
      },
      error: (err) => {
        this.errorMessage = err?.error?.message || 'Failed to initialize Stripe checkout.';
        this.isStripeLoading = false;
      }
    });
  }

  // 🟢 Step D: Authorize Visa Payment via Stripe
  async confirmPayment(): Promise<void> {
    if (!this.selectedSubForPayment) return;

    const sub = this.selectedSubForPayment;
    this.isProcessingId = sub.id;
    this.errorMessage = '';
    this.successMessage = '';

    if (!this.stripe || !this.cardElement || !this.clientSecret) {
      this.errorMessage = 'Stripe payment processor is not ready.';
      this.isProcessingId = null;
      return;
    }

    const result = await this.stripe.confirmCardPayment(this.clientSecret, {
      payment_method: { card: this.cardElement }
    });

    if (result.error) {
      this.errorMessage = result.error.message || 'Payment authorization failed.';
      this.isProcessingId = null;
    } else if (result.paymentIntent && result.paymentIntent.status === 'succeeded') {
      this.subscriptionService.paySubscription(sub.id).subscribe({
        next: () => {
          this.successMessage = `Visa Payment Successful! ${sub.amount} EGP paid for ${sub.playerName}.`;
          // notification
          this.notificationService.notifyAcademySubscriptionPaid(sub.academyId, sub.id).subscribe({
            error: (e) => console.error('Failed to notify academy of payment', e)
          });

          const parentMsg = `Your online payment of ${sub.amount} EGP for ${sub.playerName} was successful.`;
          this.notificationService.notifyPlayerParents(sub.playerId, parentMsg).subscribe({
            error: (e) => console.error('Failed to notify parent of payment success', e)
          });
          this.isProcessingId = null;
          this.closePaymentModal();
          this.loadSubscriptions();
        },
        error: (err) => {
          this.errorMessage = err?.error?.message || 'Payment authorized, but backend update failed.';
          this.isProcessingId = null;
        }
      });
    }
  }

  getStatusChipType(status: SubscriptionStatus | string | number): 'success' | 'danger' | 'warning' | 'info' {
    const s = String(status || '').toLowerCase();
    if (s === 'paid' || s === '0' || s === '1') return 'success';
    if (s === 'unpaid' || s === '2') return 'warning';
    if (s === 'grace' || s === '3') return 'danger';
    return 'info';
  }

  formatDuration(duration: SubscriptionDuration | string | number): string {
    const d = String(duration || '').toLowerCase();
    if (d === 'onemonth' || d === '1') return 'Monthly';
    if (d === 'threemonths' || d === '3') return 'Quarterly (3M)';
    if (d === 'sixmonths' || d === '6') return 'Semi-Annual (6M)';
    if (d === 'oneyear' || d === '12') return 'Annual (1Y)';
    return String(duration) || 'Monthly';
  }

  isPaid(status: SubscriptionStatus | string | number): boolean {
    const s = String(status || '').toLowerCase();
    return s === 'paid' || s === '0' || s === '1';
  }

  isValidDate(dateVal?: string | Date): boolean {
    if (!dateVal) return false;
    const strVal = String(dateVal);
    return !strVal.startsWith('0001-01-01') && strVal !== 'null' && strVal !== 'undefined';
  }
}