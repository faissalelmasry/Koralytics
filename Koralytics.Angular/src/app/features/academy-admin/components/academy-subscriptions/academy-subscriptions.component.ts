import { Component, OnInit, OnDestroy, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { SubscriptionService } from '@core/services/subscription/subscription.service';
import { AuthService } from '@core/services/auth/auth.service';
import { PlayerSubscriptionDto, CreateSubscriptionDto } from '@core/models/subscription/subscription.model';
import { SubscriptionStatus, SubscriptionDuration } from '@core/enums/koralytics.enums';
import { LoadingSpinnerComponent } from '@shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '@shared/components/empty-state/empty-state';
import { CustomButtonComponent } from '@shared/components/custom-button/custom-button';
import { StatusChipComponent } from '@shared/components/status-chip/status-chip';
import { CustomSelect, SelectOption } from '@shared/components/custom-select/custom-select';
import { SearchBarComponent } from '@shared/components/search-bar/search-bar';
import { CustomDatePicker } from '@shared/components/custom-date-picker/custom-date-picker';
import { CustomNumberInputComponent } from '@shared/components/custom-number-input/custom-number-input';
import { ConfirmDialogComponent } from '@shared/components/confirm-dialog/confirm-dialog';
import { Pagination } from '@shared/components/pagination/pagination';
import { NotificationService } from '@core/services/SignalR/notificationservice';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LocalizedDatePipe } from '@shared/pipes/localized-date.pipe';

@Component({
  selector: 'app-academy-subscriptions',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush, // 🟢 OPTIMIZATION: Halts redundant UI rendering
  imports: [
    CommonModule,
    FormsModule,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    CustomButtonComponent,
    StatusChipComponent,
    CustomSelect,
    SearchBarComponent,
    CustomDatePicker,
    CustomNumberInputComponent,
    ConfirmDialogComponent,
    Pagination,
    TranslatePipe,
    LocalizedDatePipe
  ],
  templateUrl: './academy-subscriptions.component.html',
  styleUrl: './academy-subscriptions.component.css',
})
export class AcademySubscriptions implements OnInit, OnDestroy {
  // 🟢 OPTIMIZATION: Memory cleanup crew
  private subscriptionsList = new Subscription();

  activeAcademyId!: number;

  subscriptions: PlayerSubscriptionDto[] = [];
  filteredSubscriptions: PlayerSubscriptionDto[] = [];
  paginatedSubscriptions: PlayerSubscriptionDto[] = [];
  currentPage = 1;
  pageSize = 8;
  isLoading = true;
  isProcessingCashId: number | null = null;

  errorMessage = '';
  successMessage = '';
  searchQuery = '';

  // EDIT MODAL STATE
  isEditModalOpen = false;
  isEditingSub = false;
  selectedSubForEdit: PlayerSubscriptionDto | null = null;
  editSub: CreateSubscriptionDto = {
    playerId: 0,
    academyId: 0,
    amount: 1500,
    duration: SubscriptionDuration.OneMonth,
    startDate: new Date().toISOString().substring(0, 10)
  };

  // 🟢 OPTIMIZATION: Static array prevents GC churn
  readonly durationOptions: SelectOption[] = [
    { value: SubscriptionDuration.OneMonth, label: 'ACADEMY_ADMIN.SUBSCRIPTIONS.DURATION_1M' },
    { value: SubscriptionDuration.ThreeMonths, label: 'ACADEMY_ADMIN.SUBSCRIPTIONS.DURATION_3M' },
    { value: SubscriptionDuration.SixMonths, label: 'ACADEMY_ADMIN.SUBSCRIPTIONS.DURATION_6M' },
    { value: SubscriptionDuration.OneYear, label: 'ACADEMY_ADMIN.SUBSCRIPTIONS.DURATION_1Y' }
  ];

  // PLAYER HISTORY MODAL STATE
  selectedHistoryPlayer: { id: number; name: string } | null = null;
  historySubscriptions: PlayerSubscriptionDto[] = [];
  isLoadingHistory = false;

