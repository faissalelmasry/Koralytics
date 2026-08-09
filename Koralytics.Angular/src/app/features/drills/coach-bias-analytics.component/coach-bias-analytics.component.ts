import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../shared/pipes/localized-date.pipe';
import { Component, OnInit, OnDestroy, ChangeDetectorRef, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';
import { DrillSessionService } from '../../../../core/services/drill/drill-session.service';
import { AcademyService } from '../../../../core/services/academy/academy.service';
import { AuthService } from '../../../../core/services/auth/auth.service';
import { CustomSelect, SelectOption } from '../../../../shared/components/custom-select/custom-select';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip';
import { FeatureLockComponent } from '../../../shared/components/feature-lock/feature-lock';
import { User } from '@core/interfaces/user.model';

export interface PlayerComparison {
  playerId: number;
  playerName: string;
  avgPracticeScore: number;
  avgMatchScore: number;
  delta: number;
  status: string;
}

export interface BiasReport {
  coachId: number;
  coachName?: string;
  trustPercentage: number;
  playersAnalyzedCount: number;
  remarks: string;
  playerComparisons?: PlayerComparison[];
}

// 🟢 OPTIMIZATION: Strict typings for the API payloads
interface AppUser {
  userId: number;
  email: string;
  userName: string;
  fullName: string;
  roles: string[];
  academyId?: number | null;
}

interface AcademyMember {
  userId: number;
  fullName?: string;
  email: string;
  role: string;
}

@Component({
  selector: 'app-coach-bias-analytics',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush, // 🟢 OPTIMIZATION: Maximum performance
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    CustomSelect,
    CustomButtonComponent,
    LoadingSpinnerComponent,
    StatusChipComponent,
    FeatureLockComponent
    , TranslatePipe, LocalizedDatePipe],
  templateUrl: './coach-bias-analytics.component.html',
  styleUrls: ['./coach-bias-analytics.component.css']
})
export class CoachBiasAnalyticsComponent implements OnInit {
  private translate = inject(TranslateService);
  private subscriptions = new Subscription();


  selectedCoachId: number | null = null;
  availableCoaches: { id: number; name: string }[] = [];

  // 🟢 OPTIMIZATION: Static arrays to prevent GC churn on every render cycle
  coachOptionsList: SelectOption[] = [];
  topComparisons: PlayerComparison[] = [];

  biasReport: BiasReport | null = null;
  isLoading = false;
  isCoachesLoading = false;
  isCoachView = false;
  errorMessage = '';

  constructor(
    private sessionService: DrillSessionService,
    private academyService: AcademyService,
    private authService: AuthService,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef,
    private location: Location
  ) { }

