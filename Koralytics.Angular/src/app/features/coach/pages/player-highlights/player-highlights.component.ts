import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PlayerHighlightService } from '../../../../../core/services/player/player-highlight.service';
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

  // Would normally come from auth context
  playerId = 0; // Mock ID. Assuming the logged in user is a player, or a coach viewing a player.
  academyId = 1;

  highlights = signal<PlayerHighlightDto[]>([]);
  loading = signal(false);
  error = signal('');
  
  // Upload State
  selectedFile: File | null = null;
  highlightTitle = '';
  uploading = signal(false);
  uploadError = signal('');

  ngOnInit(): void {
    // In a real app we'd get playerId from Route params or Auth Service
    this.playerId = 1; // Assuming 1 for testing
    this.loadHighlights();
  }

  loadHighlights(): void {
    this.loading.set(true);
    this.error.set('');
    
    this.highlightService.getHighlights(this.playerId).subscribe({
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

  onFileSelected(event: any): void {
    const file: File = event.target.files[0];
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

    this.highlightService.uploadHighlight(this.playerId, this.academyId, this.selectedFile, this.highlightTitle).subscribe({
      next: (newHighlight) => {
        // Unpin others if this one is pinned? No, the backend handles pinning logic. Just reload.
        this.loadHighlights();
        this.selectedFile = null;
        this.highlightTitle = '';
        this.uploading.set(false);
        // Reset file input
        const fileInput = document.getElementById('highlightFile') as HTMLInputElement;
        if (fileInput) fileInput.value = '';
      },
      error: (err) => {
        this.uploadError.set(err?.error?.message || 'Failed to upload highlight.');
        this.uploading.set(false);
      }
    });
  }

  deleteHighlight(highlightId: number): void {
    if (!confirm('Are you sure you want to delete this highlight?')) return;

    this.highlightService.deleteHighlight(this.playerId, highlightId).subscribe({
      next: () => {
        this.highlights.update(list => list.filter(h => h.id !== highlightId));
      },
      error: (err) => {
        alert(err?.error?.message || 'Failed to delete highlight.');
      }
    });
  }

  pinHighlight(highlightId: number): void {
    this.highlightService.pinHighlight(this.playerId, highlightId).subscribe({
      next: () => {
        // Backend handles unpinning the previous one, so let's just reload to get the new state
        this.loadHighlights();
      },
      error: (err) => {
        alert(err?.error?.message || 'Failed to pin highlight.');
      }
    });
  }
}
