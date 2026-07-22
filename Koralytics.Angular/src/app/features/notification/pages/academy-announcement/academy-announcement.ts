import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AcademyAnnouncementService } from '../../../../../core/services/SignalR/AcademyAnnouncementService';
import { AnnouncementResponseDto } from '../../../../../core/interfaces/AnnouncementResponse';
import { CreateAnnouncementPayload } from '../../../../../core/interfaces/CreateAnnouncementPayload';
import { extractErrorMessage } from '../../../../../core/utils/http-error.util';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomSelect, SelectOption } from '../../../../../shared/components/custom-select/custom-select';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { AnnouncementHistory } from '../announcement-history/announcement-history';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../../shared/components/footer/footer';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

export enum AnnouncementTargetType {
  All = 1,
  Team = 2,
  AgeGroup = 3,
  Role = 4,
}

export const ANNOUNCEMENT_SUPPORTED_ROLES = [
  { id: 4, name: 'Player' },
  { id: 5, name: 'Parent' },
  { id: 6, name: 'Coach' },
] as const;

@Component({
  selector: 'app-academy-announcement',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CustomInputComponent,
    CustomSelect,
    CustomButtonComponent,
    AnnouncementHistory,
    NavbarComponent,
    Footer,
    ScrollRevealDirective,
  ],
  templateUrl: './academy-announcement.html',
  styleUrl: './academy-announcement.css',
})
export class AcademyAnnouncement implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly announcementService = inject(AcademyAnnouncementService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly AnnouncementTargetType = AnnouncementTargetType;

  academyId = signal<number>(0);
  announcements = signal<AnnouncementResponseDto[]>([]);
  isLoading = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  selectedTargetType = signal<AnnouncementTargetType>(AnnouncementTargetType.All);

  readonly targetTypeOptions: SelectOption[] = [
    { value: AnnouncementTargetType.All, label: 'everyone' },
    { value: AnnouncementTargetType.Team, label: 'specific team' },
    { value: AnnouncementTargetType.AgeGroup, label: 'specific age group' },
    { value: AnnouncementTargetType.Role, label: 'specific role' },
  ];

  readonly roleOptions: SelectOption[] = ANNOUNCEMENT_SUPPORTED_ROLES.map((role) => ({
    value: role.id,
    label: role.name,
  }));

  announcementForm: FormGroup = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(100)]],
    body: ['', [Validators.required, Validators.maxLength(1000)]],
    targetType: [AnnouncementTargetType.All, [Validators.required]],
    targetId: [0],
    role: [''],
  });

  get titleError(): string {
    const c = this.announcementForm.get('title');
    if (!c || !c.touched || !c.errors) return '';
    if (c.errors['required']) return 'title is required.';
    if (c.errors['maxlength']) return 'title must be 100 characters or fewer.';
    return '';
  }

  get bodyError(): string {
    const c = this.announcementForm.get('body');
    if (!c || !c.touched || !c.errors) return '';
    if (c.errors['required']) return 'message body is required.';
    if (c.errors['maxlength']) return 'body must be 1000 characters or fewer.';
    return '';
  }

  get targetIdError(): string {
    const c = this.announcementForm.get('targetId');
    if (!c || !c.touched || !c.errors) return '';
    if (c.errors['required'] || c.errors['min']) return 'please provide a valid target id.';
    return '';
  }

  get roleError(): string {
    const c = this.announcementForm.get('role');
    if (!c || !c.touched || !c.errors) return '';
    if (c.errors['required']) return 'please select a role.';
    return '';
  }

  onTargetTypeChange(value: AnnouncementTargetType): void {
    this.announcementForm.get('targetType')?.setValue(value);
  }

  onRoleChange(value: number | string): void {
    this.announcementForm.get('role')?.setValue(value);
  }

  ngOnInit(): void {
    const idFromRoute = this.route.snapshot.paramMap.get('academyId');
    if (idFromRoute) {
      this.academyId.set(+idFromRoute);
      this.loadAnnouncements();
    }

    this.announcementForm
      .get('targetType')
      ?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((targetType: AnnouncementTargetType) => {
        this.selectedTargetType.set(Number(targetType));
        this.updateConditionalValidators(Number(targetType));
      });
  }

  private updateConditionalValidators(targetType: AnnouncementTargetType): void {
    const targetIdControl = this.announcementForm.get('targetId');
    const roleControl = this.announcementForm.get('role');

    targetIdControl?.clearValidators();
    roleControl?.clearValidators();

    if (targetType === AnnouncementTargetType.Team || targetType === AnnouncementTargetType.AgeGroup) {
      targetIdControl?.setValidators([Validators.required, Validators.min(1)]);
    } else if (targetType === AnnouncementTargetType.Role) {
      roleControl?.setValidators([Validators.required]);
    }

    targetIdControl?.updateValueAndValidity();
    roleControl?.updateValueAndValidity();
  }

  loadAnnouncements(): void {
    if (!this.academyId()) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.announcementService
      .getAnnouncements(this.academyId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.announcements.set(data);
          this.isLoading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(extractErrorMessage(err, 'Failed to load previous announcements.'));
          this.isLoading.set(false);
        },
      });
  }

  onSendAnnouncement(): void {
    if (this.announcementForm.invalid || !this.academyId()) {
      this.announcementForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const formValue = this.announcementForm.value;
    const rawTargetType = Number(formValue.targetType) as AnnouncementTargetType;

    let finalTargetId = 0;
    if (rawTargetType === AnnouncementTargetType.Team || rawTargetType === AnnouncementTargetType.AgeGroup) {
      finalTargetId = Number(formValue.targetId);
    } else if (rawTargetType === AnnouncementTargetType.Role) {
      finalTargetId = Number(formValue.role);
    }

    const dto: CreateAnnouncementPayload = {
      title: formValue.title,
      body: formValue.body,
      targetType: rawTargetType,
      targetId: finalTargetId,
      role: '',
    };

    this.announcementService
      .sendAnnouncement(this.academyId(), dto)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.successMessage.set('Announcement sent and saved successfully!');
          this.announcementForm.reset({ targetType: AnnouncementTargetType.All, targetId: 0, role: '' });
          this.selectedTargetType.set(AnnouncementTargetType.All);
          this.announcements.update((prev) => [response, ...prev]);
          this.isSubmitting.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(extractErrorMessage(err, 'Failed to send announcement. Please try again.'));
          this.isSubmitting.set(false);
        },
      });
  }
}

//! dropdown for age group and team