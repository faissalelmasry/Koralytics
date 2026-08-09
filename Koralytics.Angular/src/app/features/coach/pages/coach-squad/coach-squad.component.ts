import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import {
  SquadOverviewDto,
  SquadPlayerDto,
  SquadComparisonDto,
  CoachTeamDto,
} from '../../../../../core/interfaces/coach.interfaces';
import { MiniPlayerCardComponent } from '../../../match/mini-player-card/mini-player-card.component';
import { MiniPlayerCardModel } from '../../../../../core/models/Player/mini-player-card-model';
import { TranslatePipe } from '@ngx-translate/core';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';

@Component({
  selector: 'app-coach-squad',
  standalone: true,
  imports: [CommonModule, FormsModule, MiniPlayerCardComponent, TranslatePipe , LoadingSpinnerComponent],
  templateUrl: './coach-squad.component.html',
  styleUrls: ['./coach-squad.component.css'],
})
export class CoachSquadComponent implements OnInit {
  private squadService = inject(CoachSquadService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  squad = signal<SquadOverviewDto | null>(null);
  comparison = signal<SquadComparisonDto | null>(null);
  loading = signal(false);
  error = signal('');

  // Team selection — fetched from API for all assigned teams
  teams = signal<CoachTeamDto[]>([]);
  selectedTeamId = 0;
  coachId = 0;

  // Comparison selection
  selectedPlayerA: number | null = null;
  selectedPlayerB: number | null = null;
  showCompareModal = false;

  ngOnInit(): void {
    const user = this.authService.getCurrentUserSync();
    if (user) {
      this.coachId = user.userId;
    }
    // Fetch coach's assigned teams, then auto-load the first team's squad
    this.squadService.getCoachTeams()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (teams) => {
          this.teams.set(teams);
          if (teams.length > 0) {
            this.selectedTeamId = teams[0].teamId;
            this.loadSquad();
          }
        },
        error: () => {
          this.error.set('Failed to load your assigned teams.');
        }
      });
  }

  onTeamChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedTeamId = +select.value;
    this.loadSquad();
  }

  loadSquad(): void {
    this.loading.set(true);
    this.error.set('');
    this.squadService.getSquad(this.selectedTeamId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.squad.set(data);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err?.error?.message || 'Failed to load squad');
          this.loading.set(false);
        },
      });
  }

  getAvailabilityClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'available':
        return 'status-available';
      case 'injured':
        return 'status-injured';
      case 'loaned':
        return 'status-loaned';
      case 'suspended':
        return 'status-suspended';
      default:
        return 'status-default';
    }
  }

  togglePlayerSelection(playerId: number): void {
    if (this.selectedPlayerA === playerId) {
      this.selectedPlayerA = null;
    } else if (this.selectedPlayerB === playerId) {
      this.selectedPlayerB = null;
    } else if (!this.selectedPlayerA) {
      this.selectedPlayerA = playerId;
    } else if (!this.selectedPlayerB) {
      this.selectedPlayerB = playerId;
    }
  }

  isSelected(playerId: number): boolean {
    return this.selectedPlayerA === playerId || this.selectedPlayerB === playerId;
  }

  canCompare(): boolean {
    return this.selectedPlayerA !== null && this.selectedPlayerB !== null;
  }

  openComparison(): void {
    if (!this.canCompare()) return;
    this.squadService
      .compareSquadPlayers(this.selectedPlayerA!, this.selectedPlayerB!)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.comparison.set(data);
          this.showCompareModal = true;
        },
        error: (err) => {
          this.error.set(err?.error?.message || 'Failed to compare players');
        },
      });
  }

  closeComparison(): void {
    this.showCompareModal = false;
    this.comparison.set(null);
    this.selectedPlayerA = null;
    this.selectedPlayerB = null;
  }

  navigateToPlayer(playerId: number): void {
    this.router.navigate(['/player/profile', playerId]);
  }

  isGK(player: SquadPlayerDto): boolean {
    return player?.primaryPosition?.toUpperCase() === 'GK';
  }

  getComparisonCategories(playerA: SquadPlayerDto, playerB: SquadPlayerDto): string[] {
    const aIsGk = this.isGK(playerA);
    const bIsGk = this.isGK(playerB);
    if (aIsGk && bIsGk) {
      return ['goalkeepingRating'];
    }
    if (aIsGk || bIsGk) {
      return ['goalkeepingRating', 'paceRating', 'shootingRating', 'passingRating', 'dribblingRating', 'defendingRating', 'physicalRating'];
    }
    return ['paceRating', 'shootingRating', 'passingRating', 'dribblingRating', 'defendingRating', 'physicalRating'];
  }

  /** Safely get a numeric rating from a SquadPlayerDto by property name */
  getRating(player: SquadPlayerDto, key: string): number {
    return (player as Record<string, any>)[key] ?? 0;
  }

  /** Extract 3-letter uppercase label, e.g. 'paceRating' → 'PAC', 'goalkeepingRating' → 'GKP' */
  getCategoryLabel(key: string): string {
    switch (key) {
      case 'paceRating': return 'PAC';
      case 'shootingRating': return 'SHO';
      case 'passingRating': return 'PAS';
      case 'dribblingRating': return 'DRI';
      case 'defendingRating': return 'DEF';
      case 'physicalRating': return 'PHY';
      case 'goalkeepingRating': return 'GKP';
      default: return key.substring(0, 3).toUpperCase();
    }
  }

  mapSquadPlayerToMiniCard(player: SquadPlayerDto): MiniPlayerCardModel {
    return {
      playerId: player.playerId,
      fullName: player.fullName,
      position: player.primaryPosition || 'N/A',
      naturalPosition: player.primaryPosition || 'N/A',
      profileImageUrl: player.profileImageUrl ?? null,
      overallRating: player.overallRating || 0,
    };
  }

  get totalPlayersCount(): number {
    return this.squad()?.players?.length ?? 0;
  }

  get availablePlayersCount(): number {
    return this.squad()?.players?.filter(p => p.availabilityStatus?.toLowerCase() === 'available')?.length ?? 0;
  }

  get averageOverallRating(): number {
    const players = this.squad()?.players;
    if (!players || !players.length) return 0;
    const sum = players.reduce((acc, p) => acc + (p.overallRating || 0), 0);
    return Math.round(sum / players.length);
  }
}
