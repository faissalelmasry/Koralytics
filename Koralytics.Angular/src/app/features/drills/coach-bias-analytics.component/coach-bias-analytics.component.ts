import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { DrillSessionService } from '../../../../core/services/drill/drill-session.service';
import { AcademyService } from '../../../../core/services/academy/academy.service';
import { AuthService } from '../../../../core/services/auth/auth.service';
import { CustomSelect, SelectOption } from '../../../../shared/components/custom-select/custom-select';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip';
import { FeatureLockComponent } from '../../../shared/components/feature-lock/feature-lock';
import { TranslatePipe } from '@ngx-translate/core';

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

@Component({
  selector: 'app-coach-bias-analytics',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    RouterModule,
    CustomSelect, 
    CustomButtonComponent, 
    LoadingSpinnerComponent, 
    StatusChipComponent,
    FeatureLockComponent,
    TranslatePipe
  ],
  templateUrl: './coach-bias-analytics.component.html',
  styleUrls: ['./coach-bias-analytics.component.css']
})
export class CoachBiasAnalyticsComponent implements OnInit {

  selectedCoachId: number | null = null;
  availableCoaches: { id: number; name: string }[] = [];

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

  goBack(): void {
    this.location.back();
  }

  get coachOptions(): SelectOption[] {
    return this.availableCoaches.map(c => ({
      value: c.id,
      label: `${c.name} (ID: ${c.id})`
    }));
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
      this.authService.getCurrentUser().subscribe({
        next: (res) => {
          this.isCoachesLoading = false;
          if (res.isSuccess && res.data) {
            const user = {
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
      });
    } else {
      this.processUserAndLoadCoaches(currentUser);
    }
  }

  private processUserAndLoadCoaches(user: any): void {
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
    this.academyService.getAcademyMembers(academyId, { pageNumber: 1, pageSize: 1000 }).subscribe({
      next: (res) => {
        this.isCoachesLoading = false;
        if (res.isSuccess && res.data && res.data.items) {
          const coachMembers = res.data.items.filter(
            (m: any) => m.role === 'Coach' || m.role?.toLowerCase() === 'coach'
          );

          this.availableCoaches = coachMembers.map((c: any) => ({
            id: c.userId,
            name: c.fullName || c.email
          }));

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
    });
  }

  calculateBias(): void {
    if (!this.selectedCoachId) return;

    const coachId = Number(this.selectedCoachId);

    this.isLoading = true;
    this.errorMessage = '';
    this.biasReport = null;

    this.sessionService.getCoachBiasReport(coachId).subscribe({
      next: (report: BiasReport) => {
        const selectedCoach = this.availableCoaches.find(c => Number(c.id) === coachId);

        this.biasReport = {
          ...report,
          coachName: report.coachName || selectedCoach?.name || 'Coach'
        };

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
    });
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
    if (!this.biasReport) return 'No Data';
    if (this.biasReport.trustPercentage < 60) return 'Unreliable';
    if (this.biasReport.trustPercentage < 85) return 'Needs Alignment';
    return 'Highly Accurate';
  }

  getChipType(): 'success' | 'warning' | 'danger' | 'info' {
    if (!this.biasReport) return 'info';
    if (this.biasReport.trustPercentage < 60) return 'danger';
    if (this.biasReport.trustPercentage < 85) return 'warning';
    return 'success';
  }

  get topPlayerComparisons(): PlayerComparison[] {
    if (!this.biasReport?.playerComparisons) return [];
    return this.biasReport.playerComparisons.slice(0, 5);
  }

  getAverageDelta(): string {
    if (!this.biasReport) return '0.0';
    const delta = Math.max(0, (100 - this.biasReport.trustPercentage) / 10);
    return delta.toFixed(1);
  }

  getBiasDescription(): string {
    if (!this.biasReport) return 'No Data';
    const delta = (100 - this.biasReport.trustPercentage) / 10;
    if (delta <= 1.5) return 'Minimal Variance (Fair Assessor)';
    if (delta <= 3.0) return 'Moderate Variance (Slightly Generous/Harsh)';
    return 'High Variance (Inconsistent Scoring)';
  }
}