import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MiniPlayerCardModel } from '../../../../core/models/Player/mini-player-card-model';
import { MatchTimelineEventsComponent } from './match-timeline-events.component';
import { MatchLineupsComponent } from './match-lineups.component';
import { TimelineEvent } from './match-timeline.types';

@Component({
  selector: 'app-match-timeline',
  standalone: true,
  imports: [CommonModule, MatchTimelineEventsComponent, MatchLineupsComponent],
  templateUrl: './match-timeline.component.html',
  styleUrls: ['./match-timeline.component.css']
})
export class MatchTimelineComponent {
  @Input() matchId: number = 1;

  selectedTab: 'timeline' | 'lineups' = 'timeline';

  matchInfo = {
    homeTeam: 'Eagles Academy',
    awayTeam: 'Lions FC',
    homeScore: 2,
    awayScore: 1,
    status: 'Completed'
  };

  timelineEvents: TimelineEvent[] = [
    {
      minute: 14, eventType: 'GOAL!', eventSubtext: 'Assist: Zizo', side: 'home',
      accentColor: '#facc15',
      player: { playerId: 1, fullName: 'M. SALAH', position: 'RW', profileImageUrl: null, overallRating: 89 }
    },
    {
      minute: 28, eventType: 'YELLOW CARD', eventSubtext: 'Tactical Foul', side: 'away',
      accentColor: '#f43f5e',
      player: { playerId: 2, fullName: 'ASHRAF', position: 'LB', profileImageUrl: null, overallRating: 72 }
    },
    {
      minute: 42, eventType: 'PENALTY GOAL', eventSubtext: 'Bottom Corner', side: 'away',
      accentColor: '#facc15',
      player: { playerId: 3, fullName: 'KAHRABA', position: 'ST', profileImageUrl: null, overallRating: 76 }
    },
    {
      minute: 68, eventType: 'HEADER GOAL', eventSubtext: 'Cross by Hany', side: 'home',
      accentColor: '#facc15',
      player: { playerId: 4, fullName: 'MOSTAFA', position: 'ST', profileImageUrl: null, overallRating: 81 }
    },
    {
      minute: 75, eventType: 'SUBSTITUTION', eventSubtext: 'IN: Adel  |  OUT: Marmoush', side: 'home',
      accentColor: '#82f768',
      player: { playerId: 5, fullName: 'ADEL', position: 'LW', profileImageUrl: null, overallRating: 75 }
    }
  ];

  homeStarters: MiniPlayerCardModel[] = [
    { playerId: 10, fullName: 'M. SALAH', position: 'RW', profileImageUrl: null, overallRating: 89 },
    { playerId: 11, fullName: 'MOSTAFA', position: 'ST', profileImageUrl: null, overallRating: 81 },
    { playerId: 12, fullName: 'MARMOUSH', position: 'LW', profileImageUrl: null, overallRating: 78 },
    { playerId: 13, fullName: 'ZIZO', position: 'CM', profileImageUrl: null, overallRating: 79 },
    { playerId: 14, fullName: 'ELNENY', position: 'CDM', profileImageUrl: null, overallRating: 76 },
    { playerId: 15, fullName: 'ATTIA', position: 'CM', profileImageUrl: null, overallRating: 75 },
    { playerId: 16, fullName: 'FATOUH', position: 'LB', profileImageUrl: null, overallRating: 76 },
    { playerId: 17, fullName: 'ABDELMONEM', position: 'CB', profileImageUrl: null, overallRating: 78 },
    { playerId: 18, fullName: 'YASSER', position: 'CB', profileImageUrl: null, overallRating: 75 },
    { playerId: 19, fullName: 'HANY', position: 'RB', profileImageUrl: null, overallRating: 74 },
    { playerId: 20, fullName: 'SHENAWY', position: 'GK', profileImageUrl: null, overallRating: 77 }
  ];

