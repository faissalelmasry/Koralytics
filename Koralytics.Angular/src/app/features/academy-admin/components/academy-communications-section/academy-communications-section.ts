import { Component, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomSelect, SelectOption } from '../../../../../shared/components/custom-select/custom-select';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../../shared/pipes/localized-date.pipe';

@Component({
  selector: 'app-academy-communications-section',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CustomInputComponent,
    CustomSelect,
    CustomButtonComponent,
    LoadingSpinnerComponent,
    TranslatePipe,
    LocalizedDatePipe
  ],
  templateUrl: './academy-communications-section.html',
  styleUrls: ['./academy-communications-section.css']
})
export class AcademyCommunicationsSectionComponent implements OnInit, OnChanges {
  @Input() academyId!: number;

  announcements: any[] = [];
  isLoadingAnnouncements = false;
  isSending = false;

  announcementForm: FormGroup;
  get targetAudienceOptions(): SelectOption[] {
    return [
      { value: 1, label: 'ACADEMY_ADMIN.COMMS_SECTION.AUDIENCE_EVERYONE' },
      { value: 2, label: 'ACADEMY_ADMIN.COMMS_SECTION.AUDIENCE_TEAM' },
      { value: 3, label: 'ACADEMY_ADMIN.COMMS_SECTION.AUDIENCE_AGE_GROUP' },
      { value: 4, label: 'ACADEMY_ADMIN.COMMS_SECTION.AUDIENCE_ROLES' }
    ];
  }

  teams: any[] = [];
  ageGroups: any[] = [];
  targetIdOptions: SelectOption[] = [];

  constructor(
    private fb: FormBuilder,
    private academyService: AcademyService,
    private toast: ToastService,
    private router: Router,
    private translate: TranslateService
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

  goToAnnouncements() {
    this.router.navigate(['/academy-announcement', this.academyId]);
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['academyId'] && !changes['academyId'].isFirstChange()) {
      this.loadAllData();
    }
  }

  loadAllData() {
    this.loadAnnouncements();
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
        { value: 4, label: 'ACADEMY_ADMIN.COMMS_SECTION.ROLE_PLAYERS' },
        { value: 5, label: 'ACADEMY_ADMIN.COMMS_SECTION.ROLE_PARENTS' },
        { value: 6, label: 'ACADEMY_ADMIN.COMMS_SECTION.ROLE_COACHES' }
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
    if (type === 2) return this.translate.instant('ACADEMY_ADMIN.COMMS_SECTION.SELECT_TEAM') || 'Select Team';
    if (type === 3) return this.translate.instant('ACADEMY_ADMIN.COMMS_SECTION.SELECT_AGE_GROUP') || 'Select Age Group';
    if (type === 4) return this.translate.instant('ACADEMY_ADMIN.COMMS_SECTION.SELECT_ROLE') || 'Select Role';
    return this.translate.instant('ACADEMY_ADMIN.COMMS_SECTION.TARGET') || 'Target';
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
          this.toast.show(this.translate.instant('ACADEMY_ADMIN.COMMS_SECTION.ANNOUNCEMENT_SENT') || 'Announcement sent successfully!', 'success');
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

  getTargetBadgeInfo(ann: any): { name: string, cssClass: string } {
    const targetType = ann.targetType;
    const targetId = ann.targetId;

    if (targetType === 1 || targetType === 'All') {
      return { name: this.translate.instant('ACADEMY_ADMIN.COMMS_SECTION.AUDIENCE_EVERYONE') || 'Everyone', cssClass: 'everyone' };
    }
    if (targetType === 2 || targetType === 'Team') {
      const team = this.teams.find(t => t.id === targetId);
      return { name: team ? team.name : (this.translate.instant('ACADEMY_ADMIN.COMMS_SECTION.AUDIENCE_TEAM') || 'Team'), cssClass: 'team' };
    }
    if (targetType === 3 || targetType === 'AgeGroup') {
      const ag = this.ageGroups.find(ag => ag.id === targetId);
      return { name: ag ? ag.name : (this.translate.instant('ACADEMY_ADMIN.COMMS_SECTION.AUDIENCE_AGE_GROUP') || 'Age Group'), cssClass: 'age-group' };
    }
    if (targetType === 4 || targetType === 'Role') {
      if (targetId === 4) return { name: this.translate.instant('ACADEMY_ADMIN.COMMS_SECTION.ROLE_PLAYERS') || 'Players', cssClass: 'players' };
      if (targetId === 5) return { name: this.translate.instant('ACADEMY_ADMIN.COMMS_SECTION.ROLE_PARENTS') || 'Parents', cssClass: 'parents' };
      if (targetId === 6) return { name: this.translate.instant('ACADEMY_ADMIN.COMMS_SECTION.ROLE_COACHES') || 'Coaches', cssClass: 'coaches' };
      return { name: this.translate.instant('ACADEMY_ADMIN.COMMS_SECTION.AUDIENCE_ROLES') || 'Role', cssClass: 'role' };
    }
    return { name: this.translate.instant('ACADEMY_ADMIN.COMMS_SECTION.STATUS_UNKNOWN') || 'Unknown', cssClass: 'unknown' };
  }
}
