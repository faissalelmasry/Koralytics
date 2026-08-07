import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { AcademyLocationResponseDto } from '../../../../../core/interfaces/academy.models';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

@Component({
  selector: 'app-academy-profile-locations',
  standalone: true,
  imports: [CommonModule, TranslatePipe, EmptyStateComponent, ScrollRevealDirective],
  templateUrl: './academy-profile-locations.html',
  styleUrls: ['./academy-profile-locations.css']
})
export class AcademyProfileLocationsComponent {
  @Input() locations: AcademyLocationResponseDto[] = [];
}
