import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DrillSessionService } from '../../../../core/services/drill/drill-session.service';
import { DrillTemplateService } from '../../../../core/services/drill/drill-template.service';
import { AuthService } from '../../../../core/services/auth/auth.service';
import { CustomSelect, SelectOption } from '../../../../shared/components/custom-select/custom-select';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { CustomInputComponent } from '../../../../shared/components/custom-input-component/custom-input-component';
import { CustomNumberInputComponent } from '../../../../shared/components/custom-number-input/custom-number-input';
import { NotificationService } from '@core/services/SignalR/notificationservice';

@Component({
  selector: 'app-drill-session-details',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    StatusChipComponent, 
    CustomButtonComponent, 
    CustomSelect, 
    LoadingSpinnerComponent,
    CustomInputComponent,
    CustomNumberInputComponent
  ],
  templateUrl: './drill-session-details.component.html',
  styleUrls: ['./drill-session-details.component.css']
})
export class DrillSessionDetailsComponent implements OnInit {
  get templateOptions(): SelectOption[] {
    return this.availableTemplates.map(t => ({
      value: t.id,
      label: `${t.name} (${t.categoryName || 'General'})`
    }));
  }

  onTemplateSelect(val: any): void {
    this.selectedTemplateId = val ? Number(val) : null;
  }
  sessionId!: number;
  isLoading = true;
  errorMessage = '';
  toastMessage = '';
  toastType: 'success' | 'error' = 'success';

  // --- Confirm Modal State ---
  confirmModal = {
    isOpen: false,
    title: '',
    message: '',
    confirmText: '',
    action: () => { }
  };

  sessionData: any = null;

  // --- Add Drill Modal States ---
  isAddDrillModalOpen = false;
  availableTemplates: any[] = [];
  selectedTemplateId: number | null = null;

