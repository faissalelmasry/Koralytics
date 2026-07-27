import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { SystemAdminService } from '../../../../../core/services/system-admin/system-admin.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { CustomSelect, SelectOption } from '../../../../../shared/components/custom-select/custom-select';

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
    CustomSelect
  ],
  templateUrl: './manage-badges-section.html',
  styleUrls: ['./manage-badges-section.css']
})
export class ManageBadgesSectionComponent implements OnInit {
  private systemAdminService = inject(SystemAdminService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

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

  badgeTypes: SelectOption[] = [
    { value: 'Verified', label: 'Verified Academy' },
    { value: 'TopPerformer', label: 'Top Performer' },
    { value: 'Premium', label: 'Premium Partner' }
  ];

  ngOnInit() {
    this.initForm();
    this.loadAcademies();
  }

  getBadgeLabel(type: string): string {
    if (type === 'Verified' || type === 'Verified Academy') return 'Verified Academy';
    if (type === 'TopPerformer' || type === 'Top Performer') return 'Top Performer';
    if (type === 'Premium' || type === 'Premium Partner') return 'Premium Partner';
    return type;
  }

  private initForm() {
    this.awardForm = this.fb.group({
      academyId: [null, [Validators.required]],
      badgeType: ['Verified', [Validators.required]],
      description: ['Awarded for high platform engagement and verified standards.']
    });
  }

  loadAcademies() {
    this.isLoadingAcademies = true;
    this.systemAdminService.getAllAcademies({ pageSize: 100 }).subscribe({
      next: (res) => {
        this.isLoadingAcademies = false;
        if (res.isSuccess && res.data) {
          const data: any = res.data;
          this.academies = Array.isArray(data) ? data : (data.academies || data.items || data.data || []);
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
    const { academyId, badgeType } = this.awardForm.value;
    const payload = {
      badgeType: badgeType,
      awardedAt: new Date().toISOString()
    };

    this.systemAdminService.createBadge(academyId, payload).subscribe({
      next: (res) => {
        this.isAwarding = false;
        if (res.isSuccess || res.statusCode === 201 || res.statusCode === 200) {
          this.toast.show('Badge awarded successfully!', 'success');
          this.closeAwardModal();
          this.selectedAcademyId = academyId;
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

    if (!confirm(`Are you sure you want to revoke the "${badge.badgeType}" badge?`)) {
      return;
    }

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
