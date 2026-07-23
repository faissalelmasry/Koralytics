import { Component, signal, HostListener, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './navbar.html',
  styleUrls: ['./navbar.css']
})
export class NavbarComponent {
  private authService = inject(AuthService);

  variant = signal<'primary' | 'icon'>('icon'); 
  
  isSidebarOpen = false;
  isScrolled = false;

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
}