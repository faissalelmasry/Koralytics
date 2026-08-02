import { Component, OnInit, AfterViewInit, OnDestroy, inject, ElementRef, ViewChild, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Chart, registerables } from 'chart.js';
import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../shared/components/footer/footer';
import { PlayerCardComponent } from '../player-card/player-card';
import { TransferCanvasComponent } from '../transfer-canvas/transfer-canvas.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { CustomToggle } from '../../../../shared/components/custom-toggle/custom-toggle';
import { PlayerProfileService } from '../../../../core/services/player/player-profile.service';
import { PlayerCardService } from '../../../../core/services/player/player-card.service';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { PlayerProfileModel } from '../../../../core/models/Player/player-profile-model';
import { ScouterService } from '@core/services/Scouter/scouter.service';
import { NotificationService } from '@core/services/SignalR/notificationservice';

Chart.register(...registerables);

@Component({
  selector: 'app-player-profile',
  standalone: true,
  imports: [CommonModule, NavbarComponent, Footer, PlayerCardComponent, TransferCanvasComponent, LoadingSpinnerComponent, ScrollRevealDirective, CustomButtonComponent, ConfirmDialogComponent, CustomToggle],
  templateUrl: './player-profile.component.html',
  styleUrls: ['./player-profile.component.css']
})
export class PlayerProfileComponent implements OnInit, AfterViewInit, OnDestroy {

  // ── Dependency injection ────────────────────────────────────
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private profileService = inject(PlayerProfileService);
  private playerCardService = inject(PlayerCardService);
  private tokenStorage = inject(TokenStorageService);
  private cdr = inject(ChangeDetectorRef);
  private scouterService = inject(ScouterService);
  private notificationService = inject(NotificationService);

  // ── View children ───────────────────────────────────────────
  @ViewChild('countersSection') countersSection!: ElementRef<HTMLElement>;
  @ViewChild('radarCanvas') radarCanvas!: ElementRef<HTMLCanvasElement>;

  // ── State ───────────────────────────────────────────────────
  profile: PlayerProfileModel | null = null;
  isLoading = false;
  isFetchingCard = false;
  error = '';
  playerId: number | null = null;
  loggedInUserId: number | null = null;

  // ── Archetype overlay ───────────────────────────────────────
  showArchetypeOverlay = false;
  isCardFlipped = false;
  isRevealingArchetype = false;
  revealError = '';

  // ── Position modal ──────────────────────────────────────────
  showPositionModal = false;
  modalMode: 'ADD' | 'UPDATE_PRIMARY' | 'REMOVE' = 'ADD';
  selectedPosition: string | null = null;
  setAsPrimaryCheck = false;
  isSavingPosition = false;
  positionError = '';

  // ── Confirm dialog ──────────────────────────────────────────
  showConfirmDialog = false;
  confirmTitle = '';
  confirmMessage = '';
  confirmActionText = 'Confirm';
  private pendingConfirmMode: 'ADD' | 'UPDATE_PRIMARY' | 'REMOVE' | null = null;

  // ── All pitch positions for the modal selector ──────────────
  readonly allPitchPositions = [
    { id: 'LW', name: 'LW', top: '22%', left: '78%' },
    { id: 'ST', name: 'ST', top: '50%', left: '80%' },
    { id: 'RW', name: 'RW', top: '78%', left: '78%' },
    { id: 'CAM', name: 'CAM', top: '50%', left: '62%' },
    { id: 'LM', name: 'LM', top: '20%', left: '50%' },
    { id: 'CM', name: 'CM', top: '50%', left: '50%' },
    { id: 'RM', name: 'RM', top: '80%', left: '50%' },
    { id: 'CDM', name: 'CDM', top: '50%', left: '38%' },
    { id: 'LB', name: 'LB', top: '20%', left: '22%' },
    { id: 'CB', name: 'CB', top: '50%', left: '22%' },
    { id: 'RB', name: 'RB', top: '80%', left: '22%' },
    { id: 'GK', name: 'GK', top: '50%', left: '8%' },
  ];

