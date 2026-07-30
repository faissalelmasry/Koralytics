import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SubscriptionService } from '@core/services/subscription/subscription.service';
import { AcademyService } from '@core/services/academy/academy.service';
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

export interface PlayerOption {
  id: number;
  name: string;
}

@Component({
  selector: 'app-academy-admin-subscriptions',
  standalone: true,
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
    ConfirmDialogComponent
  ],
  templateUrl: './academy-admin-subscriptions.component.html',
  styleUrls: ['./academy-admin-subscriptions.component.css']
})
export class AcademyAdminSubscriptionsComponent implements OnInit {
  subscriptions: PlayerSubscriptionDto[] = [];
  filteredSubscriptions: PlayerSubscriptionDto[] = [];
  isLoading = true;
  isSubmitting = false;
  isProcessingCashId: number | null = null;

  // Feedback Banners
  errorMessage = '';
  successMessage = '';

  // Filters & Search
  searchQuery = '';
  selectedStatusFilter = 'ALL';

  statusFilterOptions: SelectOption[] = [
    { value: 'ALL', label: 'All Statuses' },
    { value: 'PAID', label: 'Paid' },
    { value: 'UNPAID', label: 'Unpaid' },
    { value: 'GRACE', label: 'Grace' }
  ];

  durationOptions: SelectOption[] = [
    { value: SubscriptionDuration.OneMonth, label: '1 Month (Monthly)' },
    { value: SubscriptionDuration.ThreeMonths, label: '3 Months (Quarterly)' },
    { value: SubscriptionDuration.SixMonths, label: '6 Months (Semi-Annual)' },
    { value: SubscriptionDuration.OneYear, label: '1 Year (Annual)' }
  ];

  // Modal State & New Subscription DTO
  isCreateModalOpen = false;
  newSub: CreateSubscriptionDto = {
    playerId: 0,
    academyId: 0,
    amount: 1500,
    duration: SubscriptionDuration.OneMonth,
    startDate: new Date().toISOString().substring(0, 10)
  };

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

  // PLAYER HISTORY MODAL STATE
  selectedHistoryPlayer: { id: number; name: string } | null = null;
  historySubscriptions: PlayerSubscriptionDto[] = [];
  isLoadingHistory = false;

  // Real Players List fetched directly via AcademyService
  availablePlayers: PlayerOption[] = [];
  isLoadingPlayers = false;

  readonly Status = SubscriptionStatus;
  readonly Duration = SubscriptionDuration;

  constructor(
    private subscriptionService: SubscriptionService,
    private academyService: AcademyService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    const academyId = this.activeAcademyId;
    this.newSub.academyId = academyId;
    this.loadSubscriptions();
    this.loadAcademyPlayers();
  }

  get activeAcademyId(): number {
    const user = this.authService.getCurrentUserValue();
    return user?.academyId || 2;
  }

  get playerOptions(): SelectOption[] {
    return this.availablePlayers.map(p => ({
      value: p.id,
      label: `${p.name} (ID: ${p.id})`
    }));
  }

  get totalRevenue(): number {
    return this.subscriptions
      .filter(s => this.isPaid(s.status))
      .reduce((sum, s) => sum + s.amount, 0);
  }

  get pendingDues(): number {
    return this.subscriptions
      .filter(s => !this.isPaid(s.status))
      .reduce((sum, s) => sum + s.amount, 0);
  }

  get activeSubscriptionsCount(): number {
    return this.subscriptions.filter(s => this.isPaid(s.status)).length;
  }

  loadSubscriptions(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const academyId = this.activeAcademyId;

    this.subscriptionService.getAcademySubscriptions(academyId).subscribe({
      next: (data: PlayerSubscriptionDto[]) => {
        this.subscriptions = data || [];
        this.applyFilters();
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load academy subscriptions:', err);
        this.errorMessage = err?.error?.message || 'Failed to load academy subscriptions.';
        this.isLoading = false;
      }
    });
  }

  loadAcademyPlayers(): void {
    this.isLoadingPlayers = true;
    const academyId = this.activeAcademyId;

    this.academyService.getAcademyMembers(academyId, { pageNumber: 1, pageSize: 100 }).subscribe({
      next: (res: any) => {
        const rawItems = res?.data?.items || res?.data || res || [];
        const memberList = Array.isArray(rawItems) ? rawItems : [];

        const mappedPlayers: PlayerOption[] = memberList
          .filter((m: any) => !m.role || String(m.role).toLowerCase() === 'player')
          .map((p: any) => ({
            id: p.userId ?? p.id ?? p.playerId ?? 0,
            name: p.fullName || p.name || `${p.firstName || ''} ${p.lastName || ''}`.trim() || `Player #${p.userId || p.id}`
          }));

        if (mappedPlayers.length > 0) {
          this.availablePlayers = mappedPlayers;
        } else if (memberList.length > 0) {
          this.availablePlayers = memberList.map((m: any) => ({
            id: m.userId ?? m.id ?? m.playerId ?? 0,
            name: m.fullName || m.name || `Member #${m.userId || m.id}`
          }));
        }

        if (this.availablePlayers.length > 0 && (!this.newSub.playerId || this.newSub.playerId === 0)) {
          this.newSub.playerId = this.availablePlayers[0].id;
        }
        this.isLoadingPlayers = false;
      },
      error: (err) => {
        console.error('Failed to load academy members, attempting searchPlayers:', err);
        this.academyService.searchPlayers(academyId).subscribe({
          next: (searchRes: any) => {
            const list = searchRes?.data || searchRes || [];
            if (Array.isArray(list) && list.length > 0) {
              this.availablePlayers = list.map((p: any) => ({
                id: p.id ?? p.userId ?? p.playerId ?? 0,
                name: p.fullName || p.name || `${p.firstName || ''} ${p.lastName || ''}`.trim() || `Player #${p.id}`
              }));
              if (this.availablePlayers.length > 0) {
                this.newSub.playerId = this.availablePlayers[0].id;
              }
            }
            this.isLoadingPlayers = false;
          },
          error: () => {
            this.isLoadingPlayers = false;
          }
        });
      }
    });
  }

