import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProfileService } from '../../../../core/services/profile/profile.service';
import { ToastService } from '../../../../core/services/Toast/toast';
import { ParentService, ParentPlayerJoinRequest, PlayerParent } from '../../../../core/services/parent/parent.service';
import { AcademyService } from '../../../../core/services/academy/academy.service';
import { AuthService } from '../../../../core/services/auth/auth.service';
import {
  BaseUserProfileResponse,
  PlayerProfileResponse,
  ScouterProfileResponse,
  AcademyAdminProfileResponse,
  CoachProfileResponse,
  PlayerPositionDto,
  UpdateProfileRequest
} from '../../../../core/models/profile/profile.models';
import { CustomInputComponent } from '../../../../shared/components/custom-input-component/custom-input-component';
import { CustomSelect, SelectOption } from '../../../../shared/components/custom-select/custom-select';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { ImageUpload } from '../../../../shared/components/image-upload/image-upload';
import { PhoneInputComponent } from '../../../../shared/components/phone-input/phone-input.component';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { LocalizedDatePipe } from '../../../../shared/pipes/localized-date.pipe';

@Component({
  selector: 'app-my-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CustomInputComponent,
    PhoneInputComponent,
    CustomSelect,
    CustomButtonComponent,
    LoadingSpinnerComponent,
    ScrollRevealDirective,
    ImageUpload,
    TranslatePipe,
    LocalizedDatePipe
  ],
  templateUrl: './my-profile.component.html',
  styleUrls: ['./my-profile.component.css']
})
export class MyProfileComponent implements OnInit {
  private fb = inject(FormBuilder);
  private profileService = inject(ProfileService);
  private toast = inject(ToastService);
  private parentService = inject(ParentService);
  private academyService = inject(AcademyService);
  public authService = inject(AuthService);
  private translateService = inject(TranslateService);

  profile: BaseUserProfileResponse | null = null;
  pendingParentRequests: ParentPlayerJoinRequest[] = [];
  linkedParents: PlayerParent[] = [];
  pendingAcademyRequests: any[] = [];
  isRespondingToAcademyRequest = false;
  isLoading = true;
  imageError = false;
  isEditing = false;
  isSaving = false;
  isUnlinkingParent = false;
  isUploadingImage = false;
  showImageUploadModal = false;

  readonly allPitchPositions = [
    { id: 'LW', name: 'LW', top: '22%', left: '78%' },
    { id: 'ST', name: 'ST', top: '50%', left: '80%' },
    { id: 'RW', name: 'RW', top: '78%', left: '78%' },
    { id: 'CAM', name: 'CAM', top: '50%', left: '62%' },
    { id: 'LM', name: 'LM', top: '20%', left: '50%' },
    { id: 'CM', name: 'CM', top: '50%', left: '50%' },
    { id: 'RM', name: 'RM', top: '80%', left: '50%' },
    { id: 'CDM', name: 'CDM', top: '50%', left: '38%' },
    { id: 'LB', name: 'LB', top: '20%', left: '22%' },
    { id: 'CB', name: 'CB', top: '50%', left: '22%' },
    { id: 'RB', name: 'RB', top: '80%', left: '22%' },
    { id: 'GK', name: 'GK', top: '50%', left: '8%' },
  ];

  readonly preferredFootOptions: SelectOption[] = [
    { value: 'Right', label: 'PROFILE.FOOT_RIGHT' },
    { value: 'Left', label: 'PROFILE.FOOT_LEFT' },
    { value: 'Both', label: 'PROFILE.FOOT_BOTH' }
  ];

  readonly weakFootOptions: SelectOption[] = [
    { value: 1, label: '1 / 5' },
    { value: 2, label: '2 / 5' },
    { value: 3, label: '3 / 5' },
    { value: 4, label: '4 / 5' },
    { value: 5, label: '5 / 5' }
  ];

  editPositions: PlayerPositionDto[] = [];

