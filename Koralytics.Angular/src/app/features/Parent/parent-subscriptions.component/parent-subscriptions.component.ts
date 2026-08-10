import { Component, OnInit, OnDestroy, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { loadStripe, Stripe, StripeElements, StripeCardElement } from '@stripe/stripe-js';
import { Subscription } from 'rxjs';
import { SubscriptionService } from '@core/services/subscription/subscription.service';
import { PlayerSubscriptionDto } from '@core/models/subscription/subscription.model';
import { SubscriptionStatus, SubscriptionDuration } from '@core/enums/koralytics.enums';
import { LoadingSpinnerComponent } from '@shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '@shared/components/empty-state/empty-state';
import { CustomButtonComponent } from '@shared/components/custom-button/custom-button';
import { StatusChipComponent } from '@shared/components/status-chip/status-chip';
import { Pagination } from '@shared/components/pagination/pagination';
import { NotificationService } from '@core/services/SignalR/notificationservice';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-parent-subscriptions',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush, // 🟢 OPTIMIZATION: Halts redundant UI renders
  imports: [
    CommonModule,
    FormsModule,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    CustomButtonComponent,
    StatusChipComponent,
    Pagination,
    TranslatePipe
  ],
  templateUrl: './parent-subscriptions.component.html',
  styleUrls: ['./parent-subscriptions.component.css']
})
export class ParentSubscriptionsComponent implements OnInit, OnDestroy {
  // 🟢 OPTIMIZATION: Memory cleanup crew
  private subscriptionsList = new Subscription();

  subscriptions: PlayerSubscriptionDto[] = [];
  paginatedSubscriptions: PlayerSubscriptionDto[] = [];
  currentPage = 1;
  pageSize = 6;
  isLoading = true;
  isProcessingId: number | null = null;
  errorMessage = '';
  successMessage = '';

  // 🟢 OPTIMIZATION: Static counters to prevent Getter GC churn
  totalCount = 0;
  paidCount = 0;
  unpaidCount = 0;
  graceCount = 0;

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

  constructor(
    private subscriptionService: SubscriptionService,
    private notificationService: NotificationService,
    private cdr: ChangeDetectorRef,
    private translate: TranslateService
  ) { }

  ngOnInit(): void {
    this.loadSubscriptions();
  }

  ngOnDestroy(): void {
    // 🟢 OPTIMIZATION: Nuke all pending memory tasks and Stripe iframes when leaving the page
    this.subscriptionsList.unsubscribe();
    if (this.cardElement) {
      this.cardElement.destroy();
    }
  }

