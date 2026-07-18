import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { PasswordStrengthComponent } from '../../../../../shared/components/password-strength/password-strength.component';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, CustomInputComponent, CustomButtonComponent, PasswordStrengthComponent],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.css']
})
export class ResetPasswordComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toast = inject(ToastService);

  isLoading = false;
  email = '';
  token = '';

  form = this.fb.nonNullable.group({
    newPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100), Validators.pattern(/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_])/)]],
    confirmNewPassword: ['', [Validators.required]]
  }, { validators: this.passwordMatchValidator });

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      this.email = params['email'] || '';
      this.token = params['token'] || '';

      if (!this.email || !this.token) {
        this.toast.show('Invalid password reset link', 'error');
        this.router.navigate(['/auth/login']);
      }
    });
  }

  passwordMatchValidator(g: AbstractControl) {
    return g.get('newPassword')?.value === g.get('confirmNewPassword')?.value
      ? null : { 'mismatch': true };
  }

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const formValue = this.form.getRawValue();

    this.authService.resetPassword({
      email: this.email,
      token: this.token,
      newPassword: formValue.newPassword,
      confirmNewPassword: formValue.confirmNewPassword
    }).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.isSuccess) {
          this.toast.show('Password reset successful', 'success');
          this.router.navigate(['/auth/login']);
        } else {
          this.toast.show(res.message || 'Failed to reset password', 'error');
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
          const errorMsg = err.error?.detail || err.error?.message || err.error?.title || 'Failed to reset password.';
          this.toast.show(errorMsg, 'error');
        }
      }
    });
  }

  get passwordError() {
    const control = this.form.get('newPassword');
    if (control?.touched && control?.invalid) {
      if (control.errors?.['required']) return 'New password is required';
      if (control.errors?.['pattern']) return 'Password must have upper, lower, number, and special character';
      if (control.errors?.['minlength']) return 'Minimum 8 characters';
      if (control.errors?.['maxlength']) return 'Too long';
      return 'Invalid password';
    }
    return '';
  }
}