  // ── Counter animation ───────────────────────────────────────
  animatedCounters = { matches: 0, goals: 0, assists: 0, motms: 0 };
  private countersAnimated = false;
  private observer?: IntersectionObserver;

  // ── Radar chart ─────────────────────────────────────────────
  private radarChart?: Chart<'radar'>;
  private chartInitialized = false;

  // ── Position pin mapping ────────────────────────────────────
  private readonly posPinMap: Record<string, { top: string; left: string }> = {
    'GK': { top: '50%', left: '8%' },
    'Goalkeeper': { top: '50%', left: '8%' },
    'CB': { top: '50%', left: '20%' },
    'Center Back': { top: '50%', left: '20%' },
    'Centre Back': { top: '50%', left: '20%' },
    'LB': { top: '22%', left: '22%' },
    'Left Back': { top: '22%', left: '22%' },
    'RB': { top: '78%', left: '22%' },
    'Right Back': { top: '78%', left: '22%' },
    'LWB': { top: '18%', left: '35%' },
    'Left Wing Back': { top: '18%', left: '35%' },
    'RWB': { top: '82%', left: '35%' },
    'Right Wing Back': { top: '82%', left: '35%' },
    'CDM': { top: '50%', left: '38%' },
    'Defensive Midfielder': { top: '50%', left: '38%' },
    'CM': { top: '50%', left: '54%' },
    'Central Midfielder': { top: '50%', left: '54%' },
    'Midfielder': { top: '50%', left: '54%' },
    'LM': { top: '25%', left: '55%' },
    'Left Midfielder': { top: '25%', left: '55%' },
    'RM': { top: '75%', left: '55%' },
    'Right Midfielder': { top: '75%', left: '55%' },
    'CAM': { top: '50%', left: '66%' },
    'Attacking Midfielder': { top: '50%', left: '66%' },
    'LW': { top: '20%', left: '74%' },
    'Left Wing': { top: '20%', left: '74%' },
    'Left Winger': { top: '20%', left: '74%' },
    'RW': { top: '80%', left: '74%' },
    'Right Wing': { top: '80%', left: '74%' },
    'Right Winger': { top: '80%', left: '74%' },
    'ST': { top: '50%', left: '84%' },
    'Striker': { top: '50%', left: '84%' },
    'CF': { top: '50%', left: '78%' },
    'Center Forward': { top: '50%', left: '78%' },
    'Forward': { top: '50%', left: '84%' },
  };

  // ── Lifecycle ───────────────────────────────────────────────
  ngOnInit() {
    let userRoles: string[] = [];
    const token = this.tokenStorage.getAccessToken();
    if (token) {
      const decoded = this.decodeTokenPayload(token);
      if (decoded) {
        this.loggedInUserId = decoded.userId;
        userRoles = decoded.roles;
      }
    }
    

    const paramId = this.route.snapshot.paramMap.get('playerId');

    if (paramId) {
      this.playerId = Number(paramId);
      this.loadProfile(this.playerId);
      this.logAndNotifyIfScouter(userRoles);
    } else if (this.loggedInUserId) {
      this.playerId = this.loggedInUserId;
      this.loadProfile(this.playerId);
    } else {
      this.error = 'Authentication required';
    }
  }
private logAndNotifyIfScouter(roles: string[]) {
    if (!this.playerId || !this.loggedInUserId || this.playerId === this.loggedInUserId) {
      return; 
    }

    const isScouter = roles.some(r => r.toLowerCase() === 'scouter');

    if (isScouter) {
      this.scouterService.logProfileView(this.loggedInUserId, this.playerId).subscribe({
        error: (err) => console.error('Failed to log scouter profile view in backend:', err)
      });
    }
  }
  ngAfterViewInit() { }

