import { Component, Input, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { MatchService } from '../../../../core/services/match/match.service';
import { DrillTemplateService } from '../../../../core/services/drill/drill-template.service';
import { ToastService } from '../../../../core/services/Toast/toast';
import { AuthService } from '../../../../core/services/auth/auth.service';
import { CoachSquadService } from '../../../../core/services/coach/coach-squad.service';
import { MiniPlayerCardModel } from '../../../../core/models/Player/mini-player-card-model';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { MiniPlayerCardComponent } from '../mini-player-card/mini-player-card.component';

interface CategoryRating {
  drillCategoryId: number;
  name: string;
  rating: number; // 0 to 100
}

interface PlayerRatingForm {
  player: MiniPlayerCardModel;
  teamId: number;
  isHomeSide: boolean;
  isMOTM: boolean;
  skipRating: boolean;
  minutesPlayed: number;
  coachNote: string;
  categories: CategoryRating[];
}

import { CustomNumberInputComponent } from '../../../../shared/components/custom-number-input/custom-number-input';

@Component({
  selector: 'app-match-ratings',
  standalone: true,
  imports: [CommonModule, FormsModule, CustomButtonComponent, LoadingSpinnerComponent, EmptyStateComponent, MiniPlayerCardComponent, CustomNumberInputComponent],
  templateUrl: './match-ratings.component.html',
  styleUrls: ['./match-ratings.component.css']
})
export class MatchRatingsComponent implements OnInit {
  @Input() matchId!: number;
  @Input() matchInfo!: any;
  @Input() homePlayers: MiniPlayerCardModel[] = [];
  @Input() awayPlayers: MiniPlayerCardModel[] = [];
  @Input() canSubmitRatings: boolean = false;

  private matchService = inject(MatchService);
  private drillTemplateService = inject(DrillTemplateService);
  private toastService = inject(ToastService);
  private authService = inject(AuthService);
  private coachSquadService = inject(CoachSquadService);
  private cdr = inject(ChangeDetectorRef);
  private sanitizer = inject(DomSanitizer);

  isLoading = true;
  isSubmitting = false;
  hasSubmitted = false; // Need to check if ratings were already submitted
  
  targetCategories: { id: number, name: string }[] = [];
  
  homeRatings: PlayerRatingForm[] = [];
  awayRatings: PlayerRatingForm[] = [];
  
  selectedTeam: 'home' | 'away' = 'home';
  
  canSeeHome: boolean = true;
  canSeeAway: boolean = true;
  coachTeamId: number | null = null;

  get currentUser() {
    return this.authService.getCurrentUserValue();
  }

  get userRoles(): string[] {
    return this.currentUser?.roles || [];
  }

  get isSuperAdmin(): boolean {
    return this.userRoles.includes('SuperAdmin');
  }
  
  get isCoach(): boolean {
    return this.userRoles.includes('Coach');
  }

  get matchType(): string {
    return (this.matchInfo?.type || '').toString().toLowerCase();
  }
  
  get matchFormatStr(): string {
    return (this.matchInfo?.format || '').toString().toLowerCase();
  }

  get defaultMinutes(): number {
    if (this.matchFormatStr.includes('5') || this.matchFormatStr.includes('five')) return 60;
    if (this.matchFormatStr.includes('7') || this.matchFormatStr.includes('seven')) return 60;
    return 90;
  }
  
  selectTeam(team: 'home' | 'away'): void {
    this.selectedTeam = team;
  }

  ngOnInit(): void {
    this.determineVisibleTeams();
  }
  
  private determineVisibleTeams(): void {
    if (this.isSuperAdmin) {
      this.canSeeHome = true;
      this.canSeeAway = true;
      this.selectedTeam = 'home';
      this.checkExistingRatings();
      return;
    }
    
    if (this.isCoach) {
      this.isLoading = true;
      this.coachSquadService.getCoachTeams().subscribe({
        next: (teamsRes: any) => {
          const teams = teamsRes?.data ?? teamsRes ?? [];
          const coachTeam = teams.find((t: any) =>
            (t.teamId ?? t.TeamId) === this.matchInfo.homeTeamId || (t.teamId ?? t.TeamId) === this.matchInfo.awayTeamId
          );
          
          if (coachTeam) {
            this.coachTeamId = coachTeam.teamId ?? coachTeam.TeamId;
            this.canSeeHome = this.coachTeamId === this.matchInfo.homeTeamId;
            this.canSeeAway = this.coachTeamId === this.matchInfo.awayTeamId;
            
            if (this.matchType.includes('session') && this.canSeeHome && this.canSeeAway) {
              this.canSeeHome = true;
              this.canSeeAway = true;
            }
          } else {
            this.canSeeHome = false;
            this.canSeeAway = false;
          }
          
          this.selectedTeam = this.canSeeHome ? 'home' : (this.canSeeAway ? 'away' : 'home');
          this.checkExistingRatings();
        },
        error: () => {
          this.toastService.show('Failed to load coach permissions.', 'error');
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
    } else {
      this.checkExistingRatings();
    }
  }

  submittedHomeRatings: any[] = [];
  submittedAwayRatings: any[] = [];

  private checkExistingRatings(): void {
    this.isLoading = true;
    this.matchService.getMatchRatings(this.matchId).subscribe({
      next: (res: any) => {
        const ratingsData = res.data?.ratings || [];
        if (ratingsData.length > 0) {
          this.hasSubmitted = true;
          // Split ratings into home and away based on the players list
          const homeIds = this.homePlayers.map(p => p.playerId);
          const awayIds = this.awayPlayers.map(p => p.playerId);
          
          this.submittedHomeRatings = ratingsData.filter((r: any) => homeIds.includes(r.playerId))
            .map((r: any) => this.mapSubmittedRatingToUI(r, this.homePlayers.find(p => p.playerId === r.playerId)));
            
          this.submittedAwayRatings = ratingsData.filter((r: any) => awayIds.includes(r.playerId))
            .map((r: any) => this.mapSubmittedRatingToUI(r, this.awayPlayers.find(p => p.playerId === r.playerId)));
            
          this.isLoading = false;
          this.cdr.detectChanges();
        } else {
          this.handleNoRatings();
        }
      },
      error: () => this.handleNoRatings()
    });
  }

  private handleNoRatings(): void {
    if (this.canSubmitRatings) {
      this.loadCategories();
    } else {
      this.hasSubmitted = true; // Force to read-only view
      this.submittedHomeRatings = [];
      this.submittedAwayRatings = [];
      this.isLoading = false;
      this.cdr.detectChanges();
    }
  }

  private mapSubmittedRatingToUI(rating: any, player: any): any {
    const categories = rating.categoryRatings || [];
    const total = categories.reduce((sum: number, cat: any) => sum + cat.rating, 0);
    const avg = categories.length > 0 ? total / categories.length : 0;
    
    return {
      ...rating,
      player: player,
      overallAverage: avg,
      categories: categories.map((c: any) => ({
         name: c.categoryName, // Assuming DTO has categoryName, if not we might need to map it
         score: c.rating,
         colorClass: this.getCategoryColorClass(c.rating),
         icon: this.getCategoryIcon(c.categoryName || 'General')
      }))
    };
  }

  getAverageColor(avg: number): string {
    if (avg >= 8.0) return 'color-elite neon-text-elite';
    if (avg >= 6.0) return 'color-gold neon-text-gold';
    return 'color-base neon-text-base';
  }

  getCategoryColorClass(score: number): string {
    if (score >= 8.0) return 'bg-gradient-elite';
    if (score >= 6.0) return 'bg-gradient-gold';
    return 'bg-gradient-base';
  }

  getCategoryIcon(name: string): SafeHtml {
    const n = name.toLowerCase();
    let svg = `<svg fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" /></svg>`;
    
    if (n.includes('tactical') || n.includes('defending')) svg = `<svg fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" /></svg>`;
    else if (n.includes('attack') || n.includes('shooting')) svg = `<svg fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M13 10V3L4 14h7v7l9-11h-7z" /></svg>`;
    else if (n.includes('physical') || n.includes('speed') || n.includes('stamina')) svg = `<svg fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" /></svg>`;
    else if (n.includes('passing') || n.includes('dribbling')) svg = `<svg fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" /></svg>`;
    
    return this.sanitizer.bypassSecurityTrustHtml(svg);
  }

  loadCategories(): void {
    this.drillTemplateService.getDrillCategories().subscribe({
      next: (cats) => {
        const requiredNames = ['speed', 'shooting', 'passing', 'dribbling', 'defending', 'physical', 'goalkeeping'];
        const mapped = cats.filter(c => requiredNames.includes(c.name.toLowerCase()));
        this.targetCategories = mapped;
        this.initializeForms();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastService.show('Failed to load categories.', 'error');
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  initializeForms(): void {
    this.homeRatings = this.homePlayers.map(p => this.createPlayerForm(p, this.matchInfo.homeTeamId, true));
    this.awayRatings = this.awayPlayers.map(p => this.createPlayerForm(p, this.matchInfo.awayTeamId, false));
  }

  createPlayerForm(player: MiniPlayerCardModel, teamId: number, isHomeSide: boolean): PlayerRatingForm {
    const isGK = player.position?.toUpperCase() === 'GK';
    
    const playerCats = this.targetCategories.filter(c => {
      const isGoalKeepingCat = c.name.toLowerCase() === 'goalkeeping';
      if (isGK) {
        return isGoalKeepingCat;
      } else {
        return !isGoalKeepingCat;
      }
    });

    return {
      player,
      teamId,
      isHomeSide,
      isMOTM: false,
      skipRating: false,
      minutesPlayed: this.defaultMinutes,
      coachNote: '',
      categories: playerCats.map(c => ({
        drillCategoryId: c.id,
        name: c.name,
        rating: 5 // Default middle rating out of 10
      }))
    };
  }

  incrementRating(cat: CategoryRating): void {
    if (cat.rating < 10) cat.rating++;
  }

  decrementRating(cat: CategoryRating): void {
    if (cat.rating > 0) cat.rating--;
  }

  toggleMOTM(playerForm: PlayerRatingForm): void {
    if (playerForm.skipRating) return;
    
    if (this.matchType.includes('tournament') || this.matchType.includes('session')) {
      // 1 overall MOTM for Tournament and Session matches
      this.homeRatings.forEach(r => r.isMOTM = false);
      this.awayRatings.forEach(r => r.isMOTM = false);
      playerForm.isMOTM = true;
    } else {
      // Friendly: 1 MOTM for coach's team
      // The coach should only be rating their own team, but we enforce 1 MOTM across their list
      const teamList = playerForm.isHomeSide ? this.homeRatings : this.awayRatings;
      teamList.forEach(r => r.isMOTM = false);
      playerForm.isMOTM = true;
    }
  }
  
  toggleSkipRating(playerForm: PlayerRatingForm): void {
    playerForm.skipRating = !playerForm.skipRating;
    if (playerForm.skipRating) {
      playerForm.isMOTM = false;
      playerForm.minutesPlayed = 0;
    } else {
      playerForm.minutesPlayed = this.defaultMinutes;
    }
  }
  
  canRatePlayer(playerForm: PlayerRatingForm): boolean {
    if (this.matchType.includes('tournament')) {
      return this.isSuperAdmin;
    }
    
    // Friendly or Session:
    // If Session, coach rates both teams.
    if (this.matchType.includes('session')) {
      return true; // Coach or Superadmin can rate both
    }
    
    // Friendly: coach rates only their team. Wait, we don't have coachTeamId here easily.
    // If the coach is assigned to the team, they rate them. But we passed all players.
    // We should allow rating if the coach has access. Since we're in the frontend, let's just show all for now and backend will validate if they don't have permission for that team, OR we only submit ratings for players that were changed, but API takes a list of players.
    // To be safe, we allow them to fill it out and API will handle unauthorized ratings if they try to rate opponent in friendly.
    return true; 
  }

  submitRatings(): void {
    this.isSubmitting = true;
    
    let allRatingsToSubmit: PlayerRatingForm[] = [];
    if (this.canSeeHome) {
       allRatingsToSubmit.push(...this.homeRatings);
    }
    if (this.canSeeAway) {
       allRatingsToSubmit.push(...this.awayRatings);
    }
    
    const payload = {
      ratings: allRatingsToSubmit
        .filter(r => !r.skipRating)
        .map(r => ({
          playerId: r.player.playerId,
          isMOTM: r.isMOTM,
          minutesPlayed: r.minutesPlayed,
          coachNote: r.coachNote,
          categoryRatings: r.categories.map(c => ({
            drillCategoryId: c.drillCategoryId,
            rating: c.rating
          }))
        }))
    };

    this.matchService.submitMatchRatings(this.matchId, payload).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.toastService.show('Match ratings submitted successfully!', 'success');

        // Build the read-only view from the forms that were just submitted
        const buildSubmitted = (forms: PlayerRatingForm[]) =>
          forms
            .filter(r => !r.skipRating)
            .map(r => {
              const total = r.categories.reduce((s, c) => s + c.rating, 0);
              const avg = r.categories.length > 0 ? total / r.categories.length : 0;
              return {
                playerId: r.player.playerId,
                player: r.player,
                isMOTM: r.isMOTM,
                minutesPlayed: r.minutesPlayed,
                coachNote: r.coachNote,
                overallAverage: avg,
                categories: r.categories.map(c => ({
                  name: c.name,
                  score: c.rating,
                  colorClass: this.getCategoryColorClass(c.rating),
                  icon: this.getCategoryIcon(c.name)
                }))
              };
            });

        this.submittedHomeRatings = buildSubmitted(this.homeRatings);
        this.submittedAwayRatings = buildSubmitted(this.awayRatings);
        this.hasSubmitted = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isSubmitting = false;
        const msg = err?.error?.detail ?? err?.error?.message ?? 'Failed to submit match ratings.';
        this.toastService.show(msg, 'error');
        this.cdr.detectChanges();
      }
    });
  }
}
