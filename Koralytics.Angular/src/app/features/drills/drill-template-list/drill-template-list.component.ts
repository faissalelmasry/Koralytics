import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged, finalize, tap } from 'rxjs/operators';
import { DrillTemplateService, PagedResultDto } from '../../../../core/services/drill/drill-template.service';
import {
  DrillCategoryDto,
  DrillTemplateDto,
  TemplateFilterDto
} from '../../../../core/interfaces/drill-template.model';
import { DifficultyLevel, DrillMode } from '../../../../core/enums/koralytics.enums';
import { AuthService } from '../../../../core/services/auth/auth.service';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { SearchBarComponent } from '../../../../shared/components/search-bar/search-bar';
import { CustomSelect, SelectOption } from '../../../../shared/components/custom-select/custom-select';
import { CustomInputComponent } from '../../../../shared/components/custom-input-component/custom-input-component';
import { CustomToggle } from '../../../../shared/components/custom-toggle/custom-toggle';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../shared/components/footer/footer';

@Component({
  selector: 'app-drill-template-list',
  templateUrl: './drill-template-list.component.html',
  styleUrls: ['./drill-template-list.component.css'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    Pagination,
    CustomButtonComponent,
    SearchBarComponent,
    CustomSelect,
    CustomInputComponent,
    CustomToggle,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    NavbarComponent,
    Footer
  ],
})
export class DrillTemplateListComponent implements OnInit, OnDestroy {
  // --- Data Arrays ---
  visibleTemplates: DrillTemplateDto[] = [];
  categories: DrillCategoryDto[] = [];

  // --- UI State ---
  isLoading = false;
  isSaving = false;
  isFormOpen = false;
  isEditing = false;
  errorMessage = '';     // page-level error
  formError = '';        // in-panel form error
  toastMessage = '';
  toastType: 'success' | 'error' = 'success';

  // --- Confirm Modal State ---
  confirmModal = {
    isOpen: false,
    title: '',
    message: '',
    confirmText: '',
    action: () => { }
  };

  // --- Filtering & Pagination ---
  filter: TemplateFilterDto = {
    pageNumber: 1,
    pageSize: 6,
    searchTerm: ''
  };

  // --- Auth State ---
  currentUserId: number | null = null;

  // --- Select Options for shared components ---
  categoryOptions: SelectOption[] = [];        // for filter bar (includes "All Categories")
  formCategoryOptions: SelectOption[] = [];    // for create/edit form (categories only)
  difficultyOptions: SelectOption[] = Object.values(DifficultyLevel).map(v => ({ value: v as string, label: v as string }));
  drillModeOptions: SelectOption[] = Object.values(DrillMode).map(v => ({ value: v as string, label: v as string }));

  selectedCategoryId: number | null = null;
  showSharedOnly = false;

  totalItems = 0;
  totalPages = 1;
  pagesArray: number[] = [];

  // --- Computed Stats ---
  advancedCount = 0;
  sharedCount = 0;

  // --- Forms & Enums ---
  drillForm!: FormGroup;
  selectedDrillId: number | null = null;

  difficultyLevels = Object.values(DifficultyLevel);
  drillModes = Object.values(DrillMode);

  // --- RxJS Subscriptions ---
  private searchSubject = new Subject<string>();
  private searchSubscription!: Subscription;

