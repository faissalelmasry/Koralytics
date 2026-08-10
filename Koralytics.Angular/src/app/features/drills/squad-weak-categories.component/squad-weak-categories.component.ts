import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../shared/pipes/localized-date.pipe';
import { Component, OnInit, OnDestroy, ChangeDetectorRef, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DrillSessionService } from '../../../../core/services/drill/drill-session.service';
import { DrillSessionDto } from '../../../../core/interfaces/drill-session.model';
import { Subscription } from 'rxjs';

interface CategoryPerformance {
  categoryName: string;
  averageScore: number;
  lowestPerformers?: { name: string; score: number }[];
}

interface TrainingSuggestion {
  categoryName: string;
  score: number;
  priority: 'CRITICAL' | 'MODERATE' | 'GOOD';
  recommendedFocus: string;
  lowestPerformers: { name: string; score: number }[];
}

import { CustomSelect, SelectOption } from '../../../../shared/components/custom-select/custom-select';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip';

@Component({
  selector: 'app-squad-weak-categories',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush, // 🟢 OPTIMIZATION: Maximum performance
  imports: [CommonModule, FormsModule, CustomSelect, LoadingSpinnerComponent, StatusChipComponent, TranslatePipe, LocalizedDatePipe],
  templateUrl: './squad-weak-categories.component.html',
  styleUrls: ['./squad-weak-categories.component.css']
})
export class SquadWeakCategoriesComponent implements OnInit, OnDestroy {
    private translate = inject(TranslateService);
    private subscriptions = new Subscription();

  translateCategory(name: string | null | undefined): string {
    if (!name) return '';
    const key = 'DRILLS.CAT_' + name.toUpperCase();
    const translated = this.translate.instant(key);
    return translated !== key ? translated : name;
  }

  get teamOptions(): SelectOption[] {
    return this.availableTeams.map(t => ({ value: t.id, label: t.name }));
  }

  teamOptionsList: SelectOption[] = [];

  onTeamSelect(teamIdVal: any): void {
    this.selectedTeamId = teamIdVal ? Number(teamIdVal) : 1;
    this.onTeamChange();
  }

  getPriorityChipType(priority: string): 'danger' | 'warning' | 'success' {
    if (priority === 'CRITICAL') return 'danger';
    if (priority === 'MODERATE') return 'warning';
    return 'success';
  }
  selectedTeamId: number = 1; // Default or fetched team ID
  availableTeams: { id: number; name: string }[] = [];

  categoriesPerformance: CategoryPerformance[] = [];
  suggestions: TrainingSuggestion[] = [];

  isLoading = true;
  errorMessage = '';

  constructor(
    private sessionService: DrillSessionService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.loadInitialData();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  goBack(): void {
    this.router.navigate(['/drills/sessions']);
  }

  private loadInitialData(): void {
    // Fetch available sessions/teams to populate dropdown
    const filter = { pageNumber: 1, pageSize: 50 };
    this.subscriptions.add(
      this.sessionService.getCoachSessions(filter).subscribe({
        next: (sessions: DrillSessionDto[] | { items: DrillSessionDto[] } | any) => {
          const items: DrillSessionDto[] = Array.isArray(sessions) ? sessions : (sessions.items || sessions.data?.items || []);

          // Extract unique teams
          const teamMap = new Map<number, string>();
          items.forEach(s => {
            if (s.teamId) teamMap.set(s.teamId, s.teamName || `Team #${s.teamId}`);
          });

          this.availableTeams = Array.from(teamMap.entries()).map(([id, name]) => ({ id, name }));

          if (this.availableTeams.length > 0) {
            this.selectedTeamId = this.availableTeams[0].id;
          }

          // 🟢 OPTIMIZATION: Map exactly once instead of constant Getter evaluation
          this.teamOptionsList = this.availableTeams.map(t => ({ value: t.id, label: t.name }));

          this.fetchWeakCategories();
        },
        error: () => {
          // Fallback team if no sessions exist
          this.availableTeams = [{ id: 1, name: 'U17 Team A' }];
          this.teamOptionsList = this.availableTeams.map(t => ({ value: t.id, label: t.name }));
          this.fetchWeakCategories();
        }
      })
    );
  }

  fetchWeakCategories(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.subscriptions.add(
      this.sessionService.getSquadWeakCategories(this.selectedTeamId).subscribe({
        next: (data: CategoryPerformance[]) => {
          // Sort lowest score first (weakest categories at top)
          this.categoriesPerformance = (data || []).sort((a, b) => a.averageScore - b.averageScore);
          this.generateSessionSuggestions();
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Failed to load weak categories', err);
          this.errorMessage = err.error?.message || 'Could not fetch squad analytics.';
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      })
    );
  }

  onTeamChange(): void {
    this.fetchWeakCategories();
  }

  // --- AUTOMATED TRAINING SUGGESTIONS GENERATOR ---
  private generateSessionSuggestions(): void {
    this.suggestions = this.categoriesPerformance.map(cat => {
      let priority: 'CRITICAL' | 'MODERATE' | 'GOOD' = 'GOOD';
      let recommendedFocus = this.translate.instant('DRILLS.WEAK_CATEGORIES.FOCUS_GOOD') || 'Maintain consistent rep loads during regular training.';

      if (cat.averageScore < 5.0) {
        priority = 'CRITICAL';
        recommendedFocus = this.translate.instant('DRILLS.WEAK_CATEGORIES.FOCUS_CRITICAL') || 'Urgent attention required. Dedicate 60% of next week’s field sessions to fundamental drills.';
      } else if (cat.averageScore < 7.5) {
        priority = 'MODERATE';
        recommendedFocus = this.translate.instant('DRILLS.WEAK_CATEGORIES.FOCUS_MODERATE') || 'Secondary focus area. Incorporate tactical warm-up blocks to boost efficiency.';
      }

      // Dynamic player lookup based on category name
      const lowestPerformers = cat.lowestPerformers || [];

      return {
        categoryName: this.translate.instant('DRILLS.DYNAMIC.CAT_' + cat.categoryName.toUpperCase()) !== ('DRILLS.DYNAMIC.CAT_' + cat.categoryName.toUpperCase()) ? this.translate.instant('DRILLS.DYNAMIC.CAT_' + cat.categoryName.toUpperCase()) : cat.categoryName,
        score: cat.averageScore,
        priority,
        recommendedFocus,
        lowestPerformers
      };
    });
  }

  // --- UI HELPERS ---
  getBarWidth(score: number): string {
    const percentage = Math.min(Math.max((score / 10) * 100, 5), 100);
    return `${percentage}%`;
  }

  getScoreClass(score: number): string {
    if (score < 5.0) return 'critical-score';
    if (score < 7.5) return 'moderate-score';
    return 'good-score';
  }

  getBarColor(score: number): string {
    if (score < 5.0) return '#ff4d4d'; // Red
    if (score < 7.5) return '#ffaa00'; // Orange/Yellow
    return '#00e676'; // Green
  }

  scheduleSuggestedSession(): void {
    this.router.navigate(['/drills/sessions/new']);
  }
}