  ngOnInit(): void {
    this.loadCoaches();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  goBack(): void {
    this.location.back();
  }

  getSelectedCoachName(): string {
    if (this.biasReport?.coachName) {
      return this.biasReport.coachName;
    }
    const found = this.availableCoaches.find(c => Number(c.id) === Number(this.selectedCoachId));
    if (found) return found.name;

    const user = this.authService.getCurrentUserValue();
    return user?.fullName || user?.userName || 'Coach';
  }

  loadCoaches(): void {
    const currentUser = this.authService.getCurrentUserValue();

    if (!currentUser) {
      this.isCoachesLoading = true;
      this.cdr.detectChanges();

      this.subscriptions.add(
        this.authService.getCurrentUser().subscribe({
          next: (res: any) => {
            this.isCoachesLoading = false;
            if (res.isSuccess && res.data) {
              const user: AppUser = {
                userId: Number(res.data.userId || res.data.id),
                email: res.data.email,
                userName: res.data.name || res.data.userName,
                fullName: res.data.name || res.data.fullName,
                roles: res.data.roles || [],
                academyId: res.data.academyId
              };
              this.processUserAndLoadCoaches(user);
            } else {
              this.errorMessage = 'Could not load user information.';
              this.cdr.detectChanges();
            }
          },
          error: (err) => {
            this.isCoachesLoading = false;
            console.error('Error fetching current user:', err);
            this.errorMessage = 'Failed to authenticate user.';
            this.cdr.detectChanges();
          }
        })
      );
    } else {
      const mappedUser: AppUser = {
        userId: currentUser.userId,
        email: currentUser.email || '',
        userName: currentUser.userName || '',
        fullName: currentUser.fullName || '',
        roles: currentUser.roles || [],
        academyId: currentUser.academyId
      };
      this.processUserAndLoadCoaches(mappedUser);
    }
  }

  private processUserAndLoadCoaches(user: AppUser): void {
    const routeCoachId = this.route.snapshot.params['coachId'];
    const isCoachOnly = user.roles?.includes('Coach') &&
      !user.roles?.includes('AcademyAdmin') &&
      !user.roles?.includes('SystemAdmin') &&
      !user.roles?.includes('Admin');

    if (routeCoachId) {
      this.isCoachView = true;
      this.selectedCoachId = Number(routeCoachId);
      this.calculateBias();
    } else if (isCoachOnly) {
      this.isCoachView = true;
      this.availableCoaches = [{
        id: user.userId,
        name: user.fullName || user.userName || 'Current Coach'
      }];
      this.updateCoachOptions();
      this.selectedCoachId = user.userId;
      this.calculateBias();
    } else if (user.academyId) {
      this.isCoachView = false;
      this.fetchAcademyCoaches(user.academyId);
    } else {
      this.errorMessage = 'No academy associated with current account.';
      this.cdr.detectChanges();
    }
  }

  private fetchAcademyCoaches(academyId: number): void {
    this.isCoachesLoading = true;
    this.cdr.detectChanges();

    this.subscriptions.add(
      this.academyService.getAcademyMembers(academyId, { pageNumber: 1, pageSize: 1000 }).subscribe({
        next: (res: any) => {
          this.isCoachesLoading = false;
          if (res.isSuccess && res.data && res.data.items) {
            const coachMembers: AcademyMember[] = res.data.items.filter(
              (m: AcademyMember) => m.role === 'Coach' || m.role?.toLowerCase() === 'coach'
            );

            this.availableCoaches = coachMembers.map(c => ({
              id: c.userId,
              name: c.fullName || c.email
            }));

            this.updateCoachOptions();

            if (this.availableCoaches.length > 0) {
              this.selectedCoachId = this.availableCoaches[0].id;
              this.calculateBias();
            } else {
              this.errorMessage = 'No coaches found in this academy.';
            }
          } else {
            this.errorMessage = 'Failed to load coaches list.';
          }
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.isCoachesLoading = false;
          console.error('Error fetching coaches:', err);
          this.errorMessage = 'Error loading coaches list from backend.';
          this.cdr.detectChanges();
        }
      })
    );
  }

  // 🟢 OPTIMIZATION: Maps the array exactly once
  private updateCoachOptions(): void {
    this.coachOptionsList = this.availableCoaches.map(c => ({
      value: c.id,
      label: `${c.name} (ID: ${c.id})`
    }));
  }

  calculateBias(): void {
    if (!this.selectedCoachId) return;

    const coachId = Number(this.selectedCoachId);

    this.isLoading = true;
    this.errorMessage = '';
    this.biasReport = null;
    this.topComparisons = [];
    this.cdr.detectChanges();

    this.subscriptions.add(
      this.sessionService.getCoachBiasReport(coachId).subscribe({
        next: (report: BiasReport) => {
          const selectedCoach = this.availableCoaches.find(c => Number(c.id) === coachId);

          this.biasReport = {
            ...report,
            coachName: report.coachName || selectedCoach?.name || 'Coach'
          };

          // 🟢 OPTIMIZATION: Slice the array once when data arrives
          if (this.biasReport.playerComparisons) {
            this.topComparisons = this.biasReport.playerComparisons.slice(0, 5);
          }

          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Bias Analysis Error:', err);
          this.errorMessage =
            err.error?.detail ||
            err.error?.title ||
            err.error?.message ||
            (typeof err.error === 'string' ? err.error : '') ||
            err.message ||
            'Failed to calculate Trust Index.';
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      })
    );
  }

  // --- UI GAUGE HELPERS ---

  getDashOffset(): number {
    if (!this.biasReport) return 251.2;
    const percentage = this.biasReport.trustPercentage;
    const circumference = 251.2;
    return circumference - (percentage / 100) * circumference;
  }

  getMeterColor(): string {
    if (!this.biasReport) return '#384056';
    if (this.biasReport.trustPercentage < 60) return '#ff4d4d'; // Red
    if (this.biasReport.trustPercentage < 85) return '#ffaa00'; // Amber
    return '#00f2fe'; // Neon Blue
  }

  getMeterClass(): string {
    if (!this.biasReport) return 'meter-low';
    if (this.biasReport.trustPercentage < 60) return 'meter-critical';
    if (this.biasReport.trustPercentage < 85) return 'meter-moderate';
    return 'meter-high';
  }

  getAssessmentStatus(): string {
    if (!this.biasReport) return this.translate.instant('DRILLS.BIAS_ANALYTICS.NO_DATA') || 'No Data';
    if (this.biasReport.trustPercentage < 60) return this.translate.instant('DRILLS.BIAS_ANALYTICS.STATUS_UNRELIABLE') || 'Unreliable';
    if (this.biasReport.trustPercentage < 85) return this.translate.instant('DRILLS.BIAS_ANALYTICS.STATUS_NEEDS_ALIGNMENT') || 'Needs Alignment';
    return this.translate.instant('DRILLS.BIAS_ANALYTICS.STATUS_HIGHLY_ACCURATE') || 'Highly Accurate';
  }

  getChipType(): 'success' | 'warning' | 'danger' | 'info' {
    if (!this.biasReport) return 'info';
    if (this.biasReport.trustPercentage < 60) return 'danger';
    if (this.biasReport.trustPercentage < 85) return 'warning';
    return 'success';
  }

  getAverageDelta(): string {
    if (!this.biasReport) return '0.0';
    const delta = Math.max(0, (100 - this.biasReport.trustPercentage) / 10);
    return delta.toFixed(1);
  }

  getBiasDescription(): string {
    if (!this.biasReport) return this.translate.instant('DRILLS.BIAS_ANALYTICS.NO_DATA') || 'No Data';
    const delta = (100 - this.biasReport.trustPercentage) / 10;
    if (delta <= 1.5) return this.translate.instant('DRILLS.BIAS_ANALYTICS.FAIR_ASSESSOR') || 'Minimal Variance (Fair Assessor)';
    if (delta <= 3.0) return this.translate.instant('DRILLS.BIAS_ANALYTICS.MODERATE_VARIANCE') || 'Moderate Variance (Slightly Generous/Harsh)';
    return this.translate.instant('DRILLS.BIAS_ANALYTICS.HIGH_VARIANCE') || 'High Variance (Inconsistent Scoring)';
  }
}