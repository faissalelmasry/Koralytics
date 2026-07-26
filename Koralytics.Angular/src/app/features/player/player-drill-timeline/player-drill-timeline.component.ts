import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../shared/components/footer/footer';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { CustomSelect } from '../../../../shared/components/custom-select/custom-select';
import { CustomDatePicker } from '../../../../shared/components/custom-date-picker/custom-date-picker';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { PlayerProfileService } from '../../../../core/services/player/player-profile.service';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { DrillTimelineEvent } from '../../../../core/models/Player/drill-timeline-model';

@Component({
  selector: 'app-player-drill-timeline',
  standalone: true,
  imports: [
    CommonModule,
    NavbarComponent,
    Footer,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    Pagination,
    CustomSelect,
    CustomDatePicker,
    CustomButtonComponent,
    ScrollRevealDirective
  ],
  templateUrl: './player-drill-timeline.component.html',
  styleUrls: ['./player-drill-timeline.component.css']
})
export class PlayerDrillTimelineComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private profileService = inject(PlayerProfileService);
  private tokenStorage = inject(TokenStorageService);
  private cdr = inject(ChangeDetectorRef);

  playerId: number | null = null;
  playerName = '';
  isLoading = false;
  error = '';
  filterError = '';

  currentPage = 1;
  pageSize = 10;
  totalItems = 0;

  allEvents: DrillTimelineEvent[] = [];
  events: DrillTimelineEvent[] = [];
  categories: string[] = [];

  selectedCategory = '';
  selectedDateFrom = '';
  selectedDateTo = '';

  private readonly CORE_CATEGORIES = ['Speed', 'Shooting', 'Passing', 'Dribbling', 'Defending', 'Physical'];
  categoryOptions: { value: string; label: string }[] = this.CORE_CATEGORIES.map(c => ({ value: c, label: c }));

  ngOnInit() {
    const paramId = this.route.snapshot.paramMap.get('playerId');

    if (paramId) {
      this.playerId = Number(paramId);
      this.fetchPlayerDetailsAndTimeline();
    } else {
      const user = this.tokenStorage.getUser();
      if (!user?.userId) {
        this.error = 'Invalid session';
        return;
      }
      this.playerId = user.userId;
      this.fetchPlayerDetailsAndTimeline();
    }
  }

  fetchPlayerDetailsAndTimeline() {
    if (!this.playerId) return;

    this.profileService.getPlayerProfile(this.playerId).subscribe({
      next: (profile) => {
        this.playerName = `${profile.firstName} ${profile.lastName}`;
        this.cdr.detectChanges();
      },
      error: () => {
        this.playerName = 'Player';
      }
    });

    this.loadTimeline();
  }

  loadTimeline() {
    if (!this.playerId) return;

    this.isLoading = true;
    this.error = '';

    this.profileService.getDrillTimeline(
      this.playerId,
      this.currentPage,
      this.pageSize,
      this.selectedDateFrom || undefined,
      this.selectedDateTo || undefined
    ).subscribe({
      next: (res: any) => {
        this.allEvents = this.mapEvents(res.events ?? res.Events ?? []);
        this.totalItems = res.totalCount ?? res.TotalCount ?? 0;
        this.extractCategories();
        this.applyCategoryFilter();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.error = 'Failed to load drill timeline events.';
        this.cdr.detectChanges();
      }
    });
  }

  private extractCategories() {
    const fromData = [...new Set(this.allEvents.map(e => e.drillCategoryName).filter(Boolean))] as string[];
    const merged = [...new Set([...this.CORE_CATEGORIES, ...fromData])].sort();
    this.categoryOptions = merged.map(c => ({ value: c, label: c }));
  }

  private applyCategoryFilter() {
    if (!this.selectedCategory) {
      this.events = this.allEvents;
    } else {
      this.events = this.allEvents.filter(e => e.drillCategoryName === this.selectedCategory);
    }
  }

  applyFilters() {
    this.filterError = '';

    if (this.selectedDateFrom && this.selectedDateTo && this.selectedDateFrom > this.selectedDateTo) {
      this.filterError = '"From" date must be earlier than "To" date.';
      return;
    }

    this.currentPage = 1;
    this.loadTimeline();
  }

  clearFilters() {
    this.selectedCategory = '';
    this.selectedDateFrom = '';
    this.selectedDateTo = '';
    this.filterError = '';
    this.currentPage = 1;
    this.loadTimeline();
  }

  onDateFromChange() {
    if (this.selectedDateFrom && this.selectedDateTo && this.selectedDateFrom > this.selectedDateTo) {
      this.selectedDateTo = this.selectedDateFrom;
    }
  }

  onDateToChange() {
    if (this.selectedDateFrom && this.selectedDateTo && this.selectedDateFrom > this.selectedDateTo) {
      this.selectedDateFrom = this.selectedDateTo;
    }
  }

  onPageChange(page: number) {
    this.currentPage = page;
    this.loadTimeline();
  }

  getScoreColorClass(event: DrillTimelineEvent): 'score-green' | 'score-yellow' | 'score-red' {
    if (!event.finalScore) return 'score-yellow';
    if (event.finalScore >= 8.0) return 'score-green';
    if (event.finalScore >= 6.5) return 'score-yellow';
    return 'score-red';
  }

  getScoreColorClassForCard(event: DrillTimelineEvent): string {
    if (!event.finalScore) return 'card-yellow';
    if (event.finalScore >= 8.0) return 'card-green';
    if (event.finalScore >= 6.5) return 'card-yellow';
    return 'card-red';
  }

  getCircleColorClass(event: DrillTimelineEvent): string {
    if (!event.finalScore) return 'circle-yellow';
    if (event.finalScore >= 8.0) return 'circle-green';
    if (event.finalScore >= 6.5) return 'circle-yellow';
    return 'circle-red';
  }

  getTargetOffset(event: DrillTimelineEvent): number {
    const score = event.finalScore ?? 0;
    const pct = Math.min(Math.max(score / 10, 0), 1);
    return 110 * (1 - pct);
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  private mapEvents(raw: any[]): DrillTimelineEvent[] {
    return raw.map((e: any) => ({
      date: e.date ?? e.Date ?? '',
      title: e.title ?? e.Title ?? '',
      description: e.description ?? e.Description ?? null,
      sessionId: e.sessionId ?? e.SessionId ?? 0,
      sessionType: e.sessionType ?? e.SessionType ?? '',
      drillCategoryName: e.drillCategoryName ?? e.DrillCategoryName ?? null,
      finalScore: e.finalScore ?? e.FinalScore ?? null,
      drillNotes: e.drillNotes ?? e.DrillNotes ?? null,
      drillTemplateName: e.drillTemplateName ?? e.DrillTemplateName ?? null
    }));
  }
}
