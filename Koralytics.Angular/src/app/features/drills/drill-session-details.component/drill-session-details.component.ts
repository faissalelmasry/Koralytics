import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../shared/pipes/localized-date.pipe';
import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription, Observable, forkJoin, of } from 'rxjs';
import { switchMap, catchError } from 'rxjs/operators';
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
import { DrillSessionDetailsDto, DrillDto } from '../../../../core/interfaces/drill-session.model';
import { DrillTemplateDto } from '../../../../core/interfaces/drill-template.model';

export interface PlayerAttendance {
  playerId: number;
  playerFullName: string;
  name?: string;
  position: string;
  isPresent: boolean;
}

export interface ExtendedDrillDto extends DrillDto {
  templateName?: string;
  categoryName?: string;
}

export interface PlayerScoreEntry {
  playerId: number;
  playerFullName: string;
  position: string;
  manualScore: number | null;
  doneCount: number | null;
  missedCount: number | null;
  coachNotes: string;
}

export interface ExtendedDrillSessionDetails extends DrillSessionDetailsDto {
  attendance?: PlayerAttendance[];
  teamName?: string | null;
  coachName?: string | null;
  sessionDrills: ExtendedDrillDto[];
}


@Component({
  selector: 'app-drill-session-details',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush, // 🟢 OPTIMIZATION: Massive performance boost
  imports: [
    CommonModule,
    FormsModule,
    StatusChipComponent,
    CustomButtonComponent,
    CustomSelect,
    LoadingSpinnerComponent,
    CustomInputComponent,
    CustomNumberInputComponent
    , TranslatePipe, LocalizedDatePipe],
  templateUrl: './drill-session-details.component.html',
  styleUrls: ['./drill-session-details.component.css']
})
export class DrillSessionDetailsComponent implements OnInit {
  private translate = inject(TranslateService);
  translateCategory(name: string | null | undefined): string {
    if (!name) return '';
    const key = 'DRILLS.DYNAMIC.CAT_' + name.toUpperCase();
    const translated = this.translate.instant(key);
    return translated !== key ? translated : name;
  }

  get templateOptions(): SelectOption[] {
    return this.availableTemplates.map(t => ({
      value: t.id,
      label: `${t.name} (${this.translateCategory(t.categoryName) || this.translate.instant('DRILLS.TEMPLATES.FILTER_CATEGORY') || 'General'})`
    }));
  }  // 🟢 OPTIMIZATION: Memory management
  private subscriptions = new Subscription();

  onTemplateSelect(val: string | number | null): void {
    this.selectedTemplateId = val ? Number(val) : null;
  }
  sessionId!: number;
  isLoading = true;
  errorMessage = '';
  toastMessage = '';
  toastType: 'success' | 'error' = 'success';

  // --- Confirm Modal State ---
  confirmModal: {
    isOpen: boolean;
    title: string;
    message: string;
    messageParams: any;
    confirmText: string;
    variant?: 'coral' | 'cyan' | 'accent' | 'slate' | 'amber' | 'gold';
    action: () => void;
  } = {
    isOpen: false,
    title: '',
    message: '',
    messageParams: {},
    confirmText: '',
    variant: 'coral',
    action: () => { }
  };

  sessionData: ExtendedDrillSessionDetails | null = null;

  // --- Add Drill Modal States ---
  isAddDrillModalOpen = false;
  availableTemplates: DrillTemplateDto[] = [];
  selectedTemplateId: number | null = null;

