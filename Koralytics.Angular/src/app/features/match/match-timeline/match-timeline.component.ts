import { Component, Input, Output, EventEmitter, inject, ChangeDetectorRef, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MiniPlayerCardModel } from '../../../../core/models/Player/mini-player-card-model';
import { MatchTimelineEventsComponent } from './match-timeline-events.component';
import { MatchLineupsComponent } from './match-lineups.component';
import { TimelineEvent } from './match-timeline.types';
import { MatchService } from '../../../../core/services/match/match.service';
import { PlayerCardService } from '../../../../core/services/player/player-card.service';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { ToastService } from '../../../../core/services/Toast/toast';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { MatchSignalrService } from '../../../../core/services/match-signalr.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-match-timeline',
  standalone: true,
  imports: [CommonModule, MatchTimelineEventsComponent, MatchLineupsComponent, EmptyStateComponent, CustomButtonComponent, ConfirmDialogComponent],
  templateUrl: './match-timeline.component.html',
  styleUrls: ['./match-timeline.component.css']
})
export class MatchTimelineComponent implements OnInit, OnDestroy {
  private matchService = inject(MatchService);
  private playerCardService = inject(PlayerCardService);
  private toastService = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);
  private signalrService = inject(MatchSignalrService);

  private signalrSub?: Subscription;

  @Input() matchId!: number;
  @Input() matchInfo!: {
    homeTeam: string;
    awayTeam: string;
    homeAcademy?: string;
    awayAcademy?: string;
    homeScore: number;
    awayScore: number;
    homePenaltyScore?: number | null;
    awayPenaltyScore?: number | null;
    matchDate?: string | Date | null;
    status: string;
    homeTeamId: number;
    awayTeamId: number;
    formation?: string;
    awayFormation?: string;
    type?: string
  };
  @Input() mockTimelineEvents?: TimelineEvent[];
  @Input() canLogEvents: boolean = false;
  @Input() canStartMatch: boolean = false;

  @Output() eventLogged = new EventEmitter<void>();

  selectedTab: 'timeline' | 'lineups' = 'timeline';

  timelineEvents: TimelineEvent[] = [];
  eventsLoading = false;
  eventsLoaded = false;
  isStartingMatch = false;
  isStartMatchDialogOpen = false;
  isEndingMatch = false;
  isEndMatchDialogOpen = false;

  homeStarters: MiniPlayerCardModel[][] = [];
  homeBench: MiniPlayerCardModel[] = [];

  awayStarters: MiniPlayerCardModel[][] = [];
  awayBench: MiniPlayerCardModel[] = [];

  lineupsLoading = false;
  lineupsLoaded = false;

  ngOnInit(): void {
    this.loadEvents();
    this.loadLineups();
    this.subscribeToLiveEvents();
  }

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }

  private subscribeToLiveEvents(): void {
    this.signalrSub = this.signalrService.matchEventUpdate$.subscribe(update => {
      if (update.matchId === this.matchId) {
        setTimeout(() => {
          this.loadEvents();
        }, 500);
      }
    });
  }

  get areBothLineupsSubmitted(): boolean {
    return this.homeStarters.length > 0 && this.awayStarters.length > 0;
  }

  get isLive(): boolean {
    return (this.matchInfo?.status || '').toString().toLowerCase() === 'live';
  }

  get isCompleted(): boolean {
    const s = (this.matchInfo?.status || '').toString().toLowerCase();
    return s === 'completed' || s === 'finished';
  }

  get isScheduled(): boolean {
    return (this.matchInfo?.status || '').toString().toLowerCase() === 'scheduled';
  }

  get trackTransform(): string {
    return this.selectedTab === 'timeline' ? 'translateX(0%)' : 'translateX(-50%)';
  }

  onStartMatchClick(): void {
    this.isStartMatchDialogOpen = true;
  }

  confirmStartMatch(): void {
    this.isStartMatchDialogOpen = false;

    this.isStartingMatch = true;
    this.matchService.startMatch(this.matchId).subscribe({
      next: () => {
        this.isStartingMatch = false;
        if (this.matchInfo) {
          this.matchInfo.status = 'Live';
        }
        this.canLogEvents = true;
        this.canStartMatch = false;
        this.toastService.show('Match started successfully! The match is now Live.', 'success');
        this.refresh();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isStartingMatch = false;
        const msg = err?.error?.detail || err?.error?.message || 'Failed to start match.';
        this.toastService.show(msg, 'error');
        this.cdr.detectChanges();
      }
    });
  }

  onEndMatchClick(): void {
    this.isEndMatchDialogOpen = true;
  }

  confirmEndMatch(): void {
    this.isEndMatchDialogOpen = false;
    this.isEndingMatch = true;
    this.matchService.endMatch(this.matchId).subscribe({
      next: () => {
        this.isEndingMatch = false;
        if (this.matchInfo) {
          this.matchInfo.status = 'Completed';
        }
        this.canLogEvents = false;
        this.toastService.show('Match ended successfully!', 'success');
        this.eventLogged.emit();
        this.refresh();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isEndingMatch = false;
        const msg = err?.error?.detail || err?.error?.message || 'Failed to end match.';
        this.toastService.show(msg, 'error');
        this.cdr.detectChanges();
      }
    });
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

  public refresh(): void {
    this.eventsLoaded = false;
    this.lineupsLoaded = false;

    if (this.matchId) {
      this.matchService.getMatch(this.matchId).subscribe({
        next: (res) => {
          const m = res.data ?? res;
          if (m && this.matchInfo) {
            this.matchInfo.homeScore = m.homeScore ?? m.HomeScore ?? this.matchInfo.homeScore;
            this.matchInfo.awayScore = m.awayScore ?? m.AwayScore ?? this.matchInfo.awayScore;
            this.matchInfo.homePenaltyScore = m.homePenaltyScore ?? m.HomePenaltyScore ?? this.matchInfo.homePenaltyScore;
            this.matchInfo.awayPenaltyScore = m.awayPenaltyScore ?? m.AwayPenaltyScore ?? this.matchInfo.awayPenaltyScore;
            this.matchInfo.status = m.status ?? m.Status ?? this.matchInfo.status;
            this.cdr.detectChanges();
          }
        }
      });
    }

    this.loadEvents();
    this.loadLineups();
  }

  public loadEvents(): void {
    this.eventsLoading = true;
    this.matchService.getMatchTimeline(this.matchId).subscribe({
      next: (res) => {
        const events: any[] = res.data?.events ?? [];
        const hasPenaltyShootoutEvent = events.some((e: any) =>
          e.eventType === 'PenaltyScored' || e.eventType === 'PenaltyMissed'
        );
        if (hasPenaltyShootoutEvent && this.matchInfo) {
          (this.matchInfo as any).hasPenaltyShootout = true;
        }
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
          },
          assistPlayerId: e.assistPlayerId
        }));
        this.eventsLoaded = true;
        this.eventsLoading = false;
        this.attachEventsToLineups();
        this.cdr.detectChanges();
      },
      error: () => {
        this.eventsLoading = false;
        this.eventsLoaded = true;
        this.cdr.detectChanges();
      }
    });
  }

  public loadLineups(): void {
    this.lineupsLoading = true;
    this.matchService.getLineup(this.matchId).subscribe({
      next: (res) => {
        const data = res.data ?? res;
        const lineups: any[] = data.lineup ?? data.Lineup ?? (Array.isArray(data) ? data : []);

        const homePlayers = lineups.filter((l: any) =>
          l.teamId === this.matchInfo.homeTeamId || l.isHomeSide === true
        );
        const awayPlayers = lineups.filter((l: any) =>
          l.teamId === this.matchInfo.awayTeamId || l.isHomeSide === false
        );

        this.homeStarters = this.buildStartersMatrix(homePlayers, this.matchInfo.formation || '4-3-3');
        this.homeBench = this.buildBenchList(homePlayers);

        this.awayStarters = this.buildStartersMatrix(awayPlayers, this.matchInfo.awayFormation || '4-3-3');
        this.awayBench = this.buildBenchList(awayPlayers);

        this.lineupsLoaded = true;
        this.lineupsLoading = false;
        this.attachEventsToLineups();
        this.cdr.detectChanges();
      },
      error: () => {
        this.lineupsLoading = false;
        this.lineupsLoaded = true;
        this.cdr.detectChanges();
      }
    });
  }

  private buildStartersMatrix(players: any[], formationStr: string): MiniPlayerCardModel[][] {
    const starters = players.filter((p: any) => p.isStarting || p.IsStarting);
    const parsed = formationStr.split('-').map(n => parseInt(n, 10)).filter(n => !isNaN(n));
    const lines = parsed.length > 0 ? parsed : [4, 3, 3];

    const matrix: MiniPlayerCardModel[][] = [];
    let idx = 0;

    const gk = starters.find((p: any) => (p.positionInMatch || p.PositionInMatch || '').toUpperCase() === 'GK');
    if (gk) {
      matrix.push([this.toCardModel(gk)]);
    } else if (starters.length > 0) {
      matrix.push([this.toCardModel(starters[0])]);
      idx = 1;
    }

    const fieldStarters = gk ? starters.filter((p: any) => p !== gk) : starters.slice(idx);
    let fieldIdx = 0;

    for (const count of lines) {
      const row: MiniPlayerCardModel[] = [];
      for (let c = 0; c < count && fieldIdx < fieldStarters.length; c++) {
        row.push(this.toCardModel(fieldStarters[fieldIdx++]));
      }
      if (row.length > 0) matrix.push(row);
    }

    while (fieldIdx < fieldStarters.length) {
      if (matrix.length > 1) {
        matrix[matrix.length - 1].push(this.toCardModel(fieldStarters[fieldIdx++]));
      } else {
        matrix.push([this.toCardModel(fieldStarters[fieldIdx++])]);
      }
    }

    return matrix;
  }

  private attachEventsToLineups(): void {
    if (!this.eventsLoaded || !this.lineupsLoaded) return;
    
    // 1. Group events by player Id
    const playerEvents = new Map<number, { [type: string]: number }>();
    for (const e of this.timelineEvents) {
      const pId = e.player?.playerId;
      if (pId) {
        if (!playerEvents.has(pId)) playerEvents.set(pId, {});
        const evMap = playerEvents.get(pId)!;
        evMap[e.rawType] = (evMap[e.rawType] || 0) + 1;
      }
      
      const aId = e.assistPlayerId;
      if (aId) {
        if (!playerEvents.has(aId)) playerEvents.set(aId, {});
        const evMap = playerEvents.get(aId)!;
        evMap['Assist'] = (evMap['Assist'] || 0) + 1;
      }
    }

    // 2. Attach to players
    const attach = (players: MiniPlayerCardModel[]) => {
      for (const p of players) {
        const evMap = playerEvents.get(p.playerId);
        if (evMap) {
          p.matchEvents = Object.keys(evMap).map(type => ({
            type,
            count: evMap[type]
          }));
        } else {
          p.matchEvents = [];
        }
      }
    };

    attach(this.homeBench);
    attach(this.awayBench);
    this.homeStarters.forEach(row => attach(row));
    this.awayStarters.forEach(row => attach(row));
  }

  private buildBenchList(players: any[]): MiniPlayerCardModel[] {
    const subs = players.filter((p: any) => !p.isStarting && !p.IsStarting);
    return subs.map((p: any) => this.toCardModel(p));
  }

  private toCardModel(p: any): MiniPlayerCardModel {
    return {
      playerId: p.playerId ?? p.PlayerId ?? 0,
      fullName: p.playerName ?? p.PlayerName ?? p.playerFullName ?? 'Player',
      position: p.positionInMatch ?? p.PositionInMatch ?? 'SUB',
      profileImageUrl: p.profileImageUrl ?? p.ProfileImageUrl ?? null,
      overallRating: p.overallRating ?? p.OverallRating ?? 0
    };
  }

  private formatEventType(type: string): string {
    switch (type) {
      case 'Goal': return 'Solo Goal';
      case 'OwnGoal': return 'Own Goal';
      case 'PenaltyScored': return 'Pen Scored';
      case 'PenaltyMissed': return 'Pen Missed';
      case 'Substitution': return 'Substitution';
      case 'YellowCard': return 'Yellow Card';
      case 'RedCard': return 'Red Card';
      default: return type;
    }
  }

  private eventAccentColor(type: string): string {
    switch (type) {
      case 'Goal': return '#10b981';
      case 'OwnGoal': return '#f43f5e';
      case 'PenaltyScored': return '#10b981';
      case 'PenaltyMissed': return '#f43f5e';
      case 'Substitution': return '#38bdf8';
      case 'YellowCard': return '#eab308';
      case 'RedCard': return '#ef4444';
      default: return '#82f768';
    }
  }
}
