import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CoachNoteService } from '../../../../../core/services/coach/coach-note.service';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { CoachNoteDto, SquadOverviewDto, WriteNoteDto } from '../../../../../core/interfaces/coach.interfaces';

@Component({
  selector: 'app-coach-notes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './coach-notes.component.html',
  styleUrls: ['./coach-notes.component.css']
})
export class CoachNotesComponent implements OnInit {
  private noteService = inject(CoachNoteService);
  private squadService = inject(CoachSquadService);

  // Hardcoded for now, normally from auth claims
  coachId = 0;
  teamId = 1;

  squad = signal<SquadOverviewDto | null>(null);
  selectedPlayerId: number | null = null;
  
  notes = signal<CoachNoteDto[]>([]);
  loadingNotes = signal(false);
  
  // Pagination
  currentPage = 1;
  pageSize = 10;
  hasNextPage = false;
  
  // New Note Form
  newNote: WriteNoteDto = {
    playerId: 0,
    note: '',
    isPublic: false
  };
  submittingNote = signal(false);
  error = signal('');
  successMsg = signal('');

  ngOnInit(): void {
    this.loadSquad();
  }

  loadSquad(): void {
    this.squadService.getSquad(this.coachId, this.teamId).subscribe({
      next: (data) => {
        this.squad.set(data);
        if (data.players.length > 0) {
          this.selectPlayer(data.players[0].playerId);
        }
      },
      error: (err) => {
        this.error.set('Failed to load squad.');
      }
    });
  }

  selectPlayer(playerId: number): void {
    this.selectedPlayerId = playerId;
    this.newNote.playerId = playerId;
    this.notes.set([]); // clear existing
    this.currentPage = 1;
    this.loadNotes();
  }

  loadNotes(): void {
    if (!this.selectedPlayerId) return;
    
    this.loadingNotes.set(true);
    this.noteService.getPlayerNotes(this.selectedPlayerId, this.currentPage, this.pageSize).subscribe({
      next: (res) => {
        if (this.currentPage === 1) {
          this.notes.set(res.items);
        } else {
          this.notes.update(existing => [...existing, ...res.items]);
        }
        this.hasNextPage = res.hasNextPage;
        this.loadingNotes.set(false);
      },
      error: (err) => {
        this.loadingNotes.set(false);
        this.error.set('Failed to load notes.');
      }
    });
  }

  loadMore(): void {
    if (this.hasNextPage) {
      this.currentPage++;
      this.loadNotes();
    }
  }

  submitNote(): void {
    if (!this.newNote.note.trim() || !this.selectedPlayerId) return;

    this.submittingNote.set(true);
    this.error.set('');
    this.successMsg.set('');

    this.noteService.writeNote(this.newNote).subscribe({
      next: (savedNote) => {
        // Prepend new note
        this.notes.update(existing => [savedNote, ...existing]);
        // Reset form
        this.newNote.note = '';
        this.newNote.isPublic = false;
        this.newNote.sessionId = undefined;
        this.newNote.matchId = undefined;
        
        this.successMsg.set('Note saved successfully.');
        this.submittingNote.set(false);
        
        setTimeout(() => this.successMsg.set(''), 3000);
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Failed to save note.');
        this.submittingNote.set(false);
      }
    });
  }
}
