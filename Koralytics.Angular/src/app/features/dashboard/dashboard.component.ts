import { Component, inject } from '@angular/core';
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

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, CustomButtonComponent, ScrollRevealDirective, PlayerCardComponent, MiniPlayerCardComponent, MatchCardComponent, MatchTimelineComponent],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent {
  public authService = inject(AuthService);

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

  logout() {
    this.authService.logout().subscribe(() => {
      window.location.href = '/auth/login';
    });
  }
}
