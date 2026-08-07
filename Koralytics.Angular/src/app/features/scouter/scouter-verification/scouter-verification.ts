import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';

import { ScouterProfileDto } from '../../../../core/interfaces/Scouter.interfaces';
import { extractErrorMessage } from '../../../../core/utils/http-error.util';

import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { SearchBarComponent } from '../../../../shared/components/search-bar/search-bar';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { ScouterService } from '@core/services/Scouter/scouter.service';
import { Footer } from '@shared/components/footer/footer';
import { NavbarComponent } from '@shared/components/navbar/navbar';
import { ScrollRevealDirective } from '@shared/directives/scroll-reveal.directive';

type ScouterViewMode = 'pending' | 'verified';

@Component({
  selector: 'app-scouter-verification',
  standalone: true,
  imports: [
    CommonModule,
    CustomButtonComponent,
    SearchBarComponent,
    StatusChipComponent,
    EmptyStateComponent,
    LoadingSpinnerComponent,
    Pagination,
    ConfirmDialogComponent,
    Footer,
    NavbarComponent,
    ScrollRevealDirective,
  ],
  templateUrl: './scouter-verification.html',
  styleUrls: ['./scouter-verification.css'],
})
export class ScouterVerificationComponent implements OnInit {
  private readonly scouterService = inject(ScouterService);

  viewMode = signal<ScouterViewMode>('pending');

  isLoading = signal<boolean>(false);
  errorMessage = signal<string>('');
  successMessage = signal<string>('');

  // Each list is loaded lazily the first time its tab is opened, then cached
  // (null = "not fetched yet" vs [] = "fetched, genuinely empty").
  private pendingScouters = signal<ScouterProfileDto[] | null>(null);
  private verifiedScouters = signal<ScouterProfileDto[] | null>(null);

  searchTerm = signal<string>('');
  currentPage = signal<number>(1);
  pageSize = 10;

  isVerifyingId = signal<number | null>(null);
  private confirmTarget = signal<ScouterProfileDto | null>(null);

  get isConfirmDialogOpen(): boolean {
    return this.confirmTarget() !== null;
  }

  get confirmDialogMessage(): string {
    const target = this.confirmTarget();
    return target
      ? `Are you sure you want to verify ${target.firstName} ${target.lastName}? They will gain full verified-scouter access.`
      : '';
  }

  readonly currentList = computed<ScouterProfileDto[]>(() => {
    const list = this.viewMode() === 'pending' ? this.pendingScouters() : this.verifiedScouters();
    return list ?? [];
  });

  readonly filteredList = computed<ScouterProfileDto[]>(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const list = this.currentList();
    if (!term) return list;
    return list.filter((s) => s.firstName?.toLowerCase().includes(term) || s.lastName?.toLowerCase().includes(term));
  });

  readonly paginatedList = computed<ScouterProfileDto[]>(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.filteredList().slice(start, start + this.pageSize);
  });

  readonly pendingCount = computed<number>(() => this.pendingScouters()?.length ?? 0);
  readonly verifiedCount = computed<number>(() => this.verifiedScouters()?.length ?? 0);

  ngOnInit(): void {
    this.loadPending();
  }

  switchView(mode: ScouterViewMode): void {
    if (this.viewMode() === mode) return;

    this.viewMode.set(mode);
    this.searchTerm.set('');
    this.currentPage.set(1);
    this.errorMessage.set('');

    if (mode === 'pending' && this.pendingScouters() === null) {
      this.loadPending();
    } else if (mode === 'verified' && this.verifiedScouters() === null) {
      this.loadVerified();
    }
  }

  loadPending(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.scouterService.getPendingVerificationScouters().subscribe({
      next: (data) => {
        this.pendingScouters.set(data ?? []);
        this.isLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(extractErrorMessage(err, 'Failed to load scouters pending verification.'));
        this.isLoading.set(false);
      },
    });
  }

  loadVerified(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    // There's no dedicated "verified only" endpoint -- getAllScouters()
    // returns every scouter in the system, so filter to isVerified client-side.
    this.scouterService.getAllScouters().subscribe({
      next: (data) => {
        this.verifiedScouters.set((data ?? []).filter((s) => s.isVerified));
        this.isLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(extractErrorMessage(err, 'Failed to load verified scouters.'));
        this.isLoading.set(false);
      },
    });
  }

  onSearch(term: string): void {
    this.searchTerm.set(term);
    this.currentPage.set(1);
  }

  onPageChange(page: number): void {
    this.currentPage.set(page);
  }

  openVerifyConfirm(scouter: ScouterProfileDto): void {
    this.confirmTarget.set(scouter);
  }

  closeVerifyConfirm(): void {
    this.confirmTarget.set(null);
  }

  confirmVerify(): void {
    const target = this.confirmTarget();
    if (!target) return;

    this.isVerifyingId.set(target.id);
    this.errorMessage.set('');

    this.scouterService.verifyScouter(target.id).subscribe({
      next: () => {
        this.successMessage.set(`${target.firstName} ${target.lastName} has been verified.`);

        // Move the scouter from pending -> verified locally instead of
        // refetching both endpoints.
        this.pendingScouters.update((list) => (list ?? []).filter((s) => s.id !== target.id));
        this.verifiedScouters.update((list) => {
          const verifiedEntry: ScouterProfileDto = {
            ...target,
            isVerified: true,
            verifiedAt: new Date().toISOString(),
          };
          return list === null ? null : [verifiedEntry, ...list];
        });

        this.isVerifyingId.set(null);
        this.confirmTarget.set(null);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(extractErrorMessage(err, 'Failed to verify scouter. Please try again.'));
        this.isVerifyingId.set(null);
        this.confirmTarget.set(null);
      },
    });
  }

  getInitials(fullName: string): string {
    if (!fullName) return 'SC';
    return fullName
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((p) => p[0]?.toUpperCase())
      .join('');
  }
}