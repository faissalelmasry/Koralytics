import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatchService, CreateTournamentMatchDto } from '../../../../../core/services/match/match.service';
import { TournamentService } from '../../../../../core/services/tournament/tournament.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../../shared/components/footer/footer';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomDateTimePicker } from '../../../../../shared/components/custom-date-time-picker/custom-date-time-picker';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';

@Component({
  selector: 'app-create-tournament-match',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    NavbarComponent,
    Footer,
    LoadingSpinnerComponent,
    CustomInputComponent,
    CustomDateTimePicker,
    CustomButtonComponent
  ],
  templateUrl: './create-tournament-match.component.html',
  styleUrls: ['./create-tournament-match.component.css']
})
export class CreateTournamentMatchComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private matchService = inject(MatchService);
  private tournamentService = inject(TournamentService);
  private toastService = inject(ToastService);

  fixtureId: number = 0;
  homeTeamId: number = 0;
  awayTeamId: number = 0;
  homeTeamName: string = 'Home Team';
  awayTeamName: string = 'Away Team';
  homeAcademyName: string = '';
  awayAcademyName: string = '';
  tournamentId: number | null = null;
  tournamentName: string = '';
  groupOrRoundName: string = '';

  matchDate: string = '';
  location: string = '';

  isLoadingFixture: boolean = false;
  isSubmitting: boolean = false;
  errorMessage: string | null = null;

  ngOnInit(): void {
    // 1. Read state passed via router navigation
    const state = history.state;
    if (state && state.fixtureId) {
      this.fixtureId = Number(state.fixtureId);
      if (state.homeTeamId) this.homeTeamId = Number(state.homeTeamId);
      if (state.awayTeamId) this.awayTeamId = Number(state.awayTeamId);
      if (state.homeTeamName) this.homeTeamName = state.homeTeamName;
      if (state.awayTeamName) this.awayTeamName = state.awayTeamName;
      if (state.homeAcademyName) this.homeAcademyName = state.homeAcademyName;
      if (state.awayAcademyName) this.awayAcademyName = state.awayAcademyName;
      if (state.tournamentId) this.tournamentId = Number(state.tournamentId);
      if (state.tournamentName) this.tournamentName = state.tournamentName;
      if (state.groupOrRoundName) this.groupOrRoundName = state.groupOrRoundName;
    }

    // 2. Read route path parameter synchronously from snapshot
    const pathFixtureId = this.route.snapshot.paramMap.get('fixtureId');
    if (pathFixtureId) {
      this.fixtureId = Number(pathFixtureId);
    }

    // 3. Read query parameters synchronously from snapshot
    const queryFixtureId = this.route.snapshot.queryParamMap.get('fixtureId');
    if (queryFixtureId) this.fixtureId = Number(queryFixtureId);

    const queryHomeTeamId = this.route.snapshot.queryParamMap.get('homeTeamId');
    if (queryHomeTeamId) this.homeTeamId = Number(queryHomeTeamId);
    const queryAwayTeamId = this.route.snapshot.queryParamMap.get('awayTeamId');
    if (queryAwayTeamId) this.awayTeamId = Number(queryAwayTeamId);
    const queryHomeTeamName = this.route.snapshot.queryParamMap.get('homeTeamName');
    if (queryHomeTeamName) this.homeTeamName = queryHomeTeamName;
    const queryAwayTeamName = this.route.snapshot.queryParamMap.get('awayTeamName');
    if (queryAwayTeamName) this.awayTeamName = queryAwayTeamName;
    const queryHomeAcademyName = this.route.snapshot.queryParamMap.get('homeAcademyName');
    if (queryHomeAcademyName) this.homeAcademyName = queryHomeAcademyName;
    const queryAwayAcademyName = this.route.snapshot.queryParamMap.get('awayAcademyName');
    if (queryAwayAcademyName) this.awayAcademyName = queryAwayAcademyName;

    // Default match date to tomorrow at 16:00
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    tomorrow.setHours(16, 0, 0, 0);
    this.matchDate = this.formatDateForInput(tomorrow);

    // 4. Fetch details if fixtureId exists but team details are missing
    if (this.fixtureId && (!this.homeTeamId || this.homeTeamName === 'Home Team')) {
      this.loadFixtureDetails(this.fixtureId);
    }
  }

  private loadFixtureDetails(fixtureId: number): void {
    this.isLoadingFixture = true;
    this.errorMessage = null;

    this.tournamentService.getFixtureById(fixtureId).subscribe({
      next: (res) => {
        this.isLoadingFixture = false;
        const data = res?.data || res;
        if (data) {
          this.fixtureId = data.fixtureId || data.FixtureId || fixtureId;
          this.homeTeamId = data.homeTeamId || data.HomeTeamId;
          this.awayTeamId = data.awayTeamId || data.AwayTeamId;
          this.homeTeamName = data.homeTeamName || data.HomeTeamName || 'Home Team';
          this.awayTeamName = data.awayTeamName || data.AwayTeamName || 'Away Team';
          this.homeAcademyName = data.homeAcademyName || data.HomeAcademyName || '';
          this.awayAcademyName = data.awayAcademyName || data.AwayAcademyName || '';
          this.tournamentId = data.tournamentId || data.TournamentId;
          this.tournamentName = data.tournamentName || data.TournamentName || '';
          this.groupOrRoundName = data.groupOrRoundName || data.GroupOrRoundName || '';
        }
      },
      error: (err) => {
        this.isLoadingFixture = false;
        console.error('Error fetching fixture details:', err);
        const msg = err?.error?.message || 'Failed to load fixture details. Please verify the URL or restart the backend server.';
        this.errorMessage = msg;
      }
    });
  }

  private formatDateForInput(date: Date): string {
    const pad = (n: number) => n < 10 ? '0' + n : n;
    const year = date.getFullYear();
    const month = pad(date.getMonth() + 1);
    const day = pad(date.getDate());
    const hours = pad(date.getHours());
    const minutes = pad(date.getMinutes());
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  onSubmit(): void {
    if (!this.fixtureId || !this.homeTeamId || !this.awayTeamId) {
      this.errorMessage = 'Invalid fixture or team identifiers. Please navigate from the tournament bracket.';
      return;
    }

    if (!this.matchDate) {
      this.errorMessage = 'Please select a valid scheduled match date and time.';
      return;
    }

    const selectedDate = new Date(this.matchDate);
    if (selectedDate <= new Date()) {
      this.errorMessage = 'Match date and time must be in the future.';
      return;
    }

    if (!this.location || !this.location.trim()) {
      this.errorMessage = 'Please specify the match location / stadium.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = null;

    const dto: CreateTournamentMatchDto = {
      tournamentFixtureId: this.fixtureId,
      homeTeamId: this.homeTeamId,
      awayTeamId: this.awayTeamId,
      matchDate: selectedDate.toISOString(),
      location: this.location.trim()
    };

    this.matchService.createTournamentMatch(dto).subscribe({
      next: (res) => {
        this.isSubmitting = false;
        this.toastService.show('Tournament match scheduled successfully!', 'success');
        
        const createdMatchId = res?.data?.id || res?.data?.Id;
        if (createdMatchId) {
          this.router.navigate(['/match', createdMatchId]);
        } else if (this.tournamentId) {
          this.router.navigate(['/tournament/details', this.tournamentId]);
        } else {
          this.router.navigate(['/tournament/list']);
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        const msg = err?.error?.message || 'Failed to create tournament match. Please try again.';
        this.errorMessage = msg;
        this.toastService.show(msg, 'error');
      }
    });
  }

  onCancel(): void {
    if (this.tournamentId) {
      this.router.navigate(['/tournament/details', this.tournamentId]);
    } else {
      window.history.back();
    }
  }
}
