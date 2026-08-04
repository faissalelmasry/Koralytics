import { Component, OnInit, OnChanges, SimpleChanges, Input, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DrillSessionService } from '../../../../core/services/drill/drill-session.service';
import { CustomSelect, SelectOption } from '../../../../shared/components/custom-select/custom-select';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';

interface ProgressionPoint {
  sessionDate: string;
  finalScore: number;
  drillName: string;
}

@Component({
  selector: 'app-player-drill-progression',
  standalone: true,
  imports: [CommonModule, FormsModule, CustomSelect, LoadingSpinnerComponent, CustomButtonComponent],
  templateUrl: './player-drill-progression.component.html',
  styleUrls: ['./player-drill-progression.component.css']
})
export class PlayerDrillProgressionComponent implements OnInit, OnChanges {
  @Input() playerId?: number | null;
  @Input() tierButtonVariant: 'accent' | 'coral' | 'cyan' | 'slate' | 'amber' | 'gold' = 'accent';

  categoryOptions: SelectOption[] = [];
  selectedCategoryId: number | null = null;
  categories: any[] = [];
  progressionData: ProgressionPoint[] = [];

  categoryName = '';
  isLoading = true;
  errorMessage = '';

  // Cached chart calculations to prevent change detection loops
  coordinates: Array<ProgressionPoint & { x: number; y: number }> = [];
  svgPoints = '';
  areaPath = '';
  averageScore = 0;
  peakScore = 0;

  // Active hover point for tooltips
  hoveredPoint: (ProgressionPoint & { x: number; y: number }) | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private sessionService: DrillSessionService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    if (this.playerId) {
      this.loadCategories();
    } else {
      this.route.paramMap.subscribe(params => {
        const id = params.get('id') || params.get('playerId');
        if (id) {
          this.playerId = Number(id);
          this.loadCategories();
        } else {
          this.errorMessage = 'No Player ID provided.';
          this.isLoading = false;
        }
      });
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['playerId'] && !changes['playerId'].firstChange && this.playerId) {
      this.loadCategories();
    }
  }

  onCategorySelect(catId: any): void {
    this.selectedCategoryId = catId ? Number(catId) : null;
    this.onCategoryChange();
  }

  private loadCategories(): void {
    this.sessionService.getCategories().subscribe({
      next: (cats) => {
        this.categories = cats || [];
        this.categoryOptions = this.categories.map(c => ({ value: c.id, label: c.name }));
        if (this.categories.length > 0) {
          this.selectedCategoryId = this.categories[0].id;
          this.fetchProgression();
        } else {
          this.isLoading = false;
        }
      },
      error: (err) => {
        console.error('Failed to load categories', err);
        this.errorMessage = 'Could not load drill categories.';
        this.isLoading = false;
      }
    });
  }

  onCategoryChange(): void {
    if (this.selectedCategoryId) {
      this.fetchProgression();
    }
  }

  fetchProgression(): void {
    if (!this.selectedCategoryId || !this.playerId) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.hoveredPoint = null;

    this.sessionService.getPlayerProgression(this.playerId, this.selectedCategoryId).subscribe({
      next: (res: any) => {
        this.categoryName = res.categoryName || 'Category Progression';
        this.progressionData = res.progressionChart || [];
        this.calculateChartData();
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Progression Fetch Error', err);
        this.errorMessage = err.error?.message || 'Failed to load player progression data.';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  private calculateChartData(): void {
    if (!this.progressionData.length) {
      this.coordinates = [];
      this.svgPoints = '';
      this.areaPath = '';
      this.averageScore = 0;
      this.peakScore = 0;
      return;
    }

    const width = 800;
    const height = 240; // enlarged height for clear visibility
    const paddingX = 50;

    if (this.progressionData.length === 1) {
      this.coordinates = [{
        ...this.progressionData[0],
        x: width / 2,
        y: height - (this.progressionData[0].finalScore / 10) * height + 25
      }];
    } else {
      const stepX = (width - paddingX * 2) / (this.progressionData.length - 1);
      this.coordinates = this.progressionData.map((pt, i) => {
        const score = Math.min(Math.max(pt.finalScore, 0), 10);
        const x = paddingX + i * stepX;
        const y = height - (score / 10) * height + 25;
        return { ...pt, x, y };
      });
    }

    this.svgPoints = this.coordinates.map(p => `${p.x},${p.y}`).join(' ');

    const firstX = this.coordinates[0].x;
    const lastX = this.coordinates[this.coordinates.length - 1].x;
    const linePath = this.coordinates.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x} ${p.y}`).join(' ');
    this.areaPath = `${linePath} L ${lastX} 265 L ${firstX} 265 Z`;

    const total = this.progressionData.reduce((acc, curr) => acc + curr.finalScore, 0);
    this.averageScore = Math.round((total / this.progressionData.length) * 10) / 10;
    this.peakScore = Math.max(...this.progressionData.map(p => p.finalScore));
  }

  goBack(): void {
    window.history.back();
  }

  goToDrillTimeline(): void {
    if (this.playerId) {
      this.router.navigate(['/player/drill-timeline', this.playerId]);
    } else {
      this.router.navigate(['/player/drill-timeline']);
    }
  }
}