  loadSubscriptions(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.subscriptionsList.add(
      this.subscriptionService.getMyChildrenSubscriptions().subscribe({
        next: (data: PlayerSubscriptionDto[]) => {
          this.subscriptions = data || [];
          this.currentPage = 1;
          this.calculateStats(); // 🟢 Calculated exactly once
          this.updatePaginatedSubscriptions();
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Failed to load subscriptions:', err);
          this.errorMessage = err?.error?.message || 'Failed to load subscription details. Please try again.';
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      })
    );
  }

  // 🟢 OPTIMIZATION: Calculates stats once instead of on every render cycle
  private calculateStats(): void {
    this.totalCount = this.subscriptions.length;
    this.paidCount = this.subscriptions.filter(s => this.isPaid(s.status)).length;
    this.unpaidCount = this.subscriptions.filter(s => String(s.status).toUpperCase() === 'UNPAID' || String(s.status) === '2').length;
    this.graceCount = this.subscriptions.filter(s => String(s.status).toUpperCase() === 'GRACE' || String(s.status) === '3').length;
    this.subscriptionService.getMyChildrenSubscriptions().subscribe({
      next: (data: PlayerSubscriptionDto[]) => {
        this.subscriptions = data || [];
        this.currentPage = 1;
        this.updatePaginatedSubscriptions();
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load subscriptions:', err);
        this.errorMessage = err?.error?.message || this.translate.instant('PARENT.ERRORS.LOAD_SUBS_FAILED');
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
    this.cdr.detectChanges();
  }

  // 📜 Step 2: Open History Modal & Load History
  openHistoryModal(playerId: number, playerName: string): void {
    this.selectedHistoryPlayer = { id: playerId, name: playerName };
    this.isLoadingHistory = true;
    this.historySubscriptions = [];
    this.cdr.detectChanges();

    this.subscriptionService.getPlayerSubscriptionHistory(playerId).subscribe({
      next: (data) => {
        this.historySubscriptions = data || [];
        this.isLoadingHistory = false;
      },
      error: (err) => {
        console.error('Failed to load history:', err);
        this.errorMessage = this.translate.instant('PARENT.ERRORS.LOAD_HISTORY_FAILED');
        this.isLoadingHistory = false;
      }
    });
    this.subscriptionsList.add(
      this.subscriptionService.getPlayerSubscriptionHistory(playerId).subscribe({
        next: (data) => {
          this.historySubscriptions = data || [];
          this.isLoadingHistory = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Failed to load history:', err);
          this.errorMessage = 'Failed to load subscription history.';
          this.isLoadingHistory = false;
          this.cdr.detectChanges();
        }
      })
    );
  }

  closeHistoryModal(): void {
    this.selectedHistoryPlayer = null;
    this.historySubscriptions = [];
    this.cdr.detectChanges();
  }

  // 🟢 Step A: Open Payment Modal & Fetch Payment Intent
  openPaymentModal(sub: PlayerSubscriptionDto): void {
    this.selectedSubForPayment = sub;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.initStripePayment(sub.id);
  }

  // 🟢 Step B: Clean up and Close Payment Modal
  closePaymentModal(): void {
    try {
      if (this.cardElement) {
        // Unmount first just in case, then destroy
        this.cardElement.unmount();
        this.cardElement.destroy();
      }
    } catch (e) {
      console.warn('Stripe cleanup warning:', e);
    } finally {
      this.cardElement = null;
      this.selectedSubForPayment = null;
      this.cdr.detectChanges();
    }
  }

  // 🟢 Step C: Contact .NET and Mount Stripe Input
  async initStripePayment(subscriptionId: number): Promise<void> {
    this.isStripeLoading = true;
    this.cdr.detectChanges();

    this.subscriptionsList.add(
      this.subscriptionService.createPaymentIntent(subscriptionId).subscribe({
        next: async (res) => {
          this.clientSecret = res.clientSecret;

          this.stripe = await loadStripe(res.publishableKey);

          if (this.stripe) {
            this.elements = this.stripe.elements();

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

            setTimeout(() => {
              this.cardElement?.mount('#card-element');
              this.isStripeLoading = false;
              this.cdr.detectChanges();
            }, 100);
          }
        },
        error: (err) => {
          this.errorMessage = err?.error?.message || this.translate.instant('PARENT.ERRORS.INIT_STRIPE_FAILED');
          this.isStripeLoading = false;
          this.cdr.detectChanges();
        }
      })
    );
  }

  // 🟢 Step D: Authorize Visa Payment via Stripe
  async confirmPayment(): Promise<void> {
    if (!this.selectedSubForPayment) return;

    const sub = this.selectedSubForPayment;
    this.isProcessingId = sub.id;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    if (!this.stripe || !this.cardElement || !this.clientSecret) {
      this.errorMessage = this.translate.instant('PARENT.ERRORS.STRIPE_NOT_READY');
      this.isProcessingId = null;
      this.cdr.detectChanges();
      return;
    }

    const result = await this.stripe.confirmCardPayment(this.clientSecret, {
      payment_method: { card: this.cardElement }
    });

    if (result.error) {
      this.errorMessage = result.error.message || this.translate.instant('PARENT.ERRORS.PAYMENT_FAILED');
      this.isProcessingId = null;
      this.cdr.detectChanges();
    } else if (result.paymentIntent && result.paymentIntent.status === 'succeeded') {

      this.subscriptionsList.add(
        this.subscriptionService.paySubscription(sub.id).subscribe({
          next: () => {
            this.successMessage = this.translate.instant('PARENT.TOAST.PAYMENT_SUCCESS', { amount: sub.amount, name: sub.playerName });

            // Background notifications (No need to await or block the UI for these)
            this.subscriptionsList.add(
              this.notificationService.notifyAcademySubscriptionPaid(sub.academyId, sub.id).subscribe({
                error: (e) => console.error('Failed to notify academy of payment', e)
              })
            );

            const parentMsg = `Your online payment of ${sub.amount} EGP for ${sub.playerName} was successful.`;
            this.subscriptionsList.add(
              this.notificationService.notifyPlayerParents(sub.playerId, parentMsg).subscribe({
                error: (e) => console.error('Failed to notify parent of payment success', e)
              })
            );

            this.isProcessingId = null;
            this.closePaymentModal();
            this.loadSubscriptions();
          },
          error: (err) => {
            this.errorMessage = err?.error?.message || this.translate.instant('PARENT.ERRORS.BACKEND_UPDATE_FAILED');
            this.isProcessingId = null;
            this.cdr.detectChanges();
          }
        })
      );
    } else {
      this.errorMessage = `Payment is pending or requires further action. Status: ${result.paymentIntent?.status}`;
      this.isProcessingId = null;
      this.cdr.detectChanges();
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
    if (d === 'onemonth' || d === '1') return this.translate.instant('PARENT.DURATION.MONTHLY');
    if (d === 'threemonths' || d === '3') return this.translate.instant('PARENT.DURATION.QUARTERLY');
    if (d === 'sixmonths' || d === '6') return this.translate.instant('PARENT.DURATION.SEMI_ANNUAL');
    if (d === 'oneyear' || d === '12') return this.translate.instant('PARENT.DURATION.ANNUAL');
    return this.translate.instant('PARENT.DURATION.UNKNOWN');
  }

  getStatusLabel(status: SubscriptionStatus | string | number): string {
    const s = String(status || '').toLowerCase();
    if (s === 'paid' || s === '0' || s === '1') return this.translate.instant('PARENT.STATUS.PAID');
    if (s === 'unpaid' || s === '2') return this.translate.instant('PARENT.STATUS.UNPAID');
    if (s === 'grace' || s === '3') return this.translate.instant('PARENT.STATUS.GRACE');
    return this.translate.instant('PARENT.STATUS.UNKNOWN');
  }

  isPaid(status: SubscriptionStatus | string | number): boolean {
    const s = String(status || '').toLowerCase();
    return s === 'paid' || s === '0' || s === '1';
  }

  isStripeEnabled(tier: string | number | undefined): boolean {
    if (tier === undefined || tier === null) return true;
    const t = String(tier).toLowerCase();
    return t !== 'starter' && t !== '0';
  }

  isValidDate(dateVal?: string | Date): boolean {
    if (!dateVal) return false;
    const strVal = String(dateVal);
    return !strVal.startsWith('0001-01-01') && strVal !== 'null' && strVal !== 'undefined';
  }
}