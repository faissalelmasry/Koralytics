import { Component, OnInit, inject, signal, ChangeDetectorRef, DestroyRef } from '@angular/core';
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
    EmptyStateComponent
  ],
  templateUrl: './player-highlights.component.html',
  styleUrls: ['./player-highlights.component.css']
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
  currentPage = 1;
  pageSize = 6;

  // Upload State
  selectedFile: File | null = null;
  highlightTitle = '';
  uploading = signal(false);
  uploadError = signal('');

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
      this.error.set('Authentication or Player ID required.');
    }
  }

  private loadPlayerProfile(id: number): void {
    this.profileService.getPlayerProfile(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (prof) => {
          this.profile = prof;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.warn('Could not load full profile header metadata:', err);
        }
      });
  }

  loadHighlights(): void {
    if (!this.playerId) return;

    this.loading.set(true);
    this.error.set('');

    this.highlightService.getHighlights(this.playerId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.highlights.set(data);
          this.loading.set(false);
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.error.set(err?.error?.message || 'Failed to load highlights.');
          this.loading.set(false);
          this.cdr.detectChanges();
        }
      });
  }

  // ── Pagination Getters & Handlers ──
  get totalItems(): number {
    return this.highlights().length;
  }

  get paginatedHighlights(): PlayerHighlightDto[] {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    return this.highlights().slice(startIndex, startIndex + this.pageSize);
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    window.scrollTo({ top: 300, behavior: 'smooth' });
  }

  // ── Upload Handlers ──
  onFileSelected(file: File): void {
    this.selectedFile = file;
    this.uploadError.set('');
  }

  uploadHighlight(): void {
    if (!this.selectedFile || !this.playerId) return;

    this.uploading.set(true);
    this.uploadError.set('');
    const targetPlayerId = this.playerId;

    this.highlightService.uploadHighlight(this.playerId, this.selectedFile, this.highlightTitle)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toastService.show('Highlight uploaded successfully!', 'success');
          this.selectedFile = null;
          this.highlightTitle = '';
          this.uploading.set(false);
          this.currentPage = 1;
          this.loadHighlights();

          if (targetPlayerId) {
            const scouterMessage = `Player #${targetPlayerId} has posted a new highlight video.`;
            this.notificationService.notifyScouterFollowers(targetPlayerId, scouterMessage)
              .subscribe({
                error: (e) => console.error('Failed to notify scouter followers about new highlight', e)
              });
          }
        },
        error: (err) => {
          this.uploadError.set(err?.error?.message || 'Failed to upload highlight.');
          this.uploading.set(false);
          this.toastService.show('Failed to upload highlight.', 'error');
        }
      });
  }

  // ── Action Handlers ──
  async deleteHighlight(highlightId: number): Promise<void> {
    if (!this.playerId) return;

    const confirmed = await this.modalService.open({
      title: 'Delete Highlight',
      message: 'Are you sure you want to remove this highlight video from your profile?',
      confirmText: 'Delete',
      cancelText: 'Cancel',
      variant: 'danger'
    });

    if (!confirmed) return;

    this.highlightService.deleteHighlight(this.playerId, highlightId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.highlights.update(list => list.filter(h => h.id !== highlightId));
          this.toastService.show('Highlight deleted successfully.', 'success');

          // Adjust page if current page becomes empty
          if (this.paginatedHighlights.length === 0 && this.currentPage > 1) {
            this.currentPage--;
          }
        },
        error: (err) => {
          this.toastService.show(err?.error?.message || 'Failed to delete highlight.', 'error');
        }
      });
  }

  pinHighlight(highlightId: number): void {
    if (!this.playerId) return;

    this.highlightService.pinHighlight(this.playerId, highlightId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toastService.show('Highlight pinned to top.', 'success');
          this.loadHighlights();
        },
        error: (err) => {
          this.toastService.show(err?.error?.message || 'Failed to pin highlight.', 'error');
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
          this.toastService.show('Highlight unpinned.', 'success');
          this.loadHighlights();
        },
        error: () => {
          this.highlightService.pinHighlight(this.playerId, highlightId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: () => {
                this.toastService.show('Highlight unpinned.', 'success');
                this.loadHighlights();
              },
              error: (err) => {
                this.toastService.show(err?.error?.message || 'Failed to unpin highlight.', 'error');
              }
            });
        }
      });
  }

  copyVideoLink(videoUrl: string): void {
    if (!videoUrl) return;
    if (navigator.clipboard && window.isSecureContext) {
      navigator.clipboard.writeText(videoUrl).then(() => {
        this.toastService.show('Highlight video link copied to clipboard!', 'info');
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
      this.toastService.show('Highlight video link copied to clipboard!', 'info');
    } catch (err) {
      this.toastService.show('Failed to copy video link.', 'error');
    }
    document.body.removeChild(textArea);
  }

  imageError = false;

  get profileImageUrl(): string | null {
    return this.profile?.profileImageUrl || this.profile?.playerCard?.profileImageUrl || null;
  }

  // ── Computed Display Getters ──
  get fullName(): string {
    if (!this.profile) return 'Player Highlights';
    return `${this.profile.firstName} ${this.profile.lastName}`;
  }

  get initials(): string {
    if (!this.profile) return 'PH';
    const f = this.profile.firstName?.charAt(0) || '';
    const l = this.profile.lastName?.charAt(0) || '';
    return `${f}${l}`.toUpperCase() || 'PH';
  }

  get academyName(): string {
    return this.profile?.currentAcademy?.academyName ?? 'Koralytics';
  }

  get statusLabel(): string {
    const status = this.profile?.availabilityStatus;
    if (status === undefined || status === null) return 'Available';
    if (typeof status === 'number') {
      switch (status) {
        case 1: return 'Available';
        case 2: return 'Injured';
        case 3: return 'Resting';
        case 4: return 'Suspended';
        default: return 'Available';
      }
    }
    return String(status);
  }

  get statusClass(): string {
    const label = this.statusLabel.toLowerCase();
    if (label === 'injured') return 'status-injured';
    if (label === 'resting') return 'status-resting';
    if (label === 'suspended') return 'status-suspended';
    return 'status-available';
  }

  get pinnedCount(): number {
    return this.highlights().filter(h => h.isPinned).length;
  }
}
