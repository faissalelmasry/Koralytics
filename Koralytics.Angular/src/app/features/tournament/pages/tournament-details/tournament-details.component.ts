import { Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { TournamentService } from '../../../../../core/services/tournament/tournament.service';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { Tournament, TournamentStatus, MatchFormat, TournamentStructure } from '../../../../../core/interfaces/tournament.models';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { StatusChipComponent } from '../../../../../shared/components/status-chip/status-chip';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Component({
  selector: 'app-tournament-details',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    CustomButtonComponent,
    StatusChipComponent,
    ScrollRevealDirective
  ],
  templateUrl: './tournament-details.component.html',
  styleUrls: ['./tournament-details.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TournamentDetailsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private location = inject(Location);
  private tournamentService = inject(TournamentService);
  private academyService = inject(AcademyService);
  private cdr = inject(ChangeDetectorRef);

  tournamentId!: number;
  tournament: Tournament | null = null;
  isLoading = true;
  error: string | null = null;

  // Real data from backend BracketDto
  groups: any[] = [];      // From BracketDto.groups -> GroupStandingDto[]
  rounds: any[] = [];      // From BracketDto.rounds -> RoundDto[]
  teams: any[] = [];       // From GET /tournament/{id}/teams
  availableAcademies: any[] = [];
  hallOfFame: any[] = [];
  isUpdatingStatus = false;

  // Computed flat fixture list from groups (for the Fixtures tab)
  allFixtures: any[] = [];

  // Tabs state
  activeTab: 'overview' | 'bracket' | 'fixtures' | 'teams' | 'hallOfFame' | 'aiInsights' = 'overview';
  tabs = [
    { id: 'overview', label: 'Overview & Standings', icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z' },
    { id: 'bracket', label: 'Knockout Stage', icon: 'M13 10V3L4 14h7v7l9-11h-7z' },
    { id: 'fixtures', label: 'Fixtures & Results', icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2' },
    { id: 'teams', label: 'Participating Teams', icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z' },
    { id: 'hallOfFame', label: 'Hall of Fame', icon: 'M8 21h8M12 17v4M7 4h10v5a5 5 0 0 1-10 0V4zM5 6H3a2 2 0 0 0 0 4h2M19 6h2a2 2 0 0 1 0 4h-2' },
    { id: 'aiInsights', label: 'AI Intelligence', icon: 'M12 2a10 10 0 1 0 10 10A10 10 0 0 0 12 2zm0 18a8 8 0 1 1 8-8 8 8 0 0 1-8 8zM12 6a6 6 0 1 0 6 6 6 6 0 0 0-6-6z' }
  ];

  get groupCount(): number {
    return this.groups.length;
  }

  get fixtureCount(): number {
    return this.allFixtures.length + this.rounds.reduce((total, round) => total + (round.fixtures?.length || 0), 0);
  }

  get participatingTeamCount(): number {
    return this.teams.length;
  }

  get primaryAward(): any | null {
    return this.hallOfFame.find(award => award.awardType === 'BestPlayer') || this.hallOfFame[0] || null;
  }

  get supportingAwards(): any[] {
    return this.hallOfFame.filter(award => award !== this.primaryAward);
  }

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.tournamentId = +id;
        this.loadTournamentData();
      } else {
        this.error = 'Tournament ID is missing.';
        this.isLoading = false;
      }
    });
  }

  loadTournamentData() {
    this.isLoading = true;
    this.error = null;

    forkJoin({
      details: this.tournamentService.getTournamentById(this.tournamentId).pipe(
        catchError(() => of(null))
      ),
      bracket: this.tournamentService.getBracket(this.tournamentId).pipe(
        catchError(() => of(null))
      ),
      teams: this.tournamentService.getTournamentTeams(this.tournamentId).pipe(
        catchError(() => of(null))
      ),
      hallOfFame: this.tournamentService.getHallOfFame(this.tournamentId).pipe(
        catchError(() => of(null))
      ),
      academies: this.academyService.getAcademies().pipe(
        catchError(() => of(null))
      )
    }).subscribe({
      next: (responses) => {
        // Tournament details
        this.tournament = responses.details?.data || responses.details || null;

        // Bracket data (groups + standings + rounds + fixtures)
        const bracketData = responses.bracket?.data || responses.bracket;
        if (bracketData) {
          this.groups = bracketData.groups || [];
          this.rounds = bracketData.rounds || [];

          this.allFixtures = this.groups.flatMap((g: any) =>
            (g.fixtures || []).map((f: any) => ({ ...f, groupName: g.groupName }))
          );
          
          const roundFixtures = this.rounds.flatMap((r: any) => 
            (r.fixtures || []).map((f: any) => ({ ...f, groupName: r.roundName }))
          );
          this.allFixtures = [...this.allFixtures, ...roundFixtures];

          // If tournament detail came from bracket, use it
          if (!this.tournament && bracketData.tournamentName) {
            this.tournament = {
              id: bracketData.tournamentId,
              name: bracketData.tournamentName,
              status: bracketData.status
            } as any;
          }
        }

        // Teams data
        const teamsData = responses.teams?.data || responses.teams;
        this.teams = Array.isArray(teamsData) ? teamsData : [];

        const hallOfFameData = responses.hallOfFame?.data || responses.hallOfFame;
        this.hallOfFame = Array.isArray(hallOfFameData) ? hallOfFameData : [];

        // Academies data
        const academyPayload = responses.academies?.data || responses.academies;
        const academiesArray = academyPayload?.academies || academyPayload;
        this.availableAcademies = Array.isArray(academiesArray) ? academiesArray : [];
        this.availableAcademies.forEach(a => a.inviteStatus = 'Idle');

        // Default tab based on structure
        if (this.tournament?.structure === TournamentStructure.Knockout && this.rounds.length > 0) {
          this.activeTab = 'bracket';
        }

        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Failed to load tournament data', err);
        this.error = 'Unable to load tournament. Please try again.';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  setTab(tabId: any) {
    this.activeTab = tabId;
  }

  inviteAcademy(academy: any) {
    if (academy.inviteStatus === 'Inviting' || academy.inviteStatus === 'Invited') return;

    academy.inviteStatus = 'Inviting';
    this.cdr.markForCheck();

    this.tournamentService.inviteAcademy(this.tournamentId, academy.id).subscribe({
      next: () => {
        academy.inviteStatus = 'Invited';
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Failed to invite academy', err);
        academy.inviteStatus = 'Idle';
        this.cdr.markForCheck();
      }
    });
  }

  openRegistration() {
    if (!this.tournament || this.isUpdatingStatus) return;
    this.isUpdatingStatus = true;
    this.cdr.markForCheck();

    this.tournamentService.updateStatus(this.tournamentId, TournamentStatus.Registration).subscribe({
      next: () => {
        this.tournament!.status = TournamentStatus.Registration;
        this.isUpdatingStatus = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Failed to update tournament status', err);
        this.isUpdatingStatus = false;
        this.cdr.markForCheck();
      }
    });
  }

  goBack() {
    this.location.back();
  }

  goToManagement() {
    this.router.navigate(['/tournament/manage', this.tournamentId]);
  }

  goToSquadRegistration(teamId?: number) {
    const queryParams = teamId ? { teamId } : undefined;
    this.router.navigate(['/tournament', this.tournamentId, 'squad-registration'], { queryParams });
  }

  // Helpers
  getChipType(status: TournamentStatus): 'success' | 'danger' | 'info' | 'warning' {
    switch (status) {
      case TournamentStatus.InProgress: return 'info';
      case TournamentStatus.Completed: return 'success';
      case TournamentStatus.Registration: return 'warning';
      default: return 'danger';
    }
  }

  getFormatLabel(format: MatchFormat): string {
    switch (format) {
      case MatchFormat.FiveSide: return '5 vs 5';
      case MatchFormat.SevenSide: return '7 vs 7';
      case MatchFormat.ElevenSide: return '11 vs 11';
      default: return format || '';
    }
  }

  getStructureLabel(structure: TournamentStructure): string {
    switch (structure) {
      case TournamentStructure.GroupAndKnockout: return 'Group & Knockout';
      default: return structure || '';
    }
  }

  getAwardLabel(awardType: string): string {
    switch (awardType) {
      case 'TopScorer': return 'Top Scorer';
      case 'MostAssists': return 'Most Assists';
      case 'MostMOTM': return 'Most Player of the Match';
      case 'BestGoalkeeper': return 'Best Goalkeeper';
      case 'BestPlayer': return 'Player of the Tournament';
      default: return awardType;
    }
  }

  getAwardDescription(awardType: string): string {
    switch (awardType) {
      case 'TopScorer': return 'Highest goal contribution across tournament matches.';
      case 'MostAssists': return 'Most creative provider across the competition.';
      case 'MostMOTM': return 'Most match-winning individual performances.';
      case 'BestGoalkeeper': return 'Top goalkeeper by rating and minutes played.';
      case 'BestPlayer': return 'Best overall tournament performance.';
      default: return 'Tournament award winner.';
    }
  }

  getAwardCode(awardType: string): string {
    switch (awardType) {
      case 'TopScorer': return 'GS';
      case 'MostAssists': return 'AS';
      case 'MostMOTM': return 'MP';
      case 'BestGoalkeeper': return 'GK';
      case 'BestPlayer': return 'PT';
      default: return 'AW';
    }
  }

  getInitials(name: string): string {
    return (name || 'Player')
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map(part => part[0])
      .join('')
      .toUpperCase();
  }
}
