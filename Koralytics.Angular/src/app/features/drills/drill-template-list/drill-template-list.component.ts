import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../shared/pipes/localized-date.pipe';
import { Component, OnInit, OnDestroy, ChangeDetectorRef, inject, ChangeDetectionStrategy } from '@angular/core';
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
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { SearchBarComponent } from '../../../../shared/components/search-bar/search-bar';
import { CustomSelect, SelectOption } from '../../../../shared/components/custom-select/custom-select';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { CustomToggle } from '../../../../shared/components/custom-toggle/custom-toggle';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { CustomInputComponent } from '../../../../shared/components/custom-input-component/custom-input-component';
import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../shared/components/footer/footer';

@Component({
  selector: 'app-drill-template-list',
  templateUrl: './drill-template-list.component.html',
  styleUrls: ['./drill-template-list.component.css'],
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush, // 🟢 OPTIMIZATION: Massive performance boost
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    Pagination,
    CustomButtonComponent,
    SearchBarComponent,
    CustomSelect,
    CustomToggle,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    TranslatePipe,
    Footer, LocalizedDatePipe
  ],
})
export class DrillTemplateListComponent implements OnInit, OnDestroy {


  translateCategory(name: string | null | undefined): string {
    if (!name) return '';
    const key = 'DRILLS.DYNAMIC.CAT_' + name.toUpperCase();
    const translated = this.translate.instant(key);
    return translated !== key ? translated : name;
  }

  translateDifficulty(name: string | null | undefined): string {
    if (!name) return '';
    const key = 'DRILLS.DIFF_' + name.toUpperCase();
    const translated = this.translate.instant(key);
    return translated !== key ? translated : name;
  }

  translateDrillMode(name: string | null | undefined): string {
    if (!name) return '';
    const key = 'DRILLS.MODE_' + name.toUpperCase();
    const translated = this.translate.instant(key);
    return translated !== key ? translated : name.replace(/([A-Z])/g, ' $1').trim();
  }
  // --- Data Arrays ---
  visibleTemplates: DrillTemplateDto[] = [];
  categories: DrillCategoryDto[] = [];

  // --- UI State ---
  isLoading = false;
  isSaving = false;
  isFormOpen = false;
  isEditing = false;
  errorMessage = '';
  formError = '';
  toastMessage = '';
  toastType: 'success' | 'error' = 'success';

  // --- Confirm Modal State ---
  confirmModal: any = {
    isOpen: false,
    title: '',
    message: '',
    messageParams: {},
    confirmText: '',
    action: () => { }
  };

