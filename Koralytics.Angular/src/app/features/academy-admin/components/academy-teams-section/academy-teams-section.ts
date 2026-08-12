import { Component, Input, OnInit, inject, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import {
  AgeGroupResponseDto,
  TeamResponseDto,
  AcademyLocationResponseDto,
  CreateTeamDto
} from '../../../../../core/interfaces/academy.models';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { DataTable, TableColumn } from '../../../../../shared/components/data-table/data-table';
import { Pagination } from '../../../../../shared/components/pagination/pagination';

import { CustomSelect, SelectOption } from '../../../../../shared/components/custom-select/custom-select';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../../shared/pipes/localized-date.pipe';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-academy-teams-section',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomInputComponent, CustomButtonComponent, DataTable, Pagination, CustomSelect, TranslatePipe, LocalizedDatePipe, ConfirmDialogComponent],
  templateUrl: './academy-teams-section.html',
  styleUrls: ['./academy-teams-section.css']
})
export class AcademyTeamsSectionComponent implements OnInit, OnChanges {
  @Input() academyId!: number;

  private academyService = inject(AcademyService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);
  private translate = inject(TranslateService);

  // Data State
  ageGroups: AgeGroupResponseDto[] = [];
  teams: TeamResponseDto[] = [];
  locations: AcademyLocationResponseDto[] = [];

  ageGroupOptions: SelectOption[] = [];
  locationOptions: SelectOption[] = [];

  availableCoaches: SelectOption[] = [];
  availablePlayers: SelectOption[] = [];

  // UI State
  isLoading = true;
  isAddingAgeGroup = false;
  isAddingTeam = false;
  activeTab: 'teams' | 'ageGroups' = 'teams';

  // Confirm Dialog State
  isConfirmDialogOpen = false;
  confirmDialogTitle = '';
  confirmDialogMessage = '';
  confirmDialogMessageParams: any = {};
  targetIdForConfirm: number | null = null;

  selectedCoachToAssign: { [teamId: number]: any } = {};
  selectedPlayerToAssign: { [teamId: number]: any } = {};

  // Pagination
  pageNumberTeams = 1;
  pageSizeTeams = 10;
  totalTeamsCount = 0;

  pageNumberAgeGroups = 1;
  pageSizeAgeGroups = 10;
  totalAgeGroupsCount = 0;

  predefinedAgeGroups = [
    { name: 'U5', minAge: 5, maxAge: 6 },
    { name: 'U7', minAge: 5, maxAge: 7 },
    { name: 'U9', minAge: 7, maxAge: 9 },
    { name: 'U11', minAge: 9, maxAge: 11 },
    { name: 'U13', minAge: 11, maxAge: 13 },
    { name: 'U15', minAge: 13, maxAge: 15 },
    { name: 'U17', minAge: 15, maxAge: 17 },
    { name: 'U19', minAge: 17, maxAge: 19 },
    { name: 'U21', minAge: 19, maxAge: 21 },
    { name: 'First Team', minAge: 21, maxAge: 50 }
  ];

  get predefinedAgeGroupOptions(): SelectOption[] {
    const existingNames = new Set(this.ageGroups.map(g => g.name));
    return this.predefinedAgeGroups
      .filter(g => !existingNames.has(g.name))
      .map(g => ({ value: g.name, label: g.name }));
  }

  onPredefinedAgeGroupChange(name: any) {
    const selected = this.predefinedAgeGroups.find(g => g.name === name);
    if (selected) {
      this.ageGroupForm.patchValue({
        name: selected.name,
        minAge: selected.minAge,
        maxAge: selected.maxAge
      });
    } else {
      this.ageGroupForm.patchValue({ name: '', minAge: 5, maxAge: 18 });
    }
  }

