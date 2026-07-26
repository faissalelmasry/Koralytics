import { Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { TournamentService } from '../../../../../core/services/tournament/tournament.service';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { MatchFormat, Tournament, TournamentStatus, TournamentStructure, CreateTournamentDto } from '../../../../../core/interfaces/tournament.models';

import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomSelect } from '../../../../../shared/components/custom-select/custom-select';
import { CustomToggle } from '../../../../../shared/components/custom-toggle/custom-toggle';
import { StatusChipComponent } from '../../../../../shared/components/status-chip/status-chip';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

type ManagementAction = 'status' | 'invite' | 'seeding' | 'draw' | 'advance' | 'complete';

@Component({
  selector: 'app-tournament-manage',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CustomInputComponent,
    CustomSelect,
    CustomToggle,
    StatusChipComponent,
    ScrollRevealDirective
  ],
  templateUrl: './tournament-manage.component.html',
  styleUrls: ['./tournament-manage.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TournamentManageComponent implements OnInit {
  private fb = inject(FormBuilder);
  private location = inject(Location);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private tournamentService = inject(TournamentService);
  private academyService = inject(AcademyService);
  private cdr = inject(ChangeDetectorRef);

  tournamentForm!: FormGroup;
  tournamentId: number | null = null;
  tournament: Tournament | null = null;
  teams: any[] = [];
  rounds: any[] = [];
  groups: any[] = [];
  availableAcademies: any[] = [];
  selectedStatus: TournamentStatus = TournamentStatus.Draft;
  selectedAcademyId: number | null = null;
  isLoading = false;
  isSubmitting = false;
  activeAction: ManagementAction | null = null;
  errorMessage = '';
  successMessage = '';

  formatOptions = [
    { value: MatchFormat.FiveSide, label: '5 vs 5' },
    { value: MatchFormat.SevenSide, label: '7 vs 7' },
    { value: MatchFormat.ElevenSide, label: '11 vs 11' }
  ];

  structureOptions = [
    { value: TournamentStructure.Knockout, label: 'Knockout Stage Only' },
    { value: TournamentStructure.GroupAndKnockout, label: 'Groups + Knockout' },
    { value: TournamentStructure.League, label: 'League Format' }
  ];

  statusOptions = [
    { value: TournamentStatus.Draft, label: 'Draft' },
    { value: TournamentStatus.Registration, label: 'Registration' },
    { value: TournamentStatus.InProgress, label: 'In Progress' },
    { value: TournamentStatus.Completed, label: 'Completed' },
    { value: TournamentStatus.Cancelled, label: 'Cancelled' }
  ];

  ageGroupOptions = [
    { value: 1, label: 'Under 15 (U15)' },
    { value: 2, label: 'Under 18 (U18)' },
    { value: 3, label: 'First Team' }
  ];

  ngOnInit() {
    this.initForm();
    const id = this.route.snapshot.paramMap.get('id');

    if (id) {
      this.tournamentId = +id;
      this.loadManagementData();
    }
  }

  get isEditMode(): boolean {
    return this.tournamentId !== null;
  }

  get pageTitle(): string {
    return this.isEditMode ? 'Tournament Control Center' : 'Initialize Tournament';
  }

  get pageDescription(): string {
    return this.isEditMode
      ? 'Run invitations, seeding, draw generation, progression, and status operations.'
      : 'Configure the foundational rules, structure, and dates for your new championship.';
  }

  get latestRound(): any | null {
    if (!this.rounds.length) return null;
    return [...this.rounds].sort((a, b) => (b.roundNumber || 0) - (a.roundNumber || 0))[0];
  }

  get actionDisabled(): boolean {
    return this.activeAction !== null || this.isLoading;
  }

  private initForm() {
    this.tournamentForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      format: [MatchFormat.ElevenSide, Validators.required],
      structure: [TournamentStructure.GroupAndKnockout, Validators.required],
      ageGroupId: [1, Validators.required],
      hasTwoLegs: [false],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required]
    });
  }

  loadManagementData() {
    if (!this.tournamentId) return;

    this.isLoading = true;
    this.clearMessages();
    this.cdr.markForCheck();

    forkJoin({
      details: this.tournamentService.getTournamentById(this.tournamentId).pipe(catchError(() => of(null))),
      bracket: this.tournamentService.getBracket(this.tournamentId).pipe(catchError(() => of(null))),
      teams: this.tournamentService.getTournamentTeams(this.tournamentId).pipe(catchError(() => of(null))),
      academies: this.academyService.getAcademies().pipe(catchError(() => of(null)))
    }).subscribe({
      next: (responses) => {
        this.tournament = responses.details?.data || responses.details || null;
        this.selectedStatus = this.tournament?.status || TournamentStatus.Draft;

        const bracketData = responses.bracket?.data || responses.bracket;
        this.rounds = bracketData?.rounds || [];
        this.groups = bracketData?.groups || [];

        const teamsData = responses.teams?.data || responses.teams;
        this.teams = Array.isArray(teamsData) ? teamsData : [];

        const academyPayload = responses.academies?.data || responses.academies;
        const academiesArray = academyPayload?.academies || academyPayload;
        const academies = Array.isArray(academiesArray) ? academiesArray : [];
        this.availableAcademies = academies.map((academy: any) => ({
          value: academy.id,
          label: academy.city ? `${academy.name} - ${academy.city}` : academy.name
        }));
        this.selectedAcademyId = this.availableAcademies[0]?.value || null;

        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.errorMessage = 'Unable to load tournament management data.';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  getControlValue(controlName: string) {
    return this.tournamentForm.get(controlName)?.value;
  }

  setControlValue(controlName: string, value: any) {
    this.tournamentForm.get(controlName)?.setValue(value);
    this.tournamentForm.get(controlName)?.markAsTouched();
  }

  hasError(controlName: string): string {
    const control = this.tournamentForm.get(controlName);
    if (control && control.invalid && (control.dirty || control.touched)) {
      const fieldNames: Record<string, string> = {
        name: 'Tournament name',
        ageGroupId: 'Age group',
        format: 'Match format',
        structure: 'Tournament structure',
        startDate: 'Start date',
        endDate: 'End date'
      };
      const fieldName = fieldNames[controlName] || controlName;

      if (control.hasError('required')) return `${fieldName} is required.`;
      if (control.hasError('minlength')) return `${fieldName} must be at least 3 characters.`;
      return `${fieldName} is invalid.`;
    }
    return '';
  }

  setSelectedStatus(status: TournamentStatus) {
    this.selectedStatus = status;
  }

  setSelectedAcademy(academyId: number) {
    this.selectedAcademyId = academyId;
  }

  goBack() {
    this.location.back();
  }

  onSubmit() {
    if (this.tournamentForm.invalid) {
      this.tournamentForm.markAllAsTouched();
      this.errorMessage = 'Review the highlighted fields before creating the tournament.';
      this.cdr.markForCheck();
      return;
    }

    this.isSubmitting = true;
    this.clearMessages();
    this.cdr.markForCheck();

    const dto: CreateTournamentDto = this.tournamentForm.value;

    this.tournamentService.createTournament(dto).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.cdr.markForCheck();
        this.router.navigate(['/tournament/list']);
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = this.extractError(err, 'Failed to create tournament. Please try again.');
        this.cdr.markForCheck();
      }
    });
  }

  updateStatus() {
    if (!this.tournamentId) return;
    this.runAction('status', () => this.tournamentService.updateStatus(this.tournamentId!, this.selectedStatus), 'Tournament status updated.');
  }

  inviteSelectedAcademy() {
    if (!this.tournamentId || !this.selectedAcademyId) return;
    this.runAction('invite', () => this.tournamentService.inviteAcademy(this.tournamentId!, this.selectedAcademyId!), 'Academy invited successfully.');
  }

  generateSeeding() {
    if (!this.tournamentId) return;
    this.runAction('seeding', () => this.tournamentService.generateSeeding(this.tournamentId!), 'Seeding generated successfully.');
  }

  generateDraw() {
    if (!this.tournamentId) return;
    this.runAction('draw', () => this.tournamentService.generateDraw(this.tournamentId!), 'Draw generated successfully.');
  }

  advanceKnockout() {
    if (!this.tournamentId || !this.latestRound) return;
    const roundId = this.latestRound.roundId || this.latestRound.id;
    this.runAction('advance', () => this.tournamentService.advanceKnockout(this.tournamentId!, roundId), 'Knockout round advanced successfully.');
  }

  completeTournament() {
    if (!this.tournamentId) return;

    this.activeAction = 'complete';
    this.clearMessages();
    this.cdr.markForCheck();

    this.tournamentService.completeTournament(this.tournamentId).subscribe({
      next: () => {
        this.successMessage = 'Tournament completed successfully.';
        this.activeAction = null;
        this.loadManagementData();
      },
      error: (err) => {
        this.errorMessage = this.extractError(err, 'Unable to complete tournament. Make sure all fixtures are completed first.');
        this.activeAction = null;
        this.cdr.markForCheck();
      }
    });
  }

  private runAction(action: ManagementAction, request: () => any, successMessage: string) {
    this.activeAction = action;
    this.clearMessages();
    this.cdr.markForCheck();

    request().subscribe({
      next: () => {
        this.successMessage = successMessage;
        this.activeAction = null;
        this.loadManagementData();
      },
      error: (err: any) => {
        this.errorMessage = this.extractError(err, successMessage.replace('successfully.', 'failed.'));
        this.activeAction = null;
        this.cdr.markForCheck();
      }
    });
  }

  private clearMessages() {
    this.errorMessage = '';
    this.successMessage = '';
  }

  private extractError(err: any, fallback: string): string {
    if (!err?.error) return fallback;
    if (typeof err.error === 'string') return err.error;
    if (err.error.errors) return Object.values(err.error.errors).map((e: any) => e.join(', ')).join(' | ');
    return err.error.message || err.error.detail || err.error.title || fallback;
  }
}
