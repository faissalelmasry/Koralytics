import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { MatchService } from '../../../../../core/services/match/match.service';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { MatchTimelineComponent } from '../../match-timeline/match-timeline.component';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../../shared/components/footer/footer';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { MatchSignalrService } from '../../../../../core/services/match-signalr.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-match-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatchTimelineComponent,
    NavbarComponent,
    Footer,
    LoadingSpinnerComponent
  ],
  templateUrl: './match-detail.component.html',
  styleUrls: ['./match-detail.component.css']
})
export class MatchDetailComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private matchService = inject(MatchService);
  private authService = inject(AuthService);
  private coachSquadService = inject(CoachSquadService);
  private cdr = inject(ChangeDetectorRef);
  private signalrService = inject(MatchSignalrService);

  private signalrSub?: Subscription;

  matchId!: number;
  isLoading = true;
  error = '';
  
  isCoachForThisMatch = false;

  matchInfo: any = {
    homeTeam: '',
    awayTeam: '',
    homeScore: 0,
    awayScore: 0,
    homePenaltyScore: null,
    awayPenaltyScore: null,
    matchDate: null,
    status: '',
    type: '',
    homeTeamId: 0,
    awayTeamId: 0
  };

  get currentUser() {
    return this.authService.getCurrentUserValue();
  }

  get userRoles(): string[] {
    return this.currentUser?.roles || [];
  }

  get isCoach(): boolean {
    return this.userRoles.includes('Coach');
  }

  get isSuperAdmin(): boolean {
    return this.userRoles.includes('SuperAdmin');
  }

  get canLogEvents(): boolean {
    if (!this.matchInfo || this.matchInfo.status !== 'Live') return false;

    const matchType = (this.matchInfo.type || '').toString().toLowerCase();

    if (matchType.includes('tournament')) {
      return this.isSuperAdmin;
    }

    return this.isCoachForThisMatch || this.isSuperAdmin;
  }

  get canSubmitRatings(): boolean {
    if (!this.matchInfo || this.matchInfo.status !== 'Completed') return false;

    const matchType = (this.matchInfo.type || '').toString().toLowerCase();

    if (matchType.includes('tournament')) {
      return this.isSuperAdmin;
    }

    return this.isCoachForThisMatch || this.isSuperAdmin;
  }

  get isStartMatchEligibleDate(): boolean {
    if (!this.matchInfo || !this.matchInfo.matchDate) return true;
    const matchDay = new Date(this.matchInfo.matchDate);
    matchDay.setHours(0, 0, 0, 0);

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    // Eligible if match date is TODAY or in the PAST
    return matchDay.getTime() <= today.getTime();
  }

  get canStartMatch(): boolean {
    if (!this.matchInfo || this.matchInfo.status !== 'Scheduled') return false;
    if (!this.isStartMatchEligibleDate) return false;

    const matchType = (this.matchInfo.type || '').toString().toLowerCase();

    if (matchType.includes('tournament')) {
      return this.isSuperAdmin;
    }

    return this.isCoachForThisMatch || this.isSuperAdmin;
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.matchId = Number(id);
      this.loadMatch();
      this.signalrService.joinMatchGroup(this.matchId);
      this.subscribeToLiveUpdates();
    }
  }

  ngOnDestroy(): void {
    if (this.matchId) {
      this.signalrService.leaveMatchGroup(this.matchId);
    }
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }

  private subscribeToLiveUpdates(): void {
    this.signalrSub = this.signalrService.matchScoreUpdate$.subscribe(update => {
      if (update.matchId === this.matchId) {
        this.matchInfo.homeScore = update.homeScore;
        this.matchInfo.awayScore = update.awayScore;
        this.matchInfo.homePenaltyScore = update.homePenaltyScore;
        this.matchInfo.awayPenaltyScore = update.awayPenaltyScore;
        this.matchInfo.status = update.status;
        this.cdr.detectChanges();
      }
    });
  }

  loadMatch(): void {
    this.isLoading = true;
    this.matchService.getMatch(this.matchId).subscribe({
      next: (res) => {
        const m = res.data ?? res;
        this.matchInfo = {
          homeTeam: m.homeTeamName ?? m.HomeTeamName ?? '',
          awayTeam: m.awayTeamName ?? m.AwayTeamName ?? '',
          homeAcademy: m.homeTeamAcademyName ?? m.HomeTeamAcademyName ?? '',
          awayAcademy: m.awayTeamAcademyName ?? m.AwayTeamAcademyName ?? '',
          homeScore: m.homeScore ?? m.HomeScore ?? 0,
          awayScore: m.awayScore ?? m.AwayScore ?? 0,
          homePenaltyScore: m.homePenaltyScore ?? m.HomePenaltyScore ?? null,
          awayPenaltyScore: m.awayPenaltyScore ?? m.AwayPenaltyScore ?? null,
          matchDate: m.matchDate ?? m.MatchDate ?? null,
          status: m.status ?? m.Status ?? '',
          type: m.type ?? m.Type ?? '',
          homeTeamId: m.homeTeamId ?? m.HomeTeamId ?? 0,
          awayTeamId: m.awayTeamId ?? m.AwayTeamId ?? 0,
          formation: m.formation ?? m.Formation ?? '4-3-3',
          awayFormation: m.awayFormation ?? m.AwayFormation ?? '4-3-3'
        };

        if (this.isCoach && !this.isSuperAdmin) {
          this.coachSquadService.getCoachTeams().subscribe({
            next: (teamsRes: any) => {
              const teams = teamsRes?.data ?? teamsRes ?? [];
              const coachTeam = teams.find((t: any) =>
                (t.teamId ?? t.TeamId) === this.matchInfo.homeTeamId || (t.teamId ?? t.TeamId) === this.matchInfo.awayTeamId
              );
              this.isCoachForThisMatch = !!coachTeam;
              this.isLoading = false;
              this.cdr.detectChanges();
            },
            error: () => {
              this.isLoading = false;
              this.cdr.detectChanges();
            }
          });
        } else {
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      },
      error: () => {
        this.isLoading = false;
        this.error = 'Failed to load match details.';
        this.cdr.detectChanges();
      }
    });
  }
}
