import { Component, Input, Output, EventEmitter, inject, ChangeDetectorRef, OnInit, OnDestroy, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MiniPlayerCardModel } from '../../../../core/models/Player/mini-player-card-model';
import { MatchTimelineEventsComponent } from './match-timeline-events.component';
import { MatchLineupsComponent } from './match-lineups.component';
import { MatchRatingsComponent } from './match-ratings.component';
import { MatchH2hComponent } from './match-h2h.component';
import { MatchAnalysisComponent } from './match-analysis.component';
import { TimelineEvent } from './match-timeline.types';
import { MatchService } from '../../../../core/services/match/match.service';
import { PlayerCardService } from '../../../../core/services/player/player-card.service';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { ToastService } from '../../../../core/services/Toast/toast';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { MatchSignalrService } from '../../../../core/services/match-signalr.service';
import { Subscription } from 'rxjs';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { NotificationService } from '../../../../core/services/SignalR/notificationservice';
import { RouterLink } from '@angular/router';
import { MarqueeIfOverflowDirective } from './marquee-if-overflow.directive';
import { TranslatePipe, TranslateService, LangChangeEvent } from '@ngx-translate/core';

@Component({
  selector: 'app-match-timeline',
  standalone: true,
  imports: [CommonModule, RouterLink, MatchTimelineEventsComponent, MatchLineupsComponent, MatchRatingsComponent, MatchH2hComponent, MatchAnalysisComponent, EmptyStateComponent, CustomButtonComponent, ConfirmDialogComponent, LoadingSpinnerComponent, MarqueeIfOverflowDirective, TranslatePipe],
  templateUrl: './match-timeline.component.html',
  styleUrls: ['./match-timeline.component.css']
})
export class MatchTimelineComponent implements OnInit, OnDestroy, OnChanges {
  private matchService = inject(MatchService);
  private playerCardService = inject(PlayerCardService);
  private toastService = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);
  private signalrService = inject(MatchSignalrService);

  private signalrSub?: Subscription;
  private notificationService = inject(NotificationService);
  private translate = inject(TranslateService);

  @Input() matchId!: number;
  @Input() matchInfo!: {
    homeTeam: string;
    awayTeam: string;
    homeAcademy?: string;
    awayAcademy?: string;
    homeAcademyId?: number | null;
    awayAcademyId?: number | null;
    homeAcademyLogoUrl?: string | null;
    awayAcademyLogoUrl?: string | null;
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
    type?: string;
    tournamentId?: number | null;
    tournamentName?: string | null;
  };
  @Input() mockTimelineEvents?: TimelineEvent[];
  @Input() canLogEvents: boolean = false;
  @Input() canStartMatch: boolean = false;
  @Input() canSubmitRatings: boolean = false;

  @Output() eventLogged = new EventEmitter<void>();

  homeLogoError = false;
  awayLogoError = false;

  getInitials(name?: string): string {
    if (!name) return 'KA';
    const parts = name.trim().split(' ');
    if (parts.length >= 2 && parts[0] && parts[1]) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  }

  selectedTab: 'timeline' | 'lineups' | 'h2h' | 'ratings' | 'analysis' = 'timeline';
  hasUserSelectedTab = false;

  timelineEvents: TimelineEvent[] = [];
  eventsLoading = false;
  eventsLoaded = false;
  ratingsInitialized = false;
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
  h2hInitialized = false;
  analysisInitialized = false;

  private signalrEventSub?: Subscription;
  private signalrScoreSub?: Subscription;
  private signalrDeletedSub?: Subscription;
  private langChangeSub?: Subscription;

  ngOnInit(): void {
    const status = (this.matchInfo?.status || '').toString().toLowerCase();
    if (status === 'scheduled') {
      this.selectedTab = 'lineups';
    }
    this.selectTab(this.selectedTab);

    if (this.canStartMatch && !this.lineupsLoaded && !this.lineupsLoading) {
      this.loadLineups();
    }

    this.subscribeToLiveEvents();

    this.langChangeSub = this.translate.onLangChange.subscribe((event: LangChangeEvent) => {
      if (this.timelineEvents && this.timelineEvents.length > 0) {
        this.retranslateEvents();
      }
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['matchInfo'] && changes['matchInfo'].currentValue) {
      const status = (this.matchInfo?.status || '').toString().toLowerCase();
      if (status === 'scheduled' && !this.hasUserSelectedTab && this.selectedTab !== 'lineups') {
        this.selectedTab = 'lineups';
        this.selectTab('lineups');
      }
    }
    if (changes['canStartMatch'] && this.canStartMatch && !this.lineupsLoaded && !this.lineupsLoading) {
      this.loadLineups();
    }
  }

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
    if (this.signalrEventSub) {
      this.signalrEventSub.unsubscribe();
    }
    if (this.signalrScoreSub) {
      this.signalrScoreSub.unsubscribe();
    }
    if (this.signalrDeletedSub) {
      this.signalrDeletedSub.unsubscribe();
    }
    if (this.langChangeSub) {
      this.langChangeSub.unsubscribe();
    }
  }

  private subscribeToLiveEvents(): void {
    // New event added → append at end to preserve chronological (minute-asc) order
    this.signalrEventSub = this.signalrService.matchEventUpdate$.subscribe(update => {
      // Use loose equality (==) to handle potential string/number type mismatch from SignalR
      // eslint-disable-next-line eqeqeq
      if (update.matchId == this.matchId && update.event) {
        const raw = update.event;
        const eventId = raw.id ?? raw.Id ?? raw.eventId ?? raw.EventId ?? 0;
        if (!eventId) return; // guard: skip events with no ID

        const exists = this.timelineEvents.some(evt => !!evt.id && evt.id === eventId);
        if (!exists) {
          const newEvt = this.mapSingleEventToTimelineEvent(raw);
          // Append at the end — API returns events ordered by minute asc (oldest top, newest bottom)
          this.timelineEvents = [...this.timelineEvents, newEvt];
          this.attachEventsToLineups();
          this.cdr.detectChanges();
        }
      }
    });

    // Event deleted/disallowed → remove from list for ALL viewers in real-time
    this.signalrDeletedSub = this.signalrService.matchEventDeleted$.subscribe(update => {
      // eslint-disable-next-line eqeqeq
      if (update.matchId == this.matchId && update.eventId) {
        const before = this.timelineEvents.length;
        this.timelineEvents = this.timelineEvents.filter(e => e.id !== update.eventId);
        if (this.timelineEvents.length !== before) {
          this.attachEventsToLineups();
          this.cdr.detectChanges();
        }
      }
    });

    this.signalrScoreSub = this.signalrService.matchScoreUpdate$.subscribe(update => {
      // Use loose equality (==) to handle potential string/number type mismatch from SignalR
      // eslint-disable-next-line eqeqeq
      if (update.matchId == this.matchId && this.matchInfo) {
        this.matchInfo.homeScore = update.homeScore;
        this.matchInfo.awayScore = update.awayScore;
        if (update.homePenaltyScore != null) this.matchInfo.homePenaltyScore = update.homePenaltyScore;
        if (update.awayPenaltyScore != null) this.matchInfo.awayPenaltyScore = update.awayPenaltyScore;
        if (update.status) this.matchInfo.status = update.status;
        this.cdr.detectChanges();
      }
    });
  }

  public onEventDisallowed(eventId: number): void {
    if (eventId) {
      this.timelineEvents = this.timelineEvents.filter(e => e.id !== eventId);
      this.attachEventsToLineups();
      this.cdr.detectChanges();
    }
  }

  private mapSingleEventToTimelineEvent(e: any): TimelineEvent {
    const allPlayers = [...this.allHomePlayers, ...this.allAwayPlayers];
    const foundPlayer = allPlayers.find(p => p.playerId === e.playerId);

    return {
      id: e.id ?? e.Id ?? e.eventId ?? e.EventId ?? e.matchEventId ?? e.MatchEventId ?? 0,
      minute: e.minute ?? 0,
      eventType: this.formatEventType(e.eventType),
      eventSubtext: e.assistPlayerName
        ? (e.eventType === 'Substitution' || e.rawType === 'Substitution' 
            ? `${this.translate.instant('COMMON.IN', { Default: 'In' })}: ${e.assistPlayerName}` 
            : `${this.translate.instant('MATCH.EVENT.ASSIST')}: ${e.assistPlayerName}`)
        : '',
      rawType: e.eventType,
      side: e.isHomeSide === true ? 'home' :
        e.isHomeSide === false ? 'away' :
          e.teamId === this.matchInfo?.homeTeamId ? 'home' : 'away',
      accentColor: this.eventAccentColor(e.eventType),
      player: {
        playerId: e.playerId,
        fullName: e.playerName ?? foundPlayer?.fullName ?? 'Player',
        position: foundPlayer?.position ?? foundPlayer?.naturalPosition ?? '',
        profileImageUrl: foundPlayer?.profileImageUrl ?? null,
        overallRating: foundPlayer?.overallRating ?? 0
      },
      assistPlayerId: e.assistPlayerId
    };
  }

  private retranslateEvents(): void {
    const allPlayers = [...this.allHomePlayers, ...this.allAwayPlayers];
    this.timelineEvents = this.timelineEvents.map(e => {
      let subtext = '';
      if (e.assistPlayerId) {
        const assistPlayer = allPlayers.find(p => p.playerId === e.assistPlayerId);
        const assistName = assistPlayer ? assistPlayer.fullName : '';
        if (assistName) {
          subtext = (e.rawType === 'Substitution')
            ? `${this.translate.instant('COMMON.IN', { Default: 'In' })}: ${assistName}`
            : `${this.translate.instant('MATCH.EVENT.ASSIST')}: ${assistName}`;
        }
      } else if (e.eventSubtext) {
        // Fallback if there was subtext but no assistPlayerId (e.g. from raw event mapped before)
        // This is safe since we only expect substitutions and assists to have subtext
        const isSub = e.rawType === 'Substitution';
        const parts = e.eventSubtext.split(': ');
        if (parts.length === 2) {
           subtext = isSub
             ? `${this.translate.instant('COMMON.IN', { Default: 'In' })}: ${parts[1]}`
             : `${this.translate.instant('MATCH.EVENT.ASSIST')}: ${parts[1]}`;
        }
      }

      return {
        ...e,
        eventType: this.formatEventType(e.rawType),
        eventSubtext: subtext
      };
    });
    this.cdr.detectChanges();
  }

  get areBothLineupsSubmitted(): boolean {
    return this.homeStarters.length > 0 && this.awayStarters.length > 0;
  }

  get allHomePlayers(): MiniPlayerCardModel[] {
    const starters = this.homeStarters.reduce((acc, val) => acc.concat(val), []);
    return starters.concat(this.homeBench);
  }

  get allAwayPlayers(): MiniPlayerCardModel[] {
    const starters = this.awayStarters.reduce((acc, val) => acc.concat(val), []);
    return starters.concat(this.awayBench);
  }

  get isLive(): boolean {
    return (this.matchInfo?.status || '').toString().toLowerCase() === 'live';
  }

  get isCompleted(): boolean {
    const s = (this.matchInfo?.status || '').toString().toLowerCase();
    return s === 'completed' || s === 'finished';
  }

  get isSessionMatch(): boolean {
    const t = (this.matchInfo?.type || '').toString().toLowerCase();
    return t === 'session';
  }

  get trackWidth(): string {
    return this.isSessionMatch ? '300%' : '500%';
  }

  get slidePageWidth(): string {
    return this.isSessionMatch ? '33.3333%' : '20%';
  }

  get trackTransform(): string {
    // If language is Arabic, the layout is RTL. We need positive translations to slide right.
    const isRtl = this.translate.currentLang() === 'ar';
    const sign = isRtl ? 1 : -1;

    if (this.isSessionMatch) {
      if (this.selectedTab === 'timeline') return 'translateX(0%)';
      if (this.selectedTab === 'lineups') return `translateX(${sign * 33.3333}%)`;
      return `translateX(${sign * 66.6666}%)`;
    }
    if (this.selectedTab === 'timeline') return 'translateX(0%)';
    if (this.selectedTab === 'lineups') return `translateX(${sign * 20}%)`;
    if (this.selectedTab === 'h2h') return `translateX(${sign * 40}%)`;
    if (this.selectedTab === 'analysis') return `translateX(${sign * 60}%)`;
    return `translateX(${sign * 80}%)`;
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
        this.toastService.show(this.translate.instant('MATCH.TIMELINE.TOAST_MATCH_STARTED'), 'success');
        //  TRIGGER START MATCH NOTIFICATION
        const homeTeamName = this.matchInfo?.homeTeam || 'Home Team';
        const awayTeamName = this.matchInfo?.awayTeam || 'Away Team';
        this.notificationService.triggerMatchEventNotification(
          this.matchId,
          "Match Started! ⚽",
          `${homeTeamName} vs ${awayTeamName} has officially kicked off.`,
          "MatchStarted"
        ).subscribe({
          error: (e) => {
            console.error('Failed to dispatch start match notification', e);
            const detail = e?.error?.detail || e?.error?.message || e?.message || 'Unknown error';
            this.toastService.show(this.translate.instant('MATCH.TIMELINE.TOAST_START_NOTIF_FAIL', {detail}), 'warning');
          }
        });

        this.refresh();
        this.cdr.detectChanges();

      },
      error: (err) => {
        this.isStartingMatch = false;
        const msg = err?.error?.detail || err?.error?.message || this.translate.instant('MATCH.TIMELINE.TOAST_START_FAIL');
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
        this.toastService.show(this.translate.instant('MATCH.TIMELINE.TOAST_MATCH_ENDED'), 'success');
        //TRIGGER END MATCH NOTIFICATION
        const homeTeamName = this.matchInfo?.homeTeam || 'Home Team';
        const awayTeamName = this.matchInfo?.awayTeam || 'Away Team';
        this.notificationService.triggerMatchEventNotification(
          this.matchId,
          "Full Time! 🛑",
          `The match between ${homeTeamName} and ${awayTeamName} has ended.`,
          "MatchEnded"
        ).subscribe({
          error: (e) => {
            console.error('Failed to dispatch end match notification', e);
            const detail = e?.error?.detail || e?.error?.message || e?.message || 'Unknown error';
            this.toastService.show(this.translate.instant('MATCH.TIMELINE.TOAST_END_NOTIF_FAIL', {detail}), 'warning');
          }
        });
        this.eventLogged.emit();
        this.refresh();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isEndingMatch = false;
        const msg = err?.error?.detail || err?.error?.message || this.translate.instant('MATCH.TIMELINE.TOAST_END_FAIL');
        this.toastService.show(msg, 'error');
        this.cdr.detectChanges();
      }
    });
  }

  selectTab(tab: 'timeline' | 'lineups' | 'h2h' | 'ratings' | 'analysis', isUserAction: boolean = false): void {
    if (isUserAction) {
      this.hasUserSelectedTab = true;
    }
    this.selectedTab = tab;
    if (tab === 'h2h') {
      this.h2hInitialized = true;
    }
    if (tab === 'analysis') {
      this.analysisInitialized = true;
    }
    if (tab === 'ratings') {
      this.ratingsInitialized = true;
    }
    if (tab === 'timeline' && !this.eventsLoaded && !this.eventsLoading) {
      if (this.mockTimelineEvents?.length) {
        this.timelineEvents = this.mockTimelineEvents;
        this.eventsLoaded = true;
        this.cdr.detectChanges();
      } else {
        this.loadEvents();
      }
    }
    if ((tab === 'lineups' || tab === 'ratings') && !this.lineupsLoaded && !this.lineupsLoading) {
      this.loadLineups();
    }
  }

  public refresh(): void {
    if (this.selectedTab === 'timeline') {
      this.eventsLoaded = false;
      this.loadEvents();
    } else if (this.selectedTab === 'lineups' || this.selectedTab === 'ratings') {
      this.lineupsLoaded = false;
      this.loadLineups();
    }

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

        const playerIds = Array.from(new Set(events.map((e: any) => e.playerId).filter(id => id > 0)));

        if (playerIds.length > 0) {
          this.playerCardService.getMiniPlayerCards(playerIds).subscribe({
            next: (cards: MiniPlayerCardModel[]) => {
              const rawCards = (cards as any)?.data ?? cards ?? [];
              const fetchedMap = new Map<number, MiniPlayerCardModel>();

              if (Array.isArray(rawCards)) {
                rawCards.forEach((c: any) => {
                  if (c && c.playerId > 0) {
                    fetchedMap.set(c.playerId, {
                      ...c,
                      overallRating: Math.round(c.overallRating ?? 0)
                    });
                  }
                });
              }

              this.finishLoadEvents(events, fetchedMap);
            },
            error: () => {
              this.finishLoadEvents(events, new Map());
            }
          });
        } else {
          this.finishLoadEvents(events, new Map());
        }
      },
      error: () => {
        this.eventsLoading = false;
        this.eventsLoaded = true;
        this.cdr.detectChanges();
      }
    });
  }

  private finishLoadEvents(events: any[], fetchedMap: Map<number, MiniPlayerCardModel>): void {
    this.timelineEvents = events.map((e: any) => {
      const fetched = fetchedMap.get(e.playerId);
      return {
        id: e.id ?? e.Id ?? e.eventId ?? e.EventId ?? e.matchEventId ?? e.MatchEventId ?? 0,
        minute: e.minute,
        eventType: this.formatEventType(e.eventType),
        eventSubtext: e.assistPlayerName
          ? (e.eventType === 'Substitution' || e.rawType === 'Substitution' 
              ? `${this.translate.instant('COMMON.IN', { Default: 'In' })}: ${e.assistPlayerName}` 
              : `${this.translate.instant('MATCH.EVENT.ASSIST')}: ${e.assistPlayerName}`)
          : '',
        rawType: e.eventType,
        side: e.isHomeSide === true ? 'home' :
          e.isHomeSide === false ? 'away' :
            e.teamId === this.matchInfo.homeTeamId ? 'home' : 'away',
        accentColor: this.eventAccentColor(e.eventType),
        player: {
          playerId: e.playerId,
          fullName: e.playerName,
          position: fetched?.position ?? fetched?.naturalPosition ?? '',  // real/natural position
          profileImageUrl: fetched?.profileImageUrl ?? null,
          overallRating: fetched?.overallRating ?? 0
        },
        assistPlayerId: e.assistPlayerId
      };
    });
    this.eventsLoaded = true;
    this.eventsLoading = false;
    this.attachEventsToLineups();
    this.cdr.detectChanges();
  }

  public loadLineups(): void {
    this.lineupsLoading = true;
    this.matchService.getLineup(this.matchId).subscribe({
      next: (res) => {
        const data = res.data ?? res;
        const lineups: any[] = data.lineup ?? data.Lineup ?? (Array.isArray(data) ? data : []);

        const isSession = (this.matchInfo?.type || '').toString().toLowerCase().includes('session');

        const homePlayers = lineups.filter((l: any) => {
          if (isSession) return l.isHomeSide === true;
          return l.teamId === this.matchInfo.homeTeamId || l.isHomeSide === true;
        });
        const awayPlayers = lineups.filter((l: any) => {
          if (isSession) return l.isHomeSide === false;
          return l.teamId === this.matchInfo.awayTeamId || l.isHomeSide === false;
        });

        const playerIds = lineups.map((l: any) => l.playerId ?? l.PlayerId).filter((id: number) => id > 0);

        if (playerIds.length > 0) {
          this.playerCardService.getMiniPlayerCards(playerIds).subscribe({
            next: (cards: MiniPlayerCardModel[]) => {
              const rawCards = (cards as any)?.data ?? cards ?? [];
              const fetchedMap = new Map<number, MiniPlayerCardModel>();

              if (Array.isArray(rawCards)) {
                rawCards.forEach((c: any) => {
                  if (c && c.playerId > 0) {
                    fetchedMap.set(c.playerId, {
                      ...c,
                      overallRating: Math.round(c.overallRating ?? 0)
                    });
                  }
                });
              }

              lineups.forEach((l: any) => {
                const pId = l.playerId ?? l.PlayerId;
                const fetched = fetchedMap.get(pId);

                if (fetched) {
                  l.overallRating = fetched.overallRating;
                  l.profileImageUrl = fetched.profileImageUrl;
                  // Store the player's real/natural position from mini card
                  l.naturalPosition = fetched.position;
                }
              });

              this.finishLoadLineups(homePlayers, awayPlayers);
            },
            error: () => {
              this.finishLoadLineups(homePlayers, awayPlayers);
            }
          });
        } else {
          this.finishLoadLineups(homePlayers, awayPlayers);
        }
      },
      error: () => {
        this.lineupsLoading = false;
        this.lineupsLoaded = true;
        this.cdr.detectChanges();
      }
    });
  }

  private finishLoadLineups(homePlayers: any[], awayPlayers: any[]): void {
    this.homeStarters = this.buildStartersMatrix(homePlayers, this.matchInfo.formation || '4-3-3');
    this.homeBench = this.buildBenchList(homePlayers);

    this.awayStarters = this.buildStartersMatrix(awayPlayers, this.matchInfo.awayFormation || '4-3-3');
    this.awayBench = this.buildBenchList(awayPlayers);

    this.lineupsLoaded = true;
    this.lineupsLoading = false;
    this.attachEventsToLineups();
    this.cdr.detectChanges();
  }

  private buildStartersMatrix(players: any[], formationStr: string): MiniPlayerCardModel[][] {
    const starters = players.filter((p: any) => p.isStarting || p.IsStarting);

    // Parse formation – support any format: 4-3-3, 2-2, 3-2-1, 2-3-1, etc.
    // Convention: numbers go from defense → midfield → attack (e.g. 4-3-3 = 4 DEF, 3 MID, 3 FW)
    const parsed = formationStr.split('-').map(n => parseInt(n, 10)).filter(n => !isNaN(n) && n > 0);
    const lines = parsed.length > 0 ? parsed : [4, 3, 3];

    // Separate GK from outfield players
    const gk = starters.find((p: any) => (p.positionInMatch || p.PositionInMatch || '').toUpperCase() === 'GK');
    const outfield = gk ? starters.filter((p: any) => p !== gk) : starters.slice(1);

    // Sort outfield players by compound key: (zone * 10) + lateral
    //   Zone:    0=DEF  1=CDM  2=CM/Wide-MID  3=CAM/AM  4=FW
    //   Lateral: 0=Left  1=Center  2=Right
    // This ensures correct left-right ordering within each row AND
    // correct zone ordering (e.g. CAM between CDM and FW in 4-2-3-1)
    const posKey = (pos: string): number => {
      const p = (pos || '').toUpperCase();
      switch (p) {
        // ── Defenders ──────────────────────────────
        case 'LB': return 0 * 10 + 0;
        case 'LWB': return 0 * 10 + 0;
        case 'CB': return 0 * 10 + 1;
        case 'SW': return 0 * 10 + 1;
        case 'DEF': return 0 * 10 + 1;
        case 'DF': return 0 * 10 + 1;
        case 'RB': return 0 * 10 + 2;
        case 'RWB': return 0 * 10 + 2;
        // ── Defensive Midfielders ───────────────────
        case 'CDM': return 1 * 10 + 1;
        case 'DM': return 1 * 10 + 1;
        case 'DMF': return 1 * 10 + 1;
        // ── Central / Wide Midfielders ──────────────
        case 'LM': return 2 * 10 + 0;
        case 'CM': return 2 * 10 + 1;
        case 'MID': return 2 * 10 + 1;
        case 'MF': return 2 * 10 + 1;
        case 'RM': return 2 * 10 + 2;
        // ── Attacking Midfielders ───────────────────
        case 'LAM': return 3 * 10 + 0;
        case 'CAM': return 3 * 10 + 1;
        case 'AM': return 3 * 10 + 1;
        case 'AMF': return 3 * 10 + 1;
        case 'RAM': return 3 * 10 + 2;
        // ── Forwards ───────────────────────────────
        case 'LW': return 4 * 10 + 0;
        case 'SS': return 4 * 10 + 1;
        case 'CF': return 4 * 10 + 1;
        case 'ST': return 4 * 10 + 1;
        case 'FW': return 4 * 10 + 1;
        case 'ATT': return 4 * 10 + 1;
        case 'FOR': return 4 * 10 + 1;
        case 'RW': return 4 * 10 + 2;
        default: return 2 * 10 + 1; // unknown → CM
      }
    };
    const sortedOutfield = [...outfield].sort((a, b) =>
      posKey(a.positionInMatch ?? a.PositionInMatch ?? '') -
      posKey(b.positionInMatch ?? b.PositionInMatch ?? '')
    );

    // Build matrix: GK row first, then formation rows (DEF → MID → ATT)
    const matrix: MiniPlayerCardModel[][] = [];
    if (gk) {
      matrix.push([this.toCardModel(gk)]);
    } else if (starters.length > 0) {
      matrix.push([this.toCardModel(starters[0])]);
    }

    let fieldIdx = 0;
    for (const count of lines) {
      const row: MiniPlayerCardModel[] = [];
      for (let c = 0; c < count && fieldIdx < sortedOutfield.length; c++) {
        row.push(this.toCardModel(sortedOutfield[fieldIdx++]));
      }
      if (row.length > 0) matrix.push(row);
    }

    // Any overflow players go into the last row
    while (fieldIdx < sortedOutfield.length) {
      if (matrix.length > 1) {
        matrix[matrix.length - 1].push(this.toCardModel(sortedOutfield[fieldIdx++]));
      } else {
        matrix.push([this.toCardModel(sortedOutfield[fieldIdx++])]);
      }
    }

    // Reverse: pitch renders top→bottom, but we want ATT at top and GK at bottom
    return matrix.reverse();
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
        if (e.rawType === 'Substitution' || e.eventType === 'Substitution') {
          evMap['Substitution'] = (evMap['Substitution'] || 0) + 1;
        } else {
          evMap['Assist'] = (evMap['Assist'] || 0) + 1;
        }
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
      position: p.positionInMatch ?? p.PositionInMatch ?? 'SUB',   // role in this match
      naturalPosition: p.naturalPosition ?? p.NaturalPosition ?? undefined, // player's real position
      profileImageUrl: p.profileImageUrl ?? p.ProfileImageUrl ?? null,
      overallRating: p.overallRating ?? p.OverallRating ?? 0
    };
  }

  private formatEventType(type: string): string {
    switch (type) {
      case 'Goal': return this.translate.instant('MATCH.EVENT.GOAL');
      case 'OwnGoal': return this.translate.instant('MATCH.EVENT.OWN_GOAL');
      case 'PenaltyScored': return this.translate.instant('MATCH.EVENT.PENALTY_SCORED');
      case 'PenaltyMissed': return this.translate.instant('MATCH.EVENT.PENALTY_MISSED');
      case 'Substitution': return this.translate.instant('MATCH.EVENT.SUBSTITUTION');
      case 'YellowCard': return this.translate.instant('MATCH.EVENT.YELLOW_CARD');
      case 'RedCard': return this.translate.instant('MATCH.EVENT.RED_CARD');
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