  goToTimeline() {
    if (this.playerId) {
      this.router.navigate(['/player/timeline', this.playerId]);
    } else {
      this.router.navigate(['/player/timeline']);
    }
  }

  ngOnDestroy() {
    this.observer?.disconnect();
    if (this.radarChart) {
      this.radarChart.destroy();
      this.radarChart = undefined;
    }
  }

  // ── Archetype overlay actions ───────────────────────────────
  openArchetypeOverlay() {
    this.isCardFlipped = false;
    this.revealError = '';
    this.showArchetypeOverlay = true;
  }

  flipArchetypeCard() {
    if (this.isCardFlipped || this.isRevealingArchetype || !this.playerId) return;

    this.isRevealingArchetype = true;
    this.revealError = '';

    this.playerCardService.revealArchetype(this.playerId).subscribe({
      next: (res) => {
        this.isRevealingArchetype = false;
        if (this.profile) {
          this.profile.archetypeText = res.archetypeText;
          if (this.profile.playerCard) {
            this.profile.playerCard.archetypePlayerName = res.archetypePlayerName;
            this.profile.playerCard.archetypeLastRevealedAt = res.archetypeLastRevealedAt;
          }
        }
        this.isCardFlipped = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isRevealingArchetype = false;
        this.revealError = err?.error?.message || 'Failed to reveal archetype. Please try again.';
        this.cdr.detectChanges();
      }
    });
  }

  closeArchetypeOverlay() {
    this.showArchetypeOverlay = false;
    this.isCardFlipped = false;
    this.isRevealingArchetype = false;
    this.revealError = '';
  }

  fetchPlayerCard() {
    if (!this.playerId || this.isFetchingCard) return;
    this.isFetchingCard = true;

    this.playerCardService.getPlayerCard(this.playerId).subscribe({
      next: (card) => {
        if (this.profile) {
          this.profile.playerCard = card;
        }
        this.isFetchingCard = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isFetchingCard = false;
        this.error = err?.status === 404
          ? 'Unable to generate player card'
          : 'Failed to fetch player card';
      }
    });
  }

  // ── Position pin helper ─────────────────────────────────────
  getPosPinStyle(pos: string): { top: string; left: string } {
    return this.posPinMap[pos] ?? this.posPinMap[pos.toUpperCase()] ?? { top: '50%', left: '54%' };
  }

  // ── Computed getters ────────────────────────────────────────
  get tierClass(): string {
    const rating = this.profile?.playerCard?.overallRating ?? 0;
    if (rating >= 80) return 'tier-elite';
    if (rating >= 70) return 'tier-gold';
    return 'tier-base';
  }

  get isOwnProfile(): boolean {
    return this.loggedInUserId !== null && this.loggedInUserId === this.playerId;
  }

  get tierNeon(): string {
    const rating = this.profile?.playerCard?.overallRating ?? 0;
    if (rating >= 80) return '#ff6a00';
    if (rating >= 70) return '#ffd700';
    return '#c8ff4d';
  }

  get tierLogoUrl(): string {
    const rating = this.profile?.playerCard?.overallRating ?? 0;
    if (rating >= 80) return 'images/logo/app-icon/logo-icon-orange.png';
    if (rating >= 70) return 'images/logo/app-icon/logo-icon-yellow.png';
    return 'images/logo/app-icon/logo-icon-dark.png';
  }

  get tierButtonVariant(): 'accent' | 'amber' | 'gold' {
    const rating = this.profile?.playerCard?.overallRating ?? 0;
    if (rating >= 80) return 'amber';
    if (rating >= 70) return 'gold';
    return 'accent';
  }

  get fullName(): string {
    if (!this.profile) return '';
    return `${this.profile.firstName} ${this.profile.lastName}`;
  }

  get initials(): string {
    if (!this.profile) return '';
    return (this.profile.firstName[0] + this.profile.lastName[0]).toUpperCase();
  }

