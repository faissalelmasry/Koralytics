import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { AcademyMembersComponent } from '../../components/academy-members/academy-members.component';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';

import { AcademyBadgesSectionComponent } from '../../components/academy-badges-section/academy-badges-section';
import { AcademyAdminsSectionComponent } from '../../components/academy-admins-section/academy-admins-section';
import { AcademyCoachesSectionComponent } from '../../components/academy-coaches-section/academy-coaches-section';
import { AcademyTeamsSectionComponent } from '../../components/academy-teams-section/academy-teams-section';
import { AcademyCommSubsSectionComponent } from '../../components/academy-comm-subs-section/academy-comm-subs-section';
import { AcademyResponseDto } from '../../../../../core/interfaces/academy.models';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

import { AcademyLocationsSectionComponent } from '../../components/academy-locations-section/academy-locations-section';
import { PhoneInputComponent } from '../../../../../shared/components/phone-input/phone-input.component';
import { ImageUpload } from '../../../../../shared/components/image-upload/image-upload';

@Component({
  selector: 'app-academy-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CustomInputComponent,
    PhoneInputComponent,
    CustomButtonComponent,
    AcademyMembersComponent,
    NavbarComponent,
    AcademyBadgesSectionComponent,
    AcademyAdminsSectionComponent,
    AcademyCoachesSectionComponent,
    AcademyTeamsSectionComponent,
    AcademyCommSubsSectionComponent,
    AcademyLocationsSectionComponent,
    LoadingSpinnerComponent,
    ScrollRevealDirective,
    ImageUpload
  ],
  templateUrl: './academy-admin-dashboard.component.html',
  styleUrls: ['./academy-admin-dashboard.component.css']
})
export class AcademyAdminDashboardComponent implements OnInit {
  private authService = inject(AuthService);
  private academyService = inject(AcademyService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);
  private router = inject(Router);

  currentUser = this.authService.getCurrentUserValue();

  // State
  isLoading = true;
  hasAcademy = false;
  academyDetails: AcademyResponseDto | null = null;
  totalMembersCount = 0;
  playersCount = 0;
  adminsCount = 0;
  coachesCount = 0;
  locationsCount = 0;
  initials = '';
  activeTab = 'all';
  pendingRequest: any = null;
  rejectedRequest: any = null;

  requestForm = this.fb.nonNullable.group({
    academyName: ['', [Validators.required]],
    contactPersonName: [this.currentUser?.fullName || '', [Validators.required]],
    contactEmail: [this.currentUser?.email || '', [Validators.required, Validators.email]],
    contactPhone: ['', [Validators.required]],
    location: ['', [Validators.required]]
  });

  myPendingAdminRequests: any[] = [];

  ngOnInit() {
    this.checkAcademyStatus();
  }

  setTab(tab: string) {
    this.activeTab = tab;
  }

  checkAcademyStatus() {
    if (this.currentUser?.academyId) {
      this.hasAcademy = true;
      this.loadAcademyData(this.currentUser.academyId);
    } else {
      // Check if user has a pending request to create an academy
      this.academyService.getMyAcademyRequests().subscribe({
        next: (res) => {
          if (res.isSuccess && res.data && res.data.length > 0) {
            const request = res.data[0];
            const status = request.requestStatus || request.status;
            if (status === 'Approved' || status === 2 || status === '2') { // Approved
              this.toast.show('Your academy request was approved! Please log in again to sync your account.', 'success');
              this.authService.logoutAll().subscribe();
            } else if (status === 'Pending' || status === 1 || status === '1') {
              this.pendingRequest = request;
              this.rejectedRequest = null;
            } else if (status === 'Rejected' || status === 3 || status === '3') {
              this.rejectedRequest = request;
              this.pendingRequest = null;
            }
          }
        }
      });

      // Check if user has pending invitations to JOIN an academy
      this.academyService.getMyPendingAdminRequests().subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.isSuccess && res.data) {
            this.myPendingAdminRequests = res.data;
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
          this.updateInitials();
          if (res.data.locationCount !== undefined) {
            this.locationsCount = res.data.locationCount;
          }
        }
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });

    this.academyService.getAcademyMembers(academyId, { pageNumber: 1, pageSize: 500 }).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.totalMembersCount = res.data.totalCount || res.data.items?.length || 0;
          this.coachesCount = res.data.items?.filter((m: any) => m.role === 'Coach').length || 0;
          this.playersCount = res.data.items?.filter((m: any) => m.role === 'Player').length || 0;
        }
      }
    });

    this.academyService.getAcademyAdmins(academyId, { pageNumber: 1, pageSize: 100 }).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.adminsCount = res.data.totalCount || res.data.items?.length || 0;
        }
      }
    });
  }

  updateInitials() {
    if (this.academyDetails?.name) {
      const words = this.academyDetails.name.split(' ');
      if (words.length > 1) {
        this.initials = (words[0].charAt(0) + words[1].charAt(0)).toUpperCase();
      } else {
        this.initials = this.academyDetails.name.substring(0, 2).toUpperCase();
      }
    }
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
          this.rejectedRequest = null;
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

  respondToAdminRequest(requestId: number, accept: boolean) {
    this.academyService.respondToAdminJoinRequest(requestId, { status: accept ? 2 : 3 }).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          this.toast.show(accept ? 'Request accepted! Please log in again to sync.' : 'Request rejected', 'success');
          if (accept) {
            this.authService.logoutAll().subscribe(() => {
              this.router.navigate(['/login']);
            });
          } else {
            this.myPendingAdminRequests = this.myPendingAdminRequests.filter(r => r.id !== requestId);
          }
        } else {
          this.toast.show(res.message || 'Error responding to request', 'error');
        }
      },
      error: () => {
        this.toast.show('Error responding to request', 'error');
      }
    });
  }

  viewPublicProfile() {
    if (this.academyDetails?.id) {
      this.router.navigate(['/academy/profile', this.academyDetails.id]);
    }
  }

  showLogoUploadModal = false;
  isUploadingLogo = false;

  openLogoUpload(): void {
    this.showLogoUploadModal = true;
  }

  closeLogoUpload(): void {
    this.showLogoUploadModal = false;
  }

  onLogoSelected(file: File): void {
    if (!this.academyDetails?.id) {
      this.toast.show('Academy ID not found.', 'error');
      return;
    }
    this.isUploadingLogo = true;
    this.academyService.updateAcademyLogo(this.academyDetails.id, file).subscribe({
      next: (res) => {
        this.isUploadingLogo = false;
        if (res.isSuccess && res.data && this.academyDetails) {
          this.academyDetails.logoUrl = res.data.logoUrl || res.data;
          this.showLogoUploadModal = false;
          this.toast.show('Academy logo updated successfully.', 'success');
        } else {
          this.toast.show(res.message || 'Failed to update logo.', 'error');
        }
      },
      error: (err) => {
        this.isUploadingLogo = false;
        const msg = err.error?.message || 'Failed to update academy logo.';
        this.toast.show(msg, 'error');
      }
    });
  }
}