  readonly Status = SubscriptionStatus;
  readonly Duration = SubscriptionDuration;

  constructor(
    private subscriptionService: SubscriptionService,
    private authService: AuthService,
    private notificationService: NotificationService,
    private translate: TranslateService,
    private cdr: ChangeDetectorRef // 🟢 OPTIMIZATION: Injected for precise UI updates
  ) { }

  ngOnInit(): void {
    // 🟢 OPTIMIZATION: Evaluate Active Academy ID exactly once on load
    const user = this.authService.getCurrentUserValue();
    this.activeAcademyId = user?.academyId || 2;

    this.loadSubscriptions();
  }

  ngOnDestroy(): void {
    // 🟢 OPTIMIZATION: Nuke all pending requests when navigating away
    this.subscriptionsList.unsubscribe();
  }

  loadSubscriptions(): void {
    this.isLoading = true;
    this.errorMessage = '';
    
const academyId = this.activeAcademyId;

this.subscriptionsList.add(
  this.subscriptionService.getAcademySubscriptions(academyId).subscribe({
    next: (data: PlayerSubscriptionDto[]) => {
      this.subscriptions = (data || []).filter(s => !this.isPaid(s.status));
      this.applyFilters();
      this.isLoading = false;
      this.cdr.detectChanges();
    },
    error: (err) => {
      console.error('Failed to load academy subscriptions:', err);
      this.errorMessage =
        err?.error?.message ||
        this.translate.instant('ACADEMY_ADMIN.MESSAGES.LOAD_SUBS_FAILED');
      this.isLoading = false;
      this.cdr.detectChanges();
    }
  })
);

  applyFilters(): void {
    let result = [...this.subscriptions];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(s => s.playerName.toLowerCase().includes(q));
    }

    this.filteredSubscriptions = result;
    this.currentPage = 1;
    this.updatePaginatedSubscriptions();
  }

  updatePaginatedSubscriptions(): void {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    this.paginatedSubscriptions = this.filteredSubscriptions.slice(startIndex, startIndex + this.pageSize);
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.updatePaginatedSubscriptions();
    this.cdr.detectChanges();
  }

  onSearchChange(text: string): void {
    this.searchQuery = text;
    this.applyFilters();
    this.cdr.detectChanges();
  }

  // EDIT MODAL HANDLERS
  openEditModal(sub: PlayerSubscriptionDto): void {
    this.selectedSubForEdit = sub;
    this.editSub = {
      playerId: sub.playerId,
      academyId: sub.academyId,
      amount: sub.amount,
      duration: sub.duration,
      startDate: sub.startDate ? new Date(sub.startDate).toISOString().substring(0, 10) : new Date().toISOString().substring(0, 10)
    };
    this.isEditModalOpen = true;
    this.cdr.detectChanges();
  }

  closeEditModal(): void {
    this.isEditModalOpen = false;
    this.selectedSubForEdit = null;
    this.cdr.detectChanges();
  }

  onEditDurationSelect(val: string | number | null): void {
    this.editSub.duration = val as SubscriptionDuration;
  }

  onEditStartDateChange(val: string): void {
    this.editSub.startDate = val;
  }

  onEditAmountChange(val: number | null): void {
    this.editSub.amount = val ? Number(val) : 0;
  }