  applyFilters(): void {
    let result = [...this.subscriptions];

    if (this.selectedStatusFilter !== 'ALL') {
      result = result.filter(s => String(s.status).toUpperCase() === this.selectedStatusFilter);
    }

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(s => s.playerName.toLowerCase().includes(q));
    }

    this.filteredSubscriptions = result;
  }

  onSearchChange(text: string): void {
    this.searchQuery = text;
    this.applyFilters();
  }

  onStatusFilterChange(value: any): void {
    this.selectedStatusFilter = value || 'ALL';
    this.applyFilters();
  }

  onPlayerSelect(val: any): void {
    this.newSub.playerId = Number(val);
  }

  onDurationSelect(val: any): void {
    this.newSub.duration = val;
  }

  onEditDurationSelect(val: any): void {
    this.editSub.duration = val;
  }

  onStartDateChange(val: string): void {
    this.newSub.startDate = val;
  }

  onEditStartDateChange(val: string): void {
    this.editSub.startDate = val;
  }

  onAmountChange(val: number | null): void {
    this.newSub.amount = val ? Number(val) : 0;
  }

  onEditAmountChange(val: number | null): void {
    this.editSub.amount = val ? Number(val) : 0;
  }

  // CREATE MODAL HANDLERS
  openCreateModal(): void {
    this.isCreateModalOpen = true;
    if (this.availablePlayers.length === 0) {
      this.loadAcademyPlayers();
    }
    this.newSub = {
      playerId: this.availablePlayers[0]?.id || 0,
      academyId: this.activeAcademyId,
      amount: 1500,
      duration: SubscriptionDuration.OneMonth,
      startDate: new Date().toISOString().substring(0, 10)
    };
  }

  closeCreateModal(): void {
    this.isCreateModalOpen = false;
  }

  onCreateSubscription(): void {
    this.newSub.academyId = this.activeAcademyId;
    if (!this.newSub.playerId || this.newSub.amount <= 0) {
      this.errorMessage = 'Please select a valid player and enter a valid subscription amount.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.subscriptionService.createSubscription(this.newSub).subscribe({
      next: () => {
        const selectedPlayerName = this.availablePlayers.find(p => p.id === this.newSub.playerId)?.name || `ID #${this.newSub.playerId}`;
        this.successMessage = `New subscription issued successfully for ${selectedPlayerName}!`;
        this.isSubmitting = false;
        this.closeCreateModal();
        this.loadSubscriptions();
      },
      error: (err) => {
        this.errorMessage = err?.error?.message || 'Failed to issue subscription. Please try again.';
        this.isSubmitting = false;
      }
    });
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
  }

  closeEditModal(): void {
    this.isEditModalOpen = false;
    this.selectedSubForEdit = null;
  }

  onSaveEditedSubscription(): void {
    if (!this.editSub.amount || this.editSub.amount <= 0) {
      this.errorMessage = 'Please enter a valid amount.';
      return;
    }

    this.isEditingSub = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.subscriptionService.createSubscription(this.editSub).subscribe({
      next: () => {
        this.successMessage = `Subscription plan updated successfully for ${this.selectedSubForEdit?.playerName}!`;
        this.isEditingSub = false;
        this.closeEditModal();
        this.loadSubscriptions();
      },
      error: (err) => {
        this.errorMessage = err?.error?.message || 'Failed to update subscription. Please try again.';
        this.isEditingSub = false;
      }
    });
  }

  // CONFIRM DIALOG STATE
  isConfirmDialogOpen = false;
  confirmDialogTitle = '';
  confirmDialogMessage = '';
  targetSubForCash: PlayerSubscriptionDto | null = null;

  // MARK AS PAID BY CASH HANDLER
  onMarkAsPaidByCash(sub: PlayerSubscriptionDto): void {
    this.targetSubForCash = sub;
    this.confirmDialogTitle = 'Confirm Cash Payment';
    this.confirmDialogMessage = `Confirm cash payment of ${sub.amount} EGP for ${sub.playerName}?`;
    this.isConfirmDialogOpen = true;
  }

  onConfirmDialogExecute(): void {
    if (!this.targetSubForCash) {
      this.isConfirmDialogOpen = false;
      return;
    }

    const sub = this.targetSubForCash;
    this.isConfirmDialogOpen = false;
    this.targetSubForCash = null;

    this.isProcessingCashId = sub.id;
    this.errorMessage = '';
    this.successMessage = '';

    this.subscriptionService.markAsPaidByCash(sub.id).subscribe({
      next: () => {
        this.successMessage = `Cash payment confirmed for ${sub.playerName}! Status set to Paid.`;
        this.isProcessingCashId = null;
        this.loadSubscriptions();
      },
      error: (err) => {
        console.error('Failed to mark cash payment:', err);
        this.errorMessage = err?.error?.message || 'Failed to mark payment as cash. Please try again.';
        this.isProcessingCashId = null;
      }
    });
  }

  // PLAYER HISTORY MODAL HANDLERS
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
        console.error('Failed to load player history:', err);
        this.errorMessage = 'Failed to load player subscription history.';
        this.isLoadingHistory = false;
      }
    });
  }

  closeHistoryModal(): void {
    this.selectedHistoryPlayer = null;
    this.historySubscriptions = [];
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