  get primaryPosition(): string {
    return this.profile?.positions.find(p => p.isPrimary)?.position ?? '';
  }

  get isGK(): boolean {
    return this.primaryPosition?.toUpperCase() === 'GK';
  }

  get secondaryPositions(): string[] {
    return this.profile?.positions.filter(p => !p.isPrimary).map(p => p.position) ?? [];
  }

  get academyName(): string {
    return this.profile?.currentAcademy?.academyName ?? 'No Academy';
  }

  get preferredFoot(): string {
    return this.profile?.playerCard?.preferredFoot ?? 'N/A';
  }

  get statusLabel(): string {
    const status = this.profile?.availabilityStatus;
    if (status === 0) return 'Active';
    if (status === 1) return 'Transferred';
    if (status === 2) return 'Injured';
    return 'Unknown';
  }

  get statusClass(): string {
    const status = this.profile?.availabilityStatus;
    if (status === 0) return 'status-available';
    if (status === 1) return 'status-transferred';
    if (status === 2) return 'status-injured';
    return '';
  }

  get goalsPerMatch(): string {
    if (!this.profile || this.profile.totalMatches === 0) return '0.00';
    return (this.profile.totalGoals / this.profile.totalMatches).toFixed(2);
  }

  get assistsPerMatch(): string {
    if (!this.profile || this.profile.totalMatches === 0) return '0.00';
    return (this.profile.totalAssists / this.profile.totalMatches).toFixed(2);
  }

  get motmPercentage(): string {
    if (!this.profile || this.profile.totalMatches === 0) return '0';
    return Math.round((this.profile.totalMOTMs / this.profile.totalMatches) * 100).toString();
  }

  get competitionBreakdown() {
    if (!this.profile) return [];
    return [
      { label: 'Training Sessions', iconColor: '#c8ff4d', stats: this.profile.sessionStats },
      { label: 'Friendly Matches', iconColor: '#ffd700', stats: this.profile.friendlyStats },
      { label: 'Tournaments', iconColor: '#ff6a00', stats: this.profile.tournamentStats },
    ];
  }

  get archetypeTitle(): string {
    return this.profile?.playerCard?.archetypePlayerName || 'AI ARCHETYPE';
  }

  get archetypeTextDescription(): string {
    return this.profile?.archetypeText || 'Under Evaluation...';
  }

  get daysUntilNextReveal(): number {
    const lastRevealed = this.profile?.playerCard?.archetypeLastRevealedAt;
    if (!lastRevealed) return 0;
    const date = new Date(lastRevealed);
    if (isNaN(date.getTime())) return 0;
    const diffMs = Date.now() - date.getTime();
    const diffDays = diffMs / (1000 * 60 * 60 * 24);
    const remaining = 7 - diffDays;
    return remaining > 0 ? Math.ceil(remaining) : 0;
  }

  get nextRevealNotice(): string {
    const days = this.daysUntilNextReveal;
    if (days > 0) {
      return `Next AI re-evaluation in ${days} day${days > 1 ? 's' : ''}`;
    }
    return 'AI-powered playing style analysis';
  }

  get ownedPositionIds(): string[] {
    return this.profile?.positions.map(p => p.position.toUpperCase()) ?? [];
  }

  get primaryPositionId(): string {
    return this.primaryPosition.toUpperCase();
  }

  get secondaryPositionIds(): string[] {
    return this.profile?.positions.filter(p => !p.isPrimary).map(p => p.position.toUpperCase()) ?? [];
  }

  isPositionSelectable(posId: string): boolean {
    if (this.modalMode === 'UPDATE_PRIMARY' || this.modalMode === 'REMOVE') {
      return this.secondaryPositionIds.includes(posId);
    }
    return !this.ownedPositionIds.includes(posId);
  }

  isPositionDisabled(posId: string): boolean {
    return !this.isPositionSelectable(posId);
  }