  onSaveEditedSubscription(): void {
    if (!this.editSub.amount || this.editSub.amount <= 0) {
      this.errorMessage = 'Please enter a valid amount.';
      this.cdr.detectChanges();
      return;
    }

    this.isEditingSub = true;
    this.errorMessage = '';
    this.successMessage = '';
    
this.subscriptionsList.add(
  this.subscriptionService.createSubscription(this.editSub).subscribe({
    next: () => {
      this.successMessage = `Subscription plan updated successfully for ${this.selectedSubForEdit?.playerName}!`;
      this.isEditingSub = false;
      this.closeEditModal();
      this.loadSubscriptions();
    },
    error: (err) => {
      this.errorMessage =
        err?.error?.message ||
        this.translate.instant('ACADEMY_ADMIN.MESSAGES.UPDATE_SUB_FAILED');
      this.isEditingSub = false;
      this.cdr.detectChanges();
    }
  })
);

  // CONFIRM DIALOG STATE
  isConfirmDialogOpen = false;
  confirmDialogTitle = '';
  confirmDialogMessage = '';
  targetSubForCash: PlayerSubscriptionDto | null = null;

  // MARK AS PAID BY CASH HANDLER
  onMarkAsPaidByCash(sub: PlayerSubscriptionDto): void {
    this.targetSubForCash = sub;
    this.confirmDialogTitle = this.translate.instant('ACADEMY_ADMIN.SUBSCRIPTIONS.CONFIRM_CASH_TITLE');
    this.confirmDialogMessage = this.translate.instant('ACADEMY_ADMIN.SUBSCRIPTIONS.CONFIRM_CASH_MSG', {
      amount: sub.amount,
      playerName: sub.playerName
    });
    this.isConfirmDialogOpen = true;
    this.cdr.detectChanges();
  }

  onConfirmDialogExecute(): void {
    if (!this.targetSubForCash) {
      this.isConfirmDialogOpen = false;
      this.cdr.detectChanges();
      return;
    }

    const sub = this.targetSubForCash;
    this.isConfirmDialogOpen = false;
    this.targetSubForCash = null;

    this.isProcessingCashId = sub.id;
    this.errorMessage = '';
    this.successMessage = '';
this.subscriptionsList.add(
  this.subscriptionService.markAsPaidByCash(sub.id).subscribe({
    next: () => {
      this.successMessage = `Cash payment confirmed for ${sub.playerName}! Status set to Paid.`;

      const playerMsg = `Your cash payment of ${sub.amount} EGP has been confirmed successfully.`;
      const parentMsg = `Cash payment of ${sub.amount} EGP for your child's subscription has been confirmed.`;

      this.subscriptionsList.add(
        this.notificationService.notifyPlayerMilestone(sub.playerId, playerMsg).subscribe({
          error: (e) => console.error('Failed to notify player', e)
        })
      );

      this.subscriptionsList.add(
        this.notificationService.notifyPlayerParents(sub.playerId, parentMsg).subscribe({
          error: (e) => console.error('Failed to notify parent', e)
        })
      );

      this.isProcessingCashId = null;
      this.loadSubscriptions();
    },
    error: (err) => {
      console.error('Failed to mark cash payment:', err);
      this.errorMessage =
        err?.error?.message ||
        this.translate.instant('ACADEMY_ADMIN.MESSAGES.MARK_CASH_FAILED');
      this.isProcessingCashId = null;
      this.cdr.detectChanges();
    }
  })
);
  }

  // PLAYER HISTORY MODAL HANDLERS
  mapStatusToBadge(status: number | string): string {
    if (status === 1 || status === 'Paid') return 'ACADEMY_ADMIN.SUBSCRIPTIONS.PAID';
    if (status === 2 || status === 'Unpaid') return 'ACADEMY_ADMIN.SUBSCRIPTIONS.UNPAID';
    if (status === 3 || status === 'Grace') return 'ACADEMY_ADMIN.SUBSCRIPTIONS.GRACE';
    return 'ACADEMY_ADMIN.SUBSCRIPTIONS.STATUS_UNKNOWN';
  }

  openHistoryModal(playerId: number, playerName: string): void {
    this.selectedHistoryPlayer = { id: playerId, name: playerName };
    this.isLoadingHistory = true;
    this.historySubscriptions = [];
    this.cdr.detectChanges();

    this.subscriptionsList.add(
      this.subscriptionService.getPlayerSubscriptionHistory(playerId).subscribe({
        next: (data) => {
          this.historySubscriptions = data || [];
          this.isLoadingHistory = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Failed to load player history:', err);
          this.errorMessage = 'Failed to load player subscription history.';
          this.isLoadingHistory = false;
          this.cdr.detectChanges();
        }
      })
    );
  

  closeHistoryModal(): void {
    this.selectedHistoryPlayer = null;
    this.historySubscriptions = [];
    this.cdr.detectChanges();
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