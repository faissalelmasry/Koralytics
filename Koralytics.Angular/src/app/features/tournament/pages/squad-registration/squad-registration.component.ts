import { Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { TournamentService } from '../../../../../core/services/tournament/tournament.service';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { TokenStorageService } from '../../../../../core/services/auth/token-storage.service';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { MatchFormat, Tournament, TournamentStatus, TournamentStructure } from '../../../../../core/interfaces/tournament.models';
import { CustomSelect } from '../../../../../shared/components/custom-select/custom-select';
import { StatusChipComponent } from '../../../../../shared/components/status-chip/status-chip';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';
import { NotificationService } from '@core/services/SignalR/notificationservice';

@Component({
  selector: 'app-squad-registration',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    CustomSelect,
    StatusChipComponent,
    LoadingSpinnerComponent,
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
  private academyService = inject(AcademyService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);
  private notificationService = inject(NotificationService);
  tournamentId!: number;
  tournament: Tournament | null = null;
  tournamentTeams: any[] = [];
  teamOptions: { value: number; label: string }[] = [];
  selectedTeamId: number | null = null;
  players: any[] = [];
  selectedPlayerIds = new Set<number>();
  registeredPlayerIds = new Set<number>();
  jerseyNumbers: Record<number, number> = {};
  captainPlayerId: number | null = null;
  selectedPositionFilter: 'ALL' | 'GK' | 'DEF' | 'MID' | 'FWD' = 'ALL';

  isLoading = true;
  isLoadingPlayers = false;
  isSubmitting = false;
  errorMessage = '';
  successMessage = '';

  positionFilters: { key: 'ALL' | 'GK' | 'DEF' | 'MID' | 'FWD'; label: string }[] = [
    { key: 'ALL', label: 'All Positions' },
    { key: 'GK', label: 'Goalkeepers' },
    { key: 'DEF', label: 'Defenders' },
    { key: 'MID', label: 'Midfielders' },
    { key: 'FWD', label: 'Forwards' }
  ];

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
    if (!this.captainPlayerId || !this.selectedPlayerIds.has(this.captainPlayerId)) {
      return 'You must select a team captain (C) among selected players.';
    }
    return 'Squad size and captain selection are valid.';
  }

  get canSubmitSquad(): boolean {
    const { min, max } = this.squadRules;
    const hasCaptain = !!this.captainPlayerId && this.selectedPlayerIds.has(this.captainPlayerId);
    return !!this.selectedTeamId && this.selectedCount >= min && this.selectedCount <= max && hasCaptain && !this.isSubmitting;
  }

  get selectedTeamName(): string {
    return this.teamOptions.find(option => option.value === this.selectedTeamId)?.label || 'Select a team';
  }

  get filteredPlayers(): any[] {
    if (this.selectedPositionFilter === 'ALL') return this.players;
    return this.players.filter(p => this.getPositionCategory(p.primaryPosition) === this.selectedPositionFilter);
  }

  setPositionFilter(filter: 'ALL' | 'GK' | 'DEF' | 'MID' | 'FWD') {
    this.selectedPositionFilter = filter;
  }

  getPositionCategory(position: string): 'GK' | 'DEF' | 'MID' | 'FWD' {
    const pos = (position || '').toUpperCase();
    if (pos.includes('GK') || pos.includes('GOAL') || pos.includes('KEEP')) return 'GK';
    if (pos.includes('CB') || pos.includes('LB') || pos.includes('RB') || pos.includes('DEF') || pos.includes('BACK')) return 'DEF';
    if (pos.includes('CM') || pos.includes('CDM') || pos.includes('CAM') || pos.includes('MID') || pos.includes('WING')) return 'MID';
    return 'FWD';
  }

  autoSelectTopRated() {
    const { max } = this.squadRules;
    const sorted = [...this.players]
      .filter(p => !this.registeredPlayerIds.has(p.playerId))
      .sort((a, b) => (b.overallRating || 0) - (a.overallRating || 0));

    this.selectedPlayerIds.clear();
    sorted.slice(0, max).forEach(p => {
      this.selectedPlayerIds.add(p.playerId);
      if (!this.jerseyNumbers[p.playerId]) {
        this.jerseyNumbers[p.playerId] = this.selectedPlayerIds.size;
      }
    });

    if (!this.captainPlayerId && sorted.length > 0) {
      this.captainPlayerId = sorted[0].playerId;
    }
  }

  clearAllSelections() {
    this.selectedPlayerIds.clear();
    this.captainPlayerId = null;
  }

  setJerseyNumber(playerId: number, value: string | number) {
    const num = Number(value);
    if (!isNaN(num) && num > 0) {
      this.jerseyNumbers[playerId] = num;
    } else {
      delete this.jerseyNumbers[playerId];
    }
  }

  toggleCaptain(playerId: number) {
    if (this.captainPlayerId === playerId) {
      this.captainPlayerId = null;
    } else {
      this.captainPlayerId = playerId;
    }
  }

  loadPageData() {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.markForCheck();

    const user = this.authService.getCurrentUserValue() || this.tokenStorage.getUser();
    const userAcademyId = user?.academyId || (user as any)?.AcademyId || 0;

    forkJoin({
      details: this.tournamentService.getTournamentById(this.tournamentId).pipe(catchError(() => of(null))),
      tournamentTeams: this.tournamentService.getTournamentTeams(this.tournamentId).pipe(catchError(() => of(null))),
      academyTeams: userAcademyId ? this.academyService.getTeams(userAcademyId).pipe(catchError(() => of(null))) : of(null)
    }).subscribe({
      next: (responses) => {
        this.tournament = responses.details?.data || responses.details || {
          id: this.tournamentId || 1,
          name: 'Summer Champions Cup 2026',
          format: MatchFormat.ElevenSide,
          structure: TournamentStructure.GroupAndKnockout,
          ageGroupName: 'U-17',
          hasTwoLegs: false,
          startDate: '2026-08-01',
          endDate: '2026-08-15',
          status: TournamentStatus.Registration
        };

        const tourneyAgeGroup = (this.tournament?.ageGroupName || '').trim();
        const tourneyAgeGroupId = (this.tournament as any)?.ageGroupId;

        // Raw teams from academy endpoint
        const rawAcademyTeams = responses.academyTeams?.data || responses.academyTeams || [];
        const academyTeams: any[] = Array.isArray(rawAcademyTeams) ? rawAcademyTeams : [];

        // Raw teams from tournament endpoint
        const rawTourneyTeams = responses.tournamentTeams?.data || responses.tournamentTeams || [];
        const tourneyTeams: any[] = Array.isArray(rawTourneyTeams) ? rawTourneyTeams : [];

        let candidates: any[] = [];

        if (academyTeams.length > 0) {
          candidates = academyTeams;
        } else if (tourneyTeams.length > 0) {
          candidates = tourneyTeams;
        }

        // Filter 1: Academy check - keep teams belonging ONLY to this academy
        if (userAcademyId) {
          const academyOnly = candidates.filter((t: any) => {
            const tAcadId = t.academyId ?? t.AcademyId;
            return !tAcadId || tAcadId === userAcademyId;
          });
          if (academyOnly.length > 0) {
            candidates = academyOnly;
          }
        }

        // Filter 2: Age Group check - keep teams matching this tournament's age group ONLY
        if (tourneyAgeGroup || tourneyAgeGroupId) {
          const normTourneyGroup = tourneyAgeGroup.toLowerCase().replace(/[^a-z0-9]/g, '');
          const ageGroupFiltered = candidates.filter((t: any) => {
            if (tourneyAgeGroupId && t.ageGroupId && t.ageGroupId === tourneyAgeGroupId) {
              return true;
            }
            const tGroup = (t.ageGroupName || t.AgeGroupName || '').toLowerCase().replace(/[^a-z0-9]/g, '');
            if (!tGroup || !normTourneyGroup) return true;
            return tGroup === normTourneyGroup || tGroup.includes(normTourneyGroup) || normTourneyGroup.includes(tGroup);
          });
          if (ageGroupFiltered.length > 0) {
            candidates = ageGroupFiltered;
          }
        }

        this.tournamentTeams = candidates;

        this.teamOptions = this.tournamentTeams.map(team => ({
          value: team.id ?? team.teamId ?? team.Id ?? team.TeamId,
          label: team.name || team.teamName || team.Name || team.TeamName || `Team #${team.id || team.teamId}`
        }));

        if (this.teamOptions.length > 0) {
          const queryTeamId = Number(this.route.snapshot.queryParamMap.get('teamId'));
          const exists = this.teamOptions.some(opt => opt.value === queryTeamId);
          this.selectedTeamId = exists ? queryTeamId : this.teamOptions[0].value;
        } else {
          this.selectedTeamId = null;
        }

        this.isLoading = false;
        this.cdr.markForCheck();
        this.loadPlayersForSelectedTeam();
      },
      error: (err) => {
        this.tournament = null;
        this.tournamentTeams = [];
        this.teamOptions = [];
        this.selectedTeamId = null;
        this.errorMessage = this.extractError(err, 'Unable to load tournament teams.');
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  onTeamChange(teamId: number) {
    this.selectedTeamId = teamId;
    this.selectedPlayerIds.clear();
    this.jerseyNumbers = {};
    this.captainPlayerId = null;
    this.successMessage = '';
    this.loadPlayersForSelectedTeam();
  }

  loadPlayersForSelectedTeam() {
    if (!this.selectedTeamId) return;

    const user = this.tokenStorage.getUser();
    const coachId = user?.userId || 1;
    this.isLoadingPlayers = true;
    this.errorMessage = '';
    this.cdr.markForCheck();

    forkJoin({
      squad: this.coachSquadService.getSquad(this.selectedTeamId, coachId).pipe(catchError(() => of(null))),
      registered: this.tournamentService.getRegisteredPlayerIds(this.tournamentId, this.selectedTeamId).pipe(catchError(() => of([])))
    }).subscribe({
      next: ({ squad, registered }) => {
        const data = (squad as any)?.data || squad;
        const rawPlayers = data?.players || data?.Players || [];

        this.players = this.normalizePlayers(Array.isArray(rawPlayers) ? rawPlayers : []);

        this.registeredPlayerIds.clear();
        const regIds = registered?.data || registered;
        if (Array.isArray(regIds)) {
          regIds.forEach(id => this.registeredPlayerIds.add(id));
        }

        this.isLoadingPlayers = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.players = [];
        this.errorMessage = this.extractError(err, 'Unable to load players.');
        this.isLoadingPlayers = false;
        this.cdr.markForCheck();
      }
    });
  }


  togglePlayer(playerId: number) {
    if (this.registeredPlayerIds.has(playerId)) return;
    if (this.selectedPlayerIds.has(playerId)) {
      this.selectedPlayerIds.delete(playerId);
      if (this.captainPlayerId === playerId) {
        this.captainPlayerId = null;
      }
    } else {
      this.selectedPlayerIds.add(playerId);
      if (!this.jerseyNumbers[playerId]) {
        this.jerseyNumbers[playerId] = this.selectedPlayerIds.size;
      }
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
        if (playerIds && playerIds.length > 0) {
          const tournamentName = this.tournament?.name || `Tournament #${this.tournamentId}`;
          const playerMsg = `Congratulations! You have been selected for the squad in ${tournamentName}.`;
          const parentMsg = `Your child has been selected for the squad in ${tournamentName}.`;


          this.notificationService.notifyMultiplePlayersMilestone(playerIds, playerMsg).subscribe({
            next: () => console.log('Successfully notified all squad players.'),
            error: (e) => console.error('Failed to notify squad players', e)
          });

          this.notificationService.notifyParentsOfPlayers(playerIds, parentMsg).subscribe({
            next: () => console.log('Successfully notified all parents of squad players.'),
            error: (e) => console.error('Failed to notify parents', e)
          });
        }
        this.isSubmitting = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.errorMessage = this.extractError(err, 'Could not register the squad.');
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