  // ── Position modal actions ──────────────────────────────────
  openAddPositionModal() {
    this.positionError = '';
    this.selectedPosition = null;
    this.setAsPrimaryCheck = false;
    this.modalMode = 'ADD';
    this.showPositionModal = true;
  }

  openUpdatePrimaryModal() {
    this.positionError = '';
    this.selectedPosition = null;
    this.modalMode = 'UPDATE_PRIMARY';
    this.showPositionModal = true;
  }

  openRemovePositionModal() {
    this.positionError = '';
    this.selectedPosition = null;
    this.modalMode = 'REMOVE';
    this.showPositionModal = true;
  }

  closePositionModal() {
    this.showPositionModal = false;
    this.selectedPosition = null;
    this.positionError = '';
  }

  selectPositionNode(posId: string) {
    if (!this.isPositionSelectable(posId)) return;
    this.selectedPosition = posId;
  }

  confirmPositionSelection() {
    if (!this.selectedPosition || !this.playerId) {
      this.positionError = 'Please select a position first.';
      return;
    }

    this.positionError = '';

    if (this.modalMode === 'ADD') {
      this.confirmTitle = 'Add Position';
      this.confirmMessage = `Add "${this.selectedPosition}" to your player profile${this.setAsPrimaryCheck ? ' and set it as your primary position' : ''}?`;
      this.confirmActionText = 'Add Position';
    } else if (this.modalMode === 'REMOVE') {
      this.confirmTitle = 'Remove Position';
      this.confirmMessage = `Remove "${this.selectedPosition}" from your player profile?`;
      this.confirmActionText = 'Remove Position';
    } else {
      this.confirmTitle = 'Change Primary Position';
      this.confirmMessage = `Set "${this.selectedPosition}" as your new primary position?`;
      this.confirmActionText = 'Change Primary';
    }

    this.pendingConfirmMode = this.modalMode;
    this.showConfirmDialog = true;
  }

  onConfirmAction() {
    this.showConfirmDialog = false;

    if (!this.selectedPosition || !this.playerId || !this.pendingConfirmMode) return;

    this.isSavingPosition = true;
    this.positionError = '';

    if (this.pendingConfirmMode === 'ADD') {
      this.profileService.addPlayerPosition(this.playerId, {
        position: this.selectedPosition,
        isPrimary: this.setAsPrimaryCheck,
      }).subscribe({
        next: () => this.onPositionSaved(),
        error: (err) => this.onPositionError(err, 'Failed to add position'),
      });
    } else if (this.pendingConfirmMode === 'REMOVE') {
      this.profileService.removePlayerPosition(this.playerId, {
        position: this.selectedPosition,
      }).subscribe({
        next: () => this.onPositionSaved(),
        error: (err) => this.onPositionError(err, 'Failed to remove position'),
      });
    } else {
      this.profileService.updatePrimaryPosition(this.playerId, {
        position: this.selectedPosition,
      }).subscribe({
        next: () => this.onPositionSaved(),
        error: (err) => this.onPositionError(err, 'Failed to update primary position'),
      });
    }
  }

  private onPositionSaved() {
    this.isSavingPosition = false;
    this.pendingConfirmMode = null;
    this.closePositionModal();
    this.reloadProfile();
  }

  private onPositionError(err: any, fallback: string) {
    this.isSavingPosition = false;
    this.pendingConfirmMode = null;
    this.positionError = err?.error?.message ?? fallback;
  }

  onCancelAction() {
    this.showConfirmDialog = false;
    this.pendingConfirmMode = null;
  }

  private reloadProfile() {
    if (this.playerId) {
      this.loadProfile(this.playerId);
    }
  }

