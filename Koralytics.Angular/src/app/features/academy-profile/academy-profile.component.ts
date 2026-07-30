import { Component, OnInit, OnDestroy, inject, ElementRef, ViewChild, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { NavbarComponent } from '../../../shared/components/navbar/navbar';
import { Footer } from '../../../shared/components/footer/footer';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state';
import { ScrollRevealDirective } from '../../../shared/directives/scroll-reveal.directive';
import { CustomButtonComponent } from '../../../shared/components/custom-button/custom-button';
import { AcademyService } from '../../../core/services/academy/academy.service';
import { ToastService } from '../../../core/services/Toast/toast';
import {
  AcademyResponseDto,
  AcademyBadgeResponseDto,
  AcademyBadgeType,
  AcademyLocationResponseDto,
  TeamResponseDto,
  AcademyMemberResponseDto
} from '../../../core/interfaces/academy.models';

@Component({
  selector: 'app-academy-profile',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    NavbarComponent,
    Footer,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    ScrollRevealDirective,
    CustomButtonComponent
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

  @ViewChild('countersSection') countersSection?: ElementRef<HTMLElement>;

  academyId = 0;
  isLoading = true;
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
        this.error = 'No Academy ID specified.';
        this.isLoading = false;
      }
    });

    this.route.queryParamMap.subscribe(queryParams => {
      const tab = queryParams.get('tab');
      if (tab && ['overview', 'badges', 'locations', 'teams', 'members'].includes(tab)) {
        this.activeTab = tab as any;
      }

      const expanded = queryParams.get('expanded');
      if (expanded) {
        const ids = expanded.split(',').map(id => Number(id)).filter(id => !isNaN(id));
        this.expandedTeams = new Set<number>(ids);
      } else {
        this.expandedTeams.clear();
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
          this.error = res.message || 'Failed to load academy profile.';
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.error = 'Unable to fetch academy profile. Please try again.';
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

  onSearchChange(): void {
    this.syncQueryParams();
  }

  private syncQueryParams(): void {
    const queryParams: any = {
      tab: this.activeTab === 'overview' ? null : this.activeTab,
      expanded: this.expandedTeams.size > 0 ? Array.from(this.expandedTeams).join(',') : null,
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

  // Filtered Members
  get filteredMembers(): AcademyMemberResponseDto[] {
    return this.members.filter(m => {
      const matchesRole = this.selectedMemberRole === 'All' ||
        m.role?.toLowerCase() === this.selectedMemberRole.toLowerCase();
      const matchesSearch = !this.memberSearchQuery ||
        m.fullName?.toLowerCase().includes(this.memberSearchQuery.toLowerCase()) ||
        m.position?.toLowerCase().includes(this.memberSearchQuery.toLowerCase());
      return matchesRole && matchesSearch;
    });
  }

  // Badge Display helpers
  getBadgeTypeKey(type: any): string {
    const t = Number(type) || type;
    switch (t) {
      case AcademyBadgeType.Verified: case 'Verified': case 1: return 'Verified';
      case AcademyBadgeType.TopPerformer: case 'TopPerformer': case 2: return 'TopPerformer';
      case AcademyBadgeType.Premium: case 'Premium': case 3: return 'Premium';
      default: return 'Default';
    }
  }

  getBadgeName(type: any): string {
    const t = Number(type) || type;
    switch (t) {
      case AcademyBadgeType.Verified: case 'Verified': return 'Verified Academy';
      case AcademyBadgeType.TopPerformer: case 'TopPerformer': return 'Top Performer';
      case AcademyBadgeType.Premium: case 'Premium': return 'Premium Partner';
      default: return 'Honor Badge';
    }
  }

  // Sharing
  shareProfile(): void {
    const url = window.location.href;
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(url).then(() => {
        this.toast.show('Public profile link copied to clipboard.', 'success');
      });
    } else {
      this.toast.show('Share URL: ' + url, 'info');
    }
  }

  // Team roster expansion
  expandedTeams: Set<number> = new Set<number>();

  isTeamExpanded(teamId: number): boolean {
    return this.expandedTeams.has(teamId);
  }

  toggleTeamExpand(teamId: number): void {
    if (this.expandedTeams.has(teamId)) {
      this.expandedTeams.delete(teamId);
    } else {
      this.expandedTeams.add(teamId);
    }
    this.syncQueryParams();
  }

  // Profile navigation
  viewMemberProfile(id: number | undefined, role: string = 'Player'): void {
    if (!id) return;
    if (role?.toLowerCase() === 'coach') {
      this.router.navigate(['/coach/profile', id]);
    } else {
      this.router.navigate(['/player/profile', id]);
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
