import { Component, OnInit, OnDestroy, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DrillSessionService } from '../../../../core/services/drill/drill-session.service';
import { DrillSessionDto } from '../../../../core/interfaces/drill-session.model';
import { Subscription } from 'rxjs';

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
  changeDetection: ChangeDetectionStrategy.OnPush, // 🟢 OPTIMIZATION: Maximum performance
  imports: [CommonModule, FormsModule, CustomSelect, LoadingSpinnerComponent, StatusChipComponent],
  templateUrl: './squad-weak-categories.component.html',
  styleUrls: ['./squad-weak-categories.component.css']
})
export class SquadWeakCategoriesComponent implements OnInit, OnDestroy {
  // 🟢 OPTIMIZATION: Memory management
  private subscriptions = new Subscription();

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
      let recommendedFocus = 'Maintain consistent rep loads during regular training.';

      if (cat.averageScore < 5.0) {
        priority = 'CRITICAL';
        recommendedFocus = 'Urgent attention required. Dedicate 60% of next week’s field sessions to fundamental drills.';
      } else if (cat.averageScore < 7.5) {
        priority = 'MODERATE';
        recommendedFocus = 'Secondary focus area. Incorporate tactical warm-up blocks to boost efficiency.';
      }

      // Dynamic drill lookup based on priority and category name
      const suggestedDrills = this.getRecommendedDrills(cat.categoryName, priority);

      return {
        categoryName: cat.categoryName,
        score: cat.averageScore,
        priority,
        recommendedFocus,
        suggestedDrills
      };
    });
  }

  private getRecommendedDrills(category: string, priority: 'CRITICAL' | 'MODERATE' | 'GOOD'): string[] {
    const catLower = category.toLowerCase();

    // 1. SHOOTING / FINISHING
    if (catLower.includes('shoot') || catLower.includes('finish')) {
      if (priority === 'CRITICAL') return ['1v1 Finishing Under Pressure', 'First-Touch Box Striking'];
      if (priority === 'MODERATE') return ['Volley Technique Drill', 'Long Distance Power Shots'];
      return ['Advanced Counter-Attack Finishing', 'Free Kick & Dead Ball Practice'];
    }

    // 2. PASSING / VISION
    if (catLower.includes('pass') || catLower.includes('vision')) {
      if (priority === 'CRITICAL') return ['Rondo 4v2 Quick Touch', 'Short Wall-Pass Combinations'];
      if (priority === 'MODERATE') return ['Long Diagonal Switch Drill', 'Through-Ball Timing Exercises'];
      return ['One-Touch Possession Grid', 'Over-the-Top Lofted Passes'];
    }

    // 3. PHYSICAL / SPEED / STAMINA / AGILITY
    if (catLower.includes('physic') || catLower.includes('agil') || catLower.includes('speed') || catLower.includes('stamina')) {
      if (priority === 'CRITICAL') return ['Agility Ladder Sprint Reps', 'High-Intensity Shuttle Runs'];
      if (priority === 'MODERATE') return ['Plyometric Box Jumps', 'Interval Resistance Runs'];
      return ['Match-Pace Recovery Runs', 'Explosive Acceleration Reps'];
    }

    // 4. DEFENDING
    if (catLower.includes('defend')) {
      if (priority === 'CRITICAL') return ['1v1 Jockeying & Tackling', 'Defensive Line Back-Pedal'];
      if (priority === 'MODERATE') return ['2v2 High-Press Recovery', 'Blocking Crosses in Box'];
      return ['Offside Trap Coordination', 'Zonal Marking Drills'];
    }

    // 5. GOALKEEPING
    if (catLower.includes('keep') || catLower.includes('goal')) {
      if (priority === 'CRITICAL') return ['Shot Stopping & Reaction Saves', 'Handling High Crosses'];
      if (priority === 'MODERATE') return ['Distribution & Long Kicks', '1v1 Smothering Angles'];
      return ['Sweeper Keeper Sweeping', 'Penalty Reaction Training'];
    }

    // 6. DRIBBLING
    if (catLower.includes('dribbl')) {
      if (priority === 'CRITICAL') return ['Tight-Space Cone Weaving', 'Close Control Touches'];
      if (priority === 'MODERATE') return ['1v1 Isolation Takes', 'Change of Pace Dribbling'];
      return ['High-Speed Wing Skill Moves', 'Shielding Under Pressure'];
    }

    // Default Maintenance Fallbacks
    return ['Advanced Technical Routine', 'Positional Maintenance Scrimmage'];
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