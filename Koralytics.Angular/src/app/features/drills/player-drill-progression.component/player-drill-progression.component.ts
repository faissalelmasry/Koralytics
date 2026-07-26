import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DrillSessionService } from '../../../../core/services/drill/drill-session.service';

interface ProgressionPoint {
  sessionDate: string;
  finalScore: number;
  drillName: string;
}

import { CustomSelect, SelectOption } from '../../../../shared/components/custom-select/custom-select';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';

interface ProgressionPoint {
  sessionDate: string;
  finalScore: number;
  drillName: string;
}

@Component({
  selector: 'app-player-drill-progression',
  standalone: true,
  imports: [CommonModule, FormsModule, CustomSelect, LoadingSpinnerComponent],
  templateUrl: './player-drill-progression.component.html',
  styleUrls: ['./player-drill-progression.component.css']
})
export class PlayerDrillProgressionComponent implements OnInit {
  get categoryOptions(): SelectOption[] {
    return this.categories.map(c => ({ value: c.id, label: c.name }));
  }

  onCategorySelect(catId: any): void {
    this.selectedCategoryId = catId ? Number(catId) : null;
    this.onCategoryChange();
  }
  playerId!: number;
  selectedCategoryId: number | null = null;
  categories: any[] = [];
  progressionData: ProgressionPoint[] = [];

  categoryName = '';
  isLoading = true;
  errorMessage = '';

  // Active hover point for tooltips
  hoveredPoint: (ProgressionPoint & { x: number; y: number }) | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private sessionService: DrillSessionService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id') || params.get('playerId');
      if (id) {
        this.playerId = Number(id);
        this.loadCategories();
      } else {
        this.errorMessage = 'No Player ID provided in route.';
        this.isLoading = false;
      }
    });
  }

  private loadCategories(): void {
    this.sessionService.getCategories().subscribe({
      next: (cats) => {
        this.categories = cats || [];
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
    if (!this.selectedCategoryId) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.hoveredPoint = null;

    this.sessionService.getPlayerProgression(this.playerId, this.selectedCategoryId).subscribe({
      next: (res: any) => {
        this.categoryName = res.categoryName || 'Category Progression';
        this.progressionData = res.progressionChart || [];
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Progression Fetch Error', err);
        this.errorMessage = err.error?.message || 'Failed to load player progression data.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  // --- SVG CHART CALCULATION HELPERS ---
  get svgPoints(): string {
    if (!this.progressionData.length) return '';
    return this.getCoordinates().map(p => `${p.x},${p.y}`).join(' ');
  }

  get areaPath(): string {
    const coords = this.getCoordinates();
    if (!coords.length) return '';
    const firstX = coords[0].x;
    const lastX = coords[coords.length - 1].x;
    const linePath = coords.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x} ${p.y}`).join(' ');
    return `${linePath} L ${lastX} 260 L ${firstX} 260 Z`;
  }

  getCoordinates(): Array<ProgressionPoint & { x: number; y: number }> {
    const width = 800;
    const height = 220; // 20px padding top/bottom (0 to 260)
    const paddingX = 50;

    if (this.progressionData.length === 1) {
      return [{
        ...this.progressionData[0],
        x: width / 2,
        y: height - (this.progressionData[0].finalScore / 10) * height + 20
      }];
    }

    const stepX = (width - paddingX * 2) / (this.progressionData.length - 1);

    return this.progressionData.map((pt, i) => {
      const score = Math.min(Math.max(pt.finalScore, 0), 10); // Clamp 0-10
      const x = paddingX + i * stepX;
      const y = height - (score / 10) * height + 20; // Invert Y for SVG
      return { ...pt, x, y };
    });
  }

  getAverageScore(): number {
    if (!this.progressionData.length) return 0;
    const total = this.progressionData.reduce((acc, curr) => acc + curr.finalScore, 0);
    return Math.round((total / this.progressionData.length) * 10) / 10;
  }

  getPeakScore(): number {
    if (!this.progressionData.length) return 0;
    return Math.max(...this.progressionData.map(p => p.finalScore));
  }

  goBack(): void {
    window.history.back();
  }
}