  homeBench: MiniPlayerCardModel[] = [
    { playerId: 21, fullName: 'SOBHI', position: 'GK', profileImageUrl: null, overallRating: 74 },
    { playerId: 22, fullName: 'ADEL', position: 'LW', profileImageUrl: null, overallRating: 75 },
    { playerId: 23, fullName: 'RABIA', position: 'CB', profileImageUrl: null, overallRating: 73 },
    { playerId: 24, fullName: 'FATHI', position: 'CM', profileImageUrl: null, overallRating: 72 },
    { playerId: 25, fullName: 'SHERIF', position: 'ST', profileImageUrl: null, overallRating: 74 },
    { playerId: 26, fullName: 'KAMAL', position: 'RB', profileImageUrl: null, overallRating: 71 },
    { playerId: 27, fullName: 'MAGDY', position: 'CAM', profileImageUrl: null, overallRating: 70 }
  ];

  awayStarters: MiniPlayerCardModel[] = [
    { playerId: 30, fullName: 'TREZEGUET', position: 'LW', profileImageUrl: null, overallRating: 77 },
    { playerId: 31, fullName: 'KAHRABA', position: 'ST', profileImageUrl: null, overallRating: 76 },
    { playerId: 32, fullName: 'ASHOUR', position: 'RW', profileImageUrl: null, overallRating: 78 },
    { playerId: 33, fullName: 'SHAHAT', position: 'LM', profileImageUrl: null, overallRating: 73 },
    { playerId: 34, fullName: 'HAMDY', position: 'CM', profileImageUrl: null, overallRating: 76 },
    { playerId: 35, fullName: 'KOKA', position: 'CM', profileImageUrl: null, overallRating: 74 },
    { playerId: 36, fullName: 'TAHER', position: 'RM', profileImageUrl: null, overallRating: 72 },
    { playerId: 37, fullName: 'HEGAZI', position: 'CB', profileImageUrl: null, overallRating: 79 },
    { playerId: 38, fullName: 'EL MOHAMADY', position: 'CB', profileImageUrl: null, overallRating: 73 },
    { playerId: 39, fullName: 'GABR', position: 'CB', profileImageUrl: null, overallRating: 74 },
    { playerId: 40, fullName: 'GABASKI', position: 'GK', profileImageUrl: null, overallRating: 76 }
  ];

  awayBench: MiniPlayerCardModel[] = [
    { playerId: 41, fullName: 'ASHRAF', position: 'LB', profileImageUrl: null, overallRating: 72 },
    { playerId: 42, fullName: 'LOTFI', position: 'GK', profileImageUrl: null, overallRating: 73 },
    { playerId: 43, fullName: 'RAYAN', position: 'ST', profileImageUrl: null, overallRating: 71 },
    { playerId: 44, fullName: 'DONGA', position: 'CDM', profileImageUrl: null, overallRating: 70 },
    { playerId: 45, fullName: 'ALAAM', position: 'CB', profileImageUrl: null, overallRating: 69 },
    { playerId: 46, fullName: 'ELEYAN', position: 'RW', profileImageUrl: null, overallRating: 72 },
    { playerId: 47, fullName: 'YASSIN', position: 'CAM', profileImageUrl: null, overallRating: 68 }
  ];

  get formationHome(): MiniPlayerCardModel[][] {
    return [
      [this.homeStarters[0], this.homeStarters[1], this.homeStarters[2]],
      [this.homeStarters[3], this.homeStarters[4], this.homeStarters[5]],
      [this.homeStarters[6], this.homeStarters[7], this.homeStarters[8], this.homeStarters[9]],
      [this.homeStarters[10]]
    ];
  }

  get formationAway(): MiniPlayerCardModel[][] {
    return [
      [this.awayStarters[0], this.awayStarters[1], this.awayStarters[2]],
      [this.awayStarters[3], this.awayStarters[4], this.awayStarters[5], this.awayStarters[6]],
      [this.awayStarters[7], this.awayStarters[8], this.awayStarters[9]],
      [this.awayStarters[10]]
    ];
  }

  get trackTransform(): string {
    return this.selectedTab === 'timeline' ? 'translateX(0%)' : 'translateX(-50%)';
  }

  get isLive(): boolean {
    return this.matchInfo.status === 'Live';
  }

  get isCompleted(): boolean {
    return this.matchInfo.status === 'Completed';
  }

  selectTab(tab: 'timeline' | 'lineups'): void {
    this.selectedTab = tab;
  }
}