  // Forms
  ageGroupForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(50)]],
    minAge: [5, [Validators.required, Validators.min(5), Validators.max(50)]],
    maxAge: [18, [Validators.required, Validators.min(5), Validators.max(50)]]
  });

  teamForm = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    ageGroupId: [null as number | null, Validators.required],
    locationId: [null as number | null, Validators.required]
  });

  // Table Columns
  get ageGroupColumns(): TableColumn[] {
    return [
      { key: 'name', label: 'ACADEMY_ADMIN.TEAMS_SECTION.COL_GROUP_NAME', type: 'text' },
      { key: 'minAge', label: 'ACADEMY_ADMIN.TEAMS_SECTION.COL_MIN_AGE', type: 'text' },
      { key: 'maxAge', label: 'ACADEMY_ADMIN.TEAMS_SECTION.COL_MAX_AGE', type: 'text' },
      { key: 'actions', label: 'ACADEMY_ADMIN.TEAMS_SECTION.COL_ACTIONS', type: 'action' }
    ];
  }

  get teamColumns(): TableColumn[] {
    return [
      { key: 'name', label: 'ACADEMY_ADMIN.TEAMS_SECTION.COL_TEAM_NAME', type: 'text' },
      { key: 'ageGroupName', label: 'ACADEMY_ADMIN.TEAMS_SECTION.COL_AGE_GROUP', type: 'badge' },
      { key: 'locationName', label: 'ACADEMY_ADMIN.TEAMS_SECTION.COL_LOCATION', type: 'text' },
      { key: 'playersCount', label: 'ACADEMY_ADMIN.TEAMS_SECTION.COL_PLAYERS', type: 'text' },
      { key: 'actions', label: 'ACADEMY_ADMIN.TEAMS_SECTION.COL_MANAGE_TEAM', type: 'action' }
    ];
  }

  ngOnInit() {
    this.loadData();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['academyId'] && !changes['academyId'].isFirstChange()) {
      this.loadData();
    }
  }

  loadData(showLoading: boolean = true) {
    if (!this.academyId) return;
    if (showLoading) this.isLoading = true;

    this.academyService.getLocations(this.academyId).subscribe(res => {
      if (res.isSuccess && res.data) {
        this.locations = res.data;
        this.locationOptions = this.locations.map(l => ({ value: l.id, label: l.name }));
        if (this.locations.length > 0 && !this.teamForm.value.locationId) {
          this.teamForm.patchValue({ locationId: this.locations[0].id });
        }
      }
    });

    this.academyService.getAcademyMembers(this.academyId, { pageNumber: 1, pageSize: 1000 }).subscribe(res => {
      if (res.isSuccess && res.data) {
        this.availableCoaches = res.data.items
          .filter((m: any) => m.role === 'Coach')
          .map((c: any) => ({ value: c.userId, label: c.fullName }));
        this.availablePlayers = res.data.items
          .filter((m: any) => m.role === 'Player')
          .map((p: any) => ({ value: p.userId, label: p.fullName }));
      }
    });

    this.academyService.getAgeGroups(this.academyId).subscribe(res => {
      if (res.isSuccess && res.data) {
        this.ageGroups = res.data;
        this.ageGroupOptions = this.ageGroups.map(ag => ({ value: ag.id, label: ag.name }));
        this.totalAgeGroupsCount = this.ageGroups.length;
        if (this.ageGroups.length > 0 && !this.teamForm.value.ageGroupId) {
          this.teamForm.patchValue({ ageGroupId: this.ageGroups[0].id });
        }
      }
    });

    this.academyService.getTeams(this.academyId).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.teams = res.data.map((m: any) => ({
            ...m,
            playersCount: m.players?.length || 0,
            hideAnalyze: false,
            hideDelete: false
          }));
          this.totalTeamsCount = this.teams.length;
        }
        if (showLoading) this.isLoading = false;
      },
      error: () => {
        if (showLoading) this.isLoading = false;
      }
    });
  }

  switchTab(tab: 'teams' | 'ageGroups') {
    this.activeTab = tab;
  }

  get paginatedAgeGroups() {
    const start = (this.pageNumberAgeGroups - 1) * this.pageSizeAgeGroups;
    return this.ageGroups.slice(start, start + this.pageSizeAgeGroups);
  }

  onPageChangeAgeGroups(page: number) {
    this.pageNumberAgeGroups = page;
  }

  onCreateAgeGroup() {
    if (this.ageGroupForm.invalid) {
      this.ageGroupForm.markAllAsTouched();
      return;
    }

    this.isAddingAgeGroup = true;
    const dto = this.ageGroupForm.getRawValue();

    this.academyService.createAgeGroup(this.academyId, dto).subscribe({
      next: (res) => {
        this.isAddingAgeGroup = false;
        if (res.isSuccess) {
          this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.AGE_GROUP_CREATED'), 'success');
          this.ageGroupForm.reset({ name: '', minAge: 5, maxAge: 18 });
          this.loadData();
        } else {
          this.toast.show(res.message || this.translate.instant('ACADEMY_ADMIN.MESSAGES.CREATE_AGE_GROUP_ERROR'), 'error');
        }
      },
      error: () => {
        this.isAddingAgeGroup = false;
        this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.CREATE_AGE_GROUP_ERROR'), 'error');
      }
    });
  }

  onAgeGroupAction(event: { row: any, action: string }) {
    if (event.action === 'view') {
      this.toast.show(`Viewing age group ${event.row.name}`, 'success');
    } else if (event.action === 'delete') {
      this.targetIdForConfirm = event.row.id;
      this.confirmDialogTitle = this.translate.instant('ACADEMY_ADMIN.TEAMS_SECTION.DELETE_AGE_GROUP_TITLE') || 'Delete Age Group';
      this.confirmDialogMessage = 'ACADEMY_ADMIN.TEAMS_SECTION.DELETE_AGE_GROUP_MSG';
      this.confirmDialogMessageParams = { name: event.row.name };
      this.isConfirmDialogOpen = true;
    }
  }

  onConfirmDialogExecute() {
    if (!this.targetIdForConfirm) {
      this.isConfirmDialogOpen = false;
      return;
    }

    const ageGroupId = this.targetIdForConfirm;
    this.isConfirmDialogOpen = false;
    this.targetIdForConfirm = null;

    this.academyService.deleteAgeGroup(this.academyId, ageGroupId).subscribe({
      next: (res) => {
        if (res && res.isSuccess === false) {
          this.toast.show(res.message || this.translate.instant('ACADEMY_ADMIN.MESSAGES.DELETE_AGE_GROUP_ERROR'), 'error');
        } else {
          this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.AGE_GROUP_DELETED') || 'Deleted successfully.', 'success');
          this.loadData(false);
        }
      },
      error: (err) => {
        if (err.status === 409) {
          this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.AGE_GROUP_HAS_TEAMS'), 'error');
        } else {
          const msg = err.error?.detail || err.error?.message || err.message || this.translate.instant('ACADEMY_ADMIN.MESSAGES.DELETE_AGE_GROUP_ERROR');
          this.toast.show(msg, 'error');
        }
      }
    });
  }

  get paginatedTeams() {
    const start = (this.pageNumberTeams - 1) * this.pageSizeTeams;
    return this.teams.slice(start, start + this.pageSizeTeams);
  }

  onPageChangeTeams(page: number) {
    this.pageNumberTeams = page;
  }

  onCreateTeam() {
    if (this.teamForm.invalid) {
      this.teamForm.markAllAsTouched();
      return;
    }

    const { name, ageGroupId, locationId } = this.teamForm.getRawValue();
    if (!ageGroupId || !locationId) {
      this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.AGE_GROUP_LOCATION_REQUIRED'), 'error');
      return;
    }

    this.isAddingTeam = true;
    const formValue = this.teamForm.value;
    const dto: CreateTeamDto = {
      name: formValue.name ?? '',
      ageGroupId: formValue.ageGroupId!,
      locationId: formValue.locationId!
    };

    this.academyService.createTeam(this.academyId, dto).subscribe({
      next: (res) => {
        this.isAddingTeam = false;
        if (res.isSuccess) {
          this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.TEAM_CREATED'), 'success');
          this.teamForm.reset({ name: '', ageGroupId: this.ageGroups[0]?.id || null, locationId: this.locations[0]?.id || null });
          this.loadData();
        } else {
          this.toast.show(res.message || this.translate.instant('ACADEMY_ADMIN.MESSAGES.CREATE_TEAM_ERROR'), 'error');
        }
      },
      error: () => {
        this.isAddingTeam = false;
        this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.CREATE_TEAM_ERROR'), 'error');
      }
    });
  }

  onTeamAction(event: { row: any, action: string }) {
    if (event.action === 'view') {
      this.toast.show(`Manage team ${event.row.name}`, 'success');
    } else if (event.action === 'delete') {
      this.toast.show(`Delete team ${event.row.name} not implemented`, 'error');
    } else if (event.action === 'toggleExpand') {
      // The toggle is handled internally by DataTable, but we can do extra logic if needed
    }
  }

  getAvailableCoachesForTeam(team: any): SelectOption[] {
    const assignedCoachIds = new Set((team.coaches || []).map((c: any) => c.coachId || c.id));
    return this.availableCoaches.filter(c => !assignedCoachIds.has(c.value));
  }

  getAvailablePlayersForTeam(team: any): SelectOption[] {
    const allAssignedPlayerIds = new Set<number>();
    this.teams.forEach(t => {
      (t.players || []).forEach((p: any) => {
        allAssignedPlayerIds.add(p.playerId || p.id);
      });
    });
    return this.availablePlayers.filter(p => !allAssignedPlayerIds.has(p.value));
  }

  onAssignCoach(teamId: number, coachId: any) {
    if (!coachId) return;
    this.academyService.assignCoachToTeam(teamId, coachId).subscribe({
      next: (res) => {
        if (res.isSuccess || res === null) {
          this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.COACH_ASSIGNED'), 'success');
          this.loadData(false);
        } else {
          this.toast.show(res.message || this.translate.instant('ACADEMY_ADMIN.MESSAGES.ASSIGN_COACH_ERROR'), 'error');
        }
      },
      error: () => this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.ASSIGN_COACH_ERROR'), 'error')
    });
  }

  onAssignPlayer(teamId: number, playerId: any) {
    if (!playerId) return;
    this.academyService.assignPlayerToTeam(teamId, playerId).subscribe({
      next: (res) => {
        if (res.isSuccess || res === null) {
          this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.PLAYER_ASSIGNED'), 'success');
          this.loadData(false);
        } else {
          this.toast.show(res.message || this.translate.instant('ACADEMY_ADMIN.MESSAGES.ASSIGN_PLAYER_ERROR'), 'error');
        }
      },
      error: () => this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.ASSIGN_PLAYER_ERROR'), 'error')
    });
  }

  onRemoveCoach(teamId: number, coachId: number) {
    this.academyService.removeCoachFromTeam(teamId, coachId).subscribe({
      next: () => {
        this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.COACH_REMOVED'), 'success');
        this.loadData(false);
      },
      error: () => this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.REMOVE_COACH_ERROR'), 'error')
    });
  }

  onRemovePlayer(teamId: number, playerId: number) {
    this.academyService.removePlayerFromTeam(teamId, playerId).subscribe({
      next: () => {
        this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.PLAYER_REMOVED'), 'success');
        this.loadData(false);
      },
      error: () => this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.REMOVE_PLAYER_ERROR'), 'error')
    });
  }
}
