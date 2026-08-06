import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { CustomSelect } from '../../../../../shared/components/custom-select/custom-select';
import { CustomDatePicker } from '../../../../../shared/components/custom-date-picker/custom-date-picker';
import { PasswordStrengthComponent } from '../../../../../shared/components/password-strength/password-strength.component';
import { StepperComponent } from '../../../../../shared/components/stepper/stepper.component';
import { PhoneInputComponent } from '../../../../../shared/components/phone-input/phone-input.component';
import { RegisterPlayerRequest, RegisterCoachRequest, RegisterParentRequest, RegisterAcademyAdminRequest, RegisterScouterRequest, RegisterCoachAndAdminRequest } from '../../../../../core/interfaces/auth.models';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    CustomInputComponent,
    CustomButtonComponent,
    CustomSelect,
    CustomDatePicker,
    PasswordStrengthComponent,
    StepperComponent,
    PhoneInputComponent,
    TranslatePipe
  ],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);

  isLoading = false;
  steps = ['AUTH.REGISTER.ROLE_STEP', 'AUTH.REGISTER.BASE_DETAILS_STEP', 'AUTH.REGISTER.ROLE_SPECIFIC_STEP'];
  currentStep = 0;
  selectedRole: 'Player' | 'Coach' | 'Scouter' | 'Parent' | 'AcademyAdmin' | 'CoachAndAdmin' | null = null;

  roles = [
    { id: 'Player', name: 'AUTH.ROLES.PLAYER', icon: 'M13 10V3L4 14h7v7l9-11h-7z', desc: 'AUTH.ROLES.PLAYER_DESC' },
    { id: 'Coach', name: 'AUTH.ROLES.COACH', icon: 'M12 14l9-5-9-5-9 5 9 5z', desc: 'AUTH.ROLES.COACH_DESC' },
    { id: 'Scouter', name: 'AUTH.ROLES.SCOUTER', icon: 'M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z', desc: 'AUTH.ROLES.SCOUTER_DESC' },
    { id: 'Parent', name: 'AUTH.ROLES.PARENT', icon: 'M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2', desc: 'AUTH.ROLES.PARENT_DESC' },
    { id: 'AcademyAdmin', name: 'AUTH.ROLES.ACADEMY_ADMIN', icon: 'M3 21v-4m0 0V5a2 2 0 012-2h6.5l1 1H21l-3 6 3 6h-8.5l-1-1H5a2 2 0 00-2 2zm9-13.5V9', desc: 'AUTH.ROLES.ACADEMY_ADMIN_DESC' },
    { id: 'CoachAndAdmin', name: 'AUTH.ROLES.COACH_ADMIN', icon: 'M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10', desc: 'AUTH.ROLES.COACH_ADMIN_DESC' }
  ] as const;

  footOptions = [
    { value: 'Right', label: 'AUTH.OPTIONS.FOOT_RIGHT' },
    { value: 'Left', label: 'AUTH.OPTIONS.FOOT_LEFT' },
    { value: 'Both', label: 'AUTH.OPTIONS.FOOT_BOTH' }
  ];

  ratingOptions = [
    { value: 1, label: 'AUTH.OPTIONS.RATING_1' },
    { value: 2, label: 'AUTH.OPTIONS.RATING_2' },
    { value: 3, label: 'AUTH.OPTIONS.RATING_3' },
    { value: 4, label: 'AUTH.OPTIONS.RATING_4' },
    { value: 5, label: 'AUTH.OPTIONS.RATING_5' }
  ];

  // Base Form
  baseForm = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(50)]],
    lastName: ['', [Validators.required, Validators.maxLength(50)]],
    userName: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9_]{3,20}$/)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100), Validators.pattern(/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_])/)]],
    confirmPassword: ['', [Validators.required]],
    phoneNumber: ['', [Validators.required]]
  }, { validators: this.passwordMatchValidator });

  // Role Specific Forms
  playerForm = this.fb.group({
    dateOfBirth: ['', [Validators.required]],
    nationality: ['Egypt'],
    preferredFoot: ['Right', [Validators.required]],
    weakFootRating: [3, [Validators.required, Validators.min(1), Validators.max(5)]],
    heightCm: [null as number | null, [Validators.min(50), Validators.max(220)]],
    weightKg: [null as number | null, [Validators.min(20), Validators.max(150)]]
  });

  parentForm = this.fb.group({
    childPlayerId: [null as number | null]
  });

  passwordMatchValidator(g: AbstractControl) {
    return g.get('password')?.value === g.get('confirmPassword')?.value
      ? null : { 'mismatch': true };
  }

  selectRole(roleId: any) {
    this.selectedRole = roleId;
  }

  nextStep() {
    if (this.currentStep === 0 && !this.selectedRole) {
      this.toast.show('Please select a role to continue.', 'warning');
      return;
    }
    if (this.currentStep === 1) {
      if (this.baseForm.invalid) {
        this.baseForm.markAllAsTouched();
        return;
      }
      // Coach, Scouter, Admin, and CoachAndAdmin don't have step 3 (Profile details)
      if (['Coach', 'Scouter', 'AcademyAdmin', 'CoachAndAdmin'].includes(this.selectedRole!)) {
        this.onSubmit();
        return;
      }
    }
    this.currentStep++;
  }

  prevStep() {
    if (this.currentStep > 0) this.currentStep--;
  }

  onStepClick(index: number) {
    if (index < this.currentStep) {
      this.currentStep = index;
    }
  }

  isContinueDisabled(): boolean {
    if (this.isLoading) return true;
    if (this.currentStep === 0 && !this.selectedRole) return true;
    if (this.currentStep === 1 && this.baseForm.invalid) return true;
    return false;
  }

  isSubmitDisabled(): boolean {
    if (this.isLoading) return true;
    if (this.baseForm.invalid) return true;
    if (this.selectedRole === 'Player' && this.playerForm.invalid) return true;
    return false;
  }

  onSubmit() {
    if (this.currentStep === 2) {
      if (this.selectedRole === 'Player' && this.playerForm.invalid) {
        this.playerForm.markAllAsTouched();
        return;
      }
    }

    this.isLoading = true;
    const baseData = this.baseForm.getRawValue();

    let requestObservable;

    switch (this.selectedRole) {
      case 'Player':
        const playerData = this.playerForm.getRawValue();
        const playerReq: RegisterPlayerRequest = {
          ...baseData,
          dateOfBirth: playerData.dateOfBirth!,
          nationality: playerData.nationality!,
          preferredFoot: playerData.preferredFoot!,
          weakFootRating: playerData.weakFootRating!,
          heightCm: playerData.heightCm ? Number(playerData.heightCm) : undefined,
          weightKg: playerData.weightKg ? Number(playerData.weightKg) : undefined
        };
        requestObservable = this.authService.registerPlayer(playerReq);
        break;
      
      case 'Coach':
        requestObservable = this.authService.registerCoach(baseData as RegisterCoachRequest);
        break;
        
      case 'Scouter':
        requestObservable = this.authService.registerScouter(baseData as RegisterScouterRequest);
        break;
        
      case 'Parent':
        const parentReq: RegisterParentRequest = {
          ...baseData,
          childPlayerId: null
        };
        requestObservable = this.authService.registerParent(parentReq);
        break;
        
      case 'AcademyAdmin':
        requestObservable = this.authService.registerAcademyAdmin(baseData as RegisterAcademyAdminRequest);
        break;
        
      case 'CoachAndAdmin':
        requestObservable = this.authService.registerCoachAndAdmin(baseData as RegisterCoachAndAdminRequest);
        break;
    }

    if (requestObservable) {
      requestObservable.subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.isSuccess && res.data) {
            this.toast.show('Registration successful! Please confirm your email.', 'success');
            // Navigate to confirm email and pass userId
            this.router.navigate(['/auth/confirm-email'], { state: { userId: res.data.userId } });
          } else {
            this.toast.show(res.message || 'Registration failed', 'error');
          }
        },
        error: (err) => {
          this.isLoading = false;
          if (err.status === 0) {
            this.toast.show('Cannot reach the server. Please check your connection.', 'error');
          } else if (err.error?.errors) {
            const errorMessages = Object.values(err.error.errors).flat().join(' | ');
            this.toast.show(errorMessages, 'error');
          } else {
            const errorMsg = err.error?.detail || err.error?.message || err.error?.title || 'Registration failed. Please check your inputs.';
            this.toast.show(errorMsg, 'error');
          }
        }
      });
    }
  }

  // Getters for error messages
  get baseError() { return (controlName: string) => {
    const control = this.baseForm.get(controlName);
    if (control?.touched && control?.invalid) {
      if (control.errors?.['required']) return 'AUTH.ERRORS.REQUIRED';
      if (control.errors?.['pattern']) return 'AUTH.ERRORS.INVALID_FORMAT';
      if (control.errors?.['minlength']) return 'AUTH.ERRORS.TOO_SHORT';
      if (control.errors?.['maxlength']) return 'AUTH.ERRORS.MAX_LENGTH';
      return 'AUTH.ERRORS.INVALID';
    }
    return '';
  }}
  get playerError() { return (controlName: string) => {
    const control = this.playerForm.get(controlName);
    if (control?.touched && control?.invalid) return 'AUTH.ERRORS.REQUIRED';
    return '';
  }}
  get parentError() { return (controlName: string) => {
    const control = this.parentForm.get(controlName);
    if (control?.touched && control?.invalid) return 'AUTH.ERRORS.CHILD_PLAYER_ID_REQUIRED';
    return '';
  }}
}
