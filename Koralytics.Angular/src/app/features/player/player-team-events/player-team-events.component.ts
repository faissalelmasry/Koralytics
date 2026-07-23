import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../shared/components/footer/footer';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { CustomSelect } from '../../../../shared/components/custom-select/custom-select';
import { CustomDatePicker } from '../../../../shared/components/custom-date-picker/custom-date-picker';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { PlayerProfileService } from '../../../../core/services/player/player-profile.service';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { TeamScheduledEventDto } from '../../../../core/models/Player/scheduled-event-model';

@Component({
  selector: 'app-player-team-events',
  standalone: true,
  imports: [
    CommonModule,
    NavbarComponent,
    Footer,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    Pagination,
    CustomSelect,
    CustomDatePicker,
    CustomButtonComponent,
    ScrollRevealDirective
  ],
  templateUrl: './player-team-events.component.html',
  styleUrls: ['./player-team-events.component.css']
})
export class PlayerTeamEventsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private profileService = inject(PlayerProfileService);
  private tokenStorage = inject(TokenStorageService);
  private cdr = inject(ChangeDetectorRef);

  playerId: number | null = null;
  playerName = '';
  isLoading = false;
  error = '';
  filterError = '';

  currentPage = 1;
  pageSize = 10;
  totalItems = 0;

  events: TeamScheduledEventDto[] = [];

  selectedEventType = '';
  selectedDateFrom = '';
  selectedDateTo = '';

  eventTypeOptions = [
    { value: 'Match', label: 'Match' },
    { value: 'Drill', label: 'Drill' }
  ];

  ngOnInit() {
    const user = this.tokenStorage.getUser();
    if (!user?.userId) {
      this.error = 'Invalid session';
      return;
    }
    this.playerId = user.userId;
    this.fetchPlayerDetailsAndTimeline();
  }

  fetchPlayerDetailsAndTimeline() {
    if (!this.playerId) return;

    this.profileService.getPlayerProfile(this.playerId).subscribe({
      next: (profile) => {
        this.playerName = `${profile.firstName} ${profile.lastName}`;
        this.cdr.detectChanges();
      },
      error: () => {
        this.playerName = 'Player';
      }
    });

    this.loadEvents();
  }

  loadEvents() {
    if (!this.playerId) return;

    this.isLoading = true;
    this.error = '';

    this.profileService.getTeamScheduledEvents(
      this.playerId,
      this.currentPage,
      this.pageSize,
      this.selectedEventType || undefined,
      this.selectedDateFrom || undefined,
      this.selectedDateTo || undefined
    ).subscribe({
      next: (res: any) => {
        this.events = this.mapEvents(res.events ?? res.Events ?? []);
        this.totalItems = res.totalCount ?? res.TotalCount ?? 0;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.error = 'Failed to load scheduled events.';
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters() {
    this.filterError = '';

    if (this.selectedDateFrom && this.selectedDateTo && this.selectedDateFrom > this.selectedDateTo) {
      this.filterError = '"From" date must be earlier than "To" date.';
      return;
    }

    this.currentPage = 1;
    this.loadEvents();
  }

  clearFilters() {
    this.selectedEventType = '';
    this.selectedDateFrom = '';
    this.selectedDateTo = '';
    this.filterError = '';
    this.currentPage = 1;
    this.loadEvents();
  }

  onDateFromChange() {
    if (this.selectedDateFrom && this.selectedDateTo && this.selectedDateFrom > this.selectedDateTo) {
      this.selectedDateTo = this.selectedDateFrom;
    }
  }

  onDateToChange() {
    if (this.selectedDateFrom && this.selectedDateTo && this.selectedDateFrom > this.selectedDateTo) {
      this.selectedDateFrom = this.selectedDateTo;
    }
  }

  onPageChange(page: number) {
    this.currentPage = page;
    this.loadEvents();
  }

  isMatchEvent(event: TeamScheduledEventDto): boolean {
    return event.eventType?.toLowerCase() === 'match';
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  formatTime(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  private mapEvents(raw: any[]): TeamScheduledEventDto[] {
    return raw.map((e: any) => ({
      eventType: e.eventType ?? e.EventType ?? '',
      date: e.date ?? e.Date ?? '',
      matchId: e.matchId ?? e.MatchId ?? null,
      matchType: e.matchType ?? e.MatchType ?? null,
      homeTeamName: e.homeTeamName ?? e.HomeTeamName ?? null,
      awayTeamName: e.awayTeamName ?? e.AwayTeamName ?? null,
      sessionId: e.sessionId ?? e.SessionId ?? null,
      sessionType: e.sessionType ?? e.SessionType ?? null,
      teamId: e.teamId ?? e.TeamId ?? 0,
      teamName: e.teamName ?? e.TeamName ?? '',
      notes: e.notes ?? e.Notes ?? null
    }));
  }
}
