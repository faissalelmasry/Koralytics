import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { AcademyMembersComponent } from '../../components/academy-members/academy-members.component';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';

import { AcademyHeroBannerComponent } from '../../components/academy-hero-banner/academy-hero-banner';
import { AcademyBadgesSectionComponent } from '../../components/academy-badges-section/academy-badges-section';
import { AcademyAdminsSectionComponent } from '../../components/academy-admins-section/academy-admins-section';
import { AcademyCoachesSectionComponent } from '../../components/academy-coaches-section/academy-coaches-section';
import { AcademyTeamsSectionComponent } from '../../components/academy-teams-section/academy-teams-section';
import { AcademyCommSubsSectionComponent } from '../../components/academy-comm-subs-section/academy-comm-subs-section';
import { AcademyResponseDto } from '../../../../../core/interfaces/academy.models';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

@Component({
  selector: 'app-academy-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule, 
    CustomInputComponent, 
    CustomButtonComponent, 
    AcademyMembersComponent, 
    NavbarComponent, 
    AcademyHeroBannerComponent,
    AcademyBadgesSectionComponent,
    AcademyAdminsSectionComponent,
    AcademyCoachesSectionComponent,
    AcademyTeamsSectionComponent,
    AcademyCommSubsSectionComponent,
    LoadingSpinnerComponent,
    ScrollRevealDirective
  ],
  templateUrl: './academy-admin-dashboard.component.html',
  styleUrls: ['./academy-admin-dashboard.component.css']
})
export class AcademyAdminDashboardComponent implements OnInit {
  private authService = inject(AuthService);
  private academyService = inject(AcademyService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  currentUser = this.authService.getCurrentUserValue();
  
  // State
  isLoading = true;
  hasAcademy = false;
  academyDetails: AcademyResponseDto | null = null;
  totalMembersCount = 0;
  pendingRequest: any = null;

  requestForm = this.fb.nonNullable.group({
    academyName: ['', [Validators.required]],
    contactPersonName: [this.currentUser?.fullName || '', [Validators.required]],
    contactEmail: [this.currentUser?.email || '', [Validators.required, Validators.email]],
    contactPhone: ['', [Validators.required]],
    location: ['', [Validators.required]]
  });

  ngOnInit() {
    this.checkAcademyStatus();
  }

  checkAcademyStatus() {
    if (this.currentUser?.academyId) {
      this.hasAcademy = true;
      this.loadAcademyData(this.currentUser.academyId);
    } else {
      // Check if user has a pending request
      this.academyService.getMyAcademyRequests().subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.isSuccess && res.data && res.data.length > 0) {
            const request = res.data[0]; 
            if (request.status === 1) { // Approved
              this.toast.show('Your academy request was approved! Please log in again to sync your account.', 'success');
              this.authService.logoutAll().subscribe();
            } else {
              this.pendingRequest = request;
            }
          }
        },
        error: () => {
          this.isLoading = false;
        }
      });
    }
  }

  loadAcademyData(academyId: number) {
    this.academyService.getAcademyById(academyId).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.academyDetails = res.data;
        }
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
    
    this.academyService.getAcademyMembers(academyId, { pageNumber: 1, pageSize: 1 }).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.totalMembersCount = res.data.totalCount;
        }
      }
    });
  }

  onRequestAcademy() {
    if (this.requestForm.invalid) {
      this.requestForm.markAllAsTouched();
      return;
    }
    
    this.isLoading = true;
    this.academyService.requestAcademy(this.requestForm.getRawValue()).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.isSuccess) {
          this.toast.show('Academy request submitted successfully', 'success');
          this.pendingRequest = res.data;
        } else {
          this.toast.show(res.message || 'Error submitting request', 'error');
        }
      },
      error: () => {
        this.isLoading = false;
        this.toast.show('Error submitting request', 'error');
      }
    });
  }
}
