import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatchCardModel } from '../../../../core/models/Match/match-card.model';
import { MarqueeIfOverflowDirective } from '../match-timeline/marquee-if-overflow.directive';
import { TranslatePipe } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../shared/pipes/localized-date.pipe';

const FORMAT_LABELS: Record<string, string> = {
  FiveSide: '5 v 5',
  SevenSide: '7 v 7',
  ElevenSide: '11 v 11'
};

@Component({
  selector: 'app-match-card',
  standalone: true,
  imports: [CommonModule, MarqueeIfOverflowDirective, TranslatePipe, LocalizedDatePipe],
  templateUrl: './match-card.component.html',
  styleUrls: ['./match-card.component.css']
})
export class MatchCardComponent {
  @Input({ required: true }) match!: MatchCardModel;

  @Output() cardClick = new EventEmitter<number>();

  get typeClass(): string {
    switch (this.match?.type) {
      case 'Tournament': return 'type-tournament';
      case 'Session': return 'type-session';
      case 'Friendly': return 'type-friendly';
      default: return 'type-friendly';
    }
  }

  get statusClass(): string {
    switch (this.match?.status) {
      case 'Live': return 'is-live';
      case 'Scheduled': return 'is-scheduled';
      case 'Completed': return 'is-completed';
      default: return '';
    }
  }

  get statusLabel(): string {
    switch (this.match?.status) {
      case 'Live': return 'MATCH.CARD.IN_PROGRESS';
      case 'Scheduled': return 'MATCH.CARD.SCHEDULED';
      case 'Completed': return 'MATCH.CARD.COMPLETED';
      case 'Cancelled': return 'MATCH.CARD.CANCELLED';
      default: return '';
    }
  }

  get statusStyleClass(): string {
    switch (this.match?.status) {
      case 'Live': return 'status-in-progress';
      case 'Scheduled': return 'status-scheduled';
      case 'Completed': return 'status-completed';
      default: return '';
    }
  }

  get isLive(): boolean {
    return this.match?.status === 'Live';
  }

  get isScheduled(): boolean {
    return this.match?.status === 'Scheduled';
  }

  get isSessionMatch(): boolean {
    return this.match?.homeTeamId === this.match?.awayTeamId;
  }

  get outcomeClass(): string {
    if (!this.match?.coachOutcome) return '';
    return 'outcome-' + this.match.coachOutcome;
  }

  get coachSideClass(): string {
    if (!this.match?.coachSide) return '';
    return 'coach-' + this.match.coachSide;
  }

  get isHomeWinner(): boolean {
    if (this.isSessionMatch) {
      if (this.match.homeScore > this.match.awayScore) return true;
      if (this.match.homeScore === this.match.awayScore
        && this.match.homePenaltyScore != null
        && this.match.awayPenaltyScore != null
        && this.match.homePenaltyScore > this.match.awayPenaltyScore) return true;
      return false;
    }
    return this.match?.winningTeamId != null
      && this.match.winningTeamId === this.match.homeTeamId;
  }

  get isAwayWinner(): boolean {
    if (this.isSessionMatch) {
      if (this.match.awayScore > this.match.homeScore) return true;
      if (this.match.homeScore === this.match.awayScore
        && this.match.homePenaltyScore != null
        && this.match.awayPenaltyScore != null
        && this.match.awayPenaltyScore > this.match.homePenaltyScore) return true;
      return false;
    }
    return this.match?.winningTeamId != null
      && this.match.winningTeamId === this.match.awayTeamId;
  }

  get showPenalty(): boolean {
    return this.match?.homePenaltyScore != null
      && this.match?.awayPenaltyScore != null;
  }

  get formatLabel(): string {
    const raw = this.match?.format;
    if (raw === 'FiveSide') return 'MATCH.FORMAT.FIVE_SIDE';
    if (raw === 'SevenSide') return 'MATCH.FORMAT.SEVEN_SIDE';
    if (raw === 'ElevenSide') return 'MATCH.FORMAT.ELEVEN_SIDE';
    return raw;
  }

  onClick(): void {
    this.cardClick.emit(this.match.id);
  }
}
