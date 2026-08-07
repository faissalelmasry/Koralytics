import { Component, Input, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { DataTable, TableColumn } from '../../../../../shared/components/data-table/data-table';
import { Pagination } from '../../../../../shared/components/pagination/pagination';
import { AcademyLocationResponseDto } from '../../../../../core/interfaces/academy.models';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../../shared/pipes/localized-date.pipe';

@Component({
  selector: 'app-academy-locations-section',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CustomInputComponent,
    CustomButtonComponent,
    DataTable,
    Pagination,
    ConfirmDialogComponent,
    TranslatePipe,
    LocalizedDatePipe
  ],
  templateUrl: './academy-locations-section.html',
  styleUrls: ['./academy-locations-section.css']
})
export class AcademyLocationsSectionComponent implements OnInit {
  @Input() academyId!: number;

  private academyService = inject(AcademyService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);
  private translate = inject(TranslateService);

  locations: AcademyLocationResponseDto[] = [];
  locationForm!: FormGroup;
  isAddingLocation = false;
  isLoading = false;

  // Pagination for locations
  pageNumberLocations = 1;
  pageSizeLocations = 5;

  get locationColumns(): TableColumn[] {
    return [
      { key: 'name', label: 'ACADEMY_ADMIN.LOCATIONS_SECTION.COL_LOCATION_NAME' },
      { key: 'address', label: 'ACADEMY_ADMIN.LOCATIONS_SECTION.COL_ADDRESS' },
      { key: 'city', label: 'ACADEMY_ADMIN.LOCATIONS_SECTION.COL_CITY' },
      { key: 'isMainFormatted', label: 'ACADEMY_ADMIN.LOCATIONS_SECTION.COL_TYPE' },
      { key: 'actions', label: 'ACADEMY_ADMIN.LOCATIONS_SECTION.COL_MANAGE_LOCATION', type: 'action' }
    ];
  }

  ngOnInit() {
    this.initForm();
    if (this.academyId) {
      this.loadLocations();
    }
  }

  private initForm() {
    this.locationForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      address: ['', [Validators.required, Validators.maxLength(200)]],
      city: ['', [Validators.required, Validators.maxLength(100)]]
    });
  }

  loadLocations() {
    this.isLoading = true;
    this.academyService.getLocations(this.academyId).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.isSuccess && res.data) {
          this.locations = res.data;
        }
      },
      error: () => {
        this.isLoading = false;
        this.toast.show('Failed to load locations', 'error');
      }
    });
  }

  get paginatedLocations(): any[] {
    const start = (this.pageNumberLocations - 1) * this.pageSizeLocations;
    return this.locations.slice(start, start + this.pageSizeLocations).map(loc => ({
      ...loc,
      isMainFormatted: loc.isMain ? (this.translate.instant('ACADEMY_ADMIN.LOCATIONS_SECTION.MAIN_LOCATION') || 'Main Location') : (this.translate.instant('ACADEMY_ADMIN.LOCATIONS_SECTION.BRANCH') || 'Branch'),
      hideDelete: loc.isMain && this.locations.length > 1, // Don't show delete on main if others exist
      showSetMain: true
    }));
  }

  get totalLocationsCount(): number {
    return this.locations.length;
  }

  onPageChangeLocations(page: number) {
    this.pageNumberLocations = page;
  }

  onCreateLocation() {
    if (this.locationForm.invalid) return;

    this.isAddingLocation = true;
    const dto = this.locationForm.value;

    this.academyService.addLocation(this.academyId, dto).subscribe({
      next: (res) => {
        this.isAddingLocation = false;
        if (res.isSuccess) {
          this.toast.show(this.translate.instant('ACADEMY_ADMIN.LOCATIONS_SECTION.ADDED_SUCCESS') || 'Location added successfully', 'success');
          this.locationForm.reset();
          this.loadLocations();
        } else {
          this.toast.show(res.message || 'Error adding location', 'error');
        }
      },
      error: (err) => {
        this.isAddingLocation = false;
        this.toast.show(err.error?.message || 'Server error', 'error');
      }
    });
  }

  onLocationAction(event: { row: any; action: string }) {
    if (event.action === 'delete') {
      this.deleteLocation(event.row.id);
    } else if (event.action === 'setMain') {
      this.setMainLocation(event.row.id);
    }
  }

  isConfirmDialogOpen = false;
  confirmDialogTitle = '';
  confirmDialogMessage = '';
  targetIdForConfirm: number | null = null;

  deleteLocation(locationId: number) {
    this.targetIdForConfirm = locationId;
    this.confirmDialogTitle = this.translate.instant('ACADEMY_ADMIN.LOCATIONS_SECTION.DELETE_TITLE') || 'Delete Academy Location';
    this.confirmDialogMessage = this.translate.instant('ACADEMY_ADMIN.LOCATIONS_SECTION.DELETE_MSG') || 'Are you sure you want to permanently remove this location?';
    this.isConfirmDialogOpen = true;
  }

  onConfirmDialogExecute() {
    if (!this.targetIdForConfirm) {
      this.isConfirmDialogOpen = false;
      return;
    }

    const locationId = this.targetIdForConfirm;
    this.isConfirmDialogOpen = false;
    this.targetIdForConfirm = null;

    this.academyService.deleteLocation(this.academyId, locationId).subscribe({
      next: (res) => {
        if (res.isSuccess || res.statusCode === 204) {
          this.toast.show(this.translate.instant('ACADEMY_ADMIN.LOCATIONS_SECTION.DELETE_SUCCESS') || 'Location deleted successfully', 'success');
          this.loadLocations();
        } else {
          this.toast.show(res.message || 'Error deleting location', 'error');
        }
      },
      error: (err) => {
        this.toast.show(err.error?.message || 'Server error', 'error');
      }
    });
  }

  setMainLocation(locationId: number) {
    this.academyService.setMainLocation(this.academyId, locationId).subscribe({
      next: (res) => {
        if (res.isSuccess || res.statusCode === 204) {
          this.toast.show(this.translate.instant('ACADEMY_ADMIN.LOCATIONS_SECTION.UPDATE_MAIN_SUCCESS') || 'Main location updated', 'success');
          this.loadLocations();
        } else {
          this.toast.show(res.message || 'Error setting main location', 'error');
        }
      },
      error: (err) => {
        this.toast.show(err.error?.message || 'Server error', 'error');
      }
    });
  }
}
