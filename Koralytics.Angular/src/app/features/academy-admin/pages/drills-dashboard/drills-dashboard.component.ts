import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';

@Component({
  selector: 'app-drills-dashboard',
  standalone: true,
  imports: [CommonModule, NavbarComponent],
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
