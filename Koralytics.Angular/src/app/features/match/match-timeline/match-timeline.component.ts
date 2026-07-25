import { Component, Input, inject, ChangeDetectorRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MiniPlayerCardModel } from '../../../../core/models/Player/mini-player-card-model';
import { MatchTimelineEventsComponent } from './match-timeline-events.component';
import { MatchLineupsComponent } from './match-lineups.component';
import { TimelineEvent } from './match-timeline.types';
import { MatchService } from '../../../../core/services/match/match.service';
import { PlayerCardService } from '../../../../core/services/player/player-card.service';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';

@Component({
  selector: 'app-match-timeline',
  standalone: true,
  imports: [CommonModule, MatchTimelineEventsComponent, MatchLineupsComponent, EmptyStateComponent],
  templateUrl: './match-timeline.component.html',
  styleUrls: ['./match-timeline.component.css']
})
export class MatchTimelineComponent implements OnInit {
  private matchService = inject(MatchService);
  private playerCardService = inject(PlayerCardService);
  private cdr = inject(ChangeDetectorRef);

  @Input() matchId!: number;
  @Input() matchInfo!: { homeTeam: string; awayTeam: string; homeAcademy?: string; awayAcademy?: string; homeScore: number; awayScore: number; status: string; homeTeamId: number; awayTeamId: number; formation?: string; awayFormation?: string };
  @Input() mockTimelineEvents?: TimelineEvent[];

  selectedTab: 'timeline' | 'lineups' = 'timeline';

  timelineEvents: TimelineEvent[] = [];
  eventsLoading = false;
  eventsLoaded = false;

  homeStarters: MiniPlayerCardModel[][] = [];
  homeBench: MiniPlayerCardModel[] = [];
  awayStarters: MiniPlayerCardModel[][] = [];
  awayBench: MiniPlayerCardModel[] = [];
  lineupsLoading = false;
  lineupsLoaded = false;

  get formationHome(): MiniPlayerCardModel[][] {
    return this.homeStarters;
  }

  get formationAway(): MiniPlayerCardModel[][] {
    return this.awayStarters;
  }

  get trackTransform(): string {
    return this.selectedTab === 'timeline' ? 'translateX(0%)' : 'translateX(-50%)';
  }

  get displayHomeFormation(): string {
    if (this.matchInfo?.formation) return this.matchInfo.formation;
    return this.computeFormationFromStarters(this.homeStarters);
  }

  get displayAwayFormation(): string {
    if (this.matchInfo?.awayFormation) return this.matchInfo.awayFormation;
    return this.computeFormationFromStarters(this.awayStarters);
  }

  get isLive(): boolean {
    return this.matchInfo?.status === 'Live';
  }

  get isCompleted(): boolean {
    return this.matchInfo?.status === 'Completed';
  }

  ngOnInit(): void {
    this.selectTab('timeline');
  }

  selectTab(tab: 'timeline' | 'lineups'): void {
    this.selectedTab = tab;
    if (tab === 'timeline' && !this.eventsLoaded) {
      if (this.mockTimelineEvents?.length) {
        this.timelineEvents = this.mockTimelineEvents;
        this.eventsLoaded = true;
        this.cdr.detectChanges();
      } else {
        this.loadEvents();
      }
    }
    if (tab === 'lineups' && !this.lineupsLoaded) this.loadLineups();
  }

