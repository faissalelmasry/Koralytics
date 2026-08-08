import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../shared/pipes/localized-date.pipe';
import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DrillSessionService } from '../../../../core/services/drill/drill-session.service';

interface CategoryPerformance {
  categoryName: string;
  averageScore: number;
}

interface TrainingSuggestion {
  categoryName: string;
  score: number;
  priority: 'CRITICAL' | 'MODERATE' | 'GOOD';
  recommendedFocus: string;
  suggestedDrills: string[];
}

import { CustomSelect, SelectOption } from '../../../../shared/components/custom-select/custom-select';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip';

@Component({
  selector: 'app-squad-weak-categories',
  standalone: true,
  imports: [CommonModule, FormsModule, CustomSelect, LoadingSpinnerComponent, StatusChipComponent, TranslatePipe, LocalizedDatePipe],
  templateUrl: './squad-weak-categories.component.html',
  styleUrls: ['./squad-weak-categories.component.css']
})
export class SquadWeakCategoriesComponent implements OnInit {
  private translate = inject(TranslateService);
  translateCategory(name: string | null | undefined): string {
    if (!name) return '';
    const key = 'DRILLS.CAT_' + name.toUpperCase();
    const translated = this.translate.instant(key);
    return translated !== key ? translated : name;
  }

  get teamOptions(): SelectOption[] {
    return this.availableTeams.map(t => ({ value: t.id, label: t.name }));
  }

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

  goBack(): void {
    this.router.navigate(['/drills/sessions']);
  }

  private loadInitialData(): void {
    // Fetch available sessions/teams to populate dropdown
    const filter = { pageNumber: 1, pageSize: 50 };
    this.sessionService.getCoachSessions(filter).subscribe({
      next: (sessions: any) => {
        const items = Array.isArray(sessions) ? sessions : (sessions.items || []);

        // Extract unique teams
        const teamMap = new Map<number, string>();
        items.forEach((s: any) => {
          if (s.teamId) teamMap.set(s.teamId, s.teamName || `Team #${s.teamId}`);
        });

        this.availableTeams = Array.from(teamMap.entries()).map(([id, name]) => ({ id, name }));

        if (this.availableTeams.length > 0) {
          this.selectedTeamId = this.availableTeams[0].id;
        }

        this.fetchWeakCategories();
      },
      error: () => {
        // Fallback team if no sessions exist
        this.availableTeams = [{ id: 1, name: 'U17 Team A' }];
        this.fetchWeakCategories();
      }
    });
  }

  fetchWeakCategories(): void {
    this.isLoading = true;
    this.errorMessage = '';

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
    });
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

      // Dynamic drill lookup based on priority and category name
      const suggestedDrills = this.getRecommendedDrills(cat.categoryName, priority);

      return {
        categoryName: this.translate.instant('DRILLS.DYNAMIC.CAT_' + cat.categoryName.toUpperCase()) !== ('DRILLS.DYNAMIC.CAT_' + cat.categoryName.toUpperCase()) ? this.translate.instant('DRILLS.DYNAMIC.CAT_' + cat.categoryName.toUpperCase()) : cat.categoryName,
        score: cat.averageScore,
        priority,
        recommendedFocus,
        suggestedDrills
      };
    });
  }

  private getRecommendedDrills(category: string, priority: 'CRITICAL' | 'MODERATE' | 'GOOD'): string[] {
    const catLower = category.toLowerCase();
    let drills: string[] = [];

    // 1. SHOOTING / FINISHING
    if (catLower.includes('shoot') || catLower.includes('finish')) {
      if (priority === 'CRITICAL') drills = ['1v1 Finishing Under Pressure', 'First-Touch Box Striking'];
      else if (priority === 'MODERATE') drills = ['Volley Technique Drill', 'Long Distance Power Shots'];
      else drills = ['Advanced Counter-Attack Finishing', 'Free Kick & Dead Ball Practice'];
    }
    // 2. PASSING / VISION
    else if (catLower.includes('pass') || catLower.includes('vision')) {
      if (priority === 'CRITICAL') drills = ['Rondo 4v2 Quick Touch', 'Short Wall-Pass Combinations'];
      else if (priority === 'MODERATE') drills = ['Long Diagonal Switch Drill', 'Through-Ball Timing Exercises'];
      else drills = ['One-Touch Possession Grid', 'Over-the-Top Lofted Passes'];
    }
    // 3. PHYSICAL / SPEED / STAMINA / AGILITY
    else if (catLower.includes('physic') || catLower.includes('agil') || catLower.includes('speed') || catLower.includes('stamina')) {
      if (priority === 'CRITICAL') drills = ['Agility Ladder Sprint Reps', 'High-Intensity Shuttle Runs'];
      else if (priority === 'MODERATE') drills = ['Plyometric Box Jumps', 'Interval Resistance Runs'];
      else drills = ['Match-Pace Recovery Runs', 'Explosive Acceleration Reps'];
    }
    // 4. DEFENDING
    else if (catLower.includes('defend')) {
      if (priority === 'CRITICAL') drills = ['1v1 Jockeying & Tackling', 'Defensive Line Back-Pedal'];
      else if (priority === 'MODERATE') drills = ['2v2 High-Press Recovery', 'Blocking Crosses in Box'];
      else drills = ['Offside Trap Coordination', 'Zonal Marking Drills'];
    }
    // 5. GOALKEEPING
    else if (catLower.includes('keep') || catLower.includes('goal')) {
      if (priority === 'CRITICAL') drills = ['Shot Stopping & Reaction Saves', 'Handling High Crosses'];
      else if (priority === 'MODERATE') drills = ['Distribution & Long Kicks', '1v1 Smothering Angles'];
      else drills = ['Sweeper Keeper Sweeping', 'Penalty Reaction Training'];
    }
    // 6. DRIBBLING
    else if (catLower.includes('dribbl')) {
      if (priority === 'CRITICAL') drills = ['Tight-Space Cone Weaving', 'Close Control Touches'];
      else if (priority === 'MODERATE') drills = ['1v1 Isolation Takes', 'Change of Pace Dribbling'];
      else drills = ['High-Speed Wing Skill Moves', 'Shielding Under Pressure'];
    }
    // Default Maintenance Fallbacks
    else {
      drills = ['Advanced Technical Routine', 'Positional Maintenance Scrimmage'];
    }

    return drills.map(d => {
      const key = 'DRILLS.DYNAMIC.' + d.toUpperCase().replace(/[^A-Z0-9]/g, '_').replace(/_+/g, '_');
      const translated = this.translate.instant(key);
      return translated !== key ? translated : d;
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