  form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(50)]],
    lastName: ['', [Validators.required, Validators.maxLength(50)]],
    phoneNumber: ['', [Validators.required]],
    nationality: ['', [Validators.maxLength(50)]],
    preferredFoot: [null as string | number | null],
    weakFootRating: [null as number | null],
    heightCm: [null as number | null],
    weightKg: [null as number | null],
    playStyleTag: ['']
  });

  phoneError(): string {
    const ctrl = this.form.controls.phoneNumber;
    if (!ctrl.touched && !ctrl.dirty) return '';
    if (ctrl.errors?.['required']) return this.translateService.instant('PROFILE.PHONE_REQUIRED');
    if (ctrl.errors?.['invalidPhone']) {
       const info = ctrl.errors['invalidPhone'];
       const country = this.translateService.instant(info.country);
       return this.translateService.instant('COMMON.ERRORS.INVALID_PHONE_LENGTH', {
           country: country,
           expected: Array.isArray(info.expectedLength) ? info.expectedLength.join(' or ') : info.expectedLength,
           dialCode: info.dialCode
       });
    }
    return '';
  }

  getErrorMessage(controlName: string, label: string): string {
    const ctrl = this.form.get(controlName);
    if (!ctrl || (!ctrl.touched && !ctrl.dirty)) return '';
    if (ctrl.errors?.['required']) return `${label} is required.`;
    if (ctrl.errors?.['maxlength']) return `${label} cannot exceed ${ctrl.errors['maxlength'].requiredLength} characters.`;
    return '';
  }

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading = true;
    this.profileService.getMyProfile().subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.isSuccess && res.data) {
          this.profile = res.data;
          const roles = this.authService.getCurrentUserSync()?.roles || [];
          const isPlayer = roles.includes('Player');
          const isCoach = roles.includes('Coach');
          const isAdmin = roles.includes('AcademyAdmin');
          
          if (isPlayer) {
            this.loadPendingParentRequests();
            this.loadLinkedParents();
            this.loadPendingAcademyRequests();
          } 
          if (isCoach && !isAdmin) {
            this.loadPendingAcademyRequests();
          }
        } else {
          this.toast.show(res.message || 'Failed to load profile', 'error');
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.toast.show('Error connecting to server while loading profile.', 'error');
      }
    });
  }

  loadPendingParentRequests(): void {
    this.parentService.getPlayerPendingRequests().subscribe({
      next: (res: any) => {
        const data = res.data || res;
        this.pendingParentRequests = Array.isArray(data) ? data : [];
      },
      error: (err) => console.error('Failed to load pending parent requests', err)
    });
  }

  loadLinkedParents(): void {
    this.parentService.getMyParents().subscribe({
      next: (res: any) => {
        const data = res.data || res;
        this.linkedParents = Array.isArray(data) ? data : [];
      },
      error: (err) => console.error('Failed to load linked parents', err)
    });
  }

  loadPendingAcademyRequests(): void {
    const roles = this.authService.getCurrentUserSync()?.roles || [];
    const isAdmin = roles.includes('AcademyAdmin');

    if (roles.includes('Player')) {
      this.academyService.getMyPendingPlayerRequests().subscribe({
        next: (res: any) => {
          const data = res.data || res;
          this.pendingAcademyRequests = Array.isArray(data) ? data : [];
        },
        error: (err) => console.error('Failed to load academy join requests', err)
      });
    } else if (roles.includes('Coach') && !isAdmin) {
      this.academyService.getMyPendingCoachRequests().subscribe({
        next: (res: any) => {
          const data = res.data || res;
          this.pendingAcademyRequests = Array.isArray(data) ? data : [];
        },
        error: (err) => console.error('Failed to load academy join requests', err)
      });
    }
  }

  respondToAcademyRequest(requestId: number, accept: boolean): void {
    if (this.isRespondingToAcademyRequest) return;
    const status = accept ? 2 : 3; // 2 = Accepted, 3 = Rejected
    const roles = this.authService.getCurrentUserSync()?.roles || [];
    this.isRespondingToAcademyRequest = true;

    const respond$ = roles.includes('Coach') && !roles.includes('Player')
      ? this.academyService.respondToCoachJoinRequest(requestId, { status })
      : this.academyService.respondToPlayerJoinRequest(requestId, { status });

    respond$.subscribe({
      next: () => {
        this.isRespondingToAcademyRequest = false;
        this.pendingAcademyRequests = this.pendingAcademyRequests.filter(r => r.id !== requestId);
        this.toast.show(accept ? 'Academy request accepted! Welcome aboard.' : 'Academy request declined.', 'success');
      },
      error: (err) => {
        this.isRespondingToAcademyRequest = false;
        console.error('Failed to respond to academy request', err);
        this.toast.show('Failed to respond to academy join request.', 'error');
      }
    });
  }

  respondToParentRequest(requestId: number, accept: boolean): void {
    const status = accept ? 2 : 3; // 2 = Accepted, 3 = Rejected
    this.parentService.respondToChildRequest(requestId, status).subscribe({
      next: () => {
        this.pendingParentRequests = this.pendingParentRequests.filter(r => r.id !== requestId);
        if (accept) {
          this.loadLinkedParents();
        }
        this.toast.show(accept ? 'Parent request accepted!' : 'Parent request rejected.', 'success');
      },
      error: (err) => {
        console.error('Failed to respond to parent request', err);
        this.toast.show('Failed to respond to parent request.', 'error');
      }
    });
  }

  unlinkParent(parentId: number): void {
    if (!confirm('Are you sure you want to unlink this parent/guardian from your account?')) {
      return;
    }
    this.isUnlinkingParent = true;
    this.parentService.unlinkParent(parentId).subscribe({
      next: () => {
        this.isUnlinkingParent = false;
        this.linkedParents = this.linkedParents.filter(p => p.parentId !== parentId);
        this.toast.show('Parent/Guardian unlinked successfully.', 'success');
      },
      error: (err) => {
        this.isUnlinkingParent = false;
        console.error('Failed to unlink parent', err);
        this.toast.show('Failed to unlink parent/guardian.', 'error');
      }
    });
  }

  get initials(): string {
    if (!this.profile) return '';
    return `${this.profile.firstName?.charAt(0) || ''}${this.profile.lastName?.charAt(0) || ''}`.toUpperCase();
  }

  hasRole(role: string): boolean {
    return this.authService.getCurrentUserSync()?.roles?.includes(role) || false;
  }

  get asPlayer(): PlayerProfileResponse | null {
    return this.hasRole('Player') ? (this.profile as PlayerProfileResponse) : null;
  }

  get asCoach(): CoachProfileResponse | null {
    return this.hasRole('Coach') ? (this.profile as CoachProfileResponse) : null;
  }

  get asScouter(): ScouterProfileResponse | null {
    return this.hasRole('Scouter') ? (this.profile as ScouterProfileResponse) : null;
  }

  get asAcademyAdmin(): AcademyAdminProfileResponse | null {
    return this.hasRole('AcademyAdmin') ? (this.profile as AcademyAdminProfileResponse) : null;
  }

  get footLabel(): string {
    const foot = this.asPlayer?.preferredFoot;
    if (foot === 1 || foot === 'Right' || foot === '1' || (typeof foot === 'string' && foot.toLowerCase() === 'right')) return 'PROFILE.FOOT_RIGHT';
    if (foot === 2 || foot === 'Left' || foot === '2' || (typeof foot === 'string' && foot.toLowerCase() === 'left')) return 'PROFILE.FOOT_LEFT';
    if (foot === 3 || foot === 'Both' || foot === '3' || (typeof foot === 'string' && foot.toLowerCase() === 'both')) return 'PROFILE.FOOT_BOTH';
    return 'PROFILE.NA';
  }

  get primaryPositionCode(): string {
    const primary = this.asPlayer?.positions?.find(p => p.isPrimary);
    return primary ? primary.position : 'N/A';
  }

  get secondaryPositions(): string[] {
    return (this.asPlayer?.positions || [])
      .filter(p => !p.isPrimary)
      .map(p => p.position);
  }

  startEditing(): void {
    if (!this.profile) return;
    this.isEditing = true;

    let normalizedFoot: string | null = null;
    const rawFoot = this.asPlayer?.preferredFoot;
    if (rawFoot === 1 || rawFoot === 'Right' || rawFoot === '1' || (typeof rawFoot === 'string' && rawFoot.toLowerCase() === 'right')) normalizedFoot = 'Right';
    else if (rawFoot === 2 || rawFoot === 'Left' || rawFoot === '2' || (typeof rawFoot === 'string' && rawFoot.toLowerCase() === 'left')) normalizedFoot = 'Left';
    else if (rawFoot === 3 || rawFoot === 'Both' || rawFoot === '3' || (typeof rawFoot === 'string' && rawFoot.toLowerCase() === 'both')) normalizedFoot = 'Both';

    this.form.patchValue({
      firstName: this.profile.firstName || '',
      lastName: this.profile.lastName || '',
      phoneNumber: this.profile.phoneNumber || '',
      nationality: this.asPlayer?.nationality || '',
      preferredFoot: normalizedFoot,
      weakFootRating: this.asPlayer?.weakFootRating ?? null,
      heightCm: this.asPlayer?.heightCm ?? null,
      weightKg: this.asPlayer?.weightKg ?? null,
      playStyleTag: this.asPlayer?.playStyleTag || ''
    });

    if (this.asPlayer?.positions) {
      this.editPositions = this.asPlayer.positions.map(p => ({
        position: p.position.toUpperCase(),
        isPrimary: p.isPrimary
      }));
    } else {
      this.editPositions = [];
    }
  }

  cancelEditing(): void {
    this.isEditing = false;
  }

  isPositionSelected(posId: string): boolean {
    return this.editPositions.some(p => p.position.toUpperCase() === posId.toUpperCase());
  }

  isPositionPrimary(posId: string): boolean {
    return this.editPositions.some(
      p => p.position.toUpperCase() === posId.toUpperCase() && p.isPrimary
    );
  }

  togglePosition(posId: string): void {
    const code = posId.toUpperCase();
    const idx = this.editPositions.findIndex(p => p.position.toUpperCase() === code);

    if (idx >= 0) {
      const wasPrimary = this.editPositions[idx].isPrimary;
      this.editPositions.splice(idx, 1);
      if (wasPrimary && this.editPositions.length > 0) {
        this.editPositions[0].isPrimary = true;
      }
    } else {
      if (this.editPositions.length >= 5) {
        this.toast.show('A player can have at most 5 positions.', 'warning');
        return;
      }
      this.editPositions.push({
        position: code,
        isPrimary: this.editPositions.length === 0
      });
    }
  }

  setPrimaryPosition(posId: string, event: Event): void {
    event.stopPropagation();
    const code = posId.toUpperCase();
    this.editPositions.forEach(p => {
      p.isPrimary = p.position.toUpperCase() === code;
    });
  }

  saveProfile(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      if (this.form.controls.phoneNumber.invalid) {
        this.toast.show(this.phoneError() || 'Please enter a valid phone number.', 'warning');
      } else {
        this.toast.show('Please fill in all required fields properly.', 'warning');
      }
      return;
    }

    if (this.profile?.role === 'Player') {
      if (this.editPositions.length === 0) {
        this.toast.show('Please select at least 1 position.', 'warning');
        return;
      }
      const primaryCount = this.editPositions.filter(p => p.isPrimary).length;
      if (primaryCount !== 1) {
        this.toast.show('Exactly 1 position must be marked as primary.', 'warning');
        return;
      }
    }

    this.isSaving = true;
    const val = this.form.getRawValue();

    const dto: UpdateProfileRequest = {
      firstName: val.firstName,
      lastName: val.lastName,
      phoneNumber: val.phoneNumber || null
    };

    if (this.profile?.role === 'Player') {
      dto.nationality = val.nationality || null;
      dto.preferredFoot = val.preferredFoot;
      dto.weakFootRating = val.weakFootRating;
      dto.heightCm = val.heightCm;
      dto.weightKg = val.weightKg;
      dto.playStyleTag = val.playStyleTag || null;
      dto.positions = this.editPositions;
    }

    this.profileService.updateMyProfile(dto).subscribe({
      next: (res) => {
        this.isSaving = false;
        if (res.isSuccess && res.data) {
          this.profile = res.data;
          this.isEditing = false;
          this.toast.show(res.message || 'Profile updated successfully.', 'success');
        } else {
          this.toast.show(res.message || 'Failed to update profile.', 'error');
        }
      },
      error: (err) => {
        this.isSaving = false;
        const msg = err.error?.message || 'Error occurred while saving profile.';
        this.toast.show(msg, 'error');
      }
    });
  }

  openImageUpload(): void {
    this.showImageUploadModal = true;
  }

  closeImageUpload(): void {
    this.showImageUploadModal = false;
  }

  onImageSelected(file: File): void {
    this.isUploadingImage = true;
    this.profileService.updateProfileImage(file).subscribe({
      next: (res: any) => {
        this.isUploadingImage = false;
        if (res.isSuccess) {
          const newUrl = typeof res.data === 'string' ? res.data : (res.data?.profileImageUrl || res.data);
          if (this.profile && newUrl) {
            this.profile.profileImageUrl = newUrl;
          }
          this.showImageUploadModal = false;
          this.toast.show('Profile image updated successfully.', 'success');
          this.loadProfile();
        } else {
          this.toast.show(res.message || 'Failed to upload image.', 'error');
        }
      },
      error: (err) => {
        this.isUploadingImage = false;
        const msg = err.error?.message || 'Failed to upload profile image.';
        this.toast.show(msg, 'error');
      }
    });
  }

  removeProfileImage(): void {
    if (!this.profile?.profileImageUrl) return;

    this.isUploadingImage = true;
    this.profileService.removeProfileImage().subscribe({
      next: (res) => {
        this.isUploadingImage = false;
        if (res.isSuccess) {
          if (this.profile) {
            this.profile.profileImageUrl = null;
          }
          this.showImageUploadModal = false;
          this.toast.show(res.message || 'Profile photo removed successfully.', 'success');
          this.loadProfile();
        } else {
          this.toast.show(res.message || 'Failed to remove profile photo.', 'error');
        }
      },
      error: (err) => {
        this.isUploadingImage = false;
        const msg = err.error?.message || 'Failed to remove profile photo.';
        this.toast.show(msg, 'error');
      }
    });
  }
}
