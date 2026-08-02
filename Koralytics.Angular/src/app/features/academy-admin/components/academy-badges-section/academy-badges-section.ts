import { Component, Input, OnInit, inject, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { AcademyBadgeResponseDto, AcademyBadgeType } from '../../../../../core/interfaces/academy.models';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';

@Component({
  selector: 'app-academy-badges-section',
  standalone: true,
  imports: [CommonModule, LoadingSpinnerComponent],
  templateUrl: './academy-badges-section.html',
  styleUrls: ['./academy-badges-section.css']
})
export class AcademyBadgesSectionComponent implements OnInit, OnChanges {
  @Input() academyId!: number;
  
  private academyService = inject(AcademyService);
  
  badges: AcademyBadgeResponseDto[] = [];
  isLoading = true;
  
  // Expose enum to template
  BadgeType = AcademyBadgeType;

  ngOnInit() {
    this.loadBadges();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['academyId'] && !changes['academyId'].isFirstChange()) {
      this.loadBadges();
    }
  }

  loadBadges() {
    if (!this.academyId) return;
    
    this.isLoading = true;
    this.academyService.getAcademyBadges(this.academyId).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.badges = res.data;
        }
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

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
      default: return 'Badge';
    }
  }
}
