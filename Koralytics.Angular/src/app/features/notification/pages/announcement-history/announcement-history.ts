import { Component, Input, OnInit, OnChanges, SimpleChanges, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AnnouncementResponseDto } from '../../../../../core/interfaces/AnnouncementResponse';
// NOTE: path guessed from the sibling components' folder depth -- adjust if
// AcademyService actually lives under a differently-cased or -named folder.
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state';
import { StatusChipComponent } from '../../../../../shared/components/status-chip/status-chip';
import { Pagination } from '../../../../../shared/components/pagination/pagination';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-announcement-history',
  standalone: true,
  imports: [
    CommonModule,
    StatusChipComponent,
    EmptyStateComponent,
    LoadingSpinnerComponent,
    Pagination,
    TranslatePipe
  ],
  templateUrl: './announcement-history.html',
  styleUrl: './announcement-history.css',
})
export class AnnouncementHistory implements OnInit, OnChanges {
  private readonly academyService = inject(AcademyService);
  private readonly destroyRef = inject(DestroyRef);

  @Input({ required: true }) announcements: AnnouncementResponseDto[] = [];
  @Input() isLoading: boolean = false;
  // Needed to resolve team/age-group ids to display names via AcademyService.
  @Input({ required: true }) academyId!: number;

  currentPage: number = 1;
  pageSize: number = 5;

  // id -> name lookups populated from AcademyService, so the history list
  // can show a real name instead of a bare "team #12" / "age group #5".
  // Falls back to the raw id in getTargetTypeLabel if a lookup ever misses
  // (id from a deleted team, load still in flight, etc.).
  private teamNames = new Map<number, string>();
  private ageGroupNames = new Map<number, string>();

  get paginatedAnnouncements(): AnnouncementResponseDto[] {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    return this.announcements.slice(startIndex, endIndex);
  }

  ngOnInit(): void {
    this.loadNameLookups();
  }

  ngOnChanges(changes: SimpleChanges): void {
    // Re-resolve if this instance gets reused for a different academy.
    if (changes['academyId'] && !changes['academyId'].isFirstChange()) {
      this.loadNameLookups();
    }
  }

  private loadNameLookups(): void {
    if (!this.academyId) return;

    forkJoin({
      teams: this.academyService.getTeams(this.academyId).pipe(map((res) => res.data ?? [])),
      ageGroups: this.academyService.getAgeGroups(this.academyId).pipe(map((res) => res.data ?? [])),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ teams, ageGroups }) => {
          this.teamNames = new Map(teams.map((t) => [t.id, t.name]));
          this.ageGroupNames = new Map(ageGroups.map((ag) => [ag.id, ag.name]));
        },
        // A failed lookup just means labels fall back to "team #<id>" below --
        // not worth surfacing its own error banner in a history list.
        error: () => {},
      });
  }

  onPageChange(page: number): void {
    this.currentPage = page;
  }

  // The API returns targetType as its string enum name ("All" | "Team" |
  // "AgeGroup" | "Role"), confirmed from swagger -- NOT the numeric value
  // used when *sending* an announcement. Compare against the strings here.
  getTargetTypeLabel(announcement: AnnouncementResponseDto): string {
    switch (announcement.targetType) {
      case 'Team':
        return this.teamNames.get(announcement.targetId) ?? `team #${announcement.targetId}`;
      case 'AgeGroup':
        return this.ageGroupNames.get(announcement.targetId) ?? `age group #${announcement.targetId}`;
      case 'Role':
        return this.getRoleName(announcement.targetId);
      default:
        return 'everyone';
    }
  }

  // targetId IS numeric here -- when targetType is Role, the backend still
  // sends the role as a numeric id (4/5/6), matching what the compose form
  // now submits.
  private getRoleName(roleId: number): string {
    switch (roleId) {
      case 4: return 'players';
      case 5: return 'parents';
      case 6: return 'coaches';
      default: return 'specific role';
    }
  }

  getTargetTypeChip(targetType: string): 'success' | 'danger' | 'warning' | 'info' {
    switch (targetType) {
      case 'Team':
        return 'success';
      case 'AgeGroup':
        return 'warning';
      case 'Role':
        return 'danger';
      default:
        return 'info';
    }
  }
}