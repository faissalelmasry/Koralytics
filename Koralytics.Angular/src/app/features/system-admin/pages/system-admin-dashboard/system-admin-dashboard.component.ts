import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { SystemAdminService } from '../../../../../core/services/system-admin/system-admin.service';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { User } from '../../../../../core/interfaces/user.model';

import { PendingRequestsSectionComponent } from '../../components/pending-requests-section/pending-requests-section';
import { ActiveAcademiesSectionComponent } from '../../components/active-academies-section/active-academies-section';
import { ManageBadgesSectionComponent } from '../../components/manage-badges-section/manage-badges-section';
import { ManageUsersSectionComponent } from '../../components/manage-users-section/manage-users-section';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';

@Component({
  selector: 'app-system-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    NavbarComponent,
    PendingRequestsSectionComponent,
    ActiveAcademiesSectionComponent,
    ManageBadgesSectionComponent,
    ManageUsersSectionComponent,
    ScrollRevealDirective,
    LoadingSpinnerComponent
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

  ngOnInit() {
    this.currentUser = this.authService.getCurrentUserValue();
    this.refreshPendingCount();
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
}
