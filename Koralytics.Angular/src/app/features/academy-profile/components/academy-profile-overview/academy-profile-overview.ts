import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { AcademyBadgeResponseDto, AcademyBadgeType, AcademyLocationResponseDto, AcademyMemberResponseDto, TeamResponseDto } from '../../../../../core/interfaces/academy.models';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state';
import { LocalizedDatePipe } from '../../../../../shared/pipes/localized-date.pipe';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

@Component({
  selector: 'app-academy-profile-overview',
  standalone: true,
  imports: [CommonModule, TranslatePipe, CustomButtonComponent, EmptyStateComponent, LocalizedDatePipe, ScrollRevealDirective],
  templateUrl: './academy-profile-overview.html',
  styleUrls: ['./academy-profile-overview.css']
})
export class AcademyProfileOverviewComponent {
  @Input() badges: AcademyBadgeResponseDto[] = [];
  @Input() locations: AcademyLocationResponseDto[] = [];
  @Input() teams: TeamResponseDto[] = [];
  @Input() members: AcademyMemberResponseDto[] = [];

  @Output() viewAllBadges = new EventEmitter<void>();
  @Output() viewAllLocations = new EventEmitter<void>();

  get mainLocation(): AcademyLocationResponseDto | undefined {
    return this.locations.find(l => l.isMain || l.isMainLocation) || this.locations[0];
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

  getBadgeNameTranslationKey(type: any): string {
    const key = this.getBadgeTypeKey(type);
    return `ACADEMY_PROFILE.BADGE_${key.toUpperCase()}`;
  }
}
