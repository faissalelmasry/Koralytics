import { Component, OnInit, OnDestroy, inject, ElementRef, ViewChild, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { NavbarComponent } from '../../../shared/components/navbar/navbar';
import { Footer } from '../../../shared/components/footer/footer';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { ScrollRevealDirective } from '../../../shared/directives/scroll-reveal.directive';
import { CustomButtonComponent } from '../../../shared/components/custom-button/custom-button';
import { AcademyService } from '../../../core/services/academy/academy.service';
import { ToastService } from '../../../core/services/Toast/toast';
import {
  AcademyResponseDto,
  AcademyBadgeResponseDto,
  AcademyLocationResponseDto,
  TeamResponseDto,
  AcademyMemberResponseDto
} from '../../../core/interfaces/academy.models';
import { AcademyProfileOverviewComponent } from './components/academy-profile-overview/academy-profile-overview';
import { AcademyProfileBadgesComponent } from './components/academy-profile-badges/academy-profile-badges';
import { AcademyProfileLocationsComponent } from './components/academy-profile-locations/academy-profile-locations';
import { AcademyProfileTeamsComponent } from './components/academy-profile-teams/academy-profile-teams';
import { AcademyProfileMembersComponent } from './components/academy-profile-members/academy-profile-members';

@Component({
  selector: 'app-academy-profile',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslatePipe,
    NavbarComponent,
    Footer,
    LoadingSpinnerComponent,
    ScrollRevealDirective,
    CustomButtonComponent,
    AcademyProfileOverviewComponent,
    AcademyProfileBadgesComponent,
    AcademyProfileLocationsComponent,
    AcademyProfileTeamsComponent,
    AcademyProfileMembersComponent
  ],
  templateUrl: './academy-profile.component.html',
  styleUrls: ['./academy-profile.component.css']
})
export class AcademyProfileComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private academyService = inject(AcademyService);
  private toast = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);
  private translate = inject(TranslateService);

  @ViewChild('countersSection') countersSection?: ElementRef<HTMLElement>;

  academyId = 0;
  isLoading = true;
  logoImageError = false;
  error: string | null = null;

  // Data
  academy: AcademyResponseDto | null = null;
  badges: AcademyBadgeResponseDto[] = [];
  locations: AcademyLocationResponseDto[] = [];
  teams: TeamResponseDto[] = [];
  members: AcademyMemberResponseDto[] = [];

  // Filter Panel Tabs
  activeTab: 'overview' | 'badges' | 'locations' | 'teams' | 'members' = 'overview';

  // Members search / filtering
  memberSearchQuery = '';
  selectedMemberRole = 'All';

  // KPI Animated Counters
  animatedCounters = {
    members: 0,
    teams: 0,
    locations: 0,
    badges: 0
  };
  private countersAnimated = false;
  private observer?: IntersectionObserver;

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const idParam = params.get('id') || params.get('academyId');
      if (idParam) {
        this.academyId = Number(idParam);
        this.loadAcademyData();
      } else {
        this.error = this.translate.instant('ACADEMY_PROFILE.MESSAGES.NO_ID');
        this.isLoading = false;
      }
    });

    this.route.queryParamMap.subscribe(queryParams => {
      const tab = queryParams.get('tab');
      if (tab && ['overview', 'badges', 'locations', 'teams', 'members'].includes(tab)) {
        this.activeTab = tab as any;
      }

      const role = queryParams.get('role');
      if (role && ['All', 'Player', 'Coach'].includes(role)) {
        this.selectedMemberRole = role;
      }

      const q = queryParams.get('q');
      if (q !== null) {
        this.memberSearchQuery = q;
      }
    });
  }

  ngOnDestroy(): void {
    if (this.observer) {
      this.observer.disconnect();
    }
  }

  loadAcademyData(): void {
    if (!this.academyId) return;

    this.isLoading = true;
    this.error = null;
    this.countersAnimated = false;

    // Load Academy Profile
    this.academyService.getAcademyById(this.academyId).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.academy = res.data;
          this.animateCounterField('locations', res.data.locationCount || 0);
        } else {
          this.error = res.message || this.translate.instant('ACADEMY_PROFILE.MESSAGES.LOAD_FAILED');
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.error = this.translate.instant('ACADEMY_PROFILE.MESSAGES.FETCH_FAILED');
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });

    // Load Badges
    this.academyService.getAcademyBadges(this.academyId).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.badges = res.data;
          this.animateCounterField('badges', this.badges.length);
        }
      }
    });

    // Load Locations
    this.academyService.getLocations(this.academyId).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.locations = res.data;
          this.animateCounterField('locations', this.locations.length);
        }
      }
    });

    // Load Teams
    this.academyService.getTeams(this.academyId).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.teams = res.data;
          this.animateCounterField('teams', this.teams.length);
        }
      }
    });

    // Load Members
    this.academyService.getAcademyMembers(this.academyId, { pageSize: 100 }).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.members = res.data.items || [];
          this.animateCounterField('members', res.data.totalCount || this.members.length);
        }
      }
    });
  }

  setActiveTab(tab: 'overview' | 'badges' | 'locations' | 'teams' | 'members'): void {
    this.activeTab = tab;
    this.syncQueryParams();
  }

  setMemberRole(role: string): void {
    this.selectedMemberRole = role;
    this.syncQueryParams();
  }

  onSearchChange(query: string): void {
    this.memberSearchQuery = query;
    this.syncQueryParams();
  }

  private syncQueryParams(): void {
    const queryParams: any = {
      tab: this.activeTab === 'overview' ? null : this.activeTab,
      role: this.selectedMemberRole === 'All' ? null : this.selectedMemberRole,
      q: this.memberSearchQuery ? this.memberSearchQuery : null
    };

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  // Helpers for Hero Banner
  get initials(): string {
    if (!this.academy?.name) return 'KA';
    const parts = this.academy.name.trim().split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return this.academy.name.slice(0, 2).toUpperCase();
  }

  get mainLocation(): AcademyLocationResponseDto | undefined {
    return this.locations.find(l => l.isMain || l.isMainLocation) || this.locations[0];
  }

  // Sharing
  shareProfile(): void {
    const url = window.location.href;
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(url).then(() => {
        this.toast.show(this.translate.instant('ACADEMY_PROFILE.MESSAGES.LINK_COPIED'), 'success');
      });
    } else {
      this.toast.show(this.translate.instant('ACADEMY_PROFILE.MESSAGES.SHARE_URL') + url, 'info');
    }
  }

  // KPI animation
  private animateCounterField(field: 'members' | 'teams' | 'locations' | 'badges', target: number): void {
    const start = this.animatedCounters[field];
    if (start === target) return;

    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduceMotion) {
      this.animatedCounters[field] = target;
      this.cdr.detectChanges();
      return;
    }

    this.animateValue(start, target, 750, (v) => {
      this.animatedCounters[field] = v;
      this.cdr.detectChanges();
    });
  }

  private animateValue(start: number, end: number, duration: number, callback: (v: number) => void): void {
    const startTime = performance.now();
    const easeOutCubic = (t: number) => 1 - Math.pow(1 - t, 3);

    const tick = (now: number) => {
      const p = Math.min((now - startTime) / duration, 1);
      const eased = easeOutCubic(p);
      callback(Math.round(start + eased * (end - start)));
      this.cdr.detectChanges();
      if (p < 1) requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  }
}
