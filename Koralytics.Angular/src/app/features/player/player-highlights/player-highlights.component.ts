import { Component, OnInit, inject, signal, computed, ChangeDetectorRef, DestroyRef, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

// Shared Components & Directives
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { CustomInputComponent } from '../../../../shared/components/custom-input-component/custom-input-component';
import { FileUpload } from '../../../../shared/components/file-upload/file-upload';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';

// Services & Models
import { PlayerHighlightService } from '../../../../core/services/player/player-highlight.service';
import { PlayerProfileService } from '../../../../core/services/player/player-profile.service';
import { AuthService } from '../../../../core/services/auth/auth.service';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { ModalService } from '../../../../core/services/Modal/modal';
import { ToastService } from '../../../../core/services/Toast/toast';
import { NotificationService } from '@core/services/SignalR/notificationservice';
import { PlayerHighlightDto } from '../../../../core/interfaces/highlight.interfaces';
import { PlayerProfileModel } from '../../../../core/models/Player/player-profile-model';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-player-highlights',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    LoadingSpinnerComponent,
    ScrollRevealDirective,
    CustomButtonComponent,
    CustomInputComponent,
    FileUpload,
    Pagination,
    EmptyStateComponent,
    TranslatePipe
  ],
  templateUrl: './player-highlights.component.html',
  styleUrls: ['./player-highlights.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PlayerHighlightsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private highlightService = inject(PlayerHighlightService);
  private profileService = inject(PlayerProfileService);
  private authService = inject(AuthService);
  private tokenStorage = inject(TokenStorageService);
  private modalService = inject(ModalService);
  private toastService = inject(ToastService);
  private translateService = inject(TranslateService);
  private destroyRef = inject(DestroyRef);
  private notificationService = inject(NotificationService);
  private cdr = inject(ChangeDetectorRef);

  // Identity & Auth State
  playerId = 0;
  loggedInUserId: number | null = null;
  isOwnProfile = false;
  profile: PlayerProfileModel | null = null;

  // Highlights Data State
  highlights = signal<PlayerHighlightDto[]>([]);
  loading = signal(false);
  error = signal('');

  // Pagination State
  currentPageSignal = signal(1);
  pageSize = 6;

  // Computed Signal Values
  totalItems = computed(() => this.highlights().length);
  pinnedCount = computed(() => this.highlights().filter(h => h.isPinned).length);
  paginatedHighlights = computed(() => {
    const startIndex = (this.currentPageSignal() - 1) * this.pageSize;
    return this.highlights().slice(startIndex, startIndex + this.pageSize);
  });

  get currentPage(): number {
    return this.currentPageSignal();
  }

  set currentPage(page: number) {
    this.currentPageSignal.set(page);
  }

  // Upload State
  selectedFile: File | null = null;
  highlightTitle = '';
  uploading = signal(false);
  uploadError = signal('');
  imageError = false;

  // Theater View State
  activeTheaterHighlight = signal<PlayerHighlightDto | null>(null);

  openTheater(h: PlayerHighlightDto): void {
    this.activeTheaterHighlight.set(h);
    this.cdr.markForCheck();
  }

  closeTheater(): void {
    this.activeTheaterHighlight.set(null);
    this.cdr.markForCheck();
  }

  // Memoized Header Fields
  fullName = this.translateService.instant('PLAYER.HIGHLIGHT_PAGE.TITLE');
  initials = 'PH';
  academyName = 'Koralytics';
  statusLabel = this.translateService.instant('PLAYER.STATUS_AVAILABLE');
  statusClass = 'status-available';
  profileImageUrl: string | null = null;

  ngOnInit(): void {
    const user = this.tokenStorage.getUser() || this.authService.getCurrentUserSync();
    if (user) {
      this.loggedInUserId = user.userId;
    }

    const paramId = this.route.snapshot.paramMap.get('playerId');
    if (paramId) {
      this.playerId = Number(paramId);
    } else if (this.loggedInUserId) {
      this.playerId = this.loggedInUserId;
    }

    this.isOwnProfile = (this.loggedInUserId !== null && this.loggedInUserId === this.playerId);

    if (this.playerId) {
      this.loadPlayerProfile(this.playerId);
      this.loadHighlights();
    } else {
      this.error.set(this.translateService.instant('PLAYER.HIGHLIGHT_PAGE.ERROR_AUTH'));
      this.cdr.markForCheck();
    }
  }

  private loadPlayerProfile(id: number): void {
    this.profileService.getPlayerProfile(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (prof) => {
          this.profile = prof;
          this.computeProfileDerivedState();
          this.cdr.markForCheck();
        },
        error: (err: any) => {
          console.warn('Could not load full profile header metadata:', err);
        }
      });
  }

  private computeProfileDerivedState() {
    if (!this.profile) {
      this.fullName = this.translateService.instant('PLAYER.HIGHLIGHT_PAGE.TITLE');
      this.initials = 'PH';
      this.academyName = 'Koralytics';
      this.statusLabel = 'Available'; // we still use this string for status class logic, but template uses translate pipe
      this.statusClass = 'status-available';
      this.profileImageUrl = null;
      return;
    }

    this.fullName = `${this.profile.firstName} ${this.profile.lastName}`;
    const f = this.profile.firstName?.charAt(0) || '';
    const l = this.profile.lastName?.charAt(0) || '';
    this.initials = `${f}${l}`.toUpperCase() || 'PH';
    this.academyName = this.profile.currentAcademy?.academyName ?? 'Koralytics';
    this.profileImageUrl = this.profile.profileImageUrl || this.profile.playerCard?.profileImageUrl || null;

    const status = this.profile.availabilityStatus;
    if (status === undefined || status === null) {
      this.statusLabel = 'Available';
    } else if (typeof status === 'number') {
      switch (status) {
        case 1: this.statusLabel = 'Available'; break;
        case 2: this.statusLabel = 'Injured'; break;
        case 3: this.statusLabel = 'Resting'; break;
        case 4: this.statusLabel = 'Suspended'; break;
        default: this.statusLabel = 'Available'; break;
      }
    } else {
      this.statusLabel = String(status);
    }

    const lowerLabel = this.statusLabel.toLowerCase();
    if (lowerLabel === 'injured') this.statusClass = 'status-injured';
    else if (lowerLabel === 'resting') this.statusClass = 'status-resting';
    else if (lowerLabel === 'suspended') this.statusClass = 'status-suspended';
    else this.statusClass = 'status-available';
  }

  loadHighlights(): void {
    if (!this.playerId) return;

    this.loading.set(true);
    this.error.set('');
    this.cdr.markForCheck();

    this.highlightService.getHighlights(this.playerId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.highlights.set(data);
          this.loading.set(false);
          this.cdr.markForCheck();
        },
        error: (err: any) => {
          this.error.set(err?.error?.message || this.translateService.instant('PLAYER.MESSAGES.HIGHLIGHTS_LOAD_FAILED'));
          this.loading.set(false);
          this.cdr.markForCheck();
        }
      });
  }

  // ── Pagination Handlers ──
  onPageChange(page: number): void {
    this.currentPageSignal.set(page);
    this.cdr.markForCheck();
    window.scrollTo({ top: 300, behavior: 'smooth' });
  }

  // ── Upload Handlers ──
  onFileSelected(file: File): void {
    this.selectedFile = file;
    this.uploadError.set('');
    this.cdr.markForCheck();
  }

  uploadHighlight(): void {
    if (!this.selectedFile || !this.playerId) return;

    this.uploading.set(true);
    this.uploadError.set('');
    this.cdr.markForCheck();
    const targetPlayerId = this.playerId;

    this.highlightService.uploadHighlight(this.playerId, this.selectedFile, this.highlightTitle)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toastService.show(this.translateService.instant('PLAYER.MESSAGES.HIGHLIGHT_UPLOAD_SUCCESS'), 'success');
          this.selectedFile = null;
          this.highlightTitle = '';
          this.uploading.set(false);
          this.currentPageSignal.set(1);
          this.loadHighlights();

          if (targetPlayerId) {
            const scouterMessage = `Player #${targetPlayerId} has posted a new highlight video.`;
            this.notificationService.notifyScouterFollowers(targetPlayerId, scouterMessage)
              .pipe(takeUntilDestroyed(this.destroyRef))
              .subscribe({
                error: (e) => console.error('Failed to notify scouter followers about new highlight', e)
              });
          }
        },
        error: (err: any) => {
          this.uploadError.set(err?.error?.message || this.translateService.instant('PLAYER.MESSAGES.HIGHLIGHT_UPLOAD_FAILED'));
          this.uploading.set(false);
          this.toastService.show(this.translateService.instant('PLAYER.MESSAGES.HIGHLIGHT_UPLOAD_FAILED'), 'error');
          this.cdr.markForCheck();
        }
      });
  }

  // ── Action Handlers ──
  async deleteHighlight(highlightId: number): Promise<void> {
    if (!this.playerId) return;

    const confirmed = await this.modalService.open({
      title: this.translateService.instant('PLAYER.HIGHLIGHT_PAGE.MODAL_DELETE_TITLE'),
      message: this.translateService.instant('PLAYER.HIGHLIGHT_PAGE.MODAL_DELETE_MSG'),
      confirmText: this.translateService.instant('PLAYER.HIGHLIGHT_PAGE.MODAL_DELETE_CONFIRM'),
      cancelText: this.translateService.instant('PLAYER.HIGHLIGHT_PAGE.MODAL_DELETE_CANCEL'),
      variant: 'danger'
    });

    if (!confirmed) return;

    this.highlightService.deleteHighlight(this.playerId, highlightId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.highlights.update(list => list.filter(h => h.id !== highlightId));
          this.toastService.show(this.translateService.instant('PLAYER.MESSAGES.HIGHLIGHT_DELETE_SUCCESS'), 'success');

          if (this.paginatedHighlights().length === 0 && this.currentPageSignal() > 1) {
            this.currentPageSignal.update(p => p - 1);
          }
          this.cdr.markForCheck();
        },
        error: (err: any) => {
          this.toastService.show(err?.error?.message || this.translateService.instant('PLAYER.MESSAGES.HIGHLIGHT_DELETE_FAILED'), 'error');
          this.cdr.markForCheck();
        }
      });
  }

  pinHighlight(highlightId: number): void {
    if (!this.playerId) return;

    this.highlightService.pinHighlight(this.playerId, highlightId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toastService.show(this.translateService.instant('PLAYER.MESSAGES.HIGHLIGHT_PIN_SUCCESS'), 'success');
          this.loadHighlights();
        },
        error: (err: any) => {
          this.toastService.show(err?.error?.message || this.translateService.instant('PLAYER.MESSAGES.HIGHLIGHT_PIN_FAILED'), 'error');
          this.cdr.markForCheck();
        }
      });
  }

  togglePin(highlight: PlayerHighlightDto): void {
    if (highlight.isPinned) {
      this.unpinHighlight(highlight.id);
    } else {
      this.pinHighlight(highlight.id);
    }
  }

  unpinHighlight(highlightId: number): void {
    if (!this.playerId) return;

    this.highlightService.unpinHighlight(this.playerId, highlightId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toastService.show(this.translateService.instant('PLAYER.MESSAGES.HIGHLIGHT_UNPIN_SUCCESS'), 'success');
          this.loadHighlights();
        },
        error: () => {
          this.highlightService.pinHighlight(this.playerId, highlightId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: () => {
                this.toastService.show(this.translateService.instant('PLAYER.MESSAGES.HIGHLIGHT_UNPIN_SUCCESS'), 'success');
                this.loadHighlights();
              },
              error: (err: any) => {
                this.toastService.show(err?.error?.message || this.translateService.instant('PLAYER.MESSAGES.HIGHLIGHT_UNPIN_FAILED'), 'error');
                this.cdr.markForCheck();
              }
            });
        }
      });
  }

  copyVideoLink(videoUrl: string): void {
    if (!videoUrl) return;
    if (navigator.clipboard && window.isSecureContext) {
      navigator.clipboard.writeText(videoUrl).then(() => {
        this.toastService.show(this.translateService.instant('PLAYER.MESSAGES.HIGHLIGHT_LINK_COPIED'), 'info');
      }).catch(() => {
        this.fallbackCopyTextToClipboard(videoUrl);
      });
    } else {
      this.fallbackCopyTextToClipboard(videoUrl);
    }
  }

  private fallbackCopyTextToClipboard(text: string): void {
    const textArea = document.createElement('textarea');
    textArea.value = text;
    textArea.style.position = 'fixed';
    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();
    try {
      document.execCommand('copy');
      this.toastService.show(this.translateService.instant('PLAYER.MESSAGES.HIGHLIGHT_LINK_COPIED'), 'info');
    } catch (err) {
      this.toastService.show(this.translateService.instant('PLAYER.MESSAGES.HIGHLIGHT_LINK_COPY_FAILED'), 'error');
    }
    document.body.removeChild(textArea);
  }
}
