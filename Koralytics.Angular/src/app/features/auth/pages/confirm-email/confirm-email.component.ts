import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [CommonModule, RouterModule, CustomButtonComponent],
  templateUrl: './confirm-email.component.html',
  styleUrls: ['./confirm-email.component.css']
})
export class ConfirmEmailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);
  private toast = inject(ToastService);

  mode: 'awaiting' | 'confirming' | 'success' | 'error' = 'awaiting';
  userId: number = 0;
  token: string = '';
  isResending = false;

  ngOnInit() {
    // Check if we arrived from email link (query params)
    this.route.queryParams.subscribe(params => {
      if (params['userId'] && params['token']) {
        this.mode = 'confirming';
        this.userId = Number(params['userId']);
        this.token = params['token'];
        this.confirmEmail();
      } else {
        // Arrived from registration (state)
        const state = window.history.state;
        if (state && state.userId) {
          this.userId = state.userId;
          this.mode = 'awaiting';
        } else {
          // No user ID, redirect to login
          this.router.navigate(['/auth/login']);
        }
      }
    });
  }

  private confirmEmail() {
    this.authService.confirmEmail(this.userId, this.token).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          this.mode = 'success';
          setTimeout(() => {
            this.router.navigate(['/auth/login']);
          }, 3000);
        } else {
          this.mode = 'error';
          this.toast.show(res.message || 'Email confirmation failed', 'error');
        }
      },
      error: (err) => {
        this.mode = 'error';
        if (err.status === 0) {
          this.toast.show('Cannot reach the server. Please check your connection.', 'error');
        } else if (err.error?.errors) {
          const errorMessages = Object.values(err.error.errors).flat().join(' | ');
          this.toast.show(errorMessages, 'error');
        } else {
          const errorMsg = err.error?.detail || err.error?.message || err.error?.title || 'Email confirmation failed.';
          this.toast.show(errorMsg, 'error');
        }
      }
    });
  }

  resendEmail() {
    if (!this.userId) return;
    
    this.isResending = true;
    this.authService.sendEmailConfirmation(this.userId).subscribe({
      next: (res) => {
        this.isResending = false;
        if (res.isSuccess) {
          this.toast.show('Confirmation email sent successfully', 'success');
        } else {
          this.toast.show(res.message || 'Failed to resend email', 'error');
        }
      },
      error: (err) => {
        this.isResending = false;
        if (err.status === 0) {
          this.toast.show('Cannot reach the server. Please check your connection.', 'error');
        } else if (err.error?.errors) {
          const errorMessages = Object.values(err.error.errors).flat().join(' | ');
          this.toast.show(errorMessages, 'error');
        } else {
          const errorMsg = err.error?.detail || err.error?.message || err.error?.title || 'Failed to resend email.';
          this.toast.show(errorMsg, 'error');
        }
      }
    });
  }
}
