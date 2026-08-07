import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { AcademyBadgeResponseDto, AcademyBadgeType } from '../../../../../core/interfaces/academy.models';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state';
import { LocalizedDatePipe } from '../../../../../shared/pipes/localized-date.pipe';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

@Component({
  selector: 'app-academy-profile-badges',
  standalone: true,
  imports: [CommonModule, TranslatePipe, EmptyStateComponent, LocalizedDatePipe, ScrollRevealDirective],
  templateUrl: './academy-profile-badges.html',
  styleUrls: ['./academy-profile-badges.css']
})
export class AcademyProfileBadgesComponent {
  @Input() badges: AcademyBadgeResponseDto[] = [];

  getBadgeTypeKey(type: any): string {
    const t = Number(type) || type;
    switch (t) {
      case AcademyBadgeType.Verified: case 'Verified': case 1: return 'Verified';
      case AcademyBadgeType.TopPerformer: case 'TopPerformer': case 2: return 'TopPerformer';
      case AcademyBadgeType.Premium: case 'Premium': case 3: return 'Premium';
      default: return 'Default';
    }
  }

  getBadgeNameTranslationKey(type: any): string {
    const key = this.getBadgeTypeKey(type);
    return `ACADEMY_PROFILE.BADGE_${key.toUpperCase()}`;
  }
}
