import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth/auth.service';
import { CustomButtonComponent } from '../../../shared/components/custom-button/custom-button';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, CustomButtonComponent],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent {
  public authService = inject(AuthService);

  logout() {
    this.authService.logout().subscribe(() => {
      // The auth service already handles clearing local state. 
      // AuthGuard will catch it on next navigation, but we manually navigate here just in case.
      window.location.href = '/auth/login';
    });
  }
}
