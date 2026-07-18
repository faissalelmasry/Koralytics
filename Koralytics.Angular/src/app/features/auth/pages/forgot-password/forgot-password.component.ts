import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, CustomInputComponent, CustomButtonComponent],
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.css']
})
export class ForgotPasswordComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private toast = inject(ToastService);

  isLoading = false;
  isSuccess = false;

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]]
  });

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const email = this.form.getRawValue().email;

    this.authService.forgotPassword(email).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.isSuccess) {
          this.isSuccess = true;
        } else {
          this.toast.show(res.message || 'Failed to send reset link', 'error');
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
          const errorMsg = err.error?.detail || err.error?.message || err.error?.title || 'Failed to send reset link.';
          this.toast.show(errorMsg, 'error');
        }
      }
    });
  }

  get emailError() {
    const control = this.form.get('email');
    if (control?.touched && control?.invalid) return 'Valid email is required';
    return '';
  }
}
