import { Component, OnInit, inject, ChangeDetectorRef, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../shared/pipes/localized-date.pipe';

export interface DisplayDrillTimelineEvent extends DrillTimelineEvent {
  formattedDate: string;
  translatedCategory: string;
  translatedTitle: string;
  translatedSessionType: string;
  scoreColorClass: 'score-green' | 'score-yellow' | 'score-red';
  scoreColorClassForCard: 'card-green' | 'card-yellow' | 'card-red';
  circleColorClass: 'circle-green' | 'circle-yellow' | 'circle-red';
  targetOffset: number;
}

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
    ScrollRevealDirective,
    TranslatePipe,
    LocalizedDatePipe
  ],
  templateUrl: './player-drill-timeline.component.html',
  styleUrls: ['./player-drill-timeline.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PlayerDrillTimelineComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private translate = inject(TranslateService);
  private router = inject(Router);
  private profileService = inject(PlayerProfileService);
  private tokenStorage = inject(TokenStorageService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);

  playerId: number | null = null;
  playerName = '';
  isLoading = false;
  error = '';
  filterError = '';

  currentPage = 1;
  pageSize = 10;
  totalItems = 0;

  allEvents: DisplayDrillTimelineEvent[] = [];
  events: DisplayDrillTimelineEvent[] = [];
  categories: string[] = [];
  private rawEvents: any[] = [];

  selectedCategory = '';
  selectedDateFrom = '';
  selectedDateTo = '';

  private readonly CORE_CATEGORIES = ['Speed', 'Shooting', 'Passing', 'Dribbling', 'Defending', 'Physical'];
  categoryOptions: { value: string; label: string }[] = [];

  ngOnInit() {
    this.updateCategoryOptions();

    const paramId = this.route.snapshot.paramMap.get('playerId');

    if (paramId) {
      this.playerId = Number(paramId);
      this.fetchPlayerDetailsAndTimeline();
    } else {
      const user = this.tokenStorage.getUser();
      if (!user?.userId) {
        this.error = 'Invalid session';
        this.cdr.markForCheck();
        return;
      }
      this.playerId = user.userId;
      this.fetchPlayerDetailsAndTimeline();
    }

    this.translate.onLangChange
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.updateCategoryOptions();
        if (this.rawEvents.length > 0) {
          this.allEvents = this.mapEvents(this.rawEvents);
          this.applyCategoryFilter();
        }
        this.cdr.markForCheck();
      });

    this.translate.onTranslationChange
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.updateCategoryOptions();
        if (this.rawEvents.length > 0) {
          this.allEvents = this.mapEvents(this.rawEvents);
          this.applyCategoryFilter();
        }
        this.cdr.markForCheck();
      });
  }

  private updateCategoryOptions() {
    const fromData = [...new Set(this.allEvents.map(e => e.drillCategoryName).filter(Boolean))] as string[];
    const merged = [...new Set([...this.CORE_CATEGORIES, ...fromData])].sort();
    this.categoryOptions = merged.map(c => {
      const key = 'PLAYER.CAT_' + c.toUpperCase();
      const translated = this.translate.instant(key);
      return { value: c, label: translated !== key ? translated : c };
    });
  }

  translateCategory(name: string | null | undefined): string {
    if (!name) return '';
    const key = 'PLAYER.CAT_' + name.toUpperCase();
    const translated = this.translate.instant(key);
    return translated !== key ? translated : name;
  }

  translateSessionType(name: string | null | undefined): string {
    if (!name) return '';
    let key = 'PLAYER.CAT_' + name.toUpperCase();
    let translated = this.translate.instant(key);
    if (translated !== key) return translated;

    key = 'PLAYER.MATCH_' + name.toUpperCase();
    translated = this.translate.instant(key);
    if (translated !== key) return translated;

    return name;
  }

  fetchPlayerDetailsAndTimeline() {
    if (!this.playerId) return;

    this.profileService.getPlayerProfile(this.playerId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          this.playerName = `${profile.firstName} ${profile.lastName}`;
          this.cdr.markForCheck();
        },
        error: () => {
          this.playerName = 'Player';
          this.cdr.markForCheck();
        }
      });

    this.loadTimeline();
  }

  loadTimeline() {
    if (!this.playerId) return;

    this.isLoading = true;
    this.error = '';
    this.cdr.markForCheck();

    this.profileService.getDrillTimeline(
      this.playerId,
      this.currentPage,
      this.pageSize,
      this.selectedDateFrom || undefined,
      this.selectedDateTo || undefined
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res: any) => {
          this.rawEvents = res.events ?? res.Events ?? [];
          this.allEvents = this.mapEvents(this.rawEvents);
          this.totalItems = res.totalCount ?? res.TotalCount ?? 0;
          this.extractCategories();
          this.applyCategoryFilter();
          this.isLoading = false;
          this.cdr.markForCheck();
        },
        error: () => {
          this.isLoading = false;
          this.error = 'Failed to load drill timeline events.';
          this.cdr.markForCheck();
        }
      });
  }

  private extractCategories() {
    this.updateCategoryOptions();
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
      this.cdr.markForCheck();
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
      this.cdr.markForCheck();
    }
  }

  onDateToChange() {
    if (this.selectedDateFrom && this.selectedDateTo && this.selectedDateFrom > this.selectedDateTo) {
      this.selectedDateFrom = this.selectedDateTo;
      this.cdr.markForCheck();
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

  getScoreColorClassForCard(event: DrillTimelineEvent): 'card-green' | 'card-yellow' | 'card-red' {
    if (!event.finalScore) return 'card-yellow';
    if (event.finalScore >= 8.0) return 'card-green';
    if (event.finalScore >= 6.5) return 'card-yellow';
    return 'card-red';
  }

  getCircleColorClass(event: DrillTimelineEvent): 'circle-green' | 'circle-yellow' | 'circle-red' {
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

  private mapEvents(raw: any[]): DisplayDrillTimelineEvent[] {
    return raw.map((e: any) => {
      const date = e.date ?? e.Date ?? '';
      const title = e.title ?? e.Title ?? '';
      const sessionType = e.sessionType ?? e.SessionType ?? '';
      const drillCategoryName = e.drillCategoryName ?? e.DrillCategoryName ?? null;
      const finalScore = e.finalScore ?? e.FinalScore ?? null;

      const baseEvent: DrillTimelineEvent = {
        date,
        title,
        description: e.description ?? e.Description ?? null,
        sessionId: e.sessionId ?? e.SessionId ?? 0,
        sessionType,
        drillCategoryName,
        finalScore,
        drillNotes: e.drillNotes ?? e.DrillNotes ?? null,
        drillTemplateName: e.drillTemplateName ?? e.DrillTemplateName ?? null
      };

      return {
        ...baseEvent,
        formattedDate: this.formatDate(date),
        translatedCategory: this.translateCategory(drillCategoryName),
        translatedTitle: this.translateCategory(title),
        translatedSessionType: this.translateSessionType(sessionType),
        scoreColorClass: this.getScoreColorClass(baseEvent),
        scoreColorClassForCard: this.getScoreColorClassForCard(baseEvent),
        circleColorClass: this.getCircleColorClass(baseEvent),
        targetOffset: this.getTargetOffset(baseEvent),
      };
    });
  }
}
