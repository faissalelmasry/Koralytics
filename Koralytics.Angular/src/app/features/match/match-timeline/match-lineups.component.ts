import { Component, Input, Output, EventEmitter, inject, ChangeDetectorRef, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MiniPlayerCardComponent } from '../mini-player-card/mini-player-card.component';
import { MiniPlayerCardModel } from '../../../../core/models/Player/mini-player-card-model';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { MatchService } from '../../../../core/services/match/match.service';
import { ToastService } from '../../../../core/services/Toast/toast';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { AuthService } from '../../../../core/services/auth/auth.service';

import { CoachSquadService } from '../../../../core/services/coach/coach-squad.service';
import { NotificationService } from '@core/services/SignalR/notificationservice';

export type ActionTool = 'goal_solo' | 'goal_assist' | 'penalty_scored' | 'penalty_missed' | 'own_goal' | 'sub' | 'yellow' | 'red';

@Component({
  selector: 'app-match-lineups',
  standalone: true,
  imports: [CommonModule, MiniPlayerCardComponent, EmptyStateComponent, CustomButtonComponent, ConfirmDialogComponent],
  templateUrl: './match-lineups.component.html',
  styleUrls: ['./match-lineups.component.css']
})
export class MatchLineupsComponent implements OnInit, OnChanges {
  private router = inject(Router);
  private matchService = inject(MatchService);
  private toastService = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);
  private authService = inject(AuthService);
  private coachSquadService = inject(CoachSquadService);
  private notificationService = inject(NotificationService);

  get currentUser() {
    return this.authService.getCurrentUserValue();
  }

  get isCoach(): boolean {
    return this.currentUser?.roles?.includes('Coach') ?? false;
  }

  get isSuperAdmin(): boolean {
    return this.currentUser?.roles?.includes('SystemAdmin') ?? false;
  }

  @Input({ required: true }) homeName!: string;
  @Input() homeFormation: string = '';
  @Input({ required: true }) homeStarters!: MiniPlayerCardModel[][];
  @Input({ required: true }) homeBench!: MiniPlayerCardModel[];

  @Input({ required: true }) awayName!: string;
  @Input() awayFormation: string = '';
  @Input({ required: true }) awayStarters!: MiniPlayerCardModel[][];
  @Input({ required: true }) awayBench!: MiniPlayerCardModel[];

  @Input() canLogEvents: boolean = false;
  @Input() matchId!: number;
  @Input() matchInfo!: any;
  @Input() isCoachForThisMatch: boolean = false;

  @Output() eventLogged = new EventEmitter<void>();

  selectedTeam: 'home' | 'away' = 'home';
  coachTeamIds: Set<number> = new Set();
  isCheckingCoachTeams = false;

  // Penalty Shootout Mode & Scores
  isShootoutMode = false;
  hasShootoutStarted = false;
  homePenaltyScore = 0;
  awayPenaltyScore = 0;
  isEndingMatch = false;
  isEndMatchDialogOpen = false;

  // Active Tool & Selection State
  activeTool: ActionTool = 'goal_solo';
  firstPickedPlayer: MiniPlayerCardModel | null = null;

  get isDraw(): boolean {
    if (!this.matchInfo) return false;
    return Number(this.matchInfo.homeScore ?? 0) === Number(this.matchInfo.awayScore ?? 0);
  }

  get hasShootoutEvents(): boolean {
    return Number(this.homePenaltyScore ?? 0) > 0 ||
      Number(this.awayPenaltyScore ?? 0) > 0 ||
      this.hasShootoutStarted ||
      this.isShootoutMode;
  }

  get isCurrentSelectedTeamCoachTeam(): boolean {
    if (!this.isCoach) return false;
    const currentTeamId = this.selectedTeam === 'home' ? this.matchInfo?.homeTeamId : this.matchInfo?.awayTeamId;
    return this.coachTeamIds.has(currentTeamId);
  }

  ngOnInit(): void {
    this.syncPenaltyScores();
    this.checkCoachTeams();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['matchInfo']) {
      this.syncPenaltyScores();
      this.checkCoachTeams();
    }
  }

  private checkCoachTeams(): void {
    if (this.isCoach && !this.isCheckingCoachTeams && this.coachTeamIds.size === 0) {
      this.isCheckingCoachTeams = true;
      this.coachSquadService.getCoachTeams().subscribe({
        next: (res: any) => {
          const teams = res?.data ?? res ?? [];
          const ids = teams.map((t: any) => t.teamId ?? t.TeamId);
          this.coachTeamIds = new Set(ids);
          this.isCheckingCoachTeams = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.isCheckingCoachTeams = false;
          this.cdr.detectChanges();
        }
      });
    }
  }

  private syncPenaltyScores(): void {
    if (this.matchInfo) {
      const hPen = this.matchInfo.homePenaltyScore ?? this.matchInfo.HomePenaltyScore;
      const aPen = this.matchInfo.awayPenaltyScore ?? this.matchInfo.AwayPenaltyScore;
      const hasShootoutInInfo = this.matchInfo.hasPenaltyShootout === true;

      if (hPen !== null && hPen !== undefined) {
        this.homePenaltyScore = Number(hPen);
      }
      if (aPen !== null && aPen !== undefined) {
        this.awayPenaltyScore = Number(aPen);
      }

      if (Number(hPen) > 0 || Number(aPen) > 0 || this.hasShootoutStarted || hasShootoutInInfo) {
        this.hasShootoutStarted = true;
        this.isShootoutMode = true;
        if (this.activeTool === 'goal_solo') {
          this.activeTool = 'penalty_scored';
        }
      }
    }
  }

  selectTeam(team: 'home' | 'away'): void {
    this.selectedTeam = team;
    this.firstPickedPlayer = null;
  }

  goToSubmitLineup(): void {
    if (this.matchId) {
      this.router.navigate(['/match', this.matchId, 'submit-lineup']);
    }
  }

  get instructionText(): string {
    if (this.isShootoutMode) {
      if (this.activeTool === 'penalty_scored') {
        return `Tap Penalty Taker on Pitch to Log Scored Penalty`;
      }
      if (this.activeTool === 'penalty_missed') {
        return `Tap Penalty Taker on Pitch to Log Missed Penalty`;
      }
      return `Penalty Shootout  (${this.homeName} ${this.homePenaltyScore} - ${this.awayPenaltyScore} ${this.awayName})`;
    }

    switch (this.activeTool) {
      case 'goal_solo':
        return `Tap Player on Pitch to Log Solo Goal`;
      case 'goal_assist':
        return this.firstPickedPlayer
          ? `Scorer: ${this.firstPickedPlayer.fullName} ➔ Tap Assister`
          : `Tap Goal Scorer on Pitch`;
      case 'own_goal':
        return `Tap Player who Scored Own Goal`;
      case 'sub':
        return this.firstPickedPlayer
          ? `Sub Out: ${this.firstPickedPlayer.fullName} ➔ Tap Bench Player`
          : `Tap Player on Pitch to Sub Out`;
      case 'yellow':
        return `Tap Player for Yellow Card`;
      case 'red':
        return `Tap Player for Red Card`;
      default:
        return 'Select Action';
    }
  }

  enterShootoutMode(): void {
    if (!this.isDraw && !this.hasShootoutEvents) {
      this.toastService.show('Shootout Mode is only available when the match score is a Draw!', 'info');
      return;
    }
    this.isShootoutMode = true;
    this.activeTool = 'penalty_scored';
    this.firstPickedPlayer = null;
  }

  exitShootoutMode(): void {
    if (Number(this.homePenaltyScore) > 0 || Number(this.awayPenaltyScore) > 0 || this.hasShootoutStarted) {
      this.toastService.show('Penalty shootout events have been recorded for this match.', 'info');
      return;
    }
    this.isShootoutMode = false;
    this.activeTool = 'goal_solo';
    this.firstPickedPlayer = null;
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

  setTool(tool: ActionTool): void {
    if (this.hasShootoutEvents && tool !== 'penalty_scored' && tool !== 'penalty_missed') {
      this.toastService.show('Regular match events are disabled after penalty shootout has started.', 'info');
      return;
    }
    this.activeTool = tool;
    this.firstPickedPlayer = null;
  }

  getCardAccent(player: MiniPlayerCardModel): string | undefined {
    if (this.firstPickedPlayer?.playerId === player.playerId) {
      return '#10b981';
    }
    return undefined;
  }

  onPlayerCardClick(player: MiniPlayerCardModel): void {
    if (!this.canLogEvents) {
      if (player?.playerId) {
        this.router.navigate(['/player/profile', player.playerId]);
      }
      return;
    }

    if (this.activeTool === 'goal_solo') {
      this.executeLogEvent('Goal', player, null);
    } else if (this.activeTool === 'goal_assist') {
      if (!this.firstPickedPlayer) {
        this.firstPickedPlayer = player;
      } else {
        const scorer = this.firstPickedPlayer;
        const assister = (player.playerId === scorer.playerId) ? null : player;
        this.executeLogEvent('Goal', scorer, assister);
        this.firstPickedPlayer = null;
      }
    } else if (this.activeTool === 'penalty_scored') {
      this.executeLogEvent('PenaltyScored', player, null);
    } else if (this.activeTool === 'penalty_missed') {
      this.executeLogEvent('PenaltyMissed', player, null);
    } else if (this.activeTool === 'own_goal') {
      this.executeLogEvent('OwnGoal', player, null);
    } else if (this.activeTool === 'sub') {
      this.firstPickedPlayer = player;
    } else if (this.activeTool === 'yellow') {
      this.executeLogEvent('YellowCard', player, null);
    } else if (this.activeTool === 'red') {
      this.executeLogEvent('RedCard', player, null);
    }
  }

  onBenchPlayerClick(benchPlayer: MiniPlayerCardModel): void {
    if (!this.canLogEvents) {
      if (benchPlayer?.playerId) {
        this.router.navigate(['/player/profile', benchPlayer.playerId]);
      }
      return;
    }

    if (this.activeTool === 'sub') {
      if (this.firstPickedPlayer) {
        const playerOut = this.firstPickedPlayer;
        const playerIn = benchPlayer;

        this.executeLogEvent('Substitution', playerOut, playerIn);
        this.firstPickedPlayer = null;
      }
      return;
    }

    this.onPlayerCardClick(benchPlayer);
  }

  executeLogEvent(eventType: string, player: MiniPlayerCardModel, secondaryPlayer: MiniPlayerCardModel | null): void {
    const isSession = (this.matchInfo?.type || '').toString().toLowerCase().includes('session') ||
      this.matchInfo?.homeTeamId === this.matchInfo?.awayTeamId;

    const minute = 0;
    const isHome = this.selectedTeam === 'home';
    const teamId = isHome ? this.matchInfo?.homeTeamId : this.matchInfo?.awayTeamId;

    const obs = isSession
      ? this.matchService.logSessionMatchEvent(this.matchId, {
        playerId: player.playerId,
        assistPlayerId: secondaryPlayer ? secondaryPlayer.playerId : null,
        eventType,
        minute,
        isHomeSide: isHome
      })
      : this.matchService.logMatchEvent(this.matchId, {
        teamId,
        playerId: player.playerId,
        assistPlayerId: secondaryPlayer ? secondaryPlayer.playerId : null,
        eventType,
        minute
      });

    obs.subscribe({
      next: () => {
        let eventTitle = "Match Update";
        let eventMessage = `${player.fullName} has a new match event.`;
        let eventCategory = "MatchEvent";

        if (eventType === 'PenaltyScored') {
          eventTitle = "Penalty Scored! ⚽";
          eventMessage = `${player.fullName} scored a penalty!`;
          eventCategory = "GoalScored";
          
          this.hasShootoutStarted = true;
          if (this.isShootoutMode) {
            if (isHome) {
              this.homePenaltyScore++;
              if (this.matchInfo) this.matchInfo.homePenaltyScore = this.homePenaltyScore;
            } else {
              this.awayPenaltyScore++;
              if (this.matchInfo) this.matchInfo.awayPenaltyScore = this.awayPenaltyScore;
            }
            this.toastService.show(`Penalty Scored by ${player.fullName}! (${this.homeName} ${this.homePenaltyScore} - ${this.awayPenaltyScore} ${this.awayName})`, 'success');
          } else {
            if (isHome) this.matchInfo.homeScore++; else this.matchInfo.awayScore++;
            this.toastService.show(`Penalty Goal by ${player.fullName}!`, 'success');
          }
        } else if (eventType === 'PenaltyMissed') {
          eventTitle = "Penalty Missed ❌";
          eventMessage = `${player.fullName} missed a penalty.`;
          eventCategory = "PenaltyMissed";

          if (this.isShootoutMode) {
            this.hasShootoutStarted = true;
          }
          this.toastService.show(`Penalty Missed by ${player.fullName}`, 'info');
        } else if (eventType === 'Goal') {
          eventTitle = "GOAAAL! ⚽";
          eventMessage = secondaryPlayer 
            ? `${player.fullName} scored a goal! Assist by ${secondaryPlayer.fullName}.` 
            : `${player.fullName} scored a solo goal!`;
          eventCategory = "GoalScored";

          if (isHome) this.matchInfo.homeScore++; else this.matchInfo.awayScore++;
          this.toastService.show(`GOAL by ${player.fullName}!`, 'success');
        } else if (eventType === 'OwnGoal') {
          eventTitle = "Own Goal 🥅";
          eventMessage = `${player.fullName} scored an own goal.`;
          eventCategory = "OwnGoal";

          if (isHome) this.matchInfo.awayScore++; else this.matchInfo.homeScore++;
          this.toastService.show(`Own Goal by ${player.fullName}!`, 'info');
        } else if (eventType === 'YellowCard') {
          eventTitle = "Yellow Card 🟨";
          eventMessage = `${player.fullName} received a yellow card.`;
          eventCategory = "YellowCard";
          this.toastService.show(`Yellow Card: ${player.fullName}`, 'info');
        } else if (eventType === 'RedCard') {
          eventTitle = "Red Card 🟥";
          eventMessage = `${player.fullName} was sent off with a red card.`;
          eventCategory = "PlayerSentOff";
          this.toastService.show(`Red Card: ${player.fullName}`, 'error');
        } else if (eventType === 'Substitution' && secondaryPlayer) {
          eventTitle = "Substitution 🔄";
          eventMessage = `Substitution: ${player.fullName} off, ${secondaryPlayer.fullName} on.`;
          eventCategory = "Substitution";

          const startersMatrix = isHome ? this.homeStarters : this.awayStarters;
          const benchList = isHome ? this.homeBench : this.awayBench;

          let foundRow = -1;
          let foundCol = -1;
          for (let r = 0; r < startersMatrix.length; r++) {
            const col = startersMatrix[r].findIndex(p => p.playerId === player.playerId);
            if (col !== -1) {
              foundRow = r;
              foundCol = col;
              break;
            }
          }

          const benchIdx = benchList.findIndex(p => p.playerId === secondaryPlayer.playerId);

          if (foundRow !== -1 && foundCol !== -1 && benchIdx !== -1) {
            const outP = startersMatrix[foundRow][foundCol];
            const inP = benchList[benchIdx];

            inP.position = outP.position;

            startersMatrix[foundRow][foundCol] = inP;
            benchList[benchIdx] = outP;
          }

          this.toastService.show(`Sub: OUT ${player.fullName} ➔ IN ${secondaryPlayer.fullName}`, 'success');
        }
       this.notificationService.triggerMatchEventNotification(
          this.matchId,
          eventTitle,
          eventMessage,
          eventCategory
        ).subscribe({ error: (e) => console.error('Failed to dispatch event notification', e) });

        this.eventLogged.emit();
        this.cdr.detectChanges();
      },
      error: (err) => {
        const msg = err?.error?.detail || err?.error?.message || 'Failed to log match event.';
        this.toastService.show(msg, 'error');
        this.cdr.detectChanges();
      }
    });
  }
}
