import { Component, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { ModalService } from '../../../../../core/services/Modal/modal';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { DataTable, TableColumn } from '../../../../../shared/components/data-table/data-table';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { Router } from '@angular/router';
import { AcademySubscriptions } from '../academy-subscriptions/academy-subscriptions.component';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../../shared/pipes/localized-date.pipe';

@Component({
  selector: 'app-academy-subscriptions-section',
  standalone: true,
  imports: [
    CommonModule,
    CustomButtonComponent,
    DataTable,
    LoadingSpinnerComponent,
    AcademySubscriptions,
    TranslatePipe,
    LocalizedDatePipe
  ],
  templateUrl: './academy-subscriptions-section.html',
  styleUrls: ['./academy-subscriptions-section.css']
})
export class AcademySubscriptionsSectionComponent implements OnInit, OnChanges {
  @Input() academyId!: number;

  subscriptions: any[] = [];
  subscriptionStats: any = null;
  isLoadingSubscriptions = false;

  get subColumns(): TableColumn[] {
    return [
      { key: 'playerFullName', label: 'ACADEMY_ADMIN.COMMS_SECTION.COL_PLAYER_NAME', type: 'text' },
      { key: 'statusBadge', label: 'ACADEMY_ADMIN.COMMS_SECTION.COL_STATUS', type: 'badge', translate: true },
      { key: 'graceUntil', label: 'ACADEMY_ADMIN.COMMS_SECTION.COL_GRACE_UNTIL', type: 'date' },
      { key: 'actions', label: 'ACADEMY_ADMIN.COMMS_SECTION.COL_UPDATE', type: 'action' }
    ];
  }

  constructor(
    private academyService: AcademyService,
    private modalService: ModalService,
    private router: Router,
    private translate: TranslateService
  ) {}

  ngOnInit() {
    this.loadSubscriptions();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['academyId'] && !changes['academyId'].isFirstChange()) {
      this.loadSubscriptions();
    }
  }

  goToSubscriptions() {
    this.router.navigate(['/academy-admin/subscriptions']);
  }

  loadSubscriptions() {
    if (!this.academyId) return;
    this.isLoadingSubscriptions = true;
    this.academyService.getSubscriptionStatus(this.academyId).subscribe({
      next: (res: any) => {
        if (res.isSuccess && res.data) {
          this.subscriptionStats = res.data;
          if (res.data.unpaidPlayers) {
            this.subscriptions = res.data.unpaidPlayers.map((sub: any) => ({
              ...sub,
              statusBadge: this.mapStatusToBadge(sub.status),
              statusBadgeRaw: this.getRawStatus(sub.status),
              graceUntil: sub.graceUntil
            }));
          } else {
            this.subscriptions = [];
          }
        } else {
           this.subscriptions = [];
        }
        this.isLoadingSubscriptions = false;
      },
      error: () => {
         this.subscriptions = [];
         this.isLoadingSubscriptions = false;
      }
    });
  }

  mapStatusToBadge(status: any): any {
    if (status === 1 || status === 'Paid') return 'ACADEMY_ADMIN.COMMS_SECTION.STATUS_PAID';
    if (status === 2 || status === 'Unpaid') return 'ACADEMY_ADMIN.COMMS_SECTION.STATUS_UNPAID';
    if (status === 3 || status === 'Grace') return 'ACADEMY_ADMIN.COMMS_SECTION.STATUS_GRACE';
    return 'ACADEMY_ADMIN.COMMS_SECTION.STATUS_UNKNOWN';
  }

  getRawStatus(status: any): string {
    if (status === 1 || status === 'Paid') return 'paid';
    if (status === 2 || status === 'Unpaid') return 'unpaid';
    if (status === 3 || status === 'Grace') return 'grace';
    return 'unknown';
  }

  onAction(event: any) {
    if (event.action === 'actions' || event.action === 'edit' || event.action === 'update') {
      const player = event.row;
      this.modalService.open({
        title: this.translate.instant('ACADEMY_ADMIN.COMMS_SECTION.UPDATE_SUBSCRIPTION_TITLE') || 'Update Subscription',
        message: this.translate.instant('ACADEMY_ADMIN.COMMS_SECTION.UPDATE_SUBSCRIPTION_MSG') || `Subscription feature is not yet fully linked in the backend. Updating for ${player.playerFullName || 'Player'} will be available soon.`,
        variant: 'info',
        confirmText: this.translate.instant('ACADEMY_ADMIN.COMMS_SECTION.ACKNOWLEDGE') || 'Acknowledge'
      }).then();
    }
  }
}
