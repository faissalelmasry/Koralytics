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
import { CompleteProfileAsPlayer, CompleteProfileAsParent, CompleteProfileBase, CompleteProfileAsCoach, CompleteProfileAsScouter } from '../../../../../core/interfaces/auth.models';

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
    StepperComponent
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

  isLoading = false;
  steps = ['Role', 'Complete Details'];
  currentStep = 0;
  selectedRole: 'Player' | 'Coach' | 'Scouter' | 'Parent' | 'AcademyAdmin' | null = null;
  userId: number = 0;
  temporaryToken: string = '';

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

  // Base missing details (usually just username since Google gives email/name)
  baseForm = this.fb.group({
    userName: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9_]{3,20}$/)]],
    phoneNumber: ['', [Validators.required, Validators.pattern(/^\+?[1-9]\d{1,14}$/)]]
  });

  playerForm = this.fb.group({
    dateOfBirth: ['', [Validators.required]],
    nationality: ['Egypt'],
    preferredFoot: ['Right', [Validators.required]],
    weakFootRating: [3, [Validators.required, Validators.min(1), Validators.max(5)]]
  });

  parentForm = this.fb.group({
    childPlayerId: [0, [Validators.required, Validators.min(1)]]
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
      this.toast.show('Please select a role to continue.', 'warning');
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

  onSubmit() {
    if (this.baseForm.invalid) {
      this.baseForm.markAllAsTouched();
      return;
    }

    if (this.selectedRole === 'Player' && this.playerForm.invalid) {
      this.playerForm.markAllAsTouched();
      return;
    }
    if (this.selectedRole === 'Parent' && this.parentForm.invalid) {
      this.parentForm.markAllAsTouched();
      return;
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
          weakFootRating: playerData.weakFootRating!
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
        const parentData = this.parentForm.getRawValue();
        const parentReq: CompleteProfileAsParent = {
          userName: baseData.userName || '',
          phoneNumber: baseData.phoneNumber || undefined,
          childPlayerId: parentData.childPlayerId!
        };
        requestObservable = this.authService.completeProfileAsParent(parentReq);
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
            this.toast.show('Profile completed successfully!', 'success');
            this.router.navigate(['/dashboard']);
          } else {
            this.toast.show(res.message || 'Profile completion failed', 'error');
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
            const errorMsg = err.error?.detail || err.error?.message || err.error?.title || 'Profile completion failed';
            this.toast.show(errorMsg, 'error');
          }
        }
      });
    }
  }

  get baseError() { return (controlName: string) => {
    const control = this.baseForm.get(controlName);
    if (control?.touched && control?.invalid) {
      if (control.errors?.['required']) return `${controlName} is required`;
      if (control.errors?.['pattern']) return `Invalid format for ${controlName}`;
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
