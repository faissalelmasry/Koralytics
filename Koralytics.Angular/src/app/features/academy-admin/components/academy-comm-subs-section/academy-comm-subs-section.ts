import { Component, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { ModalService } from '../../../../../core/services/Modal/modal';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomSelect, SelectOption } from '../../../../../shared/components/custom-select/custom-select';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { DataTable, TableColumn } from '../../../../../shared/components/data-table/data-table';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';

@Component({
  selector: 'app-academy-comm-subs-section',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CustomInputComponent,
    CustomSelect,
    CustomButtonComponent,
    DataTable,
    LoadingSpinnerComponent
  ],
  templateUrl: './academy-comm-subs-section.html',
  styleUrls: ['./academy-comm-subs-section.css']
})
export class AcademyCommSubsSectionComponent implements OnInit, OnChanges {
  @Input() academyId!: number;

  announcements: any[] = [];
  subscriptions: any[] = [];
  subscriptionStats: any = null;
  isLoadingAnnouncements = false;
  isLoadingSubscriptions = false;
  isSending = false;

  announcementForm: FormGroup;
  targetAudienceOptions: SelectOption[] = [
    { value: 1, label: 'Everyone' },
    { value: 2, label: 'Specific Team' },
    { value: 3, label: 'Specific Age Group' },
    { value: 4, label: 'Specific Roles' }
  ];

  subColumns: TableColumn[] = [
    { key: 'playerFullName', label: 'player name', type: 'text' },
    { key: 'statusBadge', label: 'status', type: 'badge' },
    { key: 'graceUntilFormatted', label: 'grace until', type: 'text' },
    { key: 'actions', label: 'update', type: 'action' }
  ];

  teams: any[] = [];
  ageGroups: any[] = [];
  targetIdOptions: SelectOption[] = [];

  constructor(
    private fb: FormBuilder,
    private academyService: AcademyService,
    private toast: ToastService,
    private modalService: ModalService
  ) {
    this.announcementForm = this.fb.group({
      targetType: [1, Validators.required],
      targetId: [0], // Default 0 for Everyone
      title: ['', Validators.required],
      message: ['', Validators.required]
    });

    this.announcementForm.get('targetType')?.valueChanges.subscribe(val => {
      this.updateTargetIdOptions(val);
    });
  }

  ngOnInit() {
    this.loadAllData();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['academyId'] && !changes['academyId'].isFirstChange()) {
      this.loadAllData();
    }
  }

  loadAllData() {
    this.loadAnnouncements();
    this.loadSubscriptions();
    this.loadTeams();
    this.loadAgeGroups();
  }

  loadTeams() {
    if (!this.academyId) return;
    this.academyService.getTeams(this.academyId).subscribe({
      next: (res: any) => {
        if (res.isSuccess && res.data) {
          this.teams = res.data;
          this.updateTargetIdOptions(this.announcementForm.get('targetType')?.value);
        }
      }
    });
  }

  loadAgeGroups() {
    if (!this.academyId) return;
    this.academyService.getAgeGroups(this.academyId).subscribe({
      next: (res: any) => {
        if (res.isSuccess && res.data) {
          this.ageGroups = res.data;
          this.updateTargetIdOptions(this.announcementForm.get('targetType')?.value);
        }
      }
    });
  }

  updateTargetIdOptions(targetType: number) {
    // 1=Everyone, 2=Team, 3=AgeGroup, 4=Role
    if (targetType === 2) {
      this.targetIdOptions = this.teams.map(t => ({ value: t.id, label: t.name }));
    } else if (targetType === 3) {
      this.targetIdOptions = this.ageGroups.map(ag => ({ value: ag.id, label: ag.name }));
    } else if (targetType === 4) {
      this.targetIdOptions = [
        { value: 4, label: 'Players' },
        { value: 5, label: 'Parents' },
        { value: 6, label: 'Coaches' }
      ];
    } else {
      this.targetIdOptions = [];
      this.announcementForm.get('targetId')?.setValue(0);
    }
    
    // Auto-select first option if available and current value is invalid
    if (this.targetIdOptions.length > 0) {
      const currentVal = this.announcementForm.get('targetId')?.value;
      if (!this.targetIdOptions.find(o => o.value === currentVal)) {
        this.announcementForm.get('targetId')?.setValue(this.targetIdOptions[0].value);
      }
    }
  }

  getTargetIdLabel(): string {
    const type = this.announcementForm.get('targetType')?.value;
    if (type === 2) return 'Select Team';
    if (type === 3) return 'Select Age Group';
    if (type === 4) return 'Select Role';
    return 'Target';
  }

  loadAnnouncements() {
    if (!this.academyId) return;
    this.isLoadingAnnouncements = true;
    this.academyService.getAnnouncements(this.academyId, { pageNumber: 1, pageSize: 50 }).subscribe({
      next: (res: any) => {
        if (res.isSuccess && res.data) {
          this.announcements = res.data.items;
        }
        this.isLoadingAnnouncements = false;
      },
      error: () => this.isLoadingAnnouncements = false
    });
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
              graceUntilFormatted: sub.graceUntil ? new Date(sub.graceUntil).toLocaleDateString() : 'N/A'
            }));
          } else {
            this.subscriptions = [];
          }
        } else {
           // Fallback to empty if not implemented
           this.subscriptions = [];
        }
        this.isLoadingSubscriptions = false;
      },
      error: () => {
         // Fallback if backend returns 404
         this.subscriptions = [];
         this.isLoadingSubscriptions = false;
      }
    });
  }

  mapStatusToBadge(status: any): any {
    if (status === 1 || status === 'Paid') return { text: 'Paid', type: 'success' };
    if (status === 2 || status === 'Unpaid') return { text: 'Unpaid', type: 'danger' };
    if (status === 3 || status === 'Grace') return { text: 'Grace Period', type: 'warning' };
    return { text: 'Unknown', type: 'neutral' };
  }

  onSendAnnouncement() {
    if (this.announcementForm.invalid || !this.academyId) return;

    this.isSending = true;
    const dto = {
      targetType: this.announcementForm.value.targetType,
      targetId: this.announcementForm.value.targetId,
      title: this.announcementForm.value.title,
      body: this.announcementForm.value.message 
    };

    this.academyService.sendAnnouncement(this.academyId, dto).subscribe({
      next: (res: any) => {
        if (res.isSuccess) {
          this.toast.show('Announcement sent successfully!', 'success');
          this.announcementForm.reset({ targetType: 1, targetId: 0 });
          this.loadAnnouncements();
        } else {
          this.toast.show(res.message || 'Failed to send announcement', 'error');
        }
        this.isSending = false;
      },
      error: (err: any) => {
        this.toast.show('Error sending announcement', 'error');
        this.isSending = false;
      }
    });
  }

  onAction(event: any) {
    if (event.action === 'actions' || event.action === 'edit' || event.action === 'update') {
      const player = event.row;
      this.modalService.open({
        title: 'Update Subscription',
        message: `Subscription feature is not yet fully linked in the backend. Updating for ${player.playerFullName || 'Player'} will be available soon.`,
        variant: 'info',
        confirmText: 'Acknowledge'
      }).then();
    }
  }

  getTargetTypeName(type: any): string {
    if (type === 1 || type === 'All') return 'Everyone';
    if (type === 2 || type === 'Team') return 'Team';
    if (type === 3 || type === 'AgeGroup') return 'Age Group';
    if (type === 4 || type === 'Role') return 'Role';
    return 'Unknown';
  }
}
