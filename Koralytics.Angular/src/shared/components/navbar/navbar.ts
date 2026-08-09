import { Component, signal, HostListener, inject, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth/auth.service';
import { TokenStorageService } from '../../../core/services/auth/token-storage.service';
import { ParentService } from '../../../core/services/parent/parent.service';
import { ConfirmDialogComponent } from '../confirm-dialog/confirm-dialog';
import { LanguageSwitcherComponent } from '../language-switcher/language-switcher.component';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterModule, CommonModule, ConfirmDialogComponent, LanguageSwitcherComponent, TranslatePipe],
  templateUrl: './navbar.html',
  styleUrls: ['./navbar.css']
})
export class NavbarComponent implements OnInit {
  private authService = inject(AuthService);
  private tokenStorage = inject(TokenStorageService);
  private parentService = inject(ParentService);
  private router = inject(Router);

  ngOnInit() {
    if (this.isParent) {
      this.parentService.getMyChildren().subscribe({
        next: (res: any) => {
          const children = res?.data || res || [];
          const hasElite = children.some((c: any) => c.isEliteTier || c.academyTier === 'Elite');
          const hasPro = children.some((c: any) => c.academyTier === 'Pro');
          if (hasElite) {
            this.tokenStorage.saveEffectiveTier('Elite');
          } else if (hasPro) {
            this.tokenStorage.saveEffectiveTier('Pro');
          }
        }
      });
    }
  }

  get isParent(): boolean {
    return this.authService.getUserRoles().includes('Parent');
  }

  get isEliteTier(): boolean {
    return this.tokenStorage.getTier() === 'Elite';
  }

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
  get isScouter(): boolean {
    return this.authService.getUserRoles().includes('Scouter');
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

