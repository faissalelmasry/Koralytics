import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { TokenStorageService } from '../../../../../core/services/auth/token-storage.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { CustomSelect } from '../../../../../shared/components/custom-select/custom-select';
import { CustomDatePicker } from '../../../../../shared/components/custom-date-picker/custom-date-picker';
import { StepperComponent } from '../../../../../shared/components/stepper/stepper.component';
import { PhoneInputComponent } from '../../../../../shared/components/phone-input/phone-input.component';
import { CompleteProfileAsPlayer, CompleteProfileAsParent, CompleteProfileBase, CompleteProfileAsCoach, CompleteProfileAsScouter } from '../../../../../core/interfaces/auth.models';
import { TranslateService, TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-complete-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    CustomInputComponent,
    CustomButtonComponent,
    CustomSelect,
    CustomDatePicker,
    StepperComponent,
    PhoneInputComponent,
    TranslatePipe
  ],
  templateUrl: './complete-profile.component.html',
  styleUrls: ['./complete-profile.component.css']
})
export class CompleteProfileComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private tokenStorage = inject(TokenStorageService);
  private router = inject(Router);
  private toast = inject(ToastService);
  private translate = inject(TranslateService);

  isLoading = false;
  steps = ['AUTH.REGISTER.ROLE_STEP', 'AUTH.COMPLETE_PROFILE.DETAILS_STEP'];
  currentStep = 0;
  selectedRole: 'Player' | 'Coach' | 'Scouter' | 'Parent' | 'AcademyAdmin' | null = null;
  userId: number = 0;
  temporaryToken: string = '';

  roles = [
    { id: 'Player', name: 'AUTH.ROLES.PLAYER', icon: 'M13 10V3L4 14h7v7l9-11h-7z', desc: 'AUTH.ROLES.PLAYER_DESC' },
    { id: 'Coach', name: 'AUTH.ROLES.COACH', icon: 'M12 14l9-5-9-5-9 5 9 5z', desc: 'AUTH.ROLES.COACH_DESC' },
    { id: 'Scouter', name: 'AUTH.ROLES.SCOUTER', icon: 'M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z', desc: 'AUTH.ROLES.SCOUTER_DESC' },
    { id: 'Parent', name: 'AUTH.ROLES.PARENT', icon: 'M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2', desc: 'AUTH.ROLES.PARENT_DESC' },
    { id: 'AcademyAdmin', name: 'AUTH.ROLES.ACADEMY_ADMIN', icon: 'M3 21v-4m0 0V5a2 2 0 012-2h6.5l1 1H21l-3 6 3 6h-8.5l-1-1H5a2 2 0 00-2 2zm9-13.5V9', desc: 'AUTH.ROLES.ACADEMY_ADMIN_DESC' }
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

  // Base missing details (usually just username since Google gives email/name)
  baseForm = this.fb.group({
    userName: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9_]{3,20}$/)]],
    phoneNumber: ['', [Validators.required]]
  });

  playerForm = this.fb.group({
    dateOfBirth: ['', [Validators.required]],
    nationality: ['Egypt'],
    preferredFoot: ['Right', [Validators.required]],
    weakFootRating: [3, [Validators.required, Validators.min(1), Validators.max(5)]],
    heightCm: [null as number | null, [Validators.min(50), Validators.max(220)]],
    weightKg: [null as number | null, [Validators.min(20), Validators.max(150)]]
  });



  ngOnInit() {
    const state = window.history.state;
    if (state && state.userId) {
      this.userId = state.userId;
      this.temporaryToken = state.temporaryToken || '';

      // Store the temporary token so the auth interceptor can send it as a
      // Bearer header on the complete-profile request (which is [Authorize]).
      // It will be replaced with the real tokens on successful completion.
      if (this.temporaryToken) {
        // Use sessionStorage (not rememberMe) since this is ephemeral
        this.tokenStorage.saveTokens(this.temporaryToken, '', false);
      }
    } else {
      // If we somehow got here without state, redirect to login
      this.tokenStorage.clear();
      this.router.navigate(['/auth/login']);
    }
  }

  selectRole(roleId: any) {
    this.selectedRole = roleId;
  }

  nextStep() {
    if (this.currentStep === 0 && !this.selectedRole) {
      this.toast.show(this.translate.instant('AUTH.MESSAGES.ROLE_REQUIRED'), 'warning');
      return;
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
    return false;
  }

  isSubmitDisabled(): boolean {
    if (this.isLoading) return true;
    if (this.baseForm.invalid) return true;
    if (this.selectedRole === 'Player' && this.playerForm.invalid) return true;
    return false;
  }

  onSubmit() {
    if (this.baseForm.invalid) {
      this.baseForm.markAllAsTouched();
      return;
    }

    if (this.currentStep === 1) {
      if (this.selectedRole === 'Player' && this.playerForm.invalid) {
        this.playerForm.markAllAsTouched();
        return;
      }
    }

    this.isLoading = true;
    const baseData = this.baseForm.getRawValue();

    let requestObservable;

    // Attach temporaryToken if the backend requires it in headers. 
    // Since our backend sets the cookie for the temporary token, it should just be sent automatically 
    // if we use withCredentials: true (which our interceptor handles).

    switch (this.selectedRole) {
      case 'Player':
        const playerData = this.playerForm.getRawValue();
        const playerReq: CompleteProfileAsPlayer = {
          userName: baseData.userName || '',
          phoneNumber: baseData.phoneNumber || undefined,
          dateOfBirth: playerData.dateOfBirth!,
          nationality: playerData.nationality!,
          preferredFoot: playerData.preferredFoot!,
          weakFootRating: playerData.weakFootRating!,
          heightCm: playerData.heightCm || undefined,
          weightKg: playerData.weightKg || undefined
        };
        requestObservable = this.authService.completeProfileAsPlayer(playerReq);
        break;
      
      case 'Coach':
        requestObservable = this.authService.completeProfileAsCoach({ userName: baseData.userName || '', phoneNumber: baseData.phoneNumber || undefined });
        break;
        
      case 'Scouter':
        requestObservable = this.authService.completeProfileAsScouter({ userName: baseData.userName || '', phoneNumber: baseData.phoneNumber || undefined });
        break;
        
      case 'Parent':
        requestObservable = this.authService.completeProfileAsParent({ 
          userName: baseData.userName || '', 
          phoneNumber: baseData.phoneNumber || undefined, 
          childPlayerId: null 
        });
        break;
        
      case 'AcademyAdmin':
        requestObservable = this.authService.completeProfileAsAcademyAdmin({ userName: baseData.userName || '', phoneNumber: baseData.phoneNumber || undefined });
        break;
    }

    if (requestObservable) {
      requestObservable.subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.isSuccess) {
            this.toast.show(this.translate.instant('AUTH.MESSAGES.PROFILE_COMPLETION_SUCCESS'), 'success');
            this.router.navigate([this.authService.getRoleDashboardRoute()]);
          } else {
            this.toast.show(res.message || this.translate.instant('AUTH.MESSAGES.PROFILE_COMPLETION_FAILED'), 'error');
          }
        },
        error: (err) => {
          this.isLoading = false;
          if (err.status === 0) {
            this.toast.show(this.translate.instant('AUTH.MESSAGES.NETWORK_ERROR'), 'error');
          } else if (err.error?.errors) {
            const errorMessages = Object.values(err.error.errors).flat().join(' | ');
            this.toast.show(errorMessages, 'error');
          } else {
            const errorMsg = err.error?.detail || err.error?.message || err.error?.title || this.translate.instant('AUTH.MESSAGES.PROFILE_COMPLETION_FAILED');
            this.toast.show(errorMsg, 'error');
          }
        }
      });
    }
  }

  get baseError() { return (controlName: string) => {
    const control = this.baseForm.get(controlName);
    if (control?.touched && control?.invalid) {
      if (control.errors?.['required']) return 'AUTH.ERRORS.REQUIRED';
      if (control.errors?.['pattern']) return 'AUTH.ERRORS.INVALID_FORMAT';
      return 'AUTH.ERRORS.INVALID';
    }
    return '';
  }}
  get playerError() { return (controlName: string) => {
    const control = this.playerForm.get(controlName);
    if (control?.touched && control?.invalid) return 'AUTH.ERRORS.REQUIRED';
    return '';
  }}

}