  // --- Video Modal State ---
  videoModal = {
    isOpen: false,
    drill: null as DrillTemplateDto | null,
    rawUrl: '',
    safeVideoUrl: null as SafeResourceUrl | null,
    isIframe: false
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
  get difficultyOptions(): SelectOption[] {
    return Object.values(DifficultyLevel).map(v => ({ value: v as string, label: this.translateDifficulty(v as string) }));
  }
  get drillModeOptions(): SelectOption[] {
    return Object.values(DrillMode).map(v => ({ value: v as string, label: this.translateDrillMode(v as string) }));
  }

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

  // 🟢 OPTIMIZATION: Centralized Subscription Management to prevent memory leaks
  private subscriptions = new Subscription();
  private searchSubject = new Subject<string>();

  constructor(
    private drillTemplateService: DrillTemplateService,
    private fb: FormBuilder,
    private authService: AuthService,
    private cdr: ChangeDetectorRef,
    private sanitizer: DomSanitizer,
    private translate: TranslateService
  ) {
    this.drillForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(300)]],
      categoryId: [null, Validators.required],
      difficultyLevel: [null, Validators.required],
      drillMode: [null, Validators.required],
      videoUrl: ['']
    });
  }

  ngOnInit(): void {
    // 🟢 OPTIMIZATION: Added to the cleanup crew
    this.subscriptions.add(
      this.authService.currentUser$.subscribe(user => {
        this.currentUserId = user?.userId || null;
        this.cdr.detectChanges(); // Tell OnPush to update
      })
    );

    this.fetchCategories();
    this.setupSearchDebounce();
    this.fetchTemplates();
  }

  ngOnDestroy(): void {
    // 🟢 OPTIMIZATION: Safely kills ALL subscriptions instantly to free up RAM
    this.subscriptions.unsubscribe();
  }

  // 🟢 OPTIMIZATION: Removed 'any', utilizing 'unknown' or 'string/number'
  onFormCategoryChange(val: string | number | null): void {
    this.drillForm.get('categoryId')?.setValue(val ? Number(val) : null);
  }

  onFormDifficultyChange(val: string | null): void {
    this.drillForm.get('difficultyLevel')?.setValue(val || null);
  }

  onFormModeChange(val: string | null): void {
    this.drillForm.get('drillMode')?.setValue(val || null);
  }

  // ==========================================
  // INITIALIZATION & DATA FETCHING
  // ==========================================

  private fetchCategories(): void {
    this.subscriptions.add(
      this.drillTemplateService.getDrillCategories().pipe(
        tap(response => console.log('[Categories] raw response:', response))
      ).subscribe({
        next: (response: DrillCategoryDto[]) => { // 🟢 OPTIMIZATION: Strictly typed
          this.categories = Array.isArray(response) ? response : [];

          // For filter bar: prepend "All Categories"
          this.categoryOptions = [
            { value: 0, label: this.translate.instant('DRILLS.TEMPLATE_LIST.ALL_CATEGORIES') || 'All Categories' },
            ...this.categories.map(c => ({ value: c.id as any, label: this.translateCategory(c.name) }))
          ];

          // For create/edit form: only real categories
          this.formCategoryOptions = this.categories.map(c => ({ value: c.id as any, label: this.translateCategory(c.name) }));

          console.log('[Categories] options:', this.categoryOptions);
        },
        error: (err) => console.error('[Categories] FAILED:', err)
      }));
  }

  fetchTemplates(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    const request$ = this.selectedCategoryId && this.selectedCategoryId > 0
      ? this.drillTemplateService.getTemplatesByCategory(this.selectedCategoryId, this.filter)
      : this.drillTemplateService.getTemplates(this.filter);

    this.subscriptions.add(
      request$.pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      ).subscribe({
        next: (response: PagedResultDto<DrillTemplateDto>) => {
          const items: DrillTemplateDto[] = response.items || [];

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
      })
    );
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
    this.subscriptions.add(
      this.searchSubject.pipe(
        debounceTime(400),
        distinctUntilChanged()
      ).subscribe(term => {
        this.filter.searchTerm = term;
        this.filter.pageNumber = 1;
        this.fetchTemplates();
      })
    );
  }

  onSearch(term: string): void {
    this.searchSubject.next(term);
  }

  onCategoryChange(value: string | number | null): void {
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
    this.cdr.detectChanges();
  }

  openEditForm(drill: DrillTemplateDto): void {
    this.isEditing = true;
    this.selectedDrillId = drill.id;
    this.drillForm.patchValue({
      name: drill.name,
      categoryId: drill.categoryId,
      difficultyLevel: drill.difficultyLevel,
      drillMode: drill.drillMode,
      videoUrl: drill.videoUrl || ''
    });
    this.isFormOpen = true;
    this.cdr.detectChanges();
  }

  closeForm(): void {
    this.isFormOpen = false;
    this.cdr.detectChanges();
    setTimeout(() => {
      this.drillForm.reset();
      this.formError = '';
    }, 300);
  }

  onSubmitForm(): void {
    if (this.drillForm.invalid) {
      this.drillForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.formError = '';
    this.cdr.detectChanges();

    const formData = this.drillForm.value;

    const request$ = this.isEditing && this.selectedDrillId
      ? this.drillTemplateService.updateTemplate(this.selectedDrillId, formData)
      : this.drillTemplateService.createTemplate(formData);

    this.subscriptions.add(
      request$.pipe(
        finalize(() => {
          this.isSaving = false;
          this.cdr.detectChanges();
        })
      ).subscribe({
        next: () => {
          this.closeForm();
          this.showToast(this.isEditing ? this.translate.instant('DRILLS.TEMPLATE_LIST.UPDATE_SUCCESS') || 'Template updated successfully.' : this.translate.instant('DRILLS.TEMPLATE_LIST.CREATE_SUCCESS') || 'Template created successfully.', 'success');
          this.fetchTemplates();
        },
        error: (err) => {
          const errorMsg = this.extractErrorMessage(err, 'Failed to save template.');
          this.formError = errorMsg;
          this.showErrorDialog('Save Failed', errorMsg);
        }
      })
    );
  }

  // ==========================================
  // VIDEO MODAL MANAGEMENT
  // ==========================================

  openVideoModal(drill: DrillTemplateDto): void {
    if (!drill.videoUrl) return;
    const rawUrl = drill.videoUrl.trim();
    let safeVideoUrl: SafeResourceUrl | null = null;
    let isIframe = false;

    const ytMatch = rawUrl.match(/(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=))([\w-]{11})/);
    if (ytMatch && ytMatch[1]) {
      const embedUrl = `https://www.youtube.com/embed/${ytMatch[1]}?autoplay=1`;
      safeVideoUrl = this.sanitizer.bypassSecurityTrustResourceUrl(embedUrl);
      isIframe = true;
    } else {
      const vimeoMatch = rawUrl.match(/vimeo\.com\/(?:channels\/(?:\w+\/)?|groups\/[^\/]*\/videos\/|album\/\d+\/video\/|video\/|)(\d+)/);
      if (vimeoMatch && vimeoMatch[1]) {
        const embedUrl = `https://player.vimeo.com/video/${vimeoMatch[1]}?autoplay=1`;
        safeVideoUrl = this.sanitizer.bypassSecurityTrustResourceUrl(embedUrl);
        isIframe = true;
      } else {
        isIframe = false;
      }
    }

    this.videoModal = {
      isOpen: true,
      drill: drill,
      rawUrl: rawUrl,
      safeVideoUrl: safeVideoUrl,
      isIframe: isIframe
    };
    this.cdr.detectChanges();
  }

  closeVideoModal(): void {
    this.videoModal = {
      isOpen: false,
      drill: null,
      rawUrl: '',
      safeVideoUrl: null,
      isIframe: false
    };
    this.cdr.detectChanges();
  }

  // ==========================================
  // FEEDBACK & MUTATIONS
  // ==========================================

  showToast(message: string, type: 'success' | 'error' = 'success'): void {
    this.toastMessage = message;
    this.toastType = type;
    this.cdr.detectChanges();
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
      confirmText: this.translate.instant('DRILLS.TEMPLATES.BTN_OK'),
      action: () => { this.closeConfirm(); }
    };
    this.cdr.detectChanges();
  }

  onShareTemplate(drill: DrillTemplateDto): void {
    this.drillTemplateService.shareTemplate(drill.id).subscribe({
      next: () => {
        const index = this.visibleTemplates.findIndex(t => t.id === drill.id);
        if (index !== -1) {
          this.visibleTemplates[index].isShared = !this.visibleTemplates[index].isShared;
          this.showToast(this.translate.instant(this.visibleTemplates[index].isShared ? 'DRILLS.TEMPLATES.TOAST_SHARED' : 'DRILLS.TEMPLATES.TOAST_UNSHARED'), 'success');
          this.calculateStats();
        }
      },
      error: (err) => {
        this.showErrorDialog(this.translate.instant('DRILLS.TEMPLATES.DIALOG_SHARE_FAILED'), this.extractErrorMessage(err, this.translate.instant('DRILLS.TEMPLATES.FAILED_SHARE')));
      }
    });
  }

  onDeleteTemplate(drill: DrillTemplateDto): void {
    this.confirmModal = {
      isOpen: true,
      title: this.translate.instant('DRILLS.TEMPLATES.CONFIRM_DELETE_TITLE'),
      message: this.translate.instant('DRILLS.TEMPLATES.CONFIRM_DELETE_MSG', { name: drill.name }),
      confirmText: this.translate.instant('DRILLS.TEMPLATES.CONFIRM_DELETE_BTN'),
      action: () => {
        this.drillTemplateService.deleteTemplate(drill.id).subscribe({
          next: () => {
            this.visibleTemplates = this.visibleTemplates.filter(t => t.id !== drill.id);
            if (this.totalItems > 0) this.totalItems--;
            this.showToast(this.translate.instant('DRILLS.TEMPLATES.TOAST_DELETED'), 'success');
            this.calculateStats();
            this.calculatePagination();
            this.closeConfirm();
          },
          error: (err) => {
            this.showErrorDialog(this.translate.instant('DRILLS.TEMPLATES.DIALOG_DELETE_FAILED'), this.extractErrorMessage(err, this.translate.instant('DRILLS.TEMPLATES.FAILED_DELETE')));
          }
        });
      }
    };
    this.cdr.detectChanges();
  }

  closeConfirm(): void {
    this.confirmModal.isOpen = false;
    this.cdr.detectChanges();
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
    if ((drill as any).categoryName) {
      return (drill as any).categoryName.toLowerCase();
    }

    const cat = this.categories.find(c => c.id === drill.categoryId);
    if (cat) {
      return cat.name.toLowerCase();
    }

    // 🟢 OPTIMIZATION: Removed hardcoded dictionary. Fallback to generic ID if category isn't loaded yet.
    const name = (drill as any).categoryName || this.categories.find(c => c.id === drill.categoryId)?.name || '';
    return name ? this.translateCategory(name) : `category #${drill.categoryId}`;
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
    return this.translateDifficulty(level as string);
  }

  getDrillModeLabel(mode: DrillMode): string {
    return this.translateDrillMode(mode as string);
  }

  getVisibilityLabel(drill: DrillTemplateDto): string {
    let key = '';
    let fallback = '';
    if (drill.academyId === null) {
      key = 'DRILLS.TEMPLATE_LIST.VISIBILITY_GLOBAL';
      fallback = 'Global';
    } else if (drill.isShared) {
      key = 'DRILLS.TEMPLATE_LIST.VISIBILITY_SHARED';
      fallback = 'Shared';
    } else {
      key = 'DRILLS.TEMPLATE_LIST.VISIBILITY_PRIVATE';
      fallback = 'Private';
    }
    const translated = this.translate.instant(key);
    return translated !== key ? translated : fallback;
  }

  getVisibilityClass(drill: DrillTemplateDto): string {
    if (drill.academyId === null) return 'badge-warning';
    if (drill.isShared) return 'badge-info';
    return 'badge-slate';
  }

  private extractErrorMessage(err: any, fallback: string): string {
    if (err?.error) {
      if (typeof err.error === 'string') {
        const match = err.error.match(/Exception:\s*([^<]+)/i) || err.error.match(/<title>([^<]+)<\/title>/i);
        if (match && match[1]) return match[1].trim();
        return err.error.substring(0, 100);
      }
      return err.error.upgradeMessage || err.error.message || err.error.detail || err.error.title || fallback;
    }
    return err?.upgradeMessage || err?.message || fallback;
  }
}