  // --- Enter Results Modal States ---
  isResultsModalOpen = false;
  activeDrillForResults: any = null;
  playerScoreEntries: any[] = [];
  isSubmittingResults = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private sessionService: DrillSessionService,
    private templateService: DrillTemplateService,
    private authService: AuthService,
    private notificationService: NotificationService
  ) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.sessionId = Number(id);
        this.loadSessionDetails();
      } else {
        this.errorMessage = 'Invalid Session ID';
        this.isLoading = false;
      }
    });
  }

  // --- Computed Property for Read-Only Mode ---
  get isSessionCompleted(): boolean {
    return this.sessionData?.status === 2; // 2 = Completed
  }

  get isSessionCancelled(): boolean {
    return this.sessionData?.status === 3; // 3 = Cancelled
  }

  get isAdmin(): boolean {
    const user = this.authService.getCurrentUserValue();
    if (!user || !user.roles) return false;
    return user.roles.some(r =>
      r.toLowerCase() === 'academyadmin' ||
      r.toLowerCase() === 'admin' ||
      r.toLowerCase() === 'systemadmin'
    );
  }

  get isReadOnly(): boolean {
    return this.isSessionCompleted || this.isSessionCancelled || this.isAdmin;
  }

  private loadSessionDetails(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.sessionService.getSessionById(this.sessionId).subscribe({
      next: (res) => {
        if (res) {
          this.sessionData = res;
          if (this.sessionData.sessionDate && !this.sessionData.sessionDate.endsWith('Z') && !this.sessionData.sessionDate.includes('+')) {
            this.sessionData.sessionDate += 'Z';
          }
          this.loadAttendanceRoster();
        } else {
          this.errorMessage = 'Could not load session details.';
          this.isLoading = false;
        }
      },
      error: (err) => {
        console.error('API Error:', err);
        this.errorMessage = err.error?.message || 'Failed to fetch session from the database.';
        this.isLoading = false;
      }
    });
  }

  private loadAttendanceRoster(): void {
    this.sessionService.getSessionAttendance(this.sessionId).subscribe({
      next: (roster: any) => {
        this.sessionData.attendance = roster;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Attendance Error:', err);
        this.errorMessage = 'Failed to load session attendance sheet.';
        this.isLoading = false;
      }
    });
  }

  setAttendance(player: any, isPresent: boolean): void {
    if (this.isSessionCompleted) return; // 🟢 Guard: Prevent toggling if session is locked

    player.isPresent = isPresent;

    const payload = {
      attendances: this.sessionData.attendance.map((p: any) => ({
        playerId: p.playerId,
        isPresent: p.isPresent
      }))
    };

    this.sessionService.updateAttendance(this.sessionId, payload).subscribe({
      //notification
      next: () => {
       
        if (!isPresent) {
          const playerMsg = "You were marked absent from today's drill session.";
          const parentMsg = `Your child has been marked absent from today's drill session.`;

          this.notificationService.notifyPlayerMilestone(player.playerId, playerMsg).subscribe({
            error: (e) => console.error(`Failed to notify player ${player.playerId} for absence`, e)
          });
          this.notificationService.notifyPlayerParents(player.playerId, parentMsg).subscribe({
            error: (e) => console.error(`Failed to notify parent for player ${player.playerId} absence`, e)
          });
        }
      },
      error: (err) => console.error('Failed to auto-save attendance', err)
    });
  }

  getPresentCount(): number {
    if (!this.sessionData?.attendance) return 0;
    return this.sessionData.attendance.filter((p: any) => p.isPresent).length;
  }

  // --- Add Drill Modal ---
  openAddDrillModal(): void {
    this.isAddDrillModalOpen = true;
    this.selectedTemplateId = null;

    const filter = { pageNumber: 1, pageSize: 50 };
    this.templateService.getTemplates(filter).subscribe({
      next: (res: any) => {
        this.availableTemplates = res.items || [];
      },
      error: (err) => {
        console.error('Failed to load drill templates', err);
        this.availableTemplates = [];
      }
    });
  }

  closeAddDrillModal(): void {
    this.isAddDrillModalOpen = false;
    this.selectedTemplateId = null;
  }

  saveDrillToSession(): void {
    if (!this.selectedTemplateId) return;

    const payload: any = { drillTemplateId: Number(this.selectedTemplateId) };
    this.sessionService.addDrillToSession(this.sessionId, payload).subscribe({
      next: () => {
        this.closeAddDrillModal();
        this.showToast('Drill added to session.', 'success');
        this.loadSessionDetails();
      },
      error: (err) => {
        console.error('Failed to add drill to session', err);
        this.showToast(err.error?.message || 'Failed to add drill to session.', 'error');
      }
    });
  }

  removeDrill(drillId: number): void {
    if (this.isReadOnly) return;

    const drill = this.sessionData?.sessionDrills?.find((d: any) => d.id === drillId);
    const drillName = drill ? (drill.templateName || 'this drill') : 'this drill';

    this.confirmModal = {
      isOpen: true,
      title: 'Remove Drill',
      message: `Are you sure you want to remove ${drillName} from this session?`,
      confirmText: 'Yes, Remove',
      action: () => {
        this.sessionService.removeDrillFromSession(this.sessionId, drillId).subscribe({
          next: () => {
            this.showToast('Drill removed from session.', 'success');
            this.loadSessionDetails();
            this.closeConfirm();
          },
          error: (err) => {
            console.error('Failed to remove drill', err);
            this.showToast(err.error?.message || 'Failed to remove drill from session.', 'error');
            this.closeConfirm();
          }
        });
      }
    };
  }

  closeConfirm(): void {
    this.confirmModal.isOpen = false;
  }

  executeConfirm(): void {
    if (this.confirmModal.action) {
      this.confirmModal.action();
    }
  }

  showToast(msg: string, type: 'success' | 'error' = 'success'): void {
    this.toastMessage = msg;
    this.toastType = type;
    setTimeout(() => {
      this.toastMessage = '';
    }, 3500);
  }

  // Rating preset dropdown options for quick score entry (4 performance choices)
  ratingPresetOptions: SelectOption[] = [
    { value: '', label: 'Select Rating...' },
    { value: 2.5, label: 'Poor' },
    { value: 5, label: 'Average' },
    { value: 7.5, label: 'Good' },
    { value: 10, label: 'Excellent' }
  ];

  getMatchingRatingPreset(score: any): string | number {
    if (score === null || score === undefined || score === '') return '';
    const num = Number(score);
    if (num >= 8.75) return 10;
    if (num >= 6.25) return 7.5;
    if (num >= 3.75) return 5;
    if (num > 0) return 2.5;
    return '';
  }

  onRatingPresetChange(entry: any, val: any): void {
    if (val !== null && val !== undefined && val !== '') {
      entry.manualScore = Number(val);
    }
  }

  // --- Enter Results Modal ---
  openResultsModal(drill: any): void {
    this.activeDrillForResults = drill;
    this.isResultsModalOpen = true;

    const presentPlayers = (this.sessionData.attendance || []).filter((p: any) => p.isPresent);

    this.sessionService.getDrillResults(this.sessionId, drill.id).subscribe({
      next: (existingResults: any[]) => {
        this.playerScoreEntries = presentPlayers.map((player: any) => {
          const existing = existingResults.find((r: any) => r.playerId === player.playerId);
          return {
            playerId: player.playerId,
            playerFullName: player.playerFullName,
            position: player.position || 'Player',
            manualScore: existing ? existing.manualScore : undefined,
            doneCount: existing ? existing.doneCount : undefined,
            missedCount: existing ? existing.missedCount : undefined,
            coachNotes: existing ? existing.coachNotes : ''
          };
        });
      },
      error: () => {
        this.playerScoreEntries = presentPlayers.map((player: any) => ({
          playerId: player.playerId,
          playerFullName: player.playerFullName,
          position: player.position || 'Player',
          coachNotes: ''
        }));
      }
    });
  }

  closeResultsModal(): void {
    this.isResultsModalOpen = false;
    this.activeDrillForResults = null;
    this.playerScoreEntries = [];
  }

  saveDrillResults(): void {
    if (this.isReadOnly || !this.activeDrillForResults) return;

    this.isSubmittingResults = true;
    const payload = {
      results: this.playerScoreEntries.map(entry => {
        let score = entry.manualScore !== null && entry.manualScore !== undefined && entry.manualScore !== ''
          ? Math.min(10, Math.max(0, Number(entry.manualScore)))
          : null;
        return {
          playerId: entry.playerId,
          manualScore: score,
          doneCount: entry.doneCount ? Number(entry.doneCount) : 0,
          missedCount: entry.missedCount ? Number(entry.missedCount) : 0,
          coachNotes: entry.coachNotes || ''
        };
      })
    };

    this.sessionService.submitDrillResults(this.sessionId, this.activeDrillForResults.id, payload).subscribe({
      next: () => {
        this.isSubmittingResults = false;
        // notification
        this.playerScoreEntries.forEach(entry => {
          const playerMsg = "A new drill result has been recorded.";
          const parentMsg = "A new drill result has been recorded for your child."; 
          this.notificationService.notifyPlayerMilestone(entry.playerId, playerMsg).subscribe({
            error: (e) => console.error(`Failed to notify player ${entry.playerId}`, e)
          });
          this.notificationService.notifyPlayerParents(entry.playerId, parentMsg).subscribe({
            error: (e) => console.error(`Failed to notify parent for player ${entry.playerId}`, e)
          });
        });
        this.closeResultsModal();
        this.showToast('Drill results saved successfully!', 'success');
      },
      error: (err) => {
        this.isSubmittingResults = false;
        console.error('Failed to submit results', err);
        this.showToast(err.error?.message || 'Failed to submit drill results.', 'error');
      }
    });
  }

  completeSession(): void {
    this.confirmModal = {
      isOpen: true,
      title: 'Complete Session',
      message: 'Are you sure you want to complete this session?',
      confirmText: 'Complete Session',
      action: () => {
        this.sessionService.completeSession(this.sessionId).subscribe({
          next: (res: any) => {
            this.showToast('Session completed successfully.', 'success');
            this.closeConfirm();
            setTimeout(() => {
              this.router.navigate(['/drills/sessions']);
            }, 1200);
          },
          error: (err) => {
            console.error('Failed to complete session', err);
            this.showToast(err.error?.message || 'Could not complete session.', 'error');
            this.closeConfirm();
          }
        });
      }
    };
  }
  navigateToPlayerProgression(playerId: number): void {
    if (!playerId) return;
    this.router.navigate(['/drills/players', playerId, 'progression']);
  }

  createMatchSession(): void {
    this.router.navigate(['/session', this.sessionId, 'create-match']);
  }
}