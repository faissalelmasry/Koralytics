import { Component, signal, HostListener, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth/auth.service';
import { ConfirmDialogComponent } from '../confirm-dialog/confirm-dialog';
import { LanguageSwitcherComponent } from '../language-switcher/language-switcher.component';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterModule, CommonModule, ConfirmDialogComponent, LanguageSwitcherComponent],
  templateUrl: './navbar.html',
  styleUrls: ['./navbar.css']
})
export class NavbarComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  variant = signal<'primary' | 'icon'>('icon'); 
  
  isSidebarOpen = false;
  isScrolled = false;
  showSignOutConfirm = false;

  get userDashboardRoute(): string {
    return this.authService.getRoleDashboardRoute();
  }

  get isSystemAdmin(): boolean {
    return this.authService.getUserRoles().includes('SystemAdmin');
  }

  @HostListener('window:scroll', [])
  onWindowScroll() {
    this.isScrolled = window.scrollY > 20;
  }

  toggleSidebar(status: boolean) {
    this.isSidebarOpen = status;
  }

  get isLoggedIn(): boolean {
    return this.authService.isLoggedIn();
  }

  get isCoach(): boolean {
    return this.authService.getUserRoles().includes('Coach');
  }

  get isPlayer(): boolean {
    return this.authService.getUserRoles().includes('Player');
  }

  get isAcademyAdmin(): boolean {
    return this.authService.getUserRoles().includes('AcademyAdmin');
  }

  requestLogout() {
    this.showSignOutConfirm = true;
  }

  cancelLogout() {
    this.showSignOutConfirm = false;
  }

  logout() {
    this.showSignOutConfirm = false;
    this.authService.logout().subscribe({
      next: () => {
        this.toggleSidebar(false);
        this.router.navigate(['/auth/login']);
      },
      error: () => {
        this.toggleSidebar(false);
        this.router.navigate(['/auth/login']);
      }
    });
  }
}
