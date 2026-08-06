import { Component, Input, OnInit, inject } from '@angular/core';
import { LocalizedDatePipe } from '../../../../../shared/pipes/localized-date.pipe';

import { CommonModule } from '@angular/common';
import { SystemAdminService } from '../../../../../core/services/system-admin/system-admin.service';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-academy-detail-panel',
  standalone: true,
  imports: [LocalizedDatePipe, CommonModule, LoadingSpinnerComponent, TranslatePipe],
  templateUrl: './academy-detail-panel.html',
  styleUrls: ['./academy-detail-panel.css']
})
export class AcademyDetailPanelComponent implements OnInit {
  @Input() academyId!: number;

  private systemAdminService = inject(SystemAdminService);

  activeSubTab: 'members' | 'admins' | 'locations' | 'badges' | 'subscriptions' = 'members';

  isLoading = true;
  members: any[] = [];
  admins: any[] = [];
  locations: any[] = [];
  badges: any[] = [];
  subscriptions: any = null;

  ngOnInit() {
    this.loadAllData();
  }

  loadAllData() {
    if (!this.academyId) return;

    this.isLoading = true;
    let completed = 0;
    const checkComplete = () => {
      completed++;
      if (completed >= 5) {
        this.isLoading = false;
      }
    };

    this.systemAdminService.getAcademyMembers(this.academyId, { pageSize: 50 }).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          const data: any = res.data;
          this.members = Array.isArray(data) ? data : (data.items || data.members || data.data || []);
        }
        checkComplete();
      },
      error: () => checkComplete()
    });

    this.systemAdminService.getAcademyAdmins(this.academyId).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          const data: any = res.data;
          this.admins = Array.isArray(data) ? data : (data.items || data.admins || data.data || []);
        }
        checkComplete();
      },
      error: () => checkComplete()
    });

    this.systemAdminService.getAcademyLocations(this.academyId).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          const data: any = res.data;
          this.locations = Array.isArray(data) ? data : (data.items || data.locations || data.data || []);
        }
        checkComplete();
      },
      error: () => checkComplete()
    });

    this.systemAdminService.getAcademyBadges(this.academyId).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          const data: any = res.data;
          this.badges = Array.isArray(data) ? data : (data.items || data.badges || data.data || []);
        }
        checkComplete();
      },
      error: () => checkComplete()
    });

    this.systemAdminService.getSubscriptionStatus(this.academyId).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.subscriptions = res.data;
        }
        checkComplete();
      },
      error: () => checkComplete()
    });
  }

  setSubTab(tab: 'members' | 'admins' | 'locations' | 'badges' | 'subscriptions') {
    this.activeSubTab = tab;
  }

  getBadgeLabel(type: string): string {
    if (type === 'Verified' || type === 'Verified Academy') return 'Verified Academy';
    if (type === 'TopPerformer' || type === 'Top Performer') return 'Top Performer';
    if (type === 'Premium' || type === 'Premium Partner') return 'Premium Partner';
    return type;
  }
}