  private loadEvents(): void {
    this.eventsLoading = true;
    this.matchService.getMatchTimeline(this.matchId).subscribe({
      next: (res) => {
        const events: any[] = res.data?.events ?? [];
        this.timelineEvents = events.map((e: any) => ({
          minute: e.minute,
          eventType: this.formatEventType(e.eventType),
          eventSubtext: e.assistPlayerName ? `Assist: ${e.assistPlayerName}` : '',
          rawType: e.eventType,
          side: e.isHomeSide === true ? 'home' :
                e.isHomeSide === false ? 'away' :
                e.teamId === this.matchInfo.homeTeamId ? 'home' : 'away',
          accentColor: this.eventAccentColor(e.eventType),
          player: {
            playerId: e.playerId,
            fullName: e.playerName,
            position: '',
            profileImageUrl: null,
            overallRating: 0
          }
        }));
        this.eventsLoaded = true;
        this.eventsLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.eventsLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private loadLineups(): void {
    this.lineupsLoading = true;
    this.matchService.getLineup(this.matchId).subscribe({
      next: (res) => {
        const lineups: any[] = res.data ?? [];
        const isSession = this.matchInfo.homeTeamId === this.matchInfo.awayTeamId;

        const homeStarting = isSession
          ? lineups.filter((l: any) => l.isHomeSide && l.isStarting)
          : lineups.filter((l: any) => l.teamId === this.matchInfo.homeTeamId && l.isStarting);
        const homeSitting = isSession
          ? lineups.filter((l: any) => l.isHomeSide && !l.isStarting)
          : lineups.filter((l: any) => l.teamId === this.matchInfo.homeTeamId && !l.isStarting);
        const awayStarting = isSession
          ? lineups.filter((l: any) => l.isHomeSide === false && l.isStarting)
          : lineups.filter((l: any) => l.teamId === this.matchInfo.awayTeamId && l.isStarting);
        const awaySitting = isSession
          ? lineups.filter((l: any) => l.isHomeSide === false && !l.isStarting)
          : lineups.filter((l: any) => l.teamId === this.matchInfo.awayTeamId && !l.isStarting);

        const allPlayerIds = [...new Set(lineups.map((l: any) => l.playerId))] as number[];

        if (allPlayerIds.length > 0) {
          this.playerCardService.getMiniPlayerCards(allPlayerIds).subscribe({
            next: (cards) => {
              const cardMap = new Map(cards.filter(Boolean).map(c => [c!.playerId, c!]));
              this.homeStarters = this.buildFormation(homeStarting, cardMap);
              this.homeBench = this.buildPlayers(homeSitting, cardMap);
              this.awayStarters = this.buildFormation(awayStarting, cardMap);
              this.awayBench = this.buildPlayers(awaySitting, cardMap);
              this.lineupsLoaded = true;
              this.lineupsLoading = false;
              this.cdr.detectChanges();
            },
            error: () => {
              this.lineupsLoading = false;
              this.cdr.detectChanges();
            }
          });
        } else {
          this.lineupsLoaded = true;
          this.lineupsLoading = false;
          this.cdr.detectChanges();
        }
      },
      error: () => {
        this.lineupsLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private buildFormation(lineup: any[], cardMap: Map<number, MiniPlayerCardModel>): MiniPlayerCardModel[][] {
    const players = this.buildPlayers(lineup, cardMap);
    if (players.length === 0) return [[]];

    const gk = players.filter(p => p.position?.toUpperCase() === 'GK');
    const def = players.filter(p => { const pos = p.position?.toUpperCase() ?? ''; return pos.includes('B') && pos !== 'GK'; });
    const mid = players.filter(p => { const pos = p.position?.toUpperCase() ?? ''; return pos.includes('M') && !pos.includes('B'); });
    const att = players.filter(p => { const pos = p.position?.toUpperCase() ?? ''; return !pos.includes('B') && !pos.includes('M') && pos !== 'GK'; });

    const rows: MiniPlayerCardModel[][] = [];
    if (att.length > 0) rows.push(att);
    if (mid.length > 0) rows.push(mid);
    if (def.length > 0) rows.push(def);
    if (gk.length > 0) rows.push(gk);

    if (rows.length === 0) {
      const count = players.length;
      if (count === 11) return [[players[0], players[1], players[2]], [players[3], players[4], players[5]], [players[6], players[7], players[8], players[9]], [players[10]]];
      if (count === 7) return [[players[0], players[1]], [players[2], players[3], players[4]], [players[5]], [players[6]]];
      if (count === 5) return [[players[0]], [players[1], players[2]], [players[3]], [players[4]]];
      return players.length > 0 ? [players] : [];
    }

    rows.forEach(row => {
      row.sort((a, b) => this.positionHOrder(a.position) - this.positionHOrder(b.position));
      const camIdx = row.findIndex(p => p.position?.toUpperCase() === 'CAM');
      if (camIdx !== -1) {
        const mid = Math.floor((row.length - 1) / 2);
        if (camIdx !== mid) {
          const [cam] = row.splice(camIdx, 1);
          row.splice(mid, 0, cam);
        }
      }
    });
    return rows;
  }

  private positionHOrder(pos?: string): number {
    if (!pos) return 1;
    const c = pos.charAt(0).toUpperCase();
    if (c === 'L') return 0;
    if (c === 'R') return 2;
    return 1;
  }

  private buildPlayers(lineup: any[], cardMap: Map<number, MiniPlayerCardModel>): MiniPlayerCardModel[] {
    return lineup.map((l: any) => {
      const card = cardMap.get(l.playerId);
      return card
        ? { ...card, position: l.positionInMatch ?? card.position }
        : { playerId: l.playerId, fullName: l.playerName, position: l.positionInMatch ?? '', profileImageUrl: null, overallRating: 0 };
    });
  }

  private formatEventType(type: string): string {
    switch (type) {
      case 'Goal': return 'Goal';
      case 'YellowCard': return 'Yellow Card';
      case 'RedCard': return 'Red Card';
      case 'Substitution': return 'Substitution';
      case 'OwnGoal': return 'Own Goal';
      case 'PenaltyScored': return 'Penalty Scored';
      case 'PenaltyMissed': return 'Penalty Missed';
      case 'CleanSheet': return 'Clean Sheet';
      default: return type;
    }
  }

  private eventAccentColor(type: string): string {
    switch (type) {
      case 'Goal':
      case 'PenaltyScored': return '#22c55e';
      case 'YellowCard': return '#facc15';
      case 'RedCard':
      case 'OwnGoal':
      case 'PenaltyMissed': return '#f43f5e';
      case 'Substitution': return '#82f768';
      case 'CleanSheet': return '#3b82f6';
      default: return '#facc15';
    }
  }

  private computeFormationFromStarters(formationRows: MiniPlayerCardModel[][]): string {
    const all = formationRows.flat();
    const def = all.filter(p => { const pos = p.position?.toUpperCase() ?? ''; return pos.includes('B'); });
    const mid = all.filter(p => { const pos = p.position?.toUpperCase() ?? ''; return pos.includes('M') && !pos.includes('B'); });
    const att = all.filter(p => { const pos = p.position?.toUpperCase() ?? ''; return !pos.includes('B') && !pos.includes('M') && pos !== 'GK'; });
    if (def.length + mid.length + att.length === 0) return '';
    return `${def.length}-${mid.length}-${att.length}`;
  }
}
