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

@Component({
  selector: 'app-academy-teams-section',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomInputComponent, CustomButtonComponent, DataTable, Pagination, CustomSelect],
  templateUrl: './academy-teams-section.html',
  styleUrls: ['./academy-teams-section.css']
})
export class AcademyTeamsSectionComponent implements OnInit, OnChanges {
  @Input() academyId!: number;
  
  private academyService = inject(AcademyService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);
  
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

  selectedCoachToAssign: { [teamId: number]: any } = {};
  selectedPlayerToAssign: { [teamId: number]: any } = {};

  // Pagination
  pageNumberTeams = 1;
  pageSizeTeams = 10;
  totalTeamsCount = 0;

  pageNumberAgeGroups = 1;
  pageSizeAgeGroups = 10;
  totalAgeGroupsCount = 0;
  
  // Forms
  ageGroupForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(50)]],
    minAge: [3, [Validators.required, Validators.min(3), Validators.max(30)]],
    maxAge: [18, [Validators.required, Validators.min(3), Validators.max(30)]]
  });

  teamForm = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    ageGroupId: [null as number | null, Validators.required],
    locationId: [null as number | null, Validators.required]
  });

  // Table Columns
  ageGroupColumns: TableColumn[] = [
    { key: 'name', label: 'group name', type: 'text' },
    { key: 'minAge', label: 'min age', type: 'text' },
    { key: 'maxAge', label: 'max age', type: 'text' },
    { key: 'actions', label: 'actions', type: 'action' }
  ];

  teamColumns: TableColumn[] = [
    { key: 'name', label: 'team name', type: 'text' },
    { key: 'ageGroupName', label: 'age group', type: 'badge' },
    { key: 'locationName', label: 'location', type: 'text' },
    { key: 'playersCount', label: 'players', type: 'text' },
    { key: 'actions', label: 'manage team', type: 'action' }
  ];

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
          this.toast.show('Age group created successfully', 'success');
          this.ageGroupForm.reset({ name: '', minAge: 3, maxAge: 18 });
          this.loadData();
        } else {
          this.toast.show(res.message || 'Error creating age group', 'error');
        }
      },
      error: () => {
        this.isAddingAgeGroup = false;
        this.toast.show('Error creating age group', 'error');
      }
    });
  }

  onAgeGroupAction(event: { row: any, action: string }) {
     if (event.action === 'view') {
       this.toast.show(`Viewing age group ${event.row.name}`, 'success');
     } else if (event.action === 'delete') {
       this.toast.show(`Delete age group ${event.row.name} not implemented`, 'error');
     }
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
      this.toast.show('Age Group and Location are required', 'error');
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
          this.toast.show('Team created successfully', 'success');
          this.teamForm.reset({ name: '', ageGroupId: this.ageGroups[0]?.id || null, locationId: this.locations[0]?.id || null });
          this.loadData();
        } else {
          this.toast.show(res.message || 'Error creating team', 'error');
        }
      },
      error: () => {
        this.isAddingTeam = false;
        this.toast.show('Error creating team', 'error');
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
          this.toast.show('Coach assigned successfully', 'success');
          this.loadData(false);
        } else {
          this.toast.show(res.message || 'Error assigning coach', 'error');
        }
      },
      error: () => this.toast.show('Error assigning coach', 'error')
    });
  }

  onAssignPlayer(teamId: number, playerId: any) {
    if (!playerId) return;
    this.academyService.assignPlayerToTeam(teamId, playerId).subscribe({
      next: (res) => {
        if (res.isSuccess || res === null) {
          this.toast.show('Player assigned successfully', 'success');
          this.loadData(false);
        } else {
          this.toast.show(res.message || 'Error assigning player', 'error');
        }
      },
      error: () => this.toast.show('Error assigning player', 'error')
    });
  }

  onRemoveCoach(teamId: number, coachId: number) {
    this.academyService.removeCoachFromTeam(teamId, coachId).subscribe({
      next: () => {
        this.toast.show('Coach removed successfully', 'success');
        this.loadData(false);
      },
      error: () => this.toast.show('Error removing coach', 'error')
    });
  }

  onRemovePlayer(teamId: number, playerId: number) {
    this.academyService.removePlayerFromTeam(teamId, playerId).subscribe({
      next: () => {
        this.toast.show('Player removed successfully', 'success');
        this.loadData(false);
      },
      error: () => this.toast.show('Error removing player', 'error')
    });
  }
}
