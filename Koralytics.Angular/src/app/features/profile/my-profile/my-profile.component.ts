import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProfileService } from '../../../../core/services/profile/profile.service';
import { ToastService } from '../../../../core/services/Toast/toast';
import {
  BaseUserProfileResponse,
  PlayerProfileResponse,
  ScouterProfileResponse,
  AcademyAdminProfileResponse,
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
    ImageUpload
  ],
  templateUrl: './my-profile.component.html',
  styleUrls: ['./my-profile.component.css']
})
export class MyProfileComponent implements OnInit {
  private fb = inject(FormBuilder);
  private profileService = inject(ProfileService);
  private toast = inject(ToastService);

  profile: BaseUserProfileResponse | null = null;
  isLoading = true;
  isEditing = false;
  isSaving = false;
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
    { value: 'Right', label: 'Right' },
    { value: 'Left', label: 'Left' },
    { value: 'Both', label: 'Both' }
  ];

  readonly weakFootOptions: SelectOption[] = [
    { value: 1, label: '1 ★' },
    { value: 2, label: '2 ★' },
    { value: 3, label: '3 ★' },
    { value: 4, label: '4 ★' },
    { value: 5, label: '5 ★' }
  ];

  editPositions: PlayerPositionDto[] = [];

  form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(50)]],
    lastName: ['', [Validators.required, Validators.maxLength(50)]],
    phoneNumber: ['', [Validators.required]],
    nationality: ['', [Validators.maxLength(50)]],
    preferredFoot: [null as string | number | null],
    weakFootRating: [null as number | null],
    playStyleTag: [''],
    archetypePlayerName: [''],
    archetypeText: ['']
  });

  phoneError(): string {
    const ctrl = this.form.controls.phoneNumber;
    if (!ctrl.touched && !ctrl.dirty) return '';
    if (ctrl.errors?.['required']) return 'Phone number is required.';
    if (ctrl.errors?.['invalidPhone']) return ctrl.errors['invalidPhone'].message;
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

  get initials(): string {
    if (!this.profile) return 'U';
    const f = (this.profile.firstName || '')[0] || '';
    const l = (this.profile.lastName || '')[0] || '';
    return (f + l).toUpperCase() || 'U';
  }

  get asPlayer(): PlayerProfileResponse | null {
    return this.profile?.role === 'Player' ? (this.profile as PlayerProfileResponse) : null;
  }

  get asScouter(): ScouterProfileResponse | null {
    return this.profile?.role === 'Scouter' ? (this.profile as ScouterProfileResponse) : null;
  }

  get asAcademyAdmin(): AcademyAdminProfileResponse | null {
    return this.profile?.role === 'AcademyAdmin' ? (this.profile as AcademyAdminProfileResponse) : null;
  }

  get footLabel(): string {
    const foot = this.asPlayer?.preferredFoot;
    if (foot === 1 || foot === 'Right' || foot === '1' || (typeof foot === 'string' && foot.toLowerCase() === 'right')) return 'Right';
    if (foot === 2 || foot === 'Left' || foot === '2' || (typeof foot === 'string' && foot.toLowerCase() === 'left')) return 'Left';
    if (foot === 3 || foot === 'Both' || foot === '3' || (typeof foot === 'string' && foot.toLowerCase() === 'both')) return 'Both';
    return 'N/A';
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
      playStyleTag: this.asPlayer?.playStyleTag || '',
      archetypePlayerName: this.asPlayer?.archetypePlayerName || '',
      archetypeText: this.asPlayer?.archetypeText || ''
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
      dto.playStyleTag = val.playStyleTag || null;
      dto.archetypePlayerName = val.archetypePlayerName || null;
      dto.archetypeText = val.archetypeText || null;
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
      next: (res) => {
        this.isUploadingImage = false;
        if (res.isSuccess && res.data && this.profile) {
          this.profile.profileImageUrl = res.data;
          this.showImageUploadModal = false;
          this.toast.show('Profile image updated successfully.', 'success');
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
}
