import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../../shared/components/footer/footer';
import { SystemAdminService } from '../../../../../core/services/system-admin/system-admin.service';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { User } from '../../../../../core/interfaces/user.model';

import { PendingRequestsSectionComponent } from '../../components/pending-requests-section/pending-requests-section';
import { ActiveAcademiesSectionComponent } from '../../components/active-academies-section/active-academies-section';
import { ManageBadgesSectionComponent } from '../../components/manage-badges-section/manage-badges-section';
import { ManageUsersSectionComponent } from '../../components/manage-users-section/manage-users-section';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { TranslatePipe } from '@ngx-translate/core';

export type AdminDashboardTab = 'all' | 'pending' | 'academies' | 'badges' | 'users';

@Component({
  selector: 'app-system-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    NavbarComponent,
    Footer,
    PendingRequestsSectionComponent,
    ActiveAcademiesSectionComponent,
    ManageBadgesSectionComponent,
    ManageUsersSectionComponent,
    ScrollRevealDirective,
    LoadingSpinnerComponent,
    TranslatePipe
  ],
  templateUrl: './system-admin-dashboard.component.html',
  styleUrls: ['./system-admin-dashboard.component.css']
})
export class SystemAdminDashboardComponent implements OnInit {
  private systemAdminService = inject(SystemAdminService);
  private authService = inject(AuthService);

  currentUser: User | null = null;
  isLoading = false;
  
  pendingCount = signal<number>(0);
  totalAcademies = signal<number>(0);
  totalUsers = signal<number>(0);
  activeTab = signal<AdminDashboardTab>('all');

  ngOnInit() {
    this.currentUser = this.authService.getCurrentUserValue();
    this.refreshPendingCount();
    this.loadStats();
  }

  setTab(tab: AdminDashboardTab) {
    this.activeTab.set(tab);
  }

  get initials(): string {
    if (this.currentUser?.fullName) {
      const parts = this.currentUser.fullName.trim().split(/\s+/);
      if (parts.length >= 2) {
        return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
      }
      return parts[0].substring(0, 2).toUpperCase();
    }
    if (this.currentUser?.userName) {
      return this.currentUser.userName.substring(0, 2).toUpperCase();
    }
    if (this.currentUser?.email) {
      return this.currentUser.email.substring(0, 2).toUpperCase();
    }
    return 'SA';
  }

  refreshPendingCount() {
    this.systemAdminService.getPendingAcademyRequests().subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.pendingCount.set(res.data.length);
        } else {
          this.pendingCount.set(0);
        }
      },
      error: () => this.pendingCount.set(0)
    });
  }

  loadStats() {
    this.systemAdminService.getAllAcademies({ pageNumber: 1, pageSize: 1 }).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          const data: any = res.data;
          const count = data.totalCount ?? (Array.isArray(data) ? data.length : 0);
          this.totalAcademies.set(count);
        }
      },
      error: () => {}
    });

    this.systemAdminService.getUsers({ pageNumber: 1, pageSize: 1 }).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          const data: any = res.data;
          const count = data.totalCount ?? (Array.isArray(data) ? data.length : 0);
          this.totalUsers.set(count);
        }
      },
      error: () => {}
    });
  }
}
