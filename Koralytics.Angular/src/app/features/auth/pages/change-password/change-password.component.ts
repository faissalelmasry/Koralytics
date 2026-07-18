import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { PasswordStrengthComponent } from '../../../../../shared/components/password-strength/password-strength.component';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomInputComponent, CustomButtonComponent, PasswordStrengthComponent],
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.css']
})
export class ChangePasswordComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);

  isLoading = false;

  form = this.fb.nonNullable.group({
    oldPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100), Validators.pattern(/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_])/)]],
    confirmPassword: ['', [Validators.required]]
  }, { validators: this.passwordMatchValidator });

  passwordMatchValidator(g: AbstractControl) {
    return g.get('newPassword')?.value === g.get('confirmPassword')?.value
      ? null : { 'mismatch': true };
  }

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const formValue = this.form.getRawValue();

    this.authService.changePassword({
      oldPassword: formValue.oldPassword,
      newPassword: formValue.newPassword,
      confirmPassword: formValue.confirmPassword
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.toast.show('Password changed successfully. Please log in again.', 'success');
        this.router.navigate(['/auth/login']);
      },
      error: (err) => {
        this.isLoading = false;
        if (err.status === 0) {
          this.toast.show('Cannot reach the server. Please check your connection.', 'error');
        } else if (err.error?.errors) {
          const errorMessages = Object.values(err.error.errors).flat().join(' | ');
          this.toast.show(errorMessages, 'error');
        } else {
          const errorMsg = err.error?.detail || err.error?.message || err.error?.title || 'Failed to change password.';
          this.toast.show(errorMsg, 'error');
        }
      }
    });
  }

  get newPasswordError() {
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
