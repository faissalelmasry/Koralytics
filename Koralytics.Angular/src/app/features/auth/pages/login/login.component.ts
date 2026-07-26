import { Component, inject, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { GoogleAuthService } from '../../../../../core/services/auth/google-auth.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { CustomCheckboxComponent } from '../../../../../shared/components/custom-checkbox/custom-checkbox.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule, 
    RouterModule, 
    CustomInputComponent, 
    CustomButtonComponent,
    CustomCheckboxComponent
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements AfterViewInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private googleAuth = inject(GoogleAuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private toast = inject(ToastService);

  isLoading = false;
  isGoogleLoading = false;

  loginForm = this.fb.nonNullable.group({
    emailOrUserName: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    rememberMe: [false]
  });

  onSubmit() {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const formValue = this.loginForm.getRawValue();

    this.authService.login(
      { emailOrUserName: formValue.emailOrUserName, password: formValue.password },
      formValue.rememberMe
    ).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.isSuccess) {
          this.checkEmailConfirmationAndRedirect(res.data?.userId || 0);
        } else {
          this.toast.show(res.message || 'Login failed', 'error');
        }
      },
      error: (err) => {
        this.isLoading = false;
        if (err.status === 401 || err.status === 400) {
          if (err.error?.errors) {
            const errorMessages = Object.values(err.error.errors).flat().join(' | ');
            this.toast.show(errorMessages, 'error');
          } else {
            this.toast.show(err.error?.message || err.error?.detail || err.error?.title || 'Invalid email or password', 'error');
          }
        } else if (err.status === 0) {
          this.toast.show('Cannot reach the server. Please check your connection.', 'error');
        } else {
          this.toast.show(err.error?.message || err.error?.detail || err.error?.title || 'A server error occurred. Please try again later.', 'error');
        }
      }
    });
  }

  @ViewChild('googleBtnContainer') googleBtnContainer!: ElementRef;

  ngAfterViewInit() {
    this.googleAuth.renderButton(this.googleBtnContainer.nativeElement, (idToken: string) => {
      this.handleGoogleLogin(idToken);
    });
  }

  handleGoogleLogin(idToken: string) {
    this.isGoogleLoading = true;
    const rememberMe = this.loginForm.get('rememberMe')?.value || false;
    
    this.authService.oauthLogin({ provider: 'Google', idToken }, rememberMe).subscribe({
      next: (res) => {
        this.isGoogleLoading = false;
        if (res.isSuccess && res.data) {
          if (res.data.requiresProfileCompletion) {
            this.router.navigate(['/auth/complete-profile'], { 
              state: { 
                userId: res.data.userId, 
                temporaryToken: res.data.temporaryToken 
              } 
            });
          } else {
            this.router.navigate(['/dashboard']);
          }
        } else {
          this.toast.show(res.message || 'Google login failed', 'error');
        }
      },
      error: (err) => {
        this.isGoogleLoading = false;
        if (err.status === 0) {
          this.toast.show('Cannot reach the server. Please check your connection.', 'error');
        } else if (err.error?.errors) {
          const errorMessages = Object.values(err.error.errors).flat().join(' | ');
          this.toast.show(errorMessages, 'error');
        } else {
          this.toast.show(err.error?.message || err.error?.detail || err.error?.title || 'Google login failed', 'error');
        }
      }
    });
  }

  private checkEmailConfirmationAndRedirect(userId: number) {
    // If we have a user, check if their email is confirmed (backend might enforce this, but just in case)
    this.authService.isEmailConfirmed(userId).subscribe({
      next: (res) => {
        if (res.data?.isConfirmed) {
          const user = this.authService.getCurrentUserValue();
          let defaultReturnUrl = '/dashboard';
          
          if (user?.roles?.includes('AcademyAdmin')) {
            defaultReturnUrl = '/academy-admin/dashboard';
          }
          
          const returnUrl = this.route.snapshot.queryParams['returnUrl'] || defaultReturnUrl;
          this.router.navigateByUrl(returnUrl);
        } else {
          this.toast.show('Please confirm your email address', 'warning');
          this.router.navigate(['/auth/confirm-email'], { state: { userId } });
        }
      },
      error: () => {
        // Fallback
        this.router.navigate(['/dashboard']);
      }
    });
  }

  get emailError() {
    const control = this.loginForm.get('emailOrUserName');
    if (control?.touched && control?.invalid) {
      return 'Email or username is required';
    }
    return '';
  }

  get passwordError() {
    const control = this.loginForm.get('password');
    if (control?.touched && control?.invalid) {
      if (control.errors?.['required']) return 'Password is required';
      if (control.errors?.['minlength']) return 'Minimum 6 characters';
    }
    return '';
  }
}
