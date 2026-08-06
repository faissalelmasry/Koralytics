import { Component, OnInit, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { SystemAdminService } from '../../../../../core/services/system-admin/system-admin.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { TranslatePipe } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../../shared/pipes/localized-date.pipe';
import { CustomDatePicker } from '../../../../../shared/components/custom-date-picker/custom-date-picker';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

@Component({
  selector: 'app-pending-requests-section',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CustomInputComponent,
    CustomButtonComponent,
    LoadingSpinnerComponent,
    TranslatePipe,
    LocalizedDatePipe,
    CustomDatePicker,
    ScrollRevealDirective
  ],
  templateUrl: './pending-requests-section.html',
  styleUrls: ['./pending-requests-section.css']
})
export class PendingRequestsSectionComponent implements OnInit {
  private systemAdminService = inject(SystemAdminService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  @Output() requestsChanged = new EventEmitter<void>();

  pendingRequests: any[] = [];
  isLoading = true;

  // Approve Modal state
  selectedRequestForApprove: any = null;
  approveForm!: FormGroup;
  isApproving = false;

  // Reject Modal state
  selectedRequestForReject: any = null;
  rejectForm!: FormGroup;
  isRejecting = false;

  ngOnInit() {
    this.initForms();
    this.loadPendingRequests();
  }

  private initForms() {
    this.approveForm = this.fb.group({
      name: ['', [Validators.required]],
      adminUserId: [null, [Validators.required]],
      foundedDate: [new Date().toISOString().substring(0, 10), [Validators.required]],
      logoUrl: ['images/logo/primary/logo-primary-dark.png'],
      primaryColor: ['#3b82f6'],
      secondaryColor: ['#8b5cf6'],
      requestId: [null, [Validators.required]]
    });

    this.rejectForm = this.fb.group({
      reason: ['', [Validators.required, Validators.minLength(3)]]
    });
  }

  loadPendingRequests() {
    this.isLoading = true;
    this.systemAdminService.getPendingAcademyRequests().subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.isSuccess && res.data) {
          const data: any = res.data;
          const list = Array.isArray(data) ? data : (data.requests || data.items || data.academies || data.data || []);
          this.pendingRequests = list.map((req: any) => ({
            ...req,
            hideDelete: true,
            showSetMain: true // We can use 'setMain' button as Approve in DataTable
          }));
        }
      },
      error: () => {
        this.isLoading = false;
        this.toast.show('Failed to load pending requests', 'error');
      }
    });
  }

  openApproveModal(request: any) {
    this.selectedRequestForApprove = request;
    this.approveForm.patchValue({
      name: request.academyName,
      adminUserId: request.requestedById || request.userId,
      foundedDate: new Date().toISOString().substring(0, 10),
      requestId: request.id
    });
  }

  closeApproveModal() {
    this.selectedRequestForApprove = null;
    this.approveForm.reset();
  }

  openRejectModal(request: any) {
    this.selectedRequestForReject = request;
    this.rejectForm.reset();
  }

  closeRejectModal() {
    this.selectedRequestForReject = null;
    this.rejectForm.reset();
  }

  onApproveSubmit() {
    if (this.approveForm.invalid) return;

    this.isApproving = true;
    const formVal = this.approveForm.value;

    const dto = {
      academyRequestId: Number(formVal.requestId),
      name: formVal.name,
      logoUrl: formVal.logoUrl || 'images/logo/primary/logo-primary-dark.png',
      primaryColor: formVal.primaryColor || '#3b82f6',
      secondaryColor: formVal.secondaryColor || '#8b5cf6',
      foundedAt: formVal.foundedDate ? new Date(formVal.foundedDate).toISOString() : new Date().toISOString(),
      adminUserId: Number(formVal.adminUserId)
    };

    this.systemAdminService.approveAcademyRequest(dto).subscribe({
      next: (res) => {
        this.isApproving = false;
        if (res.isSuccess) {
          this.toast.show('Academy creation request approved!', 'success');
          this.closeApproveModal();
          this.loadPendingRequests();
          this.requestsChanged.emit();
        } else {
          this.toast.show(res.message || 'Error approving request', 'error');
        }
      },
      error: (err) => {
        this.isApproving = false;
        this.toast.show(err.error?.message || 'Error approving request', 'error');
      }
    });
  }

  onRejectSubmit() {
    if (this.rejectForm.invalid) return;

    this.isRejecting = true;
    const reason = this.rejectForm.value.reason;
    const requestId = this.selectedRequestForReject.id;

    this.systemAdminService.rejectAcademyRequest(requestId, reason).subscribe({
      next: (res) => {
        this.isRejecting = false;
        if (res.isSuccess || res.statusCode === 200) {
          this.toast.show('Academy creation request rejected', 'success');
          this.closeRejectModal();
          this.loadPendingRequests();
          this.requestsChanged.emit();
        } else {
          this.toast.show(res.message || 'Error rejecting request', 'error');
        }
      },
      error: (err) => {
        this.isRejecting = false;
        this.toast.show(err.error?.message || 'Error rejecting request', 'error');
      }
    });
  }
}
