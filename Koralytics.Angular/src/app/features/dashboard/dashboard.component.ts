import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth/auth.service';
import { CustomButtonComponent } from '../../../shared/components/custom-button/custom-button';
import { ScrollRevealDirective } from '../../../shared/directives/scroll-reveal.directive';
import { PlayerCardComponent } from '../player/player-card/player-card';
import { MiniPlayerCardComponent } from '../match/mini-player-card/mini-player-card.component';
import { MiniPlayerCardModel } from '../../../core/models/Player/mini-player-card-model';
import { MatchCardComponent } from '../match/match-card/match-card.component';
import { MatchCardModel } from '../../../core/models/Match/match-card.model';
import { MatchTimelineComponent } from '../match/match-timeline/match-timeline.component';
import { MatchService } from '../../../core/services/match/match.service';
import { TimelineEvent } from '../match/match-timeline/match-timeline.types';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, CustomButtonComponent, ScrollRevealDirective, PlayerCardComponent, MiniPlayerCardComponent, MatchCardComponent, MatchTimelineComponent],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  public authService = inject(AuthService);
  private matchService = inject(MatchService);

  mockPlayerBase: MiniPlayerCardModel = {
    playerId: 1,
    fullName: 'ALI HASSAN',
    position: 'CB',
    profileImageUrl: null,
    overallRating: 65
  };

  mockPlayerGold: MiniPlayerCardModel = {
    playerId: 2,
    fullName: 'OMAR KHALED',
    position: 'ST',
    profileImageUrl: null,
    overallRating: 74
  };

  mockPlayerElite: MiniPlayerCardModel = {
    playerId: 3,
    fullName: 'MOHAMED SALAH',
    position: 'RW',
    profileImageUrl: null,
    overallRating: 89
  };

  mockMatches: MatchCardModel[] = [
    {
      id: 1,
      homeTeamName: 'Eagles Academy',
      awayTeamName: 'Lions FC',
      homeTeamAcademyName: 'NILE Academy',
      awayTeamAcademyName: 'ZED Academy',
      homeTeamId: 1,
      awayTeamId: 2,
      type: 'Tournament',
      format: 'ElevenSide',
      matchDate: new Date().toISOString(),
      location: 'Pitch A - Main',
      status: 'Live',
      homeScore: 2,
      awayScore: 1
    },
    {
      id: 2,
      homeTeamName: 'Alpha Team',
      awayTeamName: 'Bravo Squad',
      homeTeamAcademyName: 'NILE Academy',
      awayTeamAcademyName: 'El Gouna Academy',
      homeTeamId: 3,
      awayTeamId: 4,
      type: 'Session',
      format: 'SevenSide',
      matchDate: new Date().toISOString(),
      location: 'Indoor Court 2',
      status: 'Completed',
      homeScore: 3,
      awayScore: 3,
      homePenaltyScore: 5,
      awayPenaltyScore: 4,
      winningTeamId: 3
    },
    {
      id: 3,
      homeTeamName: 'Thunder FC',
      awayTeamName: 'Strikers XI',
      homeTeamAcademyName: 'ZED Academy',
      awayTeamAcademyName: 'Pyramids Academy',
      homeTeamId: 5,
      awayTeamId: 6,
      type: 'Friendly',
      format: 'FiveSide',
      matchDate: new Date(Date.now() + 86400000).toISOString(),
      location: 'Mini Pitch B',
      status: 'Scheduled',
      homeScore: 0,
      awayScore: 0
    }
  ];

  mockMatchInfo: any = { homeTeam: 'Eagles Academy', awayTeam: 'Lions FC', homeAcademy: 'NILE Academy', awayAcademy: 'ZED Academy', homeScore: 2, awayScore: 1, status: 'Completed', homeTeamId: 1, awayTeamId: 2 };

  mockEvents: TimelineEvent[] = [
    { minute: 12, eventType: 'Goal', eventSubtext: 'Assist: Omar Khaled', rawType: 'Goal', side: 'home', player: { ...this.mockPlayerBase, fullName: 'Ali Hassan' }, accentColor: '#facc15' },
    { minute: 23, eventType: 'Yellow Card', eventSubtext: '', rawType: 'YellowCard', side: 'away', player: { ...this.mockPlayerGold, fullName: 'Omar Khaled' }, accentColor: '#f43f5e' },
    { minute: 35, eventType: 'Own Goal', eventSubtext: '', rawType: 'OwnGoal', side: 'home', player: { ...this.mockPlayerBase, fullName: 'Ali Hassan' }, accentColor: '#f43f5e' },
    { minute: 42, eventType: 'Penalty Scored', eventSubtext: '', rawType: 'PenaltyScored', side: 'away', player: { ...this.mockPlayerElite, fullName: 'Mohamed Salah' }, accentColor: '#facc15' },
    { minute: 55, eventType: 'Substitution', eventSubtext: '', rawType: 'Substitution', side: 'home', player: { ...this.mockPlayerBase, fullName: 'Ali Hassan' }, accentColor: '#82f768' },
    { minute: 63, eventType: 'Red Card', eventSubtext: '', rawType: 'RedCard', side: 'away', player: { ...this.mockPlayerGold, fullName: 'Omar Khaled' }, accentColor: '#f43f5e' },
    { minute: 78, eventType: 'Penalty Missed', eventSubtext: '', rawType: 'PenaltyMissed', side: 'home', player: { ...this.mockPlayerElite, fullName: 'Mohamed Salah' }, accentColor: '#f43f5e' },
    { minute: 90, eventType: 'Clean Sheet', eventSubtext: '', rawType: 'CleanSheet', side: 'home', player: { playerId: 0, fullName: 'Eagles Academy', position: '', profileImageUrl: null, overallRating: 0 }, accentColor: '#82f768' },
    { minute: 92, eventType: 'Goal', eventSubtext: 'Assist: Mohamed Salah', rawType: 'Goal', side: 'away', player: { ...this.mockPlayerGold, fullName: 'Omar Khaled' }, accentColor: '#facc15' },
  ];

  testMatchId = 8;

  ngOnInit(): void {
    this.matchService.getMatch(this.testMatchId).subscribe({
      next: (res) => {
        const m = res.data ?? res;
        if (m.homeTeamName) {
          this.mockMatchInfo = {
            homeTeam: m.homeTeamName, awayTeam: m.awayTeamName,
            homeAcademy: m.homeTeamAcademyName ?? '',
            awayAcademy: m.awayTeamAcademyName ?? '',
            homeScore: m.homeScore ?? 0, awayScore: m.awayScore ?? 0,
            status: m.status ?? 'Completed',
            homeTeamId: m.homeTeamId ?? 1, awayTeamId: m.awayTeamId ?? 2,
            formation: m.formation, awayFormation: m.awayFormation
          };
        }
      }
    });
  }

  logout() {
    this.authService.logout().subscribe(() => {
      window.location.href = '/auth/login';
    });
  }
}