  // --- Enter Results Modal States ---
  isResultsModalOpen = false;
  activeDrillForResults: ExtendedDrillDto | null = null;
  playerScoreEntries: PlayerScoreEntry[] = [];
  isSubmittingResults = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private sessionService: DrillSessionService,
    private templateService: DrillTemplateService,
    private authService: AuthService,
    private notificationService: NotificationService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.subscriptions.add(
      this.route.paramMap.subscribe(params => {
        const id = params.get('id');
        if (id) {
          this.sessionId = Number(id);
          this.loadSessionDetails();
        } else {
          this.errorMessage = 'Invalid Session ID';
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
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

    this.subscriptions.add(
      this.sessionService.getSessionById(this.sessionId).subscribe({
        next: (res) => {
          if (res) {
            this.sessionData = res as ExtendedDrillSessionDetails;
            if (this.sessionData.sessionDate && !this.sessionData.sessionDate.endsWith('Z') && !this.sessionData.sessionDate.includes('+')) {
              this.sessionData.sessionDate += 'Z';
            }
            this.loadAttendanceRoster();
          } else {
            this.errorMessage = 'Could not load session details.';
            this.isLoading = false;
            this.cdr.detectChanges();
          }
        },
        error: (err) => {
          console.error('API Error:', err);
          this.errorMessage = err.error?.message || 'Failed to fetch session from the database.';
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      })
    );
  }

  private loadAttendanceRoster(): void {
    this.subscriptions.add(
      this.sessionService.getSessionAttendance(this.sessionId).subscribe({
        next: (roster: PlayerAttendance[]) => {
          if (this.sessionData) {
            this.sessionData.attendance = roster;
          }
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Attendance Error:', err);
          this.errorMessage = 'Failed to load session attendance sheet.';
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      })
    );
  }

  setAttendance(player: PlayerAttendance, isPresent: boolean): void {
    if (this.isSessionCompleted) return; // 🟢 Guard: Prevent toggling if session is locked

    player.isPresent = isPresent;

    const payload = {
      attendances: this.sessionData?.attendance?.map((p: PlayerAttendance) => ({
        playerId: p.playerId,
        isPresent: p.isPresent
      })) || []
    };

    this.subscriptions.add(
      this.sessionService.updateAttendance(this.sessionId, payload).pipe(
        switchMap(() => {
          if (!isPresent) {

            const playerMsg = `Hi ${player.playerFullName}, a training session is currently ongoing and you are marked as absent.`;
            const parentMsg = `Your child, ${player.playerFullName}, is currently marked as absent from the ongoing training session.`;
            return forkJoin([
              this.notificationService.notifyPlayerMilestone(player.playerId, playerMsg).pipe(
                catchError(e => { console.error(`Failed to notify player ${player.playerId} for ongoing absence`, e); return of(null); })
              ),
              this.notificationService.notifyPlayerParents(player.playerId, parentMsg).pipe(
                catchError(e => { console.error(`Failed to notify parent for player ${player.playerId} ongoing absence`, e); return of(null); })
              )
            ]);
          }
          return of(null);
        })
      ).subscribe({
        error: (err) => {
          console.error('Failed to auto-save attendance', err);
          // Optional: Revert UI state if API fails
          player.isPresent = !isPresent;
        }
      })
    );
  }
  getPresentCount(): number {
    if (!this.sessionData?.attendance) return 0;
    return this.sessionData.attendance.filter((p: PlayerAttendance) => p.isPresent).length;
  }

  // --- Add Drill Modal ---
  openAddDrillModal(): void {
    this.isAddDrillModalOpen = true;
    this.selectedTemplateId = null;
    this.cdr.detectChanges();

    const filter = { pageNumber: 1, pageSize: 50 };
    this.subscriptions.add(
      this.templateService.getTemplates(filter).subscribe({
        next: (res: any) => {
          this.availableTemplates = res.items || [];
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Failed to load drill templates', err);
          this.availableTemplates = [];
          this.cdr.detectChanges();
        }
      })
    );
  }

  closeAddDrillModal(): void {
    this.isAddDrillModalOpen = false;
    this.selectedTemplateId = null;
    this.cdr.detectChanges();
  }

  saveDrillToSession(): void {
    if (!this.selectedTemplateId) return;

    const payload: any = { drillTemplateId: Number(this.selectedTemplateId) };
    this.subscriptions.add(
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
      })
    );
  }

  removeDrill(drillId: number): void {
    if (this.isReadOnly) return;

    const drill = this.sessionData?.sessionDrills?.find((d: any) => d.id === drillId);
    const drillName = drill ? (drill.templateName || 'this drill') : 'this drill';

    this.confirmModal = {
      isOpen: true,
      title: 'DRILLS.SESSION_DETAILS.REMOVE_DRILL',
      message: 'DRILLS.SESSION_DETAILS.REMOVE_CONFIRM_MSG',
      messageParams: { name: drillName },
      confirmText: 'DRILLS.SESSION_DETAILS.REMOVE_BTN',
      variant: 'coral',
      action: () => {
        this.subscriptions.add(
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
          })
        );
      }
    };
  }

  closeConfirm(): void {
    this.confirmModal.isOpen = false;
    this.cdr.detectChanges();
  }

  executeConfirm(): void {
    if (this.confirmModal.action) {
      this.confirmModal.action();
    }
  }

  showToast(msg: string, type: 'success' | 'error' = 'success'): void {
    this.toastMessage = msg;
    this.toastType = type;
    this.cdr.detectChanges();
    setTimeout(() => {
      this.toastMessage = '';
      this.cdr.detectChanges();
    }, 3500);
  }

  // Rating preset dropdown options for quick score entry (4 performance choices)
  get ratingPresetOptions(): SelectOption[] {
    return [
      { value: '', label: this.translate.instant('DRILLS.SESSION_DETAILS.SELECT_RATING') || 'Select Rating...' },
      { value: 2.5, label: this.translate.instant('DRILLS.SESSION_DETAILS.RATING_POOR') || 'Poor' },
      { value: 5, label: this.translate.instant('DRILLS.SESSION_DETAILS.RATING_AVERAGE') || 'Average' },
      { value: 7.5, label: this.translate.instant('DRILLS.SESSION_DETAILS.RATING_GOOD') || 'Good' },
      { value: 10, label: this.translate.instant('DRILLS.SESSION_DETAILS.RATING_EXCELLENT') || 'Excellent' }
    ];
  }

  getMatchingRatingPreset(score: string | number | null | undefined): string | number {
    if (score === null || score === undefined || score === '') return '';
    const num = Number(score);
    if (num >= 8.75) return 10;
    if (num >= 6.25) return 7.5;
    if (num >= 3.75) return 5;
    if (num > 0) return 2.5;
    return '';
  }

  onRatingPresetChange(entry: PlayerScoreEntry, val: string | number | null): void {
    if (val !== null && val !== undefined && val !== '') {
      entry.manualScore = Number(val);
    } else {
      entry.manualScore = null;
    }
  }

  // --- Enter Results Modal ---
  openResultsModal(drill: any): void {
    this.activeDrillForResults = drill;
    this.isResultsModalOpen = true;
    this.cdr.detectChanges();

    const presentPlayers = (this.sessionData?.attendance || []).filter((p: PlayerAttendance) => p.isPresent);

    this.subscriptions.add(
      this.sessionService.getDrillResults(this.sessionId, drill.id).subscribe({
        next: (existingResults: any[]) => {
          this.playerScoreEntries = presentPlayers.map((player: PlayerAttendance) => {
            const existing = existingResults.find((r: any) => r.playerId === player.playerId);
            return {
              playerId: player.playerId,
              playerFullName: player.playerFullName,
              position: player.position || 'Player',
              manualScore: existing?.manualScore !== undefined ? Number(existing.manualScore) : null,
              doneCount: existing?.doneCount !== undefined ? Number(existing.doneCount) : null,
              missedCount: existing?.missedCount !== undefined ? Number(existing.missedCount) : null,
              coachNotes: existing?.coachNotes || ''
            };
          });
          this.cdr.detectChanges();
        },
        error: () => {
          this.playerScoreEntries = presentPlayers.map((player: PlayerAttendance) => ({
            playerId: player.playerId,
            playerFullName: player.playerFullName,
            position: player.position || 'Player',
            manualScore: null,
            doneCount: null,
            missedCount: null,
            coachNotes: ''
          }));
          this.cdr.detectChanges();
        }
      })
    );
  }

  closeResultsModal(): void {
    this.isResultsModalOpen = false;
    this.activeDrillForResults = null;
    this.playerScoreEntries = [];
    this.cdr.detectChanges();
  }

  saveDrillResults(): void {
    if (this.isReadOnly || !this.activeDrillForResults) return;

    this.isSubmittingResults = true;
    const payload = {
      results: this.playerScoreEntries.map(entry => {
        let score = entry.manualScore !== null && entry.manualScore !== undefined
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

    this.subscriptions.add(
      this.sessionService.submitDrillResults(this.sessionId, this.activeDrillForResults.id, payload).pipe(
        switchMap(() => {
          this.isSubmittingResults = false;
          if (this.playerScoreEntries && this.playerScoreEntries.length > 0) {
            const playerIds = this.playerScoreEntries.map(entry => entry.playerId);
            const playerMsg = "A new drill result has been recorded.";
            const parentMsg = "A new drill result has been recorded for your child.";
            return forkJoin([
              this.notificationService.notifyMultiplePlayersMilestone(playerIds, playerMsg).pipe(
                catchError(e => { console.error('Failed to notify players for the drill result', e); return of(null); })
              ),
              this.notificationService.notifyParentsOfPlayers(playerIds, parentMsg).pipe(
                catchError(e => { console.error('Failed to notify parents for the drill result', e); return of(null); })
              )
            ]);
          }
          return of(null);
        })
      ).subscribe({
        next: () => {
          this.closeResultsModal();
          this.showToast('Drill results saved successfully!', 'success');
          this.loadSessionDetails();
        },
        error: (err) => {
          this.isSubmittingResults = false;
          console.error('Failed to submit results', err);
          this.showToast(err.error?.message || 'Failed to submit drill results.', 'error');
        }
      })
    );
  }

  completeSession(): void {
    this.confirmModal = {
      isOpen: true,
      title: 'DRILLS.SESSION_DETAILS.COMPLETE_SESSION',
      message: 'DRILLS.SESSION_DETAILS.COMPLETE_CONFIRM_MSG',
      messageParams: {},
      confirmText: 'DRILLS.SESSION_DETAILS.COMPLETE_BTN',
      variant: 'cyan',
      action: () => {
        const absentPlayerIds = this.sessionData?.attendance
          ?.filter(p => !p.isPresent)
          .map(p => p.playerId) || [];
        let notifications$: Observable<any> = of(null);

        if (absentPlayerIds.length > 0) {

          const playerMsg = "Final Confirmation: The drill session has concluded, and your absence has been officially recorded.";
          const parentMsg = "Final Confirmation: The drill session has concluded, and your child's absence has been officially recorded.";

          notifications$ = forkJoin([
            this.notificationService.notifyMultiplePlayersMilestone(absentPlayerIds, playerMsg).pipe(
              catchError(e => { console.error('Failed to notify absent players', e); return of(null); })
            ),
            this.notificationService.notifyParentsOfPlayers(absentPlayerIds, parentMsg).pipe(
              catchError(e => { console.error('Failed to notify parents of absent players', e); return of(null); })
            )
          ]);
        }
        this.subscriptions.add(
          notifications$.pipe(
            switchMap(() => this.sessionService.completeSession(this.sessionId))
          ).subscribe({
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
          })
        );
      }
    };
  }
  navigateToPlayerProgression(playerId: number): void {
    if (!playerId) return;
    this.router.navigate(['/player/profile', playerId]);
  }

  createMatchSession(): void {
    this.router.navigate(['/session', this.sessionId, 'create-match']);
  }
}