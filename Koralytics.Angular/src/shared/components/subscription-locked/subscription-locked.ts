import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth/auth.service';

@Component({
  selector: 'app-subscription-locked',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './subscription-locked.html',
  styleUrls: ['./subscription-locked.css']
})
export class SubscriptionLockedComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  isAcademyAdmin = false;

  constructor() {
    this.isAcademyAdmin = this.authService.getUserRoles().includes('AcademyAdmin');
  }

  logout() {
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate(['/auth/login']);
      },
      error: () => {
        this.router.navigate(['/auth/login']);
      }
    });
  }
}
