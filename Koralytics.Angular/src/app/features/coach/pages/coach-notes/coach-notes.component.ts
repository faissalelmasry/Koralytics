import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CoachNoteService } from '../../../../../core/services/coach/coach-note.service';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { CoachNoteDto, SquadOverviewDto, WriteNoteDto, CoachTeamDto } from '../../../../../core/interfaces/coach.interfaces';
import { NotificationService } from '@core/services/SignalR/notificationservice';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-coach-notes',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './coach-notes.component.html',
  styleUrls: ['./coach-notes.component.css']
})
export class CoachNotesComponent implements OnInit {
  private noteService = inject(CoachNoteService);
  private squadService = inject(CoachSquadService);
  private authService = inject(AuthService);
  private destroyRef = inject(DestroyRef);
  private notificationService = inject(NotificationService);
  private translate = inject(TranslateService);

  // Team selection — fetched from API
  teams = signal<CoachTeamDto[]>([]);
  selectedTeamId = 0;
  coachId = 0;

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
    const user = this.authService.getCurrentUserSync();
    if (user) {
      this.coachId = user.userId;
    }
    // Fetch coach's assigned teams, then auto-load the first team's squad
    this.squadService.getCoachTeams()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (teams) => {
          this.teams.set(teams);
          if (teams.length > 0) {
            this.selectedTeamId = teams[0].teamId;
            this.loadSquad();
          }
        },
        error: () => {
          this.error.set(this.translate.instant('COACH.NOTES.FAILED_LOAD_TEAMS'));
        }
      });
  }

  onTeamChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedTeamId = +select.value;
    this.selectedPlayerId = null;
    this.notes.set([]);
    this.loadSquad();
  }

  loadSquad(): void {
    this.squadService.getSquad(this.selectedTeamId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.squad.set(data);
          if (data.players.length > 0) {
            this.selectPlayer(data.players[0].playerId);
          }
        },
        error: (err) => {
          this.error.set(this.translate.instant('COACH.NOTES.FAILED_LOAD_SQUAD'));
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
    this.noteService.getPlayerNotes(this.selectedPlayerId, this.currentPage, this.pageSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
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
          this.error.set(this.translate.instant('COACH.NOTES.FAILED_LOAD_NOTES'));
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
    const targetPlayerId = this.selectedPlayerId;
    const isPublicNote = this.newNote.isPublic;
    this.noteService.writeNote(this.newNote)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (savedNote) => {
          // Prepend new note
          this.notes.update(existing => [savedNote, ...existing]);
          //notification
          if (isPublicNote && targetPlayerId) {
            const playerMessage = this.translate.instant('COACH.NOTES.NOTIFY_PLAYER');
            const parentMessage = this.translate.instant('COACH.NOTES.NOTIFY_PARENT');

            this.notificationService.notifyPlayerMilestone(targetPlayerId, playerMessage).subscribe({
              error: (e) => console.error('Failed to notify player about the note', e)
            });


            this.notificationService.notifyPlayerParents(targetPlayerId, parentMessage).subscribe({
              error: (e) => console.error('Failed to notify parent about the note', e)
            });
          }
          // Reset form
          this.newNote.note = '';
          this.newNote.isPublic = false;
          this.newNote.sessionId = undefined;
          this.newNote.matchId = undefined;

          this.successMsg.set(this.translate.instant('COACH.NOTES.NOTE_SAVED'));
          this.submittingNote.set(false);

          setTimeout(() => this.successMsg.set(''), 3000);
        },
        error: (err) => {
          this.error.set(err?.error?.message || this.translate.instant('COACH.NOTES.FAILED_SAVE_NOTE'));
          this.submittingNote.set(false);
        }
      });
  }
}
