import { Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { forkJoin, of, take,tap } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { TournamentService } from '../../../../../core/services/tournament/tournament.service';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { User } from '../../../../../core/interfaces/user.model';
import { MatchFormat, Tournament, TournamentStatus, TournamentStructure, CreateTournamentDto } from '../../../../../core/interfaces/tournament.models';

import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomSelect } from '../../../../../shared/components/custom-select/custom-select';
import { CustomToggle } from '../../../../../shared/components/custom-toggle/custom-toggle';
import { StatusChipComponent } from '../../../../../shared/components/status-chip/status-chip';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';
import { NotificationService } from '@core/services/SignalR/notificationservice';

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
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);
  private notificationService = inject(NotificationService);

  tournamentForm!: FormGroup;
  tournamentId: number | null = null;
  tournament: Tournament | null = null;
  teams: any[] = [];
  rounds: any[] = [];
  groups: any[] = [];
  currentUser: User | null = null;
  isSystemAdmin = false;
  availableAcademies: any[] = [];
  allowedAgeGroupNames = ['U17', 'U15', 'First Team'];
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

  ageGroupOptions: { value: number; label: string }[] = [];

  ngOnInit() {
    this.initForm();
    const id = this.route.snapshot.paramMap.get('id');

    if (id) {
      this.tournamentId = +id;
      this.loadManagementData();
    } else {
      this.initCurrentUserAndLoadData();
    }
  }

  private initCurrentUserAndLoadData() {
    this.authService.currentUser$.pipe(take(1)).subscribe((user) => {
      this.currentUser = user;
      this.isSystemAdmin = user?.roles?.includes('SystemAdmin') ?? false;

      if (this.isSystemAdmin) {
        this.loadGlobalAgeGroups();
      } else {
        this.loadAgeGroups();
      }
    });
  }

  private normalizeAgeGroupName(name: string | null | undefined): string {
    if (!name) return '';
    return name.trim().toLowerCase();
  }

  private formatAgeGroupLabel(name: string): string {
    const normalized = this.normalizeAgeGroupName(name);
    if (normalized === 'first team') return 'First Team';
    return normalized.toUpperCase();
  }

  private dedupeAgeGroups(ageGroups: any[]): any[] {
    const unique = new Map<string, any>();
    const allowed = this.allowedAgeGroupNames.map(name => name.toLowerCase());

    ageGroups.forEach((ag: any) => {
      const normalizedName = this.normalizeAgeGroupName(ag.name);
      if (!allowed.includes(normalizedName)) return;
      if (!unique.has(normalizedName)) {
        unique.set(normalizedName, ag);
      }
    });

    return this.allowedAgeGroupNames
      .map(name => unique.get(name.toLowerCase()))
      .filter((ag): ag is any => !!ag);
  }

  private loadAgeGroups() {
    const academyId = this.currentUser?.academyId;

    if (!academyId) {
      this.ageGroupOptions = [];
      this.errorMessage = 'You must belong to an academy to select an age group.';
      this.cdr.markForCheck();
      return;
    }

    this.errorMessage = '';
    this.academyService.getAgeGroups(academyId).pipe(
      catchError(() => of(null))
    ).subscribe(response => {
      const data = response?.data || response;
      const ageGroups = Array.isArray(data) ? this.dedupeAgeGroups(data) : [];

      if (ageGroups.length > 0) {
        this.ageGroupOptions = ageGroups.map((ag: any) => ({
          value: ag.id,
          label: this.formatAgeGroupLabel(ag.name)
        }));
        this.tournamentForm.get('ageGroupId')?.setValue(this.ageGroupOptions[0].value);
      } else {
        this.ageGroupOptions = [];
        this.errorMessage = 'No eligible age groups are available for your academy. Please create U17, U15, or First Team age groups first.';
      }
      this.cdr.markForCheck();
    });
  }

  private loadGlobalAgeGroups() {
    this.errorMessage = '';
    this.academyService.getAgeGroupsByNames(this.allowedAgeGroupNames).pipe(
      catchError(() => of(null))
    ).subscribe(response => {
      const data = response?.data || response;
      const ageGroups = Array.isArray(data) ? this.dedupeAgeGroups(data) : [];

      if (ageGroups.length > 0) {
        this.ageGroupOptions = ageGroups.map((ag: any) => ({
          value: ag.id,
          label: this.formatAgeGroupLabel(ag.name)
        }));
        this.tournamentForm.get('ageGroupId')?.setValue(this.ageGroupOptions[0].value);
      } else {
        this.ageGroupOptions = [];
        this.errorMessage = 'No U17, U15, or First Team age groups are available. Create the required age groups in any academy first.';
      }
      this.cdr.markForCheck();
    });
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
      ageGroupId: [null as number | null, Validators.required],
      hasTwoLegs: [false],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      entryFee: [0, [Validators.required, Validators.min(0)]]
    }, { validators: this.dateRangeValidator });
  }

  private dateRangeValidator(group: FormGroup) {
    const start = group.get('startDate')?.value;
    const end = group.get('endDate')?.value;
    if (start && end && new Date(start) > new Date(end)) {
      return { invalidDateRange: true };
    }
    return null;
  }

  loadManagementData(keepMessages = false, showLoading = true) {
    if (!this.tournamentId) return;

    if (showLoading) {
      this.isLoading = true;
    }
    if (!keepMessages) {
      this.clearMessages();
    }
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

        let academies: any[] = [];
        if (Array.isArray(responses.academies)) {
          academies = responses.academies;
        } else if (Array.isArray(responses.academies?.data?.academies)) {
          academies = responses.academies.data.academies;
        } else if (Array.isArray(responses.academies?.data)) {
          academies = responses.academies.data;
        } else if (Array.isArray(responses.academies?.academies)) {
          academies = responses.academies.academies;
        } else if (Array.isArray(responses.academies?.data?.items)) {
          academies = responses.academies.data.items;
        }

        this.availableAcademies = academies.map((academy: any) => ({
          value: academy.id,
          label: academy.name ? (academy.city ? `${academy.name} - ${academy.city}` : academy.name) : `Academy #${academy.id}`
        }));
        this.selectedAcademyId = this.availableAcademies[0]?.value || null;

        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.tournament = null;
        this.availableAcademies = [];
        this.selectedAcademyId = null;
        this.errorMessage = this.extractError(err, 'Unable to load tournament details.');
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
    if (controlName === 'endDate' && this.tournamentForm.hasError('invalidDateRange')) {
      return 'End date cannot be earlier than start date.';
    }

    if (control && control.invalid && (control.dirty || control.touched)) {
      const fieldNames: Record<string, string> = {
        name: 'Tournament name',
        ageGroupId: 'Age group',
        format: 'Match format',
        structure: 'Tournament structure',
        startDate: 'Start date',
        endDate: 'End date',
        entryFee: 'Entry fee'
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

    const tournamentName = this.tournament?.name || 'The tournament';

    this.runAction(
      'status', 

      () => this.tournamentService.updateStatus(this.tournamentId!, this.selectedStatus).pipe(
        tap(() => {
          if (this.selectedStatus === TournamentStatus.Cancelled) {
            this.notifyParticipatingAcademies(`⚠️ ${tournamentName} has been cancelled.`);
          } else if (this.selectedStatus === TournamentStatus.InProgress) {
            this.notifyParticipatingAcademies(`⚽ ${tournamentName} is now officially in progress. Good luck to all teams!`);
          }
          else if (this.selectedStatus === TournamentStatus.Registration) {
            this.notifyParticipatingAcademies(`📢 ${tournamentName} is now open for registration. Teams can now sign up!`);
          }
          else if (this.selectedStatus === TournamentStatus.Completed) {
            this.notifyParticipatingAcademies(`🏆 ${tournamentName} has been completed. Check the AI Wrap-Up Report for insights and results.`);
          }
        })
      ), 
      'Tournament status updated successfully.'
    );
  }

 inviteSelectedAcademy() {
  if (!this.tournamentId || !this.selectedAcademyId) return;

  const message = `Your academy has been invited to participate in the upcoming tournament.`;
  const academyIdToNotify = this.selectedAcademyId;

  this.runAction(
    'invite', 
    () => this.tournamentService.inviteAcademy(this.tournamentId!, academyIdToNotify).pipe(
      tap(() => {
        this.notificationService.notifyAcademy(academyIdToNotify, message).subscribe({
          error: (e) => console.error('Failed to send invite notification', e)
        });
      })
    ), 
    'Academy invited successfully.'
  );
}

  generateSeeding() {
    if (!this.tournamentId) return;
    this.runAction('seeding', () => this.tournamentService.generateSeeding(this.tournamentId!), 'Seeding generated successfully.');
  }

 generateDraw() {
    if (!this.tournamentId) return;

    const tournamentName = this.tournament?.name || 'The tournament';

    this.runAction(
      'draw', 
      () => this.tournamentService.generateDraw(this.tournamentId!).pipe(
        tap(() => {
          this.notifyParticipatingAcademies(`📅 The draw for ${tournamentName} has been published. Check your groups and fixtures!`);
        })
      ), 
      'Draw generated successfully.'
    );
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
        this.successMessage =
          '🏆 Tournament finalized! The AI Wrap-Up Report is being generated in the background — ' +
          'you will receive a notification and it will appear in the AI Insights tab automatically within moments.';
        this.activeAction = null;
        this.loadManagementData(true, false);
        // Notify all participating academies about the tournament completion
        const completionMessage = `The tournament "${this.tournament?.name}" has been completed. Check the AI Wrap-Up Report for insights and results.`;
        this.notifyParticipatingAcademies(completionMessage);
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
        this.loadManagementData(true, false);
      },
      error: (err: any) => {
        this.errorMessage = this.extractError(err, 'Unable to complete tournament action.');
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
private notifyParticipatingAcademies(message: string) {
  if (!this.teams || !this.teams.length) return;
  
  
  const uniqueAcademyIds = [...new Set(this.teams.map(t => t.academyId).filter(id => !!id))];

  if (uniqueAcademyIds.length === 0) return;
  this.notificationService.notifyMultipleAcademies(uniqueAcademyIds, message).subscribe({
    next: () => console.log('Successfully broadcasted notification to all participating academies.'),
    error: (e) => console.error('Failed to broadcast academy notifications', e)
  });
}
}