  // ── Data loading ────────────────────────────────────────────
  private loadProfile(id: number) {
    this.isLoading = true;
    this.error = '';
    this.profile = null;

    if (this.radarChart) {
      this.radarChart.destroy();
      this.radarChart = undefined;
    }
    this.chartInitialized = false;
    this.countersAnimated = false;

    this.profileService.getPlayerProfile(id).subscribe({
      next: (profile) => {
        this.profile = profile;
        this.isLoading = false;
        this.cdr.detectChanges();
        setTimeout(() => {
          this.setupCountersObserver();
          this.initRadarChart();
        }, 0);
      },
      error: (err) => {
        this.isLoading = false;
        this.error = err?.status === 404
          ? 'Player not found'
          : 'Failed to load player profile';
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

  // ── Radar chart ─────────────────────────────────────────────
  private initRadarChart() {
    if (this.chartInitialized || !this.radarCanvas || !this.profile?.playerCard) return;
    if (this.isGK) return;

    const card = this.profile.playerCard;
    const ctx = this.radarCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    this.radarChart = new Chart(ctx, {
      type: 'radar',
      data: {
        labels: ['PAC', 'DRI', 'SHO', 'DEF', 'PAS', 'PHY'],
        datasets: [{
          data: [
            Math.round(card.paceRating),
            Math.round(card.dribblingRating),
            Math.round(card.shootingRating),
            Math.round(card.defendingRating),
            Math.round(card.passingRating),
            Math.round(card.physicalRating),
          ],
          backgroundColor: this.radarFillColor(),
          borderColor: this.tierNeon,
          borderWidth: 2,
          pointBackgroundColor: this.tierNeon,
          pointRadius: 2.5,
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          r: {
            angleLines: { color: 'rgba(255, 255, 255, 0.1)' },
            grid: { color: 'rgba(255, 255, 255, 0.1)' },
            pointLabels: {
              color: '#808897',
              font: { size: 9, weight: 'bold' }
            },
            ticks: { display: false },
            suggestedMin: 30,
            suggestedMax: 90,
          }
        }
      }
    });

    this.chartInitialized = true;
  }

  private radarFillColor(): string {
    const rating = this.profile?.playerCard?.overallRating ?? 0;
    if (rating >= 80) return 'rgba(255, 94, 0, 0.25)';
    if (rating >= 70) return 'rgba(255, 215, 0, 0.25)';
    return 'rgba(200, 255, 77, 0.25)';
  }

  // ── Counter animation ───────────────────────────────────────
  private setupCountersObserver() {
    if (!this.countersSection) return;

    this.observer = new IntersectionObserver(
      (entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting && !this.countersAnimated) {
            this.countersAnimated = true;
            this.animateCounters();
          }
        });
      },
      { threshold: 0.3 }
    );

    this.observer.observe(this.countersSection.nativeElement);
  }

  private animateCounters() {
    if (!this.profile) return;
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    if (reduceMotion) {
      this.animatedCounters = {
        matches: this.profile.totalMatches,
        goals: this.profile.totalGoals,
        assists: this.profile.totalAssists,
        motms: this.profile.totalMOTMs,
      };
      this.cdr.detectChanges();
      return;
    }

    this.animateValue(0, this.profile.totalMatches, 800, v => this.animatedCounters.matches = v);
    this.animateValue(0, this.profile.totalGoals, 900, v => this.animatedCounters.goals = v);
    this.animateValue(0, this.profile.totalAssists, 1000, v => this.animatedCounters.assists = v);
    this.animateValue(0, this.profile.totalMOTMs, 1100, v => this.animatedCounters.motms = v);
  }

  private animateValue(start: number, end: number, duration: number, callback: (v: number) => void) {
    const startTime = performance.now();
    const easeOutCubic = (t: number) => 1 - Math.pow(1 - t, 3);

    const tick = (now: number) => {
      const p = Math.min((now - startTime) / duration, 1);
      const eased = easeOutCubic(p);
      callback(Math.round(start + eased * (end - start)));
      this.cdr.detectChanges();
      if (p < 1) requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  }
}
