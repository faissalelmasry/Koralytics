import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatchService } from '../../../../../core/services/match/match.service';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { PlayerCardService } from '../../../../../core/services/player/player-card.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { MiniPlayerCardModel } from '../../../../../core/models/Player/mini-player-card-model';
import { MiniPlayerCardComponent } from '../../mini-player-card/mini-player-card.component';
import { CustomSelect, SelectOption } from '../../../../../shared/components/custom-select/custom-select';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

export interface PositionSlot {
  slotId: string;
  role: string;
  player: MiniPlayerCardModel | null;
}

const FORMATIONS_11v11: Record<string, string[][]> = {
  '4-3-3': [
    ['LW', 'ST', 'RW'],
    ['CM', 'CDM', 'CM'],
    ['LB', 'CB', 'CB', 'RB'],
    ['GK']
  ],
  '4-2-3-1': [
    ['ST'],
    ['LAM', 'CAM', 'RAM'],
    ['CDM', 'CDM'],
    ['LB', 'CB', 'CB', 'RB'],
    ['GK']
  ],
  '3-5-2': [
    ['ST', 'ST'],
    ['LM', 'CM', 'CAM', 'CM', 'RM'],
    ['CB', 'CB', 'CB'],
    ['GK']
  ],
  '4-4-2': [
    ['ST', 'ST'],
    ['LM', 'CM', 'CM', 'RM'],
    ['LB', 'CB', 'CB', 'RB'],
    ['GK']
  ]
};

const FORMATIONS_7v7: Record<string, string[][]> = {
  '2-3-1': [
    ['ST'],
    ['LM', 'CM', 'RM'],
    ['CB', 'CB'],
    ['GK']
  ],
  '3-2-1': [
    ['ST'],
    ['CM', 'CM'],
    ['LB', 'CB', 'RB'],
    ['GK']
  ],
  '2-2-2': [
    ['ST', 'ST'],
    ['CM', 'CM'],
    ['CB', 'CB'],
    ['GK']
  ],
  '3-1-2': [
    ['ST', 'ST'],
    ['CM'],
    ['LB', 'CB', 'RB'],
    ['GK']
  ]
};

const FORMATIONS_5v5: Record<string, string[][]> = {
  '1-2-1': [
    ['ST'],
    ['LM', 'RM'],
    ['CB'],
    ['GK']
  ],
  '2-2': [
    ['ST', 'ST'],
    ['CB', 'CB'],
    ['GK']
  ],
  '2-1-1': [
    ['ST'],
    ['CM'],
    ['LB', 'RB'],
    ['GK']
  ],
  '1-1-2': [
    ['ST', 'ST'],
    ['CM'],
    ['CB'],
    ['GK']
  ]
};

