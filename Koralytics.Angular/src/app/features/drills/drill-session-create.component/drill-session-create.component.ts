import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DrillSessionService } from '../../../../core/services/drill/drill-session.service';
import { CreateDrillSessionDto } from '../../../../core/interfaces/drill-session.model';
import { SessionType, SessionStatus } from '../../../../core/enums/koralytics.enums';

@Component({
  selector: 'app-drill-session-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './drill-session-create.component.html',
  styleUrls: ['./drill-session-create.component.css']
})
export class DrillSessionCreateComponent implements OnInit {
  sessionForm!: FormGroup;
  isSubmitting = false;
  errorMessage = '';

  // Enums for the template
  SessionType = SessionType;

  // Convert numeric enum to array for the dropdown
  sessionTypes = Object.keys(SessionType)
    .filter(key => isNaN(Number(key)))
    .map(key => ({
      value: SessionType[key as keyof typeof SessionType],
      label: key.replace(/([A-Z])/g, ' $1').trim() // "PreSeason" -> "Pre Season"
    }));

  // Mock data for dropdowns (Replace with real service calls later)
  availableTeams = [
    { id: 1, name: 'U17 Team A' },
    { id: 101, name: 'Alexandria Elite U-19' }
  ];

  availablePlayers: { id: number; name: string; position: string; selected: boolean }[] = [];

  constructor(
    private fb: FormBuilder,
    private sessionService: DrillSessionService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.initForm();
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

    // Listen for Team selection changes to load the roster
    this.sessionForm.get('teamId')?.valueChanges.subscribe(teamId => {
      if (teamId) {
        this.loadTeamRoster(Number(teamId));
      } else {
        this.availablePlayers = [];
      }
    });
  }

  // TODO: Connect this to your real PlayerService to get players by TeamId
  private loadTeamRoster(teamId: number): void {
    // Mocking a roster load for the UI
    this.availablePlayers = [
      { id: 1, name: 'Youssef Ahmed', position: 'ST', selected: true },
      { id: 2, name: 'Omar Tarek', position: 'CM', selected: true },
      { id: 3, name: 'Karim Hassan', position: 'CB', selected: true },
      { id: 4, name: 'Mahmoud Ali', position: 'GK', selected: true },
      { id: 5, name: 'Ziad Mohamed', position: 'LW', selected: true }
    ];
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

    // Combine Date and Time into a single ISO string
    const dateTimeString = `${formValue.sessionDate}T${formValue.sessionTime}:00`;

    const payload: CreateDrillSessionDto = {
      teamId: Number(formValue.teamId),
      sessionDate: new Date(dateTimeString).toISOString(),
      type: Number(formValue.type) as SessionType,
      status: SessionStatus.Scheduled, // Always defaults to Scheduled (0)
      location: formValue.location,
      notes: formValue.notes,
      playerIds: selectedPlayerIds
    };

    this.sessionService.createSession(payload).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        this.router.navigate(['/drills/sessions']);
      },
      error: (err) => {
        this.isSubmitting = false;
        console.error('Full API Error:', err);

        // 🟢 THIS WILL SHOW THE REAL C# ERROR IN THE RED BANNER
        this.errorMessage = err.error?.title || err.error?.message || err.error || 'Failed to schedule the session.';
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/drills/sessions']);
  }
}