import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { MatchService } from '../../../../../core/services/match/match.service';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { CustomSelect, SelectOption } from '../../../../../shared/components/custom-select/custom-select';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { CustomDateTimePicker } from '../../../../../shared/components/custom-date-time-picker/custom-date-time-picker';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

import { formatToLocalISO } from '../../../../../core/utils/date.util';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-friendly-match-request',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LoadingSpinnerComponent,
    CustomSelect,
    CustomInputComponent,
    CustomButtonComponent,
    CustomDateTimePicker,
    ScrollRevealDirective,
    TranslatePipe
  ],
  templateUrl: './friendly-match-request.component.html',
  styleUrls: ['./friendly-match-request.component.css']
})
export class FriendlyMatchRequestComponent implements OnInit {
  private academyService = inject(AcademyService);
  private matchService = inject(MatchService);
  private coachSquadService = inject(CoachSquadService);
  private translate = inject(TranslateService);

  isLoadingTeams = true;
  isSubmitting = false;
  error = '';
  success = '';

  coachTeams: SelectOption[] = [];
  requesterTeamId: number | null = null;

  academySearchQuery = '';
  academySearchResults: { id: number; name: string }[] = [];
  showAcademyDropdown = false;
  selectedAcademyId: number | null = null;
  selectedAcademyName = '';

  opponentTeams: SelectOption[] = [];
  targetTeamId: number | null = null;
  isLoadingOpponentTeams = false;

  format = 'ElevenSide';
  get formatOptions(): SelectOption[] {
    return [
      { value: 'ElevenSide', label: this.translate.instant('MATCH.FORMAT.ELEVEN_SIDE') },
      { value: 'SevenSide', label: this.translate.instant('MATCH.FORMAT.SEVEN_SIDE') },
      { value: 'FiveSide', label: this.translate.instant('MATCH.FORMAT.FIVE_SIDE') }
    ];
  }

  proposedDate = '';
  location = '';

  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.loadCoachTeams();
  }

  loadCoachTeams(): void {
    this.isLoadingTeams = true;
    this.coachSquadService.getCoachTeams().subscribe({
      next: (res: any) => {
        const teams = res?.data ?? res ?? [];
        this.coachTeams = teams.map((t: any) => ({
          value: t.teamId ?? t.TeamId,
          label: `${t.teamName ?? t.TeamName} (${t.ageGroupName ?? t.AgeGroupName})`
        }));
        this.isLoadingTeams = false;
      },
      error: () => {
        this.error = this.translate.instant('MATCH.ERRORS.GENERIC_LOAD');
        this.isLoadingTeams = false;
      }
    });
  }

  onAcademySearch(value: string): void {
    this.academySearchQuery = value;
    if (this.searchTimer) clearTimeout(this.searchTimer);

    if (!value.trim()) {
      this.academySearchResults = [];
      this.showAcademyDropdown = false;
      return;
    }

    this.searchTimer = setTimeout(() => {
      this.academyService.searchAcademies(value.trim()).subscribe({
        next: (res: any) => {
          const results = res?.data ?? res ?? [];
          this.academySearchResults = results.map((a: any) => ({
            id: a.id ?? a.Id,
            name: a.name ?? a.Name
          }));
          this.showAcademyDropdown = this.academySearchResults.length > 0;
        },
        error: () => {
          this.academySearchResults = [];
          this.showAcademyDropdown = false;
        }
      });
    }, 300);
  }

  selectAcademy(academy: { id: number; name: string }): void {
    this.selectedAcademyId = academy.id;
    this.selectedAcademyName = academy.name;
    this.academySearchQuery = academy.name;
    this.showAcademyDropdown = false;
    this.targetTeamId = null;
    this.opponentTeams = [];
    this.loadOpponentTeams();
  }

  clearAcademy(): void {
    this.selectedAcademyId = null;
    this.selectedAcademyName = '';
    this.academySearchQuery = '';
    this.academySearchResults = [];
    this.showAcademyDropdown = false;
    this.targetTeamId = null;
    this.opponentTeams = [];
  }

  loadOpponentTeams(): void {
    if (!this.selectedAcademyId) return;
    this.isLoadingOpponentTeams = true;
    this.academyService.getAcademyTeamsSummary(this.selectedAcademyId).subscribe({
      next: (res: any) => {
        const teams = res?.data ?? res ?? [];
        this.opponentTeams = teams.map((t: any) => ({
          value: t.id ?? t.Id,
          label: `${t.name ?? t.Name} (${t.ageGroupName ?? t.AgeGroupName})`
        }));
        this.isLoadingOpponentTeams = false;
      },
      error: () => {
        this.error = this.translate.instant('MATCH.ERRORS.GENERIC_LOAD');
        this.isLoadingOpponentTeams = false;
      }
    });
  }

  onBlurAcademySearch(): void {
    setTimeout(() => { this.showAcademyDropdown = false; }, 200);
  }

  onFocusAcademySearch(): void {
    if (this.academySearchResults.length > 0 && !this.selectedAcademyId) {
      this.showAcademyDropdown = true;
    }
  }

  get requesterTeamName(): string {
    const opt = this.coachTeams.find(t => t.value === this.requesterTeamId);
    return opt?.label?.split(' (')[0] ?? '';
  }

  get targetTeamName(): string {
    const opt = this.opponentTeams.find(t => t.value === this.targetTeamId);
    return opt?.label?.split(' (')[0] ?? '';
  }

  submit(): void {
    this.error = '';
    this.success = '';

    if (!this.requesterTeamId) {
      this.error = this.translate.instant('MATCH.ERRORS.GENERIC_LOAD');
      return;
    }
    if (!this.targetTeamId) {
      this.error = this.translate.instant('MATCH.ERRORS.GENERIC_LOAD');
      return;
    }
    if (this.requesterTeamId === this.targetTeamId) {
      this.error = this.translate.instant('MATCH.FRIENDLY_REQUEST.ERROR_SAME_TEAM');
      return;
    }
    if (!this.format) {
      this.error = this.translate.instant('MATCH.ERRORS.GENERIC_LOAD');
      return;
    }
    if (!this.proposedDate) {
      this.error = this.translate.instant('MATCH.ERRORS.GENERIC_LOAD');
      return;
    }

    this.isSubmitting = true;

    const dto = {
      requesterTeamId: this.requesterTeamId,
      targetTeamId: this.targetTeamId,
      format: this.format,
      proposedDate: formatToLocalISO(this.proposedDate),
      location: this.location || undefined
    };

    this.matchService.requestFriendlyMatch(dto).subscribe({
      next: () => {
        this.success = this.translate.instant('MATCH.FRIENDLY_REQUEST.SUCCESS');
        this.isSubmitting = false;
        this.resetForm();
      },
      error: (err: any) => {
        const msg = err?.error?.detail ?? err?.error?.message ?? err?.message ?? 'Failed to send match request.';
        this.error = msg;
        this.isSubmitting = false;
      }
    });
  }

  resetForm(): void {
    this.academySearchQuery = '';
    this.academySearchResults = [];
    this.showAcademyDropdown = false;
    this.selectedAcademyId = null;
    this.selectedAcademyName = '';
    this.targetTeamId = null;
    this.opponentTeams = [];
    this.format = 'ElevenSide';
    this.proposedDate = '';
    this.location = '';
  }
}
