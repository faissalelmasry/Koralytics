import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { SystemAdminService } from '../../../../../core/services/system-admin/system-admin.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { CustomSelect, SelectOption } from '../../../../../shared/components/custom-select/custom-select';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';
import { LocalizedDatePipe } from '../../../../../shared/pipes/localized-date.pipe';

@Component({
  selector: 'app-manage-badges-section',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    CustomInputComponent,
    CustomButtonComponent,
    LoadingSpinnerComponent,
    CustomSelect,
    ConfirmDialogComponent,
    TranslatePipe,
    ScrollRevealDirective,
    LocalizedDatePipe
  ],
  templateUrl: './manage-badges-section.html',
  styleUrls: ['./manage-badges-section.css']
})
export class ManageBadgesSectionComponent implements OnInit {
  private systemAdminService = inject(SystemAdminService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);
  private translate = inject(TranslateService);

  academies: any[] = [];
  selectedAcademyId: number | null = null;
  badges: any[] = [];
  isLoadingAcademies = true;
  isLoadingBadges = false;

  get academyOptions(): SelectOption[] {
    return this.academies.map(a => ({ label: a.name, value: a.id }));
  }

  showAwardModal = false;
  awardForm!: FormGroup;
  isAwarding = false;

  isConfirmDialogOpen = false;
  badgeToRevoke: any = null;
  confirmDialogTitle = '';
  confirmDialogMessage = '';

  get badgeTypes(): SelectOption[] {
    return [
      { value: 'Verified', label: 'SYSTEM_ADMIN.MANAGE_BADGES.BADGE_VERIFIED' },
      { value: 'TopPerformer', label: 'SYSTEM_ADMIN.MANAGE_BADGES.BADGE_TOP_PERFORMER' },
      { value: 'Premium', label: 'SYSTEM_ADMIN.MANAGE_BADGES.BADGE_PREMIUM' }
    ];
  }

  ngOnInit() {
    this.initForm();
    this.loadAcademies();
  }

  getBadgeLabel(type: string): string {
    if (type === 'Verified' || type === 'Verified Academy') return this.translate.instant('SYSTEM_ADMIN.MANAGE_BADGES.BADGE_VERIFIED');
    if (type === 'TopPerformer' || type === 'Top Performer') return this.translate.instant('SYSTEM_ADMIN.MANAGE_BADGES.BADGE_TOP_PERFORMER');
    if (type === 'Premium' || type === 'Premium Partner') return this.translate.instant('SYSTEM_ADMIN.MANAGE_BADGES.BADGE_PREMIUM');
    return type;
  }

  getBadgeDescriptionKey(type: string): string {
    if (type === 'Verified' || type === 'Verified Academy') return 'SYSTEM_ADMIN.MANAGE_BADGES.DESC_VERIFIED';
    if (type === 'TopPerformer' || type === 'Top Performer') return 'SYSTEM_ADMIN.MANAGE_BADGES.DESC_TOP_PERFORMER';
    if (type === 'Premium' || type === 'Premium Partner') return 'SYSTEM_ADMIN.MANAGE_BADGES.DESC_PREMIUM';
    return 'SYSTEM_ADMIN.MANAGE_BADGES.DEFAULT_DESC';
  }

  getBadgeDescription(badge: any): string {
    const oldDefault = 'Awarded for high platform engagement and verified standards.';
    if (badge.description && badge.description.trim() !== '' && badge.description !== oldDefault) {
      return badge.description;
    }
    return this.translate.instant(this.getBadgeDescriptionKey(badge.badgeType));
  }

  private initForm() {
    this.awardForm = this.fb.group({
      academyId: [null, [Validators.required]],
      badgeType: ['Verified', [Validators.required]],
      description: ['']
    });
  }

  loadAcademies() {
    this.isLoadingAcademies = true;
    this.systemAdminService.getAllAcademies({ pageSize: 100 }).subscribe({
      next: (res) => {
        this.isLoadingAcademies = false;
        if (res.isSuccess && res.data) {
          const data: any = res.data;
          this.academies = Array.isArray(data) ? data : (data.items || data.academies || data.data || []);
          if (this.academies.length > 0 && !this.selectedAcademyId) {
            this.selectedAcademyId = this.academies[0].id;
            this.loadBadges();
          }
        }
      },
      error: () => {
        this.isLoadingAcademies = false;
        this.toast.show('Failed to load academies', 'error');
      }
    });
  }

  onAcademyChange() {
    this.loadBadges();
  }

  loadBadges() {
    if (!this.selectedAcademyId) return;
    this.isLoadingBadges = true;
    this.systemAdminService.getAcademyBadges(this.selectedAcademyId).subscribe({
      next: (res) => {
        this.isLoadingBadges = false;
        if (res.isSuccess && res.data) {
          const data: any = res.data;
          this.badges = Array.isArray(data) ? data : (data.badges || data.items || data.data || []);
        } else {
          this.badges = [];
        }
      },
      error: () => {
        this.isLoadingBadges = false;
        this.badges = [];
      }
    });
  }

  openAwardModal() {
    this.showAwardModal = true;
    if (this.selectedAcademyId) {
      this.awardForm.patchValue({ academyId: this.selectedAcademyId });
    }
  }

  closeAwardModal() {
    this.showAwardModal = false;
  }

  onAwardSubmit() {
    if (this.awardForm.invalid) return;
    this.isAwarding = true;
    const formVal = this.awardForm.value;

    this.systemAdminService.createBadge(formVal.academyId, {
      badgeType: formVal.badgeType,
      description: formVal.description,
      awardedAt: new Date().toISOString()
    }).subscribe({
      next: (res) => {
        this.isAwarding = false;
        if (res.isSuccess || res.statusCode === 200 || res.statusCode === 201) {
          this.toast.show('Badge awarded successfully', 'success');
          this.closeAwardModal();
          this.loadBadges();
        } else {
          this.toast.show(res.message || 'Error awarding badge', 'error');
        }
      },
      error: (err) => {
        this.isAwarding = false;
        this.toast.show(err.error?.message || 'Error awarding badge', 'error');
      }
    });
  }

  deleteBadge(badge: any) {
    if (!this.selectedAcademyId) return;
    this.badgeToRevoke = badge;
    this.confirmDialogTitle = this.translate.instant('SYSTEM_ADMIN.MANAGE_BADGES.MODAL_REVOKE_TITLE');
    const translatedBadgeName = this.getBadgeLabel(badge.badgeType);
    this.confirmDialogMessage = this.translate.instant('SYSTEM_ADMIN.MANAGE_BADGES.MODAL_REVOKE_MSG', { badge: translatedBadgeName });
    this.isConfirmDialogOpen = true;
  }

  onConfirmBadgeRevoke() {
    if (!this.selectedAcademyId || !this.badgeToRevoke) {
      this.isConfirmDialogOpen = false;
      return;
    }
    const badge = this.badgeToRevoke;
    this.isConfirmDialogOpen = false;
    this.badgeToRevoke = null;

    this.systemAdminService.deleteBadge(this.selectedAcademyId, badge.id).subscribe({
      next: (res) => {
        if (res.isSuccess || res.statusCode === 200) {
          this.toast.show('Badge revoked', 'success');
          this.loadBadges();
        } else {
          this.toast.show(res.message || 'Error revoking badge', 'error');
        }
      },
      error: () => {
        this.toast.show('Error revoking badge', 'error');
      }
    });
  }
}
