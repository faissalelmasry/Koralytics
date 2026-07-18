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
import { RegisterPlayerRequest, RegisterCoachRequest, RegisterParentRequest, RegisterAcademyAdminRequest, RegisterScouterRequest } from '../../../../../core/interfaces/auth.models';

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
    StepperComponent
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
  steps = ['Role', 'Account Details', 'Profile Details'];
  currentStep = 0;
  selectedRole: 'Player' | 'Coach' | 'Scouter' | 'Parent' | 'AcademyAdmin' | null = null;

  roles = [
    { id: 'Player', name: 'Player', icon: 'M13 10V3L4 14h7v7l9-11h-7z', desc: 'Manage your profile and track stats.' },
    { id: 'Coach', name: 'Coach', icon: 'M12 14l9-5-9-5-9 5 9 5z', desc: 'Manage teams and tactical setups.' },
    { id: 'Scouter', name: 'Scouter', icon: 'M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z', desc: 'Discover and analyze talent.' },
    { id: 'Parent', name: 'Parent', icon: 'M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2', desc: 'Track your child\'s progress.' },
    { id: 'AcademyAdmin', name: 'Academy Admin', icon: 'M3 21v-4m0 0V5a2 2 0 012-2h6.5l1 1H21l-3 6 3 6h-8.5l-1-1H5a2 2 0 00-2 2zm9-13.5V9', desc: 'Manage academy operations.' }
  ] as const;

  footOptions = [
    { value: 'Right', label: 'Right' },
    { value: 'Left', label: 'Left' },
    { value: 'Both', label: 'Both' }
  ];

  ratingOptions = [
    { value: 1, label: '1 - Poor' },
    { value: 2, label: '2 - Fair' },
    { value: 3, label: '3 - Good' },
    { value: 4, label: '4 - Very Good' },
    { value: 5, label: '5 - Excellent' }
  ];

  // Base Form
  baseForm = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(50)]],
    lastName: ['', [Validators.required, Validators.maxLength(50)]],
    userName: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9_]{3,20}$/)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100), Validators.pattern(/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_])/)]],
    confirmPassword: ['', [Validators.required]],
    phoneNumber: ['', [Validators.required, Validators.pattern(/^\+?[1-9]\d{1,14}$/)]]
  }, { validators: this.passwordMatchValidator });

  // Role Specific Forms
  playerForm = this.fb.group({
    dateOfBirth: ['', [Validators.required]],
    nationality: ['Egypt'],
    preferredFoot: ['Right', [Validators.required]],
    weakFootRating: [3, [Validators.required, Validators.min(1), Validators.max(5)]]
  });

  parentForm = this.fb.group({
    childPlayerId: [0, [Validators.required, Validators.min(1)]]
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
      // Coach, Scouter, and Admin don't have step 3 (Profile details)
      if (['Coach', 'Scouter', 'AcademyAdmin'].includes(this.selectedRole!)) {
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

  onSubmit() {
    if (this.currentStep === 2) {
      if (this.selectedRole === 'Player' && this.playerForm.invalid) {
        this.playerForm.markAllAsTouched();
        return;
      }
      if (this.selectedRole === 'Parent' && this.parentForm.invalid) {
        this.parentForm.markAllAsTouched();
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
          weakFootRating: playerData.weakFootRating!
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
        const parentData = this.parentForm.getRawValue();
        const parentReq: RegisterParentRequest = {
          ...baseData,
          childPlayerId: parentData.childPlayerId!
        };
        requestObservable = this.authService.registerParent(parentReq);
        break;
        
      case 'AcademyAdmin':
        requestObservable = this.authService.registerAcademyAdmin(baseData as RegisterAcademyAdminRequest);
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
      if (control.errors?.['required']) return `${controlName} is required`;
      if (control.errors?.['pattern']) return `Invalid format for ${controlName}`;
      if (control.errors?.['minlength']) return `Too short`;
      if (control.errors?.['maxlength']) return `Too long`;
      return `${controlName} is invalid`;
    }
    return '';
  }}
  get playerError() { return (controlName: string) => {
    const control = this.playerForm.get(controlName);
    if (control?.touched && control?.invalid) return `${controlName} is required`;
    return '';
  }}
  get parentError() { return (controlName: string) => {
    const control = this.parentForm.get(controlName);
    if (control?.touched && control?.invalid) return `Child Player ID is required`;
    return '';
  }}
}
