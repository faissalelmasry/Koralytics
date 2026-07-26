import { Component, OnInit, inject, signal, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PlayerHighlightService } from '../../../../../core/services/player/player-highlight.service';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { ModalService } from '../../../../../core/services/Modal/modal';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { PlayerHighlightDto } from '../../../../../core/interfaces/highlight.interfaces';

@Component({
  selector: 'app-player-highlights',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './player-highlights.component.html',
  styleUrls: ['./player-highlights.component.css']
})
export class PlayerHighlightsComponent implements OnInit {
  private highlightService = inject(PlayerHighlightService);
  private authService = inject(AuthService);
  private modalService = inject(ModalService);
  private toastService = inject(ToastService);
  private destroyRef = inject(DestroyRef);

  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  // Resolved from auth context
  playerId = 0;
  academyId = 0; // TODO: resolve from coach/player profile API or route params

  highlights = signal<PlayerHighlightDto[]>([]);
  loading = signal(false);
  error = signal('');
  
  // Upload State
  selectedFile: File | null = null;
  highlightTitle = '';
  uploading = signal(false);
  uploadError = signal('');

  ngOnInit(): void {
    const user = this.authService.getCurrentUserSync();
    if (user) {
      this.playerId = user.userId;
    }
    this.loadHighlights();
  }

  loadHighlights(): void {
    this.loading.set(true);
    this.error.set('');
    
    this.highlightService.getHighlights(this.playerId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.highlights.set(data);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err?.error?.message || 'Failed to load highlights.');
          this.loading.set(false);
        }
      });
  }

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      if (file.size > 50 * 1024 * 1024) {
        this.uploadError.set('File is too large. Max 50MB allowed.');
        this.selectedFile = null;
        return;
      }
      this.selectedFile = file;
      this.uploadError.set('');
    }
  }

  uploadHighlight(): void {
    if (!this.selectedFile) return;

    this.uploading.set(true);
    this.uploadError.set('');

    this.highlightService.uploadHighlight(this.playerId, this.academyId, this.selectedFile, this.highlightTitle)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (newHighlight) => {
          this.loadHighlights();
          this.selectedFile = null;
          this.highlightTitle = '';
          this.uploading.set(false);
          // Reset file input via ViewChild
          if (this.fileInput) {
            this.fileInput.nativeElement.value = '';
          }
        },
        error: (err) => {
          this.uploadError.set(err?.error?.message || 'Failed to upload highlight.');
          this.uploading.set(false);
        }
      });
  }

  async deleteHighlight(highlightId: number): Promise<void> {
    const confirmed = await this.modalService.open({
      title: 'Delete Highlight',
      message: 'Are you sure you want to delete this highlight?',
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
          this.toastService.show('Highlight deleted.', 'success');
        },
        error: (err) => {
          this.toastService.show(err?.error?.message || 'Failed to delete highlight.', 'error');
        }
      });
  }

  pinHighlight(highlightId: number): void {
    this.highlightService.pinHighlight(this.playerId, highlightId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          // Backend handles unpinning the previous one, so let's just reload to get the new state
          this.loadHighlights();
        },
        error: (err) => {
          this.toastService.show(err?.error?.message || 'Failed to pin highlight.', 'error');
        }
      });
  }
}
