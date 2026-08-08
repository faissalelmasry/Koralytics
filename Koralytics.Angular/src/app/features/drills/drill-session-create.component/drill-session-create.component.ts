import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../shared/pipes/localized-date.pipe';
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DrillSessionService } from '../../../../core/services/drill/drill-session.service';
import { AcademyService } from '../../../../core/services/academy/academy.service';
import { AuthService } from '../../../../core/services/auth/auth.service'; // 🟢 Adjusted to your service
import { CreateDrillSessionDto } from '../../../../core/interfaces/drill-session.model';
import { SessionType, SessionStatus } from '../../../../core/enums/koralytics.enums';

import { CustomSelect, SelectOption } from '../../../../shared/components/custom-select/custom-select';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { formatToLocalISO } from '../../../../core/utils/date.util';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { CustomDatePicker } from '../../../../shared/components/custom-date-picker/custom-date-picker';
import { NotificationService } from '@core/services/SignalR/notificationservice';

@Component({
  selector: 'app-drill-session-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomSelect, CustomButtonComponent, CustomDatePicker, TranslatePipe, LocalizedDatePipe],
  templateUrl: './drill-session-create.component.html',
  styleUrls: ['./drill-session-create.component.css']
})
export class DrillSessionCreateComponent implements OnInit {
  private translate = inject(TranslateService);
  sessionForm!: FormGroup;
  isSubmitting = false;
  errorMessage = '';

  SessionType = SessionType;

  get teamOptions(): SelectOption[] {
    return this.availableTeams.map(t => ({ value: t.id, label: t.name }));
  }

  get typeOptions(): SelectOption[] {
    return [
      { value: SessionType.PreSeason, label: this.translate.instant('DRILLS.SESSION_CREATE.PRE_SEASON') || 'Pre-Season' },
      { value: SessionType.Regular, label: this.translate.instant('DRILLS.SESSION_CREATE.REGULAR') || 'Regular' },
      { value: SessionType.OffSeason, label: this.translate.instant('DRILLS.SESSION_CREATE.OFF_SEASON') || 'Off-Season' },
      { value: SessionType.SessionMatch, label: this.translate.instant('DRILLS.SESSION_CREATE.MATCH') || 'Match' }
    ];
  }

  onTeamChangeCustom(val: any): void {
    this.sessionForm.get('teamId')?.setValue(val);
  }

  onTypeChangeCustom(val: any): void {
    this.sessionForm.get('type')?.setValue(val);
  }

  fullTeamsData: any[] = [];
  availableTeams: { id: number; name: string }[] = [];
  availablePlayers: { id: number; name: string; position: string; selected: boolean }[] = [];

  // 🟢 No hardcoded data!
  currentAcademyId!: number;

  constructor(
    private fb: FormBuilder,
    private sessionService: DrillSessionService,
    private academyService: AcademyService,
    private router: Router,
    private authService: AuthService ,// 🟢 Auth Service Injected
    private notificationService: NotificationService
  ) { }

  ngOnInit(): void {
    const currentUser = this.authService.getCurrentUserValue();
    if (currentUser?.roles?.some(r => r.toLowerCase() === 'academyadmin')) {
      this.router.navigate(['/drills/sessions']);
      return;
    }
    this.initForm();
    this.setDynamicAcademyId(); // Load the ID before making API calls
  }

  private setDynamicAcademyId(): void {
    // 🟢 Use your specific method: getCurrentUserValue()
    const currentUser = this.authService.getCurrentUserValue();

    if (currentUser && currentUser.academyId) {
      this.currentAcademyId = currentUser.academyId;

      // Once we have the real Academy ID, fetch their specific teams!
      this.loadAcademyTeams();
    } else {
      console.error('Authentication Error: No Academy ID found for this user.');
      this.errorMessage = 'Could not verify your Academy credentials. Please log in again.';
    }
  }

  private initForm(): void {
    const today = new Date().toISOString().split('T')[0];

    this.sessionForm = this.fb.group({
      teamId: ['', Validators.required],
      sessionDate: [today, Validators.required],
      sessionTime: ['16:00', Validators.required],
      type: [SessionType.Regular, Validators.required],
      location: ['Main Pitch - North', Validators.required],
      notes: ['']
    });

    this.sessionForm.get('teamId')?.valueChanges.subscribe(teamId => {
      if (teamId) {
        this.loadTeamRoster(Number(teamId));
      } else {
        this.availablePlayers = [];
      }
    });
  }

  private loadAcademyTeams(): void {
    this.academyService.getTeams(this.currentAcademyId).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.fullTeamsData = res.data;
          this.availableTeams = res.data.map((t: any) => ({
            id: t.id,
            name: t.name
          }));
        }
      },
      error: (err) => console.error('Failed to load teams', err)
    });
  }

  private loadTeamRoster(teamId: number): void {
    const selectedTeam = this.fullTeamsData.find(t => t.id === teamId);

    if (selectedTeam && selectedTeam.players) {
      this.availablePlayers = selectedTeam.players.map((p: any) => ({
        id: p.playerId,
        name: p.playerFullName,
        position: p.position || "Squad Player",
        selected: true
      }));
    } else {
      this.availablePlayers = [];
    }
  }

  togglePlayerSelection(player: any): void {
    player.selected = !player.selected;
  }

  selectAllPlayers(): void {
    this.availablePlayers.forEach(p => p.selected = true);
  }

  deselectAllPlayers(): void {
    this.availablePlayers.forEach(p => p.selected = false);
  }

  onSubmit(): void {
    if (this.sessionForm.invalid) {
      this.sessionForm.markAllAsTouched();
      return;
    }

    const selectedPlayerIds = this.availablePlayers
      .filter(p => p.selected)
      .map(p => p.id);

    if (selectedPlayerIds.length === 0) {
      this.errorMessage = 'You must select at least one player for the session.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    const formValue = this.sessionForm.value;
    const dateTimeString = `${formValue.sessionDate}T${formValue.sessionTime}:00`;

    const payload: CreateDrillSessionDto = {
      teamId: Number(formValue.teamId),
      sessionDate: formatToLocalISO(dateTimeString),
      type: Number(formValue.type) as SessionType,
      status: SessionStatus.Scheduled,
      location: formValue.location,
      notes: formValue.notes,
      playerIds: selectedPlayerIds
    };

    this.sessionService.createSession(payload).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        //notification
        selectedPlayerIds.forEach(playerId => {
          const playerMsg = "A new training session has been scheduled for your team.";
          const parentMsg = "A new training session has been scheduled for your child's team.";

          this.notificationService.notifyPlayerMilestone(playerId, playerMsg).subscribe({
            error: (e) => console.error(`Failed to notify player ${playerId} for new session`, e)
          });

          this.notificationService.notifyPlayerParents(playerId, parentMsg).subscribe({
            error: (e) => console.error(`Failed to notify parent for player ${playerId} new session`, e)
          });
        });
        this.router.navigate(['/drills/sessions']);
      },
      error: (err) => {
        this.isSubmitting = false;
        console.error('Full API Error:', err);
        this.errorMessage = err.error?.title || err.error?.message || err.error || 'Failed to schedule the session.';
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/drills/sessions']);
  }
}