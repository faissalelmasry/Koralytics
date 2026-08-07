import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { TranslatePipe } from '@ngx-translate/core';
import { FeatureLockComponent } from '../../../../shared/components/feature-lock/feature-lock';

@Component({
  selector: 'app-drills-dashboard',
  standalone: true,
  imports: [CommonModule, NavbarComponent, FeatureLockComponent, TranslatePipe],
  templateUrl: './drills-dashboard.component.html',
  styleUrls: ['./drills-dashboard.component.css']
})
export class DrillsDashboardComponent {
  constructor(private router: Router) { }

  navigateToDrillTemplates(): void {
    this.router.navigate(['/drills']);
  }

  navigateToDrillSessions(): void {
    this.router.navigate(['/drills/sessions']);
  }

  navigateToCoachBias(): void {
    this.router.navigate(['/drills/analytics/coach-bias']);
  }
}