@Component({
  selector: 'app-submit-lineup',
  standalone: true,
  imports: [
    CommonModule,
    MiniPlayerCardComponent,
    CustomSelect,
    CustomButtonComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    ScrollRevealDirective
  ],
  templateUrl: './submit-lineup.component.html',
  styleUrls: ['./submit-lineup.component.css']
})
export class SubmitLineupComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private matchService = inject(MatchService);
  private coachSquadService = inject(CoachSquadService);
  private playerCardService = inject(PlayerCardService);
  private toast = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);

  matchId = 0;
  matchDetails: any = null;
  coachTeamId = 0;
  coachTeamName = '';
  formatStartingCount = 11;

  isLoading = true;
  isSubmitting = false;
  error = '';

  // Formations
  selectedFormation = '4-3-3';
  formationOptions: SelectOption[] = [];

  // Squad and Pitch layout
  availableSquad: MiniPlayerCardModel[] = [];
  pitchRows: PositionSlot[][] = [];
  benchSlots: (MiniPlayerCardModel | null)[] = Array(7).fill(null);

  // Drag state
  draggedPlayer: MiniPlayerCardModel | null = null;
  dragSource: 'sidebar' | 'pitch' | 'bench' = 'sidebar';
  dragSourceIndex: { row?: number; col?: number; benchIdx?: number } = {};

  // Selected player for click-to-assign
  selectedPlayer: MiniPlayerCardModel | null = null;

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.matchId = idParam ? parseInt(idParam, 10) : 0;
    if (this.matchId > 0) {
      this.loadMatchData();
    } else {
      this.error = 'Invalid Match ID';
      this.isLoading = false;
    }
  }

  loadMatchData(): void {
    this.isLoading = true;
    this.error = '';

    this.matchService.getMatch(this.matchId).subscribe({
      next: (res: any) => {
        const data = res?.data ?? res;
        this.matchDetails = data;
        this.formatStartingCount = this.getStartingCountForFormat(data?.format);
        this.setupFormations(data?.format);

        // Fetch coach teams to know which side the coach manages
        this.coachSquadService.getCoachTeams().subscribe({
          next: (teamsRes: any) => {
            const teams = teamsRes?.data ?? teamsRes ?? [];
            const coachTeam = teams.find((t: any) =>
              (t.teamId ?? t.TeamId) === data?.homeTeamId || (t.teamId ?? t.TeamId) === data?.awayTeamId
            );

            if (!coachTeam) {
              this.error = 'You are not assigned as coach for either team in this match.';
              this.isLoading = false;
              this.cdr.detectChanges();
              return;
            }

            this.coachTeamId = coachTeam.teamId ?? coachTeam.TeamId;
            this.coachTeamName = coachTeam.teamName ?? coachTeam.TeamName;

            this.loadSquadAndExistingLineup();
          },
          error: () => {
            this.error = 'Failed to fetch coach team information.';
            this.isLoading = false;
            this.cdr.detectChanges();
          }
        });
      },
      error: () => {
        this.error = 'Failed to load match details.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  setupFormations(format: string): void {
    const formatStr = (format || '').toLowerCase();
    let map: Record<string, string[][]> = FORMATIONS_11v11;

    if (formatStr.includes('five') || formatStr.includes('5')) {
      map = FORMATIONS_5v5;
    } else if (formatStr.includes('seven') || formatStr.includes('7')) {
      map = FORMATIONS_7v7;
    }

    this.formationOptions = Object.keys(map).map(f => ({ value: f, label: f }));
    this.selectedFormation = this.formationOptions[0]?.value || '4-3-3';
    this.renderPitchSlots(map[this.selectedFormation]);
  }

  getStartingCountForFormat(format: string): number {
    const f = (format || '').toLowerCase();
    if (f.includes('five') || f.includes('5')) return 5;
    if (f.includes('seven') || f.includes('7')) return 7;
    return 11;
  }

  renderPitchSlots(schema: string[][]): void {
    this.pitchRows = schema.map((rowRoles, rowIndex) =>
      rowRoles.map((role, colIndex) => ({
        slotId: `pitch-${rowIndex}-${colIndex}`,
        role,
        player: null
      }))
    );
  }

  onFormationChange(): void {
    const formatStr = (this.matchDetails?.format || '').toLowerCase();
    let map: Record<string, string[][]> = FORMATIONS_11v11;

    if (formatStr.includes('five') || formatStr.includes('5')) {
      map = FORMATIONS_5v5;
    } else if (formatStr.includes('seven') || formatStr.includes('7')) {
      map = FORMATIONS_7v7;
    }

    const schema = map[this.selectedFormation] || map[Object.keys(map)[0]];
    const oldPitchPlayers = this.getAllAssignedPitchPlayers();
    this.renderPitchSlots(schema);

    // Reassign existing starters into new schema slots
    let idx = 0;
    for (const row of this.pitchRows) {
      for (const slot of row) {
        if (idx < oldPitchPlayers.length) {
          slot.player = oldPitchPlayers[idx];
          idx++;
        }
      }
    }
  }

  loadSquadAndExistingLineup(): void {
    this.coachSquadService.getSquad(this.coachTeamId).subscribe({
      next: (squadRes: any) => {
        const squadData = squadRes?.data ?? squadRes;
        const playersList: any[] = Array.isArray(squadData)
          ? squadData
          : (squadData?.players ?? squadData?.Players ?? []);

        const playerIds: number[] = playersList
          .map((item: any) => item.playerId ?? item.PlayerId ?? item.id ?? item.Id)
          .filter((id: number) => id > 0);

        if (playerIds.length === 0) {
          this.isLoading = false;
          this.cdr.detectChanges();
          return;
        }

        // Helper to construct mini-card from squad item
        const buildFallbackCard = (item: any): MiniPlayerCardModel => {
          const pId = item.playerId ?? item.PlayerId ?? item.id ?? item.Id;
          const name = (item.fullName ?? item.FullName ?? `${item.firstName ?? item.FirstName ?? ''} ${item.lastName ?? item.LastName ?? ''}`.trim()) || 'Unknown Player';
          const pos = item.primaryPosition ?? item.PrimaryPosition ?? item.position ?? item.Position ?? 'CM';
          const img = item.profileImageUrl ?? item.ProfileImageUrl ?? null;
          const rating = Math.round(item.overallRating ?? item.OverallRating ?? 0);

          return {
            playerId: pId,
            fullName: name,
            position: pos,
            profileImageUrl: img,
            overallRating: rating
          };
        };

        // Fetch rich mini cards for squad members
        this.playerCardService.getMiniPlayerCards(playerIds).subscribe({
          next: (cards: MiniPlayerCardModel[]) => {
            const rawCards = (cards as any)?.data ?? cards ?? [];
            const fetchedMap = new Map<number, MiniPlayerCardModel>();

            if (Array.isArray(rawCards)) {
              rawCards.forEach((c: any) => {
                if (c && c.playerId > 0) {
                  fetchedMap.set(c.playerId, {
                    ...c,
                    overallRating: Math.round(c.overallRating ?? 0)
                  });
                }
              });
            }

            // Map all squad players, using fetched card if available or fallback card with 0 rating
            this.availableSquad = playersList.map((item: any) => {
              const pId = item.playerId ?? item.PlayerId ?? item.id ?? item.Id;
              const fetched = fetchedMap.get(pId);
              if (fetched) return fetched;
              return buildFallbackCard(item);
            });

            // Load existing lineup if present
            this.matchService.getLineup(this.matchId).subscribe({
              next: (lineupRes: any) => {
                const existing = lineupRes?.data ?? lineupRes ?? [];
                const teamLineup = existing.filter((l: any) => l.teamId === this.coachTeamId);

                if (teamLineup.length > 0) {
                  this.toast.show('Lineup for your team has already been submitted for this match.', 'info');
                  this.router.navigate(['/match', this.matchId]);
                  return;
                }

                this.isLoading = false;
                this.cdr.detectChanges();
              },
              error: () => {
                this.isLoading = false;
                this.cdr.detectChanges();
              }
            });
          },
          error: () => {
            // If card service fails, generate fallback cards for all squad members
            this.availableSquad = playersList.map(item => buildFallbackCard(item));
            this.isLoading = false;
            this.cdr.detectChanges();
          }
        });
      },
      error: () => {
        this.error = 'Failed to fetch team squad roster.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  applyExistingLineup(lineups: any[]): void {
    const starters = lineups.filter(l => l.isStarting);
    const bench = lineups.filter(l => !l.isStarting);

    // Fill pitch starting slots
    let starterIdx = 0;
    for (const row of this.pitchRows) {
      for (const slot of row) {
        if (starterIdx < starters.length) {
          const item = starters[starterIdx];
          const found = this.availableSquad.find(p => p.playerId === item.playerId);
          if (found) {
            slot.player = found;
            this.removeFromAvailableSquad(found.playerId);
          }
          starterIdx++;
        }
      }
    }

    // Fill bench slots
    bench.forEach((item, i) => {
      if (i < 7) {
        const found = this.availableSquad.find(p => p.playerId === item.playerId);
        if (found) {
          this.benchSlots[i] = found;
          this.removeFromAvailableSquad(found.playerId);
        }
      }
    });
  }

  // Drag & Drop Handling
  onDragStart(event: DragEvent, player: MiniPlayerCardModel, source: 'sidebar' | 'pitch' | 'bench', posInfo?: any): void {
    this.draggedPlayer = player;
    this.dragSource = source;
    this.dragSourceIndex = posInfo || {};
    if (event.dataTransfer) {
      event.dataTransfer.setData('text/plain', player.playerId.toString());
      event.dataTransfer.effectAllowed = 'move';

      const target = event.currentTarget as HTMLElement;
      const cardEl = (target.querySelector('.mini-card-wrapper') as HTMLElement) || target;
      if (cardEl && event.dataTransfer.setDragImage) {
        event.dataTransfer.setDragImage(cardEl, cardEl.offsetWidth / 2, cardEl.offsetHeight / 2);
      }
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
    const target = event.currentTarget as HTMLElement;
    if (target) {
      target.classList.add('drag-over');
    }
  }

  onDragLeave(event: DragEvent): void {
    const target = event.currentTarget as HTMLElement;
    if (target) {
      target.classList.remove('drag-over');
    }
  }

  onDropPitch(event: DragEvent, rowIdx: number, colIdx: number): void {
    event.preventDefault();
    const target = event.currentTarget as HTMLElement;
    if (target) target.classList.remove('drag-over');

    if (!this.draggedPlayer) return;

    // Same slot, do nothing
    if (this.dragSource === 'pitch' && this.dragSourceIndex.row === rowIdx && this.dragSourceIndex.col === colIdx) {
      this.resetDragState();
      return;
    }

    const targetSlot = this.pitchRows[rowIdx][colIdx];
    const existingTargetPlayer = targetSlot.player;

    // Place dragged player in target pitch slot
    targetSlot.player = this.draggedPlayer;

    // Swap back existing player to source
    if (this.dragSource === 'pitch') {
      const { row, col } = this.dragSourceIndex;
      if (row !== undefined && col !== undefined) {
        this.pitchRows[row][col].player = existingTargetPlayer;
      }
    } else if (this.dragSource === 'bench') {
      const { benchIdx } = this.dragSourceIndex;
      if (benchIdx !== undefined) {
        this.benchSlots[benchIdx] = existingTargetPlayer;
      }
    } else if (this.dragSource === 'sidebar') {
      this.removeFromAvailableSquad(this.draggedPlayer.playerId);
      if (existingTargetPlayer) {
        this.availableSquad.push(existingTargetPlayer);
      }
    }

    this.resetDragState();
  }

  onDropBench(event: DragEvent, benchIdx: number): void {
    event.preventDefault();
    const target = event.currentTarget as HTMLElement;
    if (target) target.classList.remove('drag-over');

    if (!this.draggedPlayer) return;

    // Same slot, do nothing
    if (this.dragSource === 'bench' && this.dragSourceIndex.benchIdx === benchIdx) {
      this.resetDragState();
      return;
    }

    const existingBenchPlayer = this.benchSlots[benchIdx];

    // Place dragged player in target bench slot
    this.benchSlots[benchIdx] = this.draggedPlayer;

    // Swap back existing player to source
    if (this.dragSource === 'pitch') {
      const { row, col } = this.dragSourceIndex;
      if (row !== undefined && col !== undefined) {
        this.pitchRows[row][col].player = existingBenchPlayer;
      }
    } else if (this.dragSource === 'bench') {
      const { benchIdx: srcIdx } = this.dragSourceIndex;
      if (srcIdx !== undefined) {
        this.benchSlots[srcIdx] = existingBenchPlayer;
      }
    } else if (this.dragSource === 'sidebar') {
      this.removeFromAvailableSquad(this.draggedPlayer.playerId);
      if (existingBenchPlayer) {
        this.availableSquad.push(existingBenchPlayer);
      }
    }

    this.resetDragState();
  }

  onDropSidebar(event: DragEvent): void {
    event.preventDefault();
    const target = event.currentTarget as HTMLElement;
    if (target) target.classList.remove('drag-over');

    if (!this.draggedPlayer || this.dragSource === 'sidebar') return;

    this.removeDraggedPlayerFromSource();
    this.availableSquad.push(this.draggedPlayer);
    this.resetDragState();
  }

  removeDraggedPlayerFromSource(): void {
    if (!this.draggedPlayer) return;

    if (this.dragSource === 'sidebar') {
      this.removeFromAvailableSquad(this.draggedPlayer.playerId);
    } else if (this.dragSource === 'pitch') {
      const { row, col } = this.dragSourceIndex;
      if (row !== undefined && col !== undefined) {
        this.pitchRows[row][col].player = null;
      }
    } else if (this.dragSource === 'bench') {
      const { benchIdx } = this.dragSourceIndex;
      if (benchIdx !== undefined) {
        this.benchSlots[benchIdx] = null;
      }
    }
  }

  returnPlayerToSource(player: MiniPlayerCardModel): void {
    if (this.dragSource === 'pitch') {
      const { row, col } = this.dragSourceIndex;
      if (row !== undefined && col !== undefined) {
        this.pitchRows[row][col].player = player;
      }
    } else if (this.dragSource === 'bench') {
      const { benchIdx } = this.dragSourceIndex;
      if (benchIdx !== undefined) {
        this.benchSlots[benchIdx] = player;
      }
    }
  }

  resetDragState(): void {
    this.draggedPlayer = null;
    this.dragSource = 'sidebar';
    this.dragSourceIndex = {};
  }

  // Click-to-assign for mobile/touch usability
  selectPlayerCard(player: MiniPlayerCardModel): void {
    if (this.selectedPlayer?.playerId === player.playerId) {
      this.selectedPlayer = null;
    } else {
      this.selectedPlayer = player;
    }
  }

  assignSelectedToPitchSlot(rowIdx: number, colIdx: number): void {
    if (!this.selectedPlayer) return;
    const targetSlot = this.pitchRows[rowIdx][colIdx];
    const existing = targetSlot.player;

    targetSlot.player = this.selectedPlayer;
    this.removeFromAvailableSquad(this.selectedPlayer.playerId);

    if (existing) {
      this.availableSquad.push(existing);
    }
    this.selectedPlayer = null;
  }

  assignSelectedToBenchSlot(benchIdx: number): void {
    if (!this.selectedPlayer) return;
    const existing = this.benchSlots[benchIdx];

    this.benchSlots[benchIdx] = this.selectedPlayer;
    this.removeFromAvailableSquad(this.selectedPlayer.playerId);

    if (existing) {
      this.availableSquad.push(existing);
    }
    this.selectedPlayer = null;
  }

  removePitchPlayer(rowIdx: number, colIdx: number): void {
    const player = this.pitchRows[rowIdx][colIdx].player;
    if (player) {
      this.pitchRows[rowIdx][colIdx].player = null;
      this.availableSquad.push(player);
    }
  }

  removeBenchPlayer(benchIdx: number): void {
    const player = this.benchSlots[benchIdx];
    if (player) {
      this.benchSlots[benchIdx] = null;
      this.availableSquad.push(player);
    }
  }

  removeFromAvailableSquad(playerId: number): void {
    this.availableSquad = this.availableSquad.filter(p => p.playerId !== playerId);
  }

  getAllAssignedPitchPlayers(): MiniPlayerCardModel[] {
    const list: MiniPlayerCardModel[] = [];
    for (const row of this.pitchRows) {
      for (const slot of row) {
        if (slot.player) list.push(slot.player);
      }
    }
    return list;
  }

  getStartingCount(): number {
    return this.getAllAssignedPitchPlayers().length;
  }

  submitLineup(): void {
    const startingPlayers = this.getAllAssignedPitchPlayers();
    if (startingPlayers.length !== this.formatStartingCount) {
      this.toast.show(
        `Starting lineup requires exactly ${this.formatStartingCount} players (${startingPlayers.length} assigned).`,
        'error'
      );
      return;
    }

    const payloadPlayers: any[] = [];

    // Starting Players
    for (const row of this.pitchRows) {
      for (const slot of row) {
        if (slot.player) {
          payloadPlayers.push({
            playerId: slot.player.playerId,
            teamId: this.coachTeamId,
            isStarting: true,
            jerseyNumber: (slot.player as any).jerseyNumber ?? undefined,
            positionInMatch: slot.role
          });
        }
      }
    }

    // Bench Players
    this.benchSlots.forEach(player => {
      if (player) {
        payloadPlayers.push({
          playerId: player.playerId,
          teamId: this.coachTeamId,
          isStarting: false,
          jerseyNumber: (player as any).jerseyNumber ?? undefined,
          positionInMatch: 'SUB'
        });
      }
    });

    this.isSubmitting = true;
    const dto = {
      formation: this.selectedFormation,
      players: payloadPlayers
    };

    this.matchService.submitLineup(this.matchId, dto).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.toast.show('Match day squad lineup submitted successfully!', 'success');
        this.router.navigate(['/match', this.matchId]);
      },
      error: (err: any) => {
        this.isSubmitting = false;
        const msg = err?.error?.detail ?? err?.error?.message ?? 'Failed to submit lineup.';
        this.toast.show(msg, 'error');
        this.cdr.detectChanges();
      }
    });
  }
}