  constructor(
    private drillTemplateService: DrillTemplateService,
    private fb: FormBuilder,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {
    this.drillForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(300)]],
      categoryId: [null, Validators.required],
      difficultyLevel: [null, Validators.required],
      drillMode: [null, Validators.required]
    });
  }

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUserId = user?.userId || null;
    });

    this.fetchCategories();
    this.setupSearchDebounce();
    this.fetchTemplates();
  }

  ngOnDestroy(): void {
    if (this.searchSubscription) {
      this.searchSubscription.unsubscribe();
    }
  }

  // ==========================================
  // INITIALIZATION & DATA FETCHING
  // ==========================================

  private fetchCategories(): void {
    this.drillTemplateService.getDrillCategories().pipe(
      tap(response => console.log('[Categories] raw response:', response))
    ).subscribe({
      next: (response: any) => {
        this.categories = Array.isArray(response) ? response : (response?.data ?? []);
        console.log('[Categories] parsed:', this.categories);

        // For filter bar: prepend "All Categories"
        this.categoryOptions = [
          { value: 0, label: 'All Categories' },
          ...this.categories.map(c => ({ value: c.id as any, label: c.name }))
        ];

        // For create/edit form: only real categories
        this.formCategoryOptions = this.categories.map(c => ({ value: c.id as any, label: c.name }));

        console.log('[Categories] options:', this.categoryOptions);
      },
      error: (err) => console.error('[Categories] FAILED:', err)
    });
  }

  fetchTemplates(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const request$ = this.selectedCategoryId && this.selectedCategoryId > 0
      ? this.drillTemplateService.getTemplatesByCategory(this.selectedCategoryId, this.filter)
      : this.drillTemplateService.getTemplates(this.filter);

    request$.pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (response: PagedResultDto<DrillTemplateDto>) => {
        const items: DrillTemplateDto[] = response.items || [];

        // Apply frontend "Shared Only" filter if toggled
        this.visibleTemplates = this.showSharedOnly
          ? items.filter((d: DrillTemplateDto) => d.isShared)
          : items;

        this.totalItems = response.totalCount;
        this.calculateStats();
        this.calculatePagination();
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Failed to load templates.';
      }
    });
  }

  private calculateStats(): void {
    this.advancedCount = this.visibleTemplates.filter(d => d.difficultyLevel === DifficultyLevel.Advanced).length;
    this.sharedCount = this.visibleTemplates.filter(d => d.isShared).length;
  }

  private calculatePagination(): void {
    this.totalPages = Math.ceil(this.totalItems / this.filter.pageSize) || 1;
    this.pagesArray = Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  // ==========================================
  // NATIVE DOM EVENT HANDLERS
  // ==========================================

  private setupSearchDebounce(): void {
    this.searchSubscription = this.searchSubject.pipe(
      debounceTime(400),
      distinctUntilChanged()
    ).subscribe(term => {
      this.filter.searchTerm = term;
      this.filter.pageNumber = 1;
      this.fetchTemplates();
    });
  }

  onSearch(term: string): void {
    this.searchSubject.next(term);
  }

  onCategoryChange(value: any): void {
    this.selectedCategoryId = value && value !== 0 ? Number(value) : null;
    this.filter.pageNumber = 1;
    this.fetchTemplates();
  }

  onToggleShared(checked: boolean): void {
    this.showSharedOnly = checked;
    this.fetchTemplates();
  }

  changePage(page: number): void {
    if (page >= 1 && page <= this.totalPages && page !== this.filter.pageNumber) {
      this.filter.pageNumber = page;
      this.fetchTemplates();
    }
  }

  // ==========================================
  // SLIDE-IN FORM MANAGEMENT
  // ==========================================

  openCreateForm(): void {
    this.isEditing = false;
    this.selectedDrillId = null;
    this.drillForm.reset();
    this.isFormOpen = true;
  }

  openEditForm(drill: DrillTemplateDto): void {
    this.isEditing = true;
    this.selectedDrillId = drill.id;
    this.drillForm.patchValue({
      name: drill.name,
      categoryId: drill.categoryId,
      difficultyLevel: drill.difficultyLevel,
      drillMode: drill.drillMode
    });
    this.isFormOpen = true;
  }

  closeForm(): void {
    this.isFormOpen = false;
    setTimeout(() => this.drillForm.reset(), 300);
  }

  onSubmitForm(): void {
    if (this.drillForm.invalid) {
      this.drillForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.formError = '';
    const formData = this.drillForm.value;

    const request$ = this.isEditing && this.selectedDrillId
      ? this.drillTemplateService.updateTemplate(this.selectedDrillId, formData)
      : this.drillTemplateService.createTemplate(formData);

    request$.pipe(
      finalize(() => this.isSaving = false)
    ).subscribe({
      next: () => {
        this.closeForm();
        this.showToast(this.isEditing ? 'Template updated successfully.' : 'Template created successfully.', 'success');
        this.fetchTemplates();
      },
      error: (err) => {
        const errorMsg = this.extractErrorMessage(err, 'Failed to save template.');
        this.formError = errorMsg;
        this.showErrorDialog('Save Failed', errorMsg);
      }
    });
  }

  showToast(message: string, type: 'success' | 'error' = 'success'): void {
    console.log('[Toast]', type, message);
    this.toastMessage = message;
    this.toastType = type;
    this.cdr.detectChanges(); // force view update
    setTimeout(() => {
      this.toastMessage = '';
      this.cdr.detectChanges();
    }, 4500);
  }

  showErrorDialog(title: string, message: string): void {
    this.confirmModal = {
      isOpen: true,
      title: title,
      message: message,
      confirmText: 'OK',
      action: () => { this.closeConfirm(); }
    };
    this.cdr.detectChanges();
  }

  // ==========================================
  // MUTATIONS (Share / Delete)
  // ==========================================

  onShareTemplate(drill: DrillTemplateDto): void {
    this.drillTemplateService.shareTemplate(drill.id).subscribe({
      next: () => {
        const index = this.visibleTemplates.findIndex(t => t.id === drill.id);
        if (index !== -1) {
          this.visibleTemplates[index].isShared = !this.visibleTemplates[index].isShared;
          this.showToast(this.visibleTemplates[index].isShared ? 'Template shared successfully.' : 'Template unshared successfully.', 'success');
          this.calculateStats();
        }
      },
      error: (err) => {
        this.showErrorDialog('Share Failed', this.extractErrorMessage(err, 'Failed to toggle share status.'));
      }
    });
  }

  onDeleteTemplate(drill: DrillTemplateDto): void {
    this.confirmModal = {
      isOpen: true,
      title: 'Delete Template',
      message: `Are you sure you want to delete "${drill.name}"? This action cannot be undone.`,
      confirmText: 'Yes, Delete',
      action: () => {
        this.drillTemplateService.deleteTemplate(drill.id).subscribe({
          next: () => {
            this.visibleTemplates = this.visibleTemplates.filter(t => t.id !== drill.id);
            if (this.totalItems > 0) this.totalItems--;
            this.showToast('Template deleted successfully.', 'success');
            this.calculateStats();
            this.calculatePagination();
            this.closeConfirm();
          },
          error: (err) => {
            this.showErrorDialog('Cannot Delete Template', this.extractErrorMessage(err, 'Cannot delete this template.'));
          }
        });
      }
    };
  }

  closeConfirm(): void {
    this.confirmModal.isOpen = false;
  }

  executeConfirm(): void {
    if (this.confirmModal.action) {
      this.confirmModal.action();
    }
  }

  // ==========================================
  // UI FORMATTING HELPERS
  // ==========================================

  getCategoryLabel(drill: DrillTemplateDto): string {
    // 1. If the drill DTO has a categoryName string directly from backend, use it
    if ((drill as any).categoryName) {
      return (drill as any).categoryName.toLowerCase();
    }

    // 2. Otherwise, look it up from our loaded categories array
    const cat = this.categories.find(c => c.id === drill.categoryId);
    if (cat) {
      return cat.name.toLowerCase();
    }

    // 3. Fallback hardcoded dictionary matching your SQL IDs just in case the API array is slow
    const fallbackMap: { [key: number]: string } = {
      1: 'passing',
      2: 'shooting',
      3: 'dribbling',
      4: 'defending',
      5: 'goalkeeping',
      6: 'speed',
      7: 'physical'
    };

    return fallbackMap[drill.categoryId] || `category #${drill.categoryId}`;
  }

  getDifficultyClass(level: DifficultyLevel | string): string {
    switch (level) {
      case DifficultyLevel.Beginner: return 'difficulty-beginner';
      case DifficultyLevel.Intermediate: return 'difficulty-intermediate';
      case DifficultyLevel.Advanced: return 'difficulty-advanced';
      default: return 'difficulty-beginner';
    }
  }

  getDifficultyBars(level: DifficultyLevel | string): string {
    switch (level) {
      case DifficultyLevel.Beginner: return '▰▱▱';
      case DifficultyLevel.Intermediate: return '▰▰▱';
      case DifficultyLevel.Advanced: return '▰▰▰';
      default: return '▰▱▱';
    }
  }

  getDifficultyLabel(level: DifficultyLevel | string): string {
    switch (level) {
      case DifficultyLevel.Beginner: return 'low';
      case DifficultyLevel.Intermediate: return 'med';
      case DifficultyLevel.Advanced: return 'high';
      default: return 'low';
    }
  }

  getDrillModeLabel(mode: DrillMode): string {
    return mode.toString().replace(/([A-Z])/g, ' $1').trim().toLowerCase();
  }

  getVisibilityLabel(drill: DrillTemplateDto): string {
    if (drill.academyId === null) return 'global';
    if (drill.isShared) return 'shared';
    return 'private';
  }

  getVisibilityClass(drill: DrillTemplateDto): string {
    if (drill.academyId === null) return 'badge-warning'; // global
    if (drill.isShared) return 'badge-info'; // shared
    return 'badge-slate'; // private
  }

  // Helper to extract ASP.NET Core exception messages safely
  private extractErrorMessage(err: any, fallback: string): string {
    if (err?.error) {
      if (typeof err.error === 'string') {
        // Look for common ASP.NET exception patterns in HTML
        const match = err.error.match(/Exception:\s*([^<]+)/i) || err.error.match(/<title>([^<]+)<\/title>/i);
        if (match && match[1]) {
          return match[1].trim();
        }
        return err.error.substring(0, 100); // Return raw text if small enough
      }
      return err.error.message || err.error.detail || err.error.title || fallback;
    }
    return err?.message || fallback;
  }
}