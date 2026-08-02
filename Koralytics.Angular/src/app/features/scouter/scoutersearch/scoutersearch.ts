import { Component, OnInit, inject, signal, computed, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';

import { ScouterService } from '../../../../core/services/Scouter/scouter.service';
import { PlayerCardDto, PlayerSearchFiltersDto } from '../../../../core/interfaces/Scouter.interfaces';
import { ToastService } from '../../../../core/services/Toast/toast';
import { extractErrorMessage } from '../../../../core/utils/http-error.util';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../shared/components/footer/footer';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { PlayerCardComponent } from '../../player/player-card/player-card';

const POSITION_OPTIONS = ['GK', 'CB', 'LB', 'RB', 'CDM', 'CM', 'CAM', 'LM', 'RM', 'LW', 'RW', 'ST'];
const FOOT_OPTIONS = ['Left', 'Right', 'Both'];

const POSITION_COORDS: Record<string, { top: string; left: string }> = {
  GK: { top: '50%', left: '8%' },

  LB: { top: '18%', left: '25%' },
  CB: { top: '50%', left: '25%' },
  RB: { top: '82%', left: '25%' },

  CDM: { top: '50%', left: '40%' },

  LM: { top: '18%', left: '55%' },
  CM: { top: '50%', left: '55%' },
  RM: { top: '82%', left: '55%' },

  CAM: { top: '50%', left: '70%' },

  LW: { top: '22%', left: '82%' },
  ST: { top: '50%', left: '88%' },
  RW: { top: '78%', left: '82%' },
};

const POSITION_LINE_COLOR: Record<string, string> = {
  GK: '#4fd8ff',
  CB: '#5b8cff', LB: '#5b8cff', RB: '#5b8cff',
  CDM: '#b58cff', CM: '#b58cff', CAM: '#b58cff', LM: '#b58cff', RM: '#b58cff',
  LW: '#ff7a5c', RW: '#ff7a5c', ST: '#ff7a5c',
};

interface FilterTag {
  label: string;
  clear: () => void;
}

interface FilterState {
  positions: string[];
  minAge: number | null;
  maxAge: number | null;
  preferredFoot: string;
  academyId: number | null;
  minRating: number | null;
  maxRating: number | null;
}

function emptyFilters(): FilterState {
  return {
    positions: [],
    minAge: null,
    maxAge: null,
    preferredFoot: '',
    academyId: null,
    minRating: null,
    maxRating: null,
  };
}

@Component({
  selector: 'app-scouter-search',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    CustomButtonComponent,
    Pagination,
    NavbarComponent,
    Footer,
    ScrollRevealDirective,
    PlayerCardComponent
  ],
  templateUrl: './scoutersearch.html',
  styleUrls: ['./scoutersearch.css']
})
export class ScouterSearchComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private scouterService = inject(ScouterService);
  private tokenStorage = inject(TokenStorageService);
  private toastService = inject(ToastService);
  private destroyRef = inject(DestroyRef);

  public readonly positionOptions = POSITION_OPTIONS;
  public readonly footOptions = FOOT_OPTIONS;

  public getPositionCoords(pos: string): { top: string; left: string } {
    return POSITION_COORDS[pos] ?? { top: '50%', left: '50%' };
  }

  public getPositionLineColor(pos: string): string {
    return POSITION_LINE_COLOR[pos] ?? '#8b909a';
  }

  public filters: FilterState = emptyFilters();

  public nlQuery: string = '';

  public results = signal<PlayerCardDto[]>([]);
  public totalCount = signal<number>(0);
  public isLoading = signal<boolean>(false);
  public hasSearched = signal<boolean>(false);

  public apiErrorMessage = signal<string | null>(null);

  public getActiveFilterTags(): FilterTag[] {
    const f = this.filters;
    const tags: FilterTag[] = [];

    for (const pos of f.positions) {
      tags.push({ label: pos, clear: () => { this.togglePosition(pos); this.search(); } });
    }

    if (f.minAge !== null || f.maxAge !== null) {
      const label = `Age ${f.minAge ?? 'any'}–${f.maxAge ?? 'any'}`;
      tags.push({ label, clear: () => { this.filters.minAge = null; this.filters.maxAge = null; this.search(); } });
    }

    if (f.minRating !== null || f.maxRating !== null) {
      const label = `Rating ${f.minRating ?? 'any'}–${f.maxRating ?? 'any'}`;
      tags.push({ label, clear: () => { this.filters.minRating = null; this.filters.maxRating = null; this.search(); } });
    }

    if (f.preferredFoot) {
      tags.push({ label: `${f.preferredFoot} foot`, clear: () => { this.setFoot(f.preferredFoot); this.search(); } });
    }

    if (f.academyId !== null) {
      const id = f.academyId;
      tags.push({ label: `Academy #${id}`, clear: () => { this.filters.academyId = null; this.search(); } });
    }

    return tags;
  }

  private readonly pageSize = 12;
  public readonly pageSizeValue = this.pageSize;
  public pageNumber = signal<number>(1);
  public totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));

  private currentScouterId = 0;

  ngOnInit(): void {
    const paramId = this.route.snapshot.paramMap.get('scouterId');
    if (paramId) {
      this.currentScouterId = Number(paramId);
    } else {
      const token = this.tokenStorage.getAccessToken();
      const decoded = token ? this.decodeTokenPayload(token) : null;
      if (decoded) {
        this.currentScouterId = decoded.userId;
      }
    }
  }

  public togglePosition(position: string): void {
    const has = this.filters.positions.includes(position);
    this.filters.positions = has
      ? this.filters.positions.filter(p => p !== position)
      : [...this.filters.positions, position];
  }

  public isPositionSelected(position: string): boolean {
    return this.filters.positions.includes(position);
  }

  public setFoot(foot: string): void {
    this.filters.preferredFoot = this.filters.preferredFoot === foot ? '' : foot;
  }

  public resetFilters(): void {
   this.filters = emptyFilters();
    this.results.set([]);
    this.totalCount.set(0);
    this.hasSearched.set(false);
    this.apiErrorMessage.set(null);
  }

  public search(): void {
    if (this.isLoading()) return;
    this.pageNumber.set(1);
    this.runSearch();
  }

  public goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.pageNumber() || this.isLoading()) return;
    this.pageNumber.set(page);
    this.runSearch();
  }

  private runSearch(): void {
    const f = this.filters;
    const payload: PlayerSearchFiltersDto = {
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize,
    };

    if (f.positions.length) payload.positions = f.positions;
    if (f.minAge !== null) payload.minAge = f.minAge;
    if (f.maxAge !== null) payload.maxAge = f.maxAge;

    if (f.preferredFoot) {
      const footMap: Record<string, number> = {
        'Right': 1,
        'Left': 2,
        'Both': 3
      };
      payload.preferredFoot = footMap[f.preferredFoot] as any;
    }

    if (f.academyId !== null) payload.academyId = f.academyId;
    if (f.minRating !== null) payload.minRating = f.minRating;
    if (f.maxRating !== null) payload.maxRating = f.maxRating;

    this.isLoading.set(true);
    this.hasSearched.set(true);
    this.apiErrorMessage.set(null);

    this.scouterService.searchPlayers(payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.results.set(result.items);
          this.totalCount.set(result.totalCount);
          this.isLoading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          console.error('Player search failed', err);
          const message = extractErrorMessage(err, 'Search failed. Please try again.');
          this.apiErrorMessage.set(message);
          this.toastService.show(message, 'error');
          this.isLoading.set(false);
        }
      });
  }

  public dismissApiError(): void {
    this.apiErrorMessage.set(null);
  }

  public onNlSearchSubmit(): void {
    this.toastService.show('Natural-language query engine is coming soon.', 'info');
  }

  public viewProfile(playerId: number): void {
    this.router.navigate(['player/profile/', playerId]);
  }

  public addToShortlist(playerId: number): void {
    if (!this.currentScouterId) return;
    this.scouterService.addToShortlist(this.currentScouterId, playerId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.toastService.show('Added to shortlist.', 'success'),
        error: (err: HttpErrorResponse) => {
          console.error('Failed to add to shortlist', err);
          this.toastService.show(extractErrorMessage(err, 'Failed to add to shortlist.'), 'error');
        }
      });
  }

  public followPlayer(playerId: number): void {
    if (!this.currentScouterId) return;
    this.scouterService.followPlayer(this.currentScouterId, playerId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.toastService.show('Now following this player.', 'success'),
        error: (err: HttpErrorResponse) => {
          console.error('Failed to follow player', err);
          this.toastService.show(extractErrorMessage(err, 'Failed to follow player.'), 'error');
        }
      });
  }

  private decodeTokenPayload(token: string): { userId: number; roles: string[] } | null {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;

      let payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      while (payload.length % 4) payload += '=';
      const decoded = JSON.parse(atob(payload));

      const userId = parseInt(
        decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? '0',
        10
      );
      if (!userId) return null;

      const rawRoles = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      const roles: string[] = Array.isArray(rawRoles) ? rawRoles : rawRoles ? [rawRoles] : [];

      return { userId, roles };
    } catch {
      return null;
    }
  }
}
