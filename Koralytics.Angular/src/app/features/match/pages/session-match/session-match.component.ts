import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatchService } from '../../../../../core/services/match/match.service';
import { DrillSessionService } from '../../../../../core/services/drill/drill-session.service';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { PlayerCardService } from '../../../../../core/services/player/player-card.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { MiniPlayerCardModel } from '../../../../../core/models/Player/mini-player-card-model';
import { MiniPlayerCardComponent } from '../../mini-player-card/mini-player-card.component';
import { CustomSelect, SelectOption } from '../../../../../shared/components/custom-select/custom-select';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';
import { NavbarComponent } from "../../../../../shared/components/navbar/navbar";
import { Footer } from "../../../../../shared/components/footer/footer";
import { formatToLocalISO } from '../../../../../core/utils/date.util';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

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

const FORMAT_MAP_ENUM: Record<string, number> = {
  'FiveSide': 5,
  'SevenSide': 7,
  'ElevenSide': 11
};

@Component({
  selector: 'app-session-match',
  standalone: true,
  imports: [
    CommonModule,
    MiniPlayerCardComponent,
    CustomSelect,
    CustomButtonComponent,
    LoadingSpinnerComponent,
    ScrollRevealDirective,
    NavbarComponent,
    Footer,
    TranslatePipe
],
  templateUrl: './session-match.component.html',
  styleUrls: ['./session-match.component.css']
})
export class SessionMatchComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private matchService = inject(MatchService);
  private drillSessionService = inject(DrillSessionService);
  private coachSquadService = inject(CoachSquadService);
  private playerCardService = inject(PlayerCardService);
  private toast = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);
  private translate = inject(TranslateService);

  sessionId = 0;
  sessionData: any = null;
  teamId = 0;

  isLoading = true;
  isSubmitting = false;
  isAutoSplitting = false;
  error = '';

  // Active Team Side Slider
  activeSide: 'home' | 'away' = 'home';

  // Format & Options
  selectedFormat = 'SevenSide';
  formatOptions: SelectOption[] = [
    { value: 'FiveSide', label: '5-Side' },
    { value: 'SevenSide', label: '7-Side' },
    { value: 'ElevenSide', label: '11-Side' }
  ];

  formatStartingCount = 7;
  matchDate = '';
  location = '';

  // Formations for Home & Away
  homeFormation = '2-3-1';
  awayFormation = '2-3-1';
  formationOptions: SelectOption[] = [];

  get activeFormation(): string {
    return this.activeSide === 'home' ? this.homeFormation : this.awayFormation;
  }
  set activeFormation(val: string) {
    if (this.activeSide === 'home') {
      this.homeFormation = val;
    } else {
      this.awayFormation = val;
    }
  }

  // Squad Lists
  unassignedSquad: MiniPlayerCardModel[] = [];

  // Home Side Layout
  homePitchRows: PositionSlot[][] = [];
  homeBenchSlots: (MiniPlayerCardModel | null)[] = Array(7).fill(null);

  // Away Side Layout
  awayPitchRows: PositionSlot[][] = [];
  awayBenchSlots: (MiniPlayerCardModel | null)[] = Array(7).fill(null);

  get activePitchRows(): PositionSlot[][] {
    return this.activeSide === 'home' ? this.homePitchRows : this.awayPitchRows;
  }

  get activeBenchSlots(): (MiniPlayerCardModel | null)[] {
    return this.activeSide === 'home' ? this.homeBenchSlots : this.awayBenchSlots;
  }

  // Drag State
  draggedPlayer: MiniPlayerCardModel | null = null;
  dragSource: 'unassigned' | 'home-pitch' | 'home-bench' | 'away-pitch' | 'away-bench' = 'unassigned';
  dragSourceIndex: { row?: number; col?: number; benchIdx?: number } = {};

  // Click to assign selection
  selectedPlayer: MiniPlayerCardModel | null = null;

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('sessionId');
    this.sessionId = idParam ? parseInt(idParam, 10) : 0;
    if (this.sessionId > 0) {
      this.loadSessionData();
    } else {
      this.error = this.translate.instant('MATCH.REPORT.ERROR_INVALID_ID', { Default: 'Invalid Session ID' });
      this.isLoading = false;
    }
  }

  loadSessionData(): void {
    this.isLoading = true;
    this.error = '';

    this.drillSessionService.getSessionById(this.sessionId).subscribe({
      next: (res: any) => {
        const data = res?.data ?? res;
        this.sessionData = data;
        this.teamId = data?.teamId ?? data?.TeamId ?? 0;

        const rawDate = data?.sessionDate ?? data?.SessionDate ?? new Date().toISOString();
        this.matchDate = rawDate;
        this.location = data?.location ?? data?.Location ?? 'Training Pitch';

        this.updateFormatAndFormations('SevenSide');
        this.loadSessionPresentSquad();
      },
      error: () => {
        this.error = this.translate.instant('MATCH.SESSION_MATCH.ERROR_LOAD', { Default: 'Failed to load session details.' });
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadSessionPresentSquad(): void {
    const attendances: any[] =
      this.sessionData?.attendance ??
      this.sessionData?.Attendance ??
      this.sessionData?.sessionAttendances ??
      this.sessionData?.SessionAttendances ?? [];

    if (attendances.length > 0) {
      this.processAttendances(attendances);
    } else {
      this.drillSessionService.getSessionAttendance(this.sessionId).subscribe({
        next: (res: any) => {
          const list = res?.data ?? res ?? [];
          this.processAttendances(Array.isArray(list) ? list : []);
        },
        error: () => {
          this.unassignedSquad = [];
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
    }
  }

  private processAttendances(attendances: any[]): void {
    const presentAttendance = attendances.filter((a: any) => a.isPresent || a.IsPresent);

    if (presentAttendance.length > 0) {
      const playerIds = presentAttendance.map((a: any) => a.playerId ?? a.PlayerId);
      this.fetchMiniCardsForSquad(playerIds, presentAttendance);
    } else {
      this.unassignedSquad = [];
      this.isLoading = false;
      this.cdr.detectChanges();
    }
  }

  fetchMiniCardsForSquad(playerIds: number[], rawItems: any[]): void {
    const validIds = playerIds.filter(id => id > 0);
    if (validIds.length === 0) {
      this.isLoading = false;
      this.cdr.detectChanges();
      return;
    }

    const buildFallbackCard = (item: any): MiniPlayerCardModel => {
      const pId = item.playerId ?? item.PlayerId ?? item.id ?? item.Id;
      const playerObj = item.player ?? item.Player ?? item;
      const name = (
        playerObj.playerFullName ??
        playerObj.PlayerFullName ??
        playerObj.fullName ??
        playerObj.FullName ??
        `${playerObj.firstName ?? playerObj.FirstName ?? ''} ${playerObj.lastName ?? playerObj.LastName ?? ''}`.trim()
      ) || 'Player #' + pId;
      const pos = playerObj.position ?? playerObj.Position ?? playerObj.primaryPosition ?? playerObj.PrimaryPosition ?? 'CM';
      const img = playerObj.profileImageUrl ?? playerObj.ProfileImageUrl ?? null;
      const rating = Math.round(playerObj.overallRating ?? playerObj.OverallRating ?? 0);

      return {
        playerId: pId,
        fullName: name,
        position: pos,
        profileImageUrl: img,
        overallRating: rating
      };
    };

    this.playerCardService.getMiniPlayerCards(validIds).subscribe({
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

        this.unassignedSquad = rawItems.map((item: any) => {
          const pId = item.playerId ?? item.PlayerId ?? item.id ?? item.Id;
          const fetched = fetchedMap.get(pId);
          if (fetched) return fetched;
          return buildFallbackCard(item);
        });

        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.unassignedSquad = rawItems.map(item => buildFallbackCard(item));
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  autoSplitSquad(): void {
    if (!this.sessionId) return;

    this.isAutoSplitting = true;
    this.coachSquadService.splitTrainingTeams(this.sessionId).subscribe({
      next: (res: any) => {
        const splitData = res?.data ?? res;
        const teamAPlayers: any[] = splitData?.teamA ?? splitData?.TeamA ?? [];
        const teamBPlayers: any[] = splitData?.teamB ?? splitData?.TeamB ?? [];

        if (teamAPlayers.length === 0 && teamBPlayers.length === 0) {
          this.toast.show(this.translate.instant('MATCH.SESSION_MATCH.ERROR_CREATE', { Default: 'No players available to split.' }), 'error');
          this.isAutoSplitting = false;
          this.cdr.detectChanges();
          return;
        }

        // Reset all current assignments first
        this.resetAllAssignedPlayersToUnassigned();

        const squadMap = new Map<number, MiniPlayerCardModel>();
        this.unassignedSquad.forEach(p => squadMap.set(p.playerId, p));

        const mapToMiniCard = (dto: any): MiniPlayerCardModel => {
          const pId = dto.playerId ?? dto.PlayerId ?? dto.id;
          const existing = squadMap.get(pId);
          if (existing) return existing;
          return {
            playerId: pId,
            fullName: dto.fullName ?? dto.FullName ?? ('Player #' + pId),
            position: dto.primaryPosition ?? dto.PrimaryPosition ?? dto.position ?? 'CM',
            profileImageUrl: dto.profileImageUrl ?? dto.ProfileImageUrl ?? null,
            overallRating: Math.round(dto.overallRating ?? dto.OverallRating ?? 0)
          };
        };

        const convertedTeamA = teamAPlayers.map(mapToMiniCard);
        const convertedTeamB = teamBPlayers.map(mapToMiniCard);

        // Populate Home Side (Team A)
        this.populateTeamSide(this.homePitchRows, this.homeBenchSlots, convertedTeamA);

        // Populate Away Side (Team B)
        this.populateTeamSide(this.awayPitchRows, this.awayBenchSlots, convertedTeamB);

        // Remove assigned players from unassignedSquad
        const assignedIds = new Set<number>();
        this.getAssignedPlayers(this.homePitchRows).forEach(p => assignedIds.add(p.playerId));
        this.homeBenchSlots.forEach(p => p && assignedIds.add(p.playerId));
        this.getAssignedPlayers(this.awayPitchRows).forEach(p => assignedIds.add(p.playerId));
        this.awayBenchSlots.forEach(p => p && assignedIds.add(p.playerId));

        this.unassignedSquad = this.unassignedSquad.filter(p => !assignedIds.has(p.playerId));

        const avgA = this.calculateAverageRating(this.getAssignedPlayers(this.homePitchRows));
        const avgB = this.calculateAverageRating(this.getAssignedPlayers(this.awayPitchRows));

        this.toast.show(
          this.translate.instant('MATCH.SESSION_MATCH.AUTO_SPLIT_SUCCESS', { Default: 'Squad auto-split successfully!' }),
          'success'
        );

        this.isAutoSplitting = false;
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.isAutoSplitting = false;
        const msg = err?.error?.message || err?.error?.detail || this.translate.instant('MATCH.SESSION_MATCH.ERROR_CREATE', { Default: 'Failed to auto-split squad.' });
        this.toast.show(msg, 'error');
        this.cdr.detectChanges();
      }
    });
  }

  private populateTeamSide(
    pitchRows: PositionSlot[][],
    benchSlots: (MiniPlayerCardModel | null)[],
    teamPlayers: MiniPlayerCardModel[]
  ): void {
    // 0. Reset all pitch and bench slots for this side first
    for (const row of pitchRows) {
      for (const slot of row) {
        slot.player = null;
      }
    }
    for (let i = 0; i < benchSlots.length; i++) {
      benchSlots[i] = null;
    }

    if (teamPlayers.length === 0) return;

    const available = [...teamPlayers];

    const isGk = (p: MiniPlayerCardModel) =>
      p.position && (p.position.toUpperCase() === 'GK' || p.position.toUpperCase().includes('GOAL'));

    // 1. Identify Goalkeeper for GK pitch slot
    let gkPlayerIndex = available.findIndex(isGk);
    if (gkPlayerIndex === -1 && available.length > 0) {
      gkPlayerIndex = 0;
    }

    let gkPlayer: MiniPlayerCardModel | null = null;
    if (gkPlayerIndex !== -1) {
      gkPlayer = available.splice(gkPlayerIndex, 1)[0];
    }

    // Place GK into pitch slot where role === 'GK'
    if (gkPlayer) {
      for (const row of pitchRows) {
        for (const slot of row) {
          if (slot.role === 'GK') {
            slot.player = gkPlayer;
            break;
          }
        }
      }
    }

    // 2. Fill remaining pitch slots with position-matched players first
    for (const row of pitchRows) {
      for (const slot of row) {
        if (slot.player) continue;

        const matchIdx = available.findIndex(p =>
          p.position && p.position.toUpperCase() === slot.role.toUpperCase()
        );

        if (matchIdx !== -1) {
          slot.player = available.splice(matchIdx, 1)[0];
        }
      }
    }

    // 3. Fill any empty starting pitch slots with remaining available players
    for (const row of pitchRows) {
      for (const slot of row) {
        if (slot.player) continue;

        if (available.length > 0) {
          slot.player = available.shift()!;
        }
      }
    }

    // 4. Fill bench slots with all remaining players
    for (let b = 0; b < benchSlots.length && available.length > 0; b++) {
      benchSlots[b] = available.shift()!;
    }
  }

  private calculateAverageRating(players: MiniPlayerCardModel[]): number {
    if (players.length === 0) return 0;
    const sum = players.reduce((acc, p) => acc + (p.overallRating || 0), 0);
    return sum / players.length;
  }

  onFormatChange(): void {
    this.updateFormatAndFormations(this.selectedFormat);
  }

  resetAllAssignedPlayersToUnassigned(): void {
    const assignedPlayers: MiniPlayerCardModel[] = [];

    for (const row of this.homePitchRows) {
      for (const slot of row) {
        if (slot.player) {
          assignedPlayers.push(slot.player);
          slot.player = null;
        }
      }
    }

    for (const row of this.awayPitchRows) {
      for (const slot of row) {
        if (slot.player) {
          assignedPlayers.push(slot.player);
          slot.player = null;
        }
      }
    }

    for (let i = 0; i < this.homeBenchSlots.length; i++) {
      if (this.homeBenchSlots[i]) {
        assignedPlayers.push(this.homeBenchSlots[i]!);
        this.homeBenchSlots[i] = null;
      }
    }

    for (let i = 0; i < this.awayBenchSlots.length; i++) {
      if (this.awayBenchSlots[i]) {
        assignedPlayers.push(this.awayBenchSlots[i]!);
        this.awayBenchSlots[i] = null;
      }
    }

    if (assignedPlayers.length > 0) {
      this.unassignedSquad = [...this.unassignedSquad, ...assignedPlayers];
    }

    this.resetDragState();
    this.selectedPlayer = null;
  }

  updateFormatAndFormations(format: string): void {
    this.resetAllAssignedPlayersToUnassigned();
    this.selectedFormat = format;

    let map: Record<string, string[][]> = FORMATIONS_7v7;
    if (format === 'FiveSide') {
      this.formatStartingCount = 5;
      map = FORMATIONS_5v5;
    } else if (format === 'SevenSide') {
      this.formatStartingCount = 7;
      map = FORMATIONS_7v7;
    } else {
      this.formatStartingCount = 11;
      map = FORMATIONS_11v11;
    }

    this.formationOptions = Object.keys(map).map(f => ({ value: f, label: f }));

    if (!map[this.homeFormation]) this.homeFormation = this.formationOptions[0].value;
    if (!map[this.awayFormation]) this.awayFormation = this.formationOptions[0].value;

    this.renderHomePitch(map[this.homeFormation]);
    this.renderAwayPitch(map[this.awayFormation]);
  }

  onActiveFormationChange(): void {
    const map = this.getFormationMap();
    const schema = map[this.activeFormation] || map[Object.keys(map)[0]];
    const targetRows = this.activeSide === 'home' ? this.homePitchRows : this.awayPitchRows;
    const oldStarters = this.getAssignedPlayers(targetRows);

    if (this.activeSide === 'home') {
      this.renderHomePitch(schema);
      this.reassignOldStarters(this.homePitchRows, oldStarters);
    } else {
      this.renderAwayPitch(schema);
      this.reassignOldStarters(this.awayPitchRows, oldStarters);
    }
  }

  getFormationMap(): Record<string, string[][]> {
    if (this.selectedFormat === 'FiveSide') return FORMATIONS_5v5;
    if (this.selectedFormat === 'SevenSide') return FORMATIONS_7v7;
    return FORMATIONS_11v11;
  }

  renderHomePitch(schema: string[][]): void {
    this.homePitchRows = schema.map((rowRoles, rIdx) =>
      rowRoles.map((role, cIdx) => ({
        slotId: `home-pitch-${rIdx}-${cIdx}`,
        role,
        player: null
      }))
    );
  }

  renderAwayPitch(schema: string[][]): void {
    this.awayPitchRows = schema.map((rowRoles, rIdx) =>
      rowRoles.map((role, cIdx) => ({
        slotId: `away-pitch-${rIdx}-${cIdx}`,
        role,
        player: null
      }))
    );
  }

  getAssignedPlayers(rows: PositionSlot[][]): MiniPlayerCardModel[] {
    const list: MiniPlayerCardModel[] = [];
    for (const r of rows) {
      for (const slot of r) {
        if (slot.player) list.push(slot.player);
      }
    }
    return list;
  }

  reassignOldStarters(targetRows: PositionSlot[][], players: MiniPlayerCardModel[]): void {
    let idx = 0;
    for (const r of targetRows) {
      for (const slot of r) {
        if (idx < players.length) {
          slot.player = players[idx];
          idx++;
        }
      }
    }
  }

  // Drag & Drop Handlers
  onDragStart(
    event: DragEvent,
    player: MiniPlayerCardModel,
    source: 'unassigned' | 'home-pitch' | 'home-bench' | 'away-pitch' | 'away-bench',
    posInfo?: any
  ): void {
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
    if (target) target.classList.add('drag-over');
  }

  onDragLeave(event: DragEvent): void {
    const target = event.currentTarget as HTMLElement;
    if (target) target.classList.remove('drag-over');
  }

  onDropPitchSlot(event: DragEvent, rIdx: number, cIdx: number): void {
    event.preventDefault();
    const target = event.currentTarget as HTMLElement;
    if (target) target.classList.remove('drag-over');

    if (!this.draggedPlayer) return;

    const targetRows = this.activeSide === 'home' ? this.homePitchRows : this.awayPitchRows;
    const targetSlot = targetRows[rIdx][cIdx];
    const existing = targetSlot.player;

    targetSlot.player = this.draggedPlayer;
    this.swapSourcePlayer(existing);
    this.resetDragState();
  }

  onDropBenchSlot(event: DragEvent, bIdx: number): void {
    event.preventDefault();
    const target = event.currentTarget as HTMLElement;
    if (target) target.classList.remove('drag-over');

    if (!this.draggedPlayer) return;

    const targetBench = this.activeSide === 'home' ? this.homeBenchSlots : this.awayBenchSlots;
    const existing = targetBench[bIdx];

    targetBench[bIdx] = this.draggedPlayer;
    this.swapSourcePlayer(existing);
    this.resetDragState();
  }

  onDropUnassigned(event: DragEvent): void {
    event.preventDefault();
    const target = event.currentTarget as HTMLElement;
    if (target) target.classList.remove('drag-over');

    if (!this.draggedPlayer || this.dragSource === 'unassigned') return;

    this.clearPlayerFromSource();
    this.unassignedSquad.push(this.draggedPlayer);
    this.resetDragState();
  }

  swapSourcePlayer(replacementPlayer: MiniPlayerCardModel | null): void {
    if (this.dragSource === 'unassigned') {
      this.removeFromUnassigned(this.draggedPlayer!.playerId);
      if (replacementPlayer) {
        this.unassignedSquad.push(replacementPlayer);
      }
    } else if (this.dragSource === 'home-pitch') {
      const { row, col } = this.dragSourceIndex;
      if (row !== undefined && col !== undefined) {
        this.homePitchRows[row][col].player = replacementPlayer;
      }
    } else if (this.dragSource === 'away-pitch') {
      const { row, col } = this.dragSourceIndex;
      if (row !== undefined && col !== undefined) {
        this.awayPitchRows[row][col].player = replacementPlayer;
      }
    } else if (this.dragSource === 'home-bench') {
      const { benchIdx } = this.dragSourceIndex;
      if (benchIdx !== undefined) {
        this.homeBenchSlots[benchIdx] = replacementPlayer;
      }
    } else if (this.dragSource === 'away-bench') {
      const { benchIdx } = this.dragSourceIndex;
      if (benchIdx !== undefined) {
        this.awayBenchSlots[benchIdx] = replacementPlayer;
      }
    }
  }

  clearPlayerFromSource(): void {
    if (!this.draggedPlayer) return;

    if (this.dragSource === 'unassigned') {
      this.removeFromUnassigned(this.draggedPlayer.playerId);
    } else if (this.dragSource === 'home-pitch') {
      const { row, col } = this.dragSourceIndex;
      if (row !== undefined && col !== undefined) {
        this.homePitchRows[row][col].player = null;
      }
    } else if (this.dragSource === 'away-pitch') {
      const { row, col } = this.dragSourceIndex;
      if (row !== undefined && col !== undefined) {
        this.awayPitchRows[row][col].player = null;
      }
    } else if (this.dragSource === 'home-bench') {
      const { benchIdx } = this.dragSourceIndex;
      if (benchIdx !== undefined) {
        this.homeBenchSlots[benchIdx] = null;
      }
    } else if (this.dragSource === 'away-bench') {
      const { benchIdx } = this.dragSourceIndex;
      if (benchIdx !== undefined) {
        this.awayBenchSlots[benchIdx] = null;
      }
    }
  }

  resetDragState(): void {
    this.draggedPlayer = null;
    this.dragSource = 'unassigned';
    this.dragSourceIndex = {};
  }

  removeFromUnassigned(playerId: number): void {
    this.unassignedSquad = this.unassignedSquad.filter(p => p.playerId !== playerId);
  }

  // Click to Assign (Touch/Mobile Support)
  selectPlayerCard(player: MiniPlayerCardModel): void {
    if (this.selectedPlayer?.playerId === player.playerId) {
      this.selectedPlayer = null;
    } else {
      this.selectedPlayer = player;
    }
  }

  assignSelectedToPitchSlot(rIdx: number, cIdx: number): void {
    if (!this.selectedPlayer) return;
    const targetRows = this.activeSide === 'home' ? this.homePitchRows : this.awayPitchRows;
    const targetSlot = targetRows[rIdx][cIdx];
    const existing = targetSlot.player;

    targetSlot.player = this.selectedPlayer;
    this.removeFromUnassigned(this.selectedPlayer.playerId);
    if (existing) this.unassignedSquad.push(existing);
    this.selectedPlayer = null;
  }

  assignSelectedToBenchSlot(bIdx: number): void {
    if (!this.selectedPlayer) return;
    const targetBench = this.activeSide === 'home' ? this.homeBenchSlots : this.awayBenchSlots;
    const existing = targetBench[bIdx];

    targetBench[bIdx] = this.selectedPlayer;
    this.removeFromUnassigned(this.selectedPlayer.playerId);
    if (existing) this.unassignedSquad.push(existing);
    this.selectedPlayer = null;
  }

  removePitchPlayer(rIdx: number, cIdx: number): void {
    const targetRows = this.activeSide === 'home' ? this.homePitchRows : this.awayPitchRows;
    const p = targetRows[rIdx][cIdx].player;
    if (p) {
      targetRows[rIdx][cIdx].player = null;
      this.unassignedSquad.push(p);
    }
  }

  removeBenchPlayer(bIdx: number): void {
    const targetBench = this.activeSide === 'home' ? this.homeBenchSlots : this.awayBenchSlots;
    const p = targetBench[bIdx];
    if (p) {
      targetBench[bIdx] = null;
      this.unassignedSquad.push(p);
    }
  }

  getHomeStartingCount(): number {
    return this.getAssignedPlayers(this.homePitchRows).length;
  }

  getAwayStartingCount(): number {
    return this.getAssignedPlayers(this.awayPitchRows).length;
  }

  submitSessionMatch(): void {
    const homeStarters = this.getAssignedPlayers(this.homePitchRows);
    const awayStarters = this.getAssignedPlayers(this.awayPitchRows);

    if (homeStarters.length !== this.formatStartingCount) {
      this.toast.show(
        this.translate.instant('MATCH.SESSION_MATCH.ERROR_CREATE', { Default: `Home Side requires exactly ${this.formatStartingCount} starting players (${homeStarters.length} assigned).` }),
        'error'
      );
      return;
    }

    if (awayStarters.length !== this.formatStartingCount) {
      this.toast.show(
        this.translate.instant('MATCH.SESSION_MATCH.ERROR_CREATE', { Default: `Away Side requires exactly ${this.formatStartingCount} starting players (${awayStarters.length} assigned).` }),
        'error'
      );
      return;
    }

    const homePlayersPayload: any[] = [];
    for (const r of this.homePitchRows) {
      for (const slot of r) {
        if (slot.player) {
          homePlayersPayload.push({
            playerId: slot.player.playerId,
            isStarting: true,
            jerseyNumber: (slot.player as any).jerseyNumber ?? undefined,
            positionInMatch: slot.role
          });
        }
      }
    }
    this.homeBenchSlots.forEach(p => {
      if (p) {
        homePlayersPayload.push({
          playerId: p.playerId,
          isStarting: false,
          jerseyNumber: (p as any).jerseyNumber ?? undefined,
          positionInMatch: 'SUB'
        });
      }
    });

    const awayPlayersPayload: any[] = [];
    for (const r of this.awayPitchRows) {
      for (const slot of r) {
        if (slot.player) {
          awayPlayersPayload.push({
            playerId: slot.player.playerId,
            isStarting: true,
            jerseyNumber: (slot.player as any).jerseyNumber ?? undefined,
            positionInMatch: slot.role
          });
        }
      }
    }
    this.awayBenchSlots.forEach(p => {
      if (p) {
        awayPlayersPayload.push({
          playerId: p.playerId,
          isStarting: false,
          jerseyNumber: (p as any).jerseyNumber ?? undefined,
          positionInMatch: 'SUB'
        });
      }
    });

    this.isSubmitting = true;

    const dto = {
      sessionId: this.sessionId,
      format: FORMAT_MAP_ENUM[this.selectedFormat] ?? 1,
      matchDate: formatToLocalISO(this.matchDate || new Date()),
      location: this.location || 'Training Pitch',
      formation: this.homeFormation,
      awayFormation: this.awayFormation,
      homePlayers: homePlayersPayload,
      awayPlayers: awayPlayersPayload
    };

    this.matchService.createSessionMatch(dto).subscribe({
      next: (res: any) => {
        this.isSubmitting = false;
        const createdId = res?.data?.id ?? res?.id;
        this.toast.show(this.translate.instant('MATCH.SESSION_MATCH.SUCCESS', { Default: 'Internal Session Match created successfully!' }), 'success');

        if (createdId) {
          this.router.navigate(['/match', createdId]);
        } else {
          this.router.navigate(['/drills/sessions']);
        }
      },
      error: (err: any) => {
        this.isSubmitting = false;
        const msg = err?.error?.detail ?? err?.error?.message ?? this.translate.instant('MATCH.SESSION_MATCH.ERROR_CREATE', { Default: 'Failed to create session match.' });
        this.toast.show(msg, 'error');
        this.cdr.detectChanges();
      }
    });
  }
}
