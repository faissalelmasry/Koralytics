import { Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { TournamentService } from '../../../../../core/services/tournament/tournament.service';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { TokenStorageService } from '../../../../../core/services/auth/token-storage.service';
import { MatchFormat, Tournament } from '../../../../../core/interfaces/tournament.models';
import { CustomSelect } from '../../../../../shared/components/custom-select/custom-select';
import { StatusChipComponent } from '../../../../../shared/components/status-chip/status-chip';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

@Component({
  selector: 'app-squad-registration',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    CustomSelect,
    StatusChipComponent,
    ScrollRevealDirective
  ],
  templateUrl: './squad-registration.component.html',
  styleUrls: ['./squad-registration.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SquadRegistrationComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private location = inject(Location);
  private tournamentService = inject(TournamentService);
  private coachSquadService = inject(CoachSquadService);
  private tokenStorage = inject(TokenStorageService);
  private cdr = inject(ChangeDetectorRef);

  tournamentId!: number;
  tournament: Tournament | null = null;
  tournamentTeams: any[] = [];
  teamOptions: { value: number; label: string }[] = [];
  selectedTeamId: number | null = null;
  players: any[] = [];
  selectedPlayerIds = new Set<number>();
  registeredPlayerIds = new Set<number>();
  isLoading = true;
  isLoadingPlayers = false;
  isSubmitting = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit() {
    this.tournamentId = Number(this.route.snapshot.paramMap.get('id'));
    const queryTeamId = Number(this.route.snapshot.queryParamMap.get('teamId'));
    this.selectedTeamId = queryTeamId || null;
    this.loadPageData();
  }

  get selectedCount(): number {
    return this.selectedPlayerIds.size;
  }

  get requiredRangeLabel(): string {
    const rules = this.squadRules;
    return `${rules.min}-${rules.max} players`;
  }

  get squadRules(): { min: number; max: number } {
    const format = String(this.tournament?.format ?? '');
    if (format === MatchFormat.FiveSide || format === '5') return { min: 5, max: 10 };
    if (format === MatchFormat.SevenSide || format === '7') return { min: 7, max: 14 };
    return { min: 11, max: 23 };
  }

  get selectionHint(): string {
    const { min, max } = this.squadRules;
    if (this.selectedCount < min) return `Select at least ${min} players for this format.`;
    if (this.selectedCount > max) return `Remove ${this.selectedCount - max} player(s) to stay within the ${max}-player limit.`;
    return 'Squad size is valid.';
  }

  get canSubmitSquad(): boolean {
    const { min, max } = this.squadRules;
    return !!this.selectedTeamId && this.selectedCount >= min && this.selectedCount <= max && !this.isSubmitting;
  }

  get selectedTeamName(): string {
    return this.teamOptions.find(option => option.value === this.selectedTeamId)?.label || 'Select a team';
  }

  loadPageData() {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.markForCheck();

    forkJoin({
      details: this.tournamentService.getTournamentById(this.tournamentId).pipe(catchError(() => of(null))),
      teams: this.tournamentService.getTournamentTeams(this.tournamentId).pipe(catchError(() => of(null)))
    }).subscribe({
      next: (responses) => {
        this.tournament = responses.details?.data || responses.details || null;
        const teamPayload = responses.teams?.data || responses.teams;
        this.tournamentTeams = Array.isArray(teamPayload) ? teamPayload : [];
        this.teamOptions = this.tournamentTeams.map(team => ({
          value: team.teamId,
          label: team.teamName || `Team #${team.teamId}`
        }));

        if (!this.selectedTeamId && this.teamOptions.length > 0) {
          this.selectedTeamId = this.teamOptions[0].value;
        }

        this.isLoading = false;
        this.cdr.markForCheck();
        this.loadPlayersForSelectedTeam();
      },
      error: () => {
        this.errorMessage = 'Unable to load tournament teams.';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  onTeamChange(teamId: number) {
    this.selectedTeamId = teamId;
    this.selectedPlayerIds.clear();
    this.successMessage = '';
    this.loadPlayersForSelectedTeam();
  }

  loadPlayersForSelectedTeam() {
    if (!this.selectedTeamId) return;

    const user = this.tokenStorage.getUser();
    const coachId = user?.userId;
    this.isLoadingPlayers = true;
    this.errorMessage = '';
    this.cdr.markForCheck();

    if (!coachId) {
      this.players = [];
      this.errorMessage = 'Unable to load squad players for the current coach.';
      this.isLoadingPlayers = false;
      this.cdr.markForCheck();
      return;
    }

    forkJoin({
      squad: this.coachSquadService.getSquad(coachId, this.selectedTeamId).pipe(catchError(() => of(null))),
      registered: this.tournamentService.getRegisteredPlayerIds(this.tournamentId, this.selectedTeamId).pipe(catchError(() => of([])))
    }).subscribe({
      next: ({ squad, registered }) => {
        const data = squad?.data || squad;
        const players = data?.players || data?.Players || [];
        this.players = this.normalizePlayers(Array.isArray(players) ? players : []);
        
        this.registeredPlayerIds.clear();
        const regIds = registered?.data || registered;
        if (Array.isArray(regIds)) {
          regIds.forEach(id => this.registeredPlayerIds.add(id));
        }

        this.isLoadingPlayers = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.players = [];
        this.errorMessage = 'Unable to load squad players for the selected team.';
        this.isLoadingPlayers = false;
        this.cdr.markForCheck();
      }
    });
  }

  togglePlayer(playerId: number) {
    if (this.registeredPlayerIds.has(playerId)) return;
    if (this.selectedPlayerIds.has(playerId)) {
      this.selectedPlayerIds.delete(playerId);
    } else {
      this.selectedPlayerIds.add(playerId);
    }
  }

  isSelected(playerId: number): boolean {
    return this.selectedPlayerIds.has(playerId);
  }

  submitSquad() {
    if (!this.selectedTeamId || this.selectedPlayerIds.size === 0) {
      this.errorMessage = 'Choose a team and select at least one player.';
      return;
    }

    if (!this.canSubmitSquad) {
      this.errorMessage = this.selectionHint;
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.markForCheck();

    const playerIds = Array.from(this.selectedPlayerIds);
    this.tournamentService.registerSquad(this.tournamentId, this.selectedTeamId, playerIds).subscribe({
      next: () => {
        playerIds.forEach(id => this.registeredPlayerIds.add(id));
        this.selectedPlayerIds.clear();
        this.successMessage = 'Squad registered successfully.';
        this.isSubmitting = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.errorMessage = this.extractError(err, 'Unable to register squad. Please review the player count and try again.');
        this.isSubmitting = false;
        this.cdr.markForCheck();
      }
    });
  }

  goBack() {
    this.location.back();
  }

  private extractError(err: any, fallback: string): string {
    if (!err?.error) return fallback;
    if (typeof err.error === 'string') return err.error;
    if (err.error.errors) return Object.values(err.error.errors).map((e: any) => e.join(', ')).join(' | ');
    return err.error.message || err.error.detail || err.error.title || fallback;
  }

  private normalizePlayers(players: any[]) {
    return players.map(player => ({
      playerId: player.playerId ?? player.PlayerId,
      fullName: player.fullName ?? player.FullName ?? 'Player',
      primaryPosition: player.primaryPosition ?? player.PrimaryPosition ?? 'Position pending',
      availabilityStatus: player.availabilityStatus ?? player.AvailabilityStatus ?? 'Available',
      overallRating: player.overallRating ?? player.OverallRating ?? 0
    }));
  }
}
