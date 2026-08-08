import { Component, OnInit, OnDestroy, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { TournamentService } from '../../../../../core/services/tournament/tournament.service';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { SignalRService } from '../../../../../core/services/SignalR/signalrservice';
import { Tournament, TournamentStatus, MatchFormat, TournamentStructure, GoalEventDto, UpdateFixtureStatsDto } from '../../../../../core/interfaces/tournament.models';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { StatusChipComponent } from '../../../../../shared/components/status-chip/status-chip';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';
import { forkJoin, of, Subscription } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { FormsModule } from '@angular/forms';
import { NotificationService } from '@core/services/SignalR/notificationservice';

@Component({
  selector: 'app-tournament-details',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    CustomButtonComponent,
    StatusChipComponent,
    ScrollRevealDirective
  ],
  templateUrl: './tournament-details.component.html',
  styleUrls: [
    './tournament-details.component.css',
    './report-design.css'
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TournamentDetailsComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private location = inject(Location);
  private tournamentService = inject(TournamentService);
  private academyService = inject(AcademyService);
  private coachSquadService = inject(CoachSquadService);
  private signalRService = inject(SignalRService);
  private cdr = inject(ChangeDetectorRef);
  private announcementSub?: Subscription;
  private notificationService = inject(NotificationService);

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
  report: any | null = null;
  parsedReport: { meta: string[]; sections: { title: string; content: string[] }[] } | null = null;
  isReportLoading = false;
  isUpdatingStatus = false;
  hoveredTeamName: string | null = null;
  selectedReportTab = 'summary';
  isSimulating = false;
  isRegenerating = false;

  // Computed flat fixture list from groups (for the Fixtures tab)
  allFixtures: any[] = [];
  fixtureStatusFilter: 'ALL' | 'Scheduled' | 'Completed' = 'ALL';

  get filteredFixtures(): any[] {
    if (this.fixtureStatusFilter === 'ALL') {
      return this.allFixtures;
    }
    return this.allFixtures.filter(f => (f.status || 'Scheduled') === this.fixtureStatusFilter);
  }

  setFixtureStatusFilter(filter: 'ALL' | 'Scheduled' | 'Completed') {
    this.fixtureStatusFilter = filter;
    this.cdr.markForCheck();
  }

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
        this.listenForReportReady();
      } else {
        this.error = 'Tournament ID is missing.';
        this.isLoading = false;
      }
    });
  }

  ngOnDestroy() {
    this.announcementSub?.unsubscribe();
  }

  /** Subscribe to SignalR announcements and auto-refresh the report when ready. */
  private listenForReportReady() {
    this.announcementSub = this.signalRService.announcement$.subscribe(notification => {
      const payload = notification?.payload as any;
      if (
        notification?.type === 'TournamentReportReady' &&
        payload?.TournamentId === this.tournamentId
      ) {
        this.refreshReport();
      }
    });
  }

  /** Reload only the report from the API (lightweight — avoids full bracket/teams reload). */
  private refreshReport() {
    this.tournamentService.getTournamentReport(this.tournamentId).pipe(
      catchError(() => of(null))
    ).subscribe(response => {
      const reportData = response?.data || response;
      if (reportData) {
        this.report = reportData;
        if (this.report && this.report.reportText) {
          this.parsedReport = this.parseReportText(this.report.reportText);
        } else {
          this.parsedReport = null;
        }
        this.cdr.markForCheck();
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
      ),
      report: this.tournamentService.getTournamentReport(this.tournamentId).pipe(
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

        const reportData = responses.report?.data || responses.report;
        this.report = reportData || null;
        if (this.report && this.report.reportText) {
          this.parsedReport = this.parseReportText(this.report.reportText);
        } else {
          this.parsedReport = null;
        }

        // Academies data
        const academyPayload = responses.academies?.data || responses.academies;
        const academiesArray = academyPayload?.academies || academyPayload;
        this.availableAcademies = Array.isArray(academiesArray) ? academiesArray : [];
        this.availableAcademies.forEach(a => a.inviteStatus = 'Idle');

        if (!this.report && this.tournament?.status === TournamentStatus.Completed) {
          this.report = { reportText: '', isPending: true };
        }

        // Default tab based on structure
        if (this.tournament?.structure === TournamentStructure.Knockout && this.rounds.length > 0) {
          this.activeTab = 'bracket';
        }

        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isLoading = false;
        this.error = 'Unable to load tournament details. Please refresh or try again later.';
        console.error('Tournament load failed', err);
        this.cdr.markForCheck();
      }
    });
  }

  setTab(tabId: any) {
    this.activeTab = tabId;
  }

  runSimulation() {
    if (this.isSimulating) return;
    this.isSimulating = true;
    this.cdr.markForCheck();
    this.tournamentService.simulateTournament(this.tournamentId).subscribe({
      next: () => {
        this.isSimulating = false;
        this.loadTournamentData();
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Simulation failed', err);
        this.isSimulating = false;
        this.cdr.markForCheck();
      }
    });
  }

  setReportTab(tabId: string) {
    this.selectedReportTab = tabId;
    this.cdr.markForCheck();
  }

  parseReportText(text: string): { meta: string[]; sections: { title: string; content: string[] }[] } {
    if (!text) return { meta: [], sections: [] };

    const rawLines = text.split('\n').map(l => l.trim()).filter(l => l.length > 0);
    const sections: { title: string; content: string[] }[] = [];
    let currentSection: { title: string; content: string[] } | null = null;
    const meta: string[] = [];

    const isHeaderPattern = (line: string): boolean => {
      if (line.startsWith('#')) return true;
      if (/^[⭐️💡📌🎯📊⚽🏆🛡️❄️]\s*/.test(line)) return true;
      if (/^[\d+[\.\-\)]\s*[\u0600-\u06FF\w]/.test(line)) return true;
      if (line.endsWith(':') && line.length < 80) return true;
      if (line.startsWith(':') && line.length < 80) return true;
      return false;
    };

    for (const line of rawLines) {
      if (isHeaderPattern(line)) {
        if (currentSection) {
          sections.push(currentSection);
        }
        let cleanTitle = line
          .replace(/^#+\s*/, '')
          .replace(/^:\s*/, '')
          .replace(/:\s*$/, '')
          .trim();
        currentSection = { title: cleanTitle, content: [] };
      } else {
        if (currentSection) {
          currentSection.content.push(line);
        } else {
          meta.push(line);
        }
      }
    }

    if (currentSection) {
      sections.push(currentSection);
    }

    if (sections.length === 0 && rawLines.length > 0) {
      sections.push({
        title: 'التقرير التحليلي الفني والملخص الشامل',
        content: rawLines
      });
    }

    return { meta, sections };
  }

  isBulletLine(text: string): boolean {
    if (!text) return false;
    const trimmed = text.trim();
    return (
      trimmed.startsWith('-') ||
      trimmed.startsWith('*') ||
      trimmed.startsWith('•') ||
      trimmed.startsWith('›') ||
      /^\d+[\.\)]/.test(trimmed) ||
      trimmed.includes('🔑') ||
      trimmed.includes('📌') ||
      trimmed.includes('⚽') ||
      trimmed.includes(':')
    );
  }

  regenerateReport() {
    if (this.isRegenerating) return;
    this.isRegenerating = true;
    this.cdr.markForCheck();

    this.tournamentService.regenerateTournamentReport(this.tournamentId).subscribe({
      next: () => {
        this.isRegenerating = false;
        this.loadTournamentData();
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Failed to regenerate report', err);
        this.isRegenerating = false;
        this.cdr.markForCheck();
      }
    });
  }

  inviteAcademy(academy: any) {
    if (academy.inviteStatus === 'Inviting' || academy.inviteStatus === 'Invited') return;

    academy.inviteStatus = 'Inviting';
    this.cdr.markForCheck();

    this.tournamentService.inviteAcademy(this.tournamentId, academy.id).subscribe({
      next: () => {
        academy.inviteStatus = 'Invited';
        //notification
        const tournamentName = this.tournament?.name || `Tournament #${this.tournamentId}`;
        const message = `Your academy has been invited to participate in ${tournamentName}.`;

        this.notificationService.notifyAcademy(academy.id, message).subscribe({
          error: (e) => console.error('Failed to notify academy', e)
        });
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

  setHoveredTeam(teamName: string | null) {
    this.hoveredTeamName = teamName;
  }

  exportStandingsCSV() {
    if (!this.groups.length) return;

    let csvContent = 'data:text/csv;charset=utf-8,Group,Pos,Club,P,W,D,L,GD,Pts\n';

    this.groups.forEach(group => {
      (group.standings || []).forEach((row: any, i: number) => {
        csvContent += `"${group.groupName}",${i + 1},"${row.teamName}",${row.played},${row.won},${row.drawn},${row.lost},${row.goalDifference},${row.points}\n`;
      });
    });

    const encodedUri = encodeURI(csvContent);
    const link = document.createElement('a');
    link.setAttribute('href', encodedUri);
    link.setAttribute('download', `${(this.tournament?.name || 'Tournament').replace(/\s+/g, '_')}_Standings.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }

  exportFixturesCSV() {
    if (!this.allFixtures.length) return;

    let csvContent = 'data:text/csv;charset=utf-8,Stage/Round,Home Team,Home Score,Away Score,Away Team,Status\n';

    this.allFixtures.forEach(f => {
      const homeScore = f.homeScore ?? '-';
      const awayScore = f.awayScore ?? '-';
      csvContent += `"${f.groupName || 'Fixture'}","${f.homeTeamName || 'TBD'}",${homeScore},${awayScore},"${f.awayTeamName || 'TBD'}","${f.status || 'Scheduled'}"\n`;
    });

    const encodedUri = encodeURI(csvContent);
    const link = document.createElement('a');
    link.setAttribute('href', encodedUri);
    link.setAttribute('download', `${(this.tournament?.name || 'Tournament').replace(/\s+/g, '_')}_Fixtures.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }

  printSchedule() {
    window.print();
  }

  // Fixture score editing
  editingFixtureId: number | null = null;
  editHomeScore: number = 0;
  editAwayScore: number = 0;
  isSavingScore: boolean = false;

  startEditingFixture(fixture: any, event?: Event) {
    if (event) event.stopPropagation();
    this.editingFixtureId = fixture.fixtureId;
    this.editHomeScore = fixture.homeScore ?? 0;
    this.editAwayScore = fixture.awayScore ?? 0;
    this.cdr.markForCheck();
  }

  cancelEditingFixture() {
    this.editingFixtureId = null;
    this.cdr.markForCheck();
  }

  saveFixtureResult(fixtureId: number) {
    if (this.editHomeScore < 0 || this.editAwayScore < 0) return;
    this.isSavingScore = true;
    this.cdr.markForCheck();

    this.tournamentService.updateFixtureResult(fixtureId, this.editHomeScore, this.editAwayScore).subscribe({
      next: () => {
        this.isSavingScore = false;
        this.editingFixtureId = null;
        this.loadTournamentData();
      },
      error: (err) => {
        this.isSavingScore = false;
        this.cdr.markForCheck();
      }
    });
  }

  // Knockout generation
  isGeneratingKnockout = false;
  knockoutGenerateError: string | null = null;
  knockoutGenerateSuccess: string | null = null;

  generateKnockout() {
    this.isGeneratingKnockout = true;
    this.knockoutGenerateError = null;
    this.knockoutGenerateSuccess = null;
    this.cdr.markForCheck();

    this.tournamentService.generateKnockoutFromGroups(this.tournamentId).subscribe({
      next: () => {
        this.isGeneratingKnockout = false;
        this.knockoutGenerateSuccess = '🏆 Knockout stage generated! Check the Knockout Stage tab.';
        this.loadTournamentData();
      },
      error: (err) => {
        this.isGeneratingKnockout = false;
        this.knockoutGenerateError = err?.error?.message || 'Failed to generate knockout stage.';
        this.cdr.markForCheck();
      }
    });
  }

  // Stats Editing
  editingStatsFixture: any = null;
  homePlayers: any[] = [];
  awayPlayers: any[] = [];
  allPlayers: any[] = [];
  isLoadingPlayers = false;
  statsError: string | null = null;

  statsForm: UpdateFixtureStatsDto = {
    goals: [],
    motmPlayerId: undefined
  };

  isSavingStats = false;
  statsSaved = false;

  getPlayerName(p: any): string {
    return p.fullName || p.FullName || p.playerName || p.name || `Player #${p.playerId || p.id}`;
  }

  get maxHomeGoals(): number {
    return this.editingStatsFixture?.homeScore ?? 0;
  }

  get maxAwayGoals(): number {
    return this.editingStatsFixture?.awayScore ?? 0;
  }

  get currentHomeGoalsCount(): number {
    return (this.statsForm.goals || []).filter(g => g.isHomeSide).length;
  }

  get currentAwayGoalsCount(): number {
    return (this.statsForm.goals || []).filter(g => !g.isHomeSide).length;
  }

  get canAddGoal(): boolean {
    return (
      this.currentHomeGoalsCount < this.maxHomeGoals ||
      this.currentAwayGoalsCount < this.maxAwayGoals
    );
  }

  openStatsModal(fixture: any, event?: Event) {
    if (event) event.stopPropagation();
    this.editingStatsFixture = fixture;
    this.statsForm = { goals: [], motmPlayerId: undefined };
    this.homePlayers = [];
    this.awayPlayers = [];
    this.allPlayers = [];
    this.isLoadingPlayers = true;
    this.statsError = null;
    this.statsSaved = false;
    this.cdr.markForCheck();

    this.tournamentService.getFixtureById(fixture.fixtureId).subscribe({
      next: (res: any) => {
        const detail = res?.data || res;
        if (detail) {
          this.homePlayers = (detail.homePlayers || []).map((p: any) => ({
            id: p.playerId ?? p.PlayerId,
            playerId: p.playerId ?? p.PlayerId,
            fullName: p.fullName ?? p.FullName ?? 'Player',
            primaryPosition: p.primaryPosition ?? p.PrimaryPosition ?? 'FWD'
          }));

          this.awayPlayers = (detail.awayPlayers || []).map((p: any) => ({
            id: p.playerId ?? p.PlayerId,
            playerId: p.playerId ?? p.PlayerId,
            fullName: p.fullName ?? p.FullName ?? 'Player',
            primaryPosition: p.primaryPosition ?? p.PrimaryPosition ?? 'FWD'
          }));

          if (this.homePlayers.length === 0) {
            this.homePlayers = Array.from({ length: 11 }, (_, i) => ({
              id: -(i + 1),
              playerId: -(i + 1),
              fullName: `${fixture.homeTeamName || 'Home'} Player #${i + 1}`,
              primaryPosition: 'FWD'
            }));
          }

          if (this.awayPlayers.length === 0) {
            this.awayPlayers = Array.from({ length: 11 }, (_, i) => ({
              id: -(i + 100),
              playerId: -(i + 100),
              fullName: `${fixture.awayTeamName || 'Away'} Player #${i + 1}`,
              primaryPosition: 'FWD'
            }));
          }

          this.updateAllPlayers();

          this.statsForm = {
            goals: (detail.goals || []).map((g: any) => ({
              playerId: g.playerId ?? g.PlayerId ?? 0,
              assistPlayerId: g.assistPlayerId ?? g.AssistPlayerId ?? undefined,
              minute: g.minute ?? g.Minute ?? 1,
              isHomeSide: g.isHomeSide ?? g.IsHomeSide ?? true
            })),
            motmPlayerId: detail.motmPlayerId || undefined
          };

          this.isLoadingPlayers = false;
          this.cdr.markForCheck();
        }
      },
      error: (err) => {
        console.error('Failed to load fixture details', err);
        this.isLoadingPlayers = false;
        this.cdr.markForCheck();
      }
    });
  }

  private normalizePlayers(players: any[]) {
    return (players || []).map(p => ({
      id: p.playerId ?? p.PlayerId ?? p.id ?? p.Id ?? 0,
      playerId: p.playerId ?? p.PlayerId ?? p.id ?? p.Id ?? 0,
      fullName: p.fullName ?? p.FullName ?? p.playerName ?? p.PlayerName ?? p.name ?? 'Player',
      primaryPosition: p.primaryPosition ?? p.PrimaryPosition ?? 'FWD',
      overallRating: p.overallRating ?? p.OverallRating ?? 0
    }));
  }

  private updateAllPlayers() {
    this.allPlayers = [...this.homePlayers, ...this.awayPlayers];
  }

  closeStatsModal() {
    this.editingStatsFixture = null;
  }

  addGoal() {
    if (!this.canAddGoal) {
      this.statsError = `Cannot add more goals. Score limit reached (${this.editingStatsFixture?.homeTeamName}: ${this.maxHomeGoals}, ${this.editingStatsFixture?.awayTeamName}: ${this.maxAwayGoals}).`;
      return;
    }
    this.statsError = null;

    const isHome = this.currentHomeGoalsCount < this.maxHomeGoals;
    const defaultPlayer = isHome
      ? (this.homePlayers[0] || this.allPlayers[0])
      : (this.awayPlayers[0] || this.allPlayers[0]);

    this.statsForm.goals.push({
      playerId: defaultPlayer ? (defaultPlayer.playerId ?? defaultPlayer.id) : 0,
      minute: 1,
      isHomeSide: isHome
    });
  }

  removeGoal(index: number) {
    this.statsForm.goals.splice(index, 1);
    this.statsError = null;
  }

  onGoalPlayerChange(goal: GoalEventDto) {
    const id = goal.playerId;
    goal.isHomeSide = this.homePlayers.some(p => (p.playerId ?? p.id) === id);
    this.statsError = null;
  }

  saveStats() {
    if (!this.editingStatsFixture) return;

    // Check goal score limit before saving
    if (this.currentHomeGoalsCount > this.maxHomeGoals) {
      this.statsError = `Home team has ${this.currentHomeGoalsCount} goals assigned, but match score was ${this.maxHomeGoals}.`;
      return;
    }
    if (this.currentAwayGoalsCount > this.maxAwayGoals) {
      this.statsError = `Away team has ${this.currentAwayGoalsCount} goals assigned, but match score was ${this.maxAwayGoals}.`;
      return;
    }

    this.isSavingStats = true;
    this.statsError = null;
    this.cdr.markForCheck();
    this.tournamentService.updateFixtureStats(this.editingStatsFixture.fixtureId, this.statsForm).subscribe({
      next: () => {
        this.isSavingStats = false;
        this.statsSaved = true;
        this.cdr.markForCheck();
        setTimeout(() => {
          this.closeStatsModal();
          this.loadTournamentData();
        }, 1200);
      },
      error: (err: any) => {
        this.isSavingStats = false;
        this.statsError = err?.error?.message || err?.message || 'Failed to save stats. Please try again.';
        this.cdr.markForCheck();
      }
    });
  }
}
