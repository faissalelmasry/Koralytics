import { Component, OnInit, AfterViewInit, OnDestroy, inject, ElementRef, ViewChild, ChangeDetectorRef, ChangeDetectionStrategy, NgZone, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Chart, registerables } from 'chart.js';
import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../shared/components/footer/footer';
import { PlayerCardComponent } from '../player-card/player-card';
import { TransferCanvasComponent } from '../transfer-canvas/transfer-canvas.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { FeatureLockComponent } from '../../../shared/components/feature-lock/feature-lock';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { CustomToggle } from '../../../../shared/components/custom-toggle/custom-toggle';
import { PlayerProfileService } from '../../../../core/services/player/player-profile.service';
import { PlayerCardService } from '../../../../core/services/player/player-card.service';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { PlayerProfileModel } from '../../../../core/models/Player/player-profile-model';
import { ScouterService } from '@core/services/Scouter/scouter.service';
import { NotificationService } from '@core/services/SignalR/notificationservice';
import { ToastService } from '../../../../core/services/Toast/toast';
import { PlayerDrillProgressionComponent } from '../../drills/player-drill-progression.component/player-drill-progression.component';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ParentService } from '../../../../core/services/parent/parent.service';

Chart.register(...registerables);

@Component({
  selector: 'app-player-profile',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    NavbarComponent,
    Footer,
    PlayerCardComponent,
    TransferCanvasComponent,
    LoadingSpinnerComponent,
    ScrollRevealDirective,
    FeatureLockComponent,
    CustomButtonComponent,
    ConfirmDialogComponent,
    CustomToggle,
    PlayerDrillProgressionComponent,
    TranslatePipe
  ],
  templateUrl: './player-profile.component.html',
  styleUrls: ['./player-profile.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
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
  private toastService = inject(ToastService);
  private translate = inject(TranslateService);
  private parentService = inject(ParentService);
  private ngZone = inject(NgZone);
  private destroyRef = inject(DestroyRef);

  // ── Scouter Follow / Shortlist State ─────────────────────────
  isScouter = false;
  isFollowing = false;
  isFollowLoading = false;
  isShortlisted = false;
  isShortlistLoading = false;
  currentScouterId: number | null = null;

  // ── View children ───────────────────────────────────────────
  @ViewChild('countersSection') countersSection!: ElementRef<HTMLElement>;
  @ViewChild('radarCanvas') radarCanvas!: ElementRef<HTMLCanvasElement>;

  // ── State ───────────────────────────────────────────────────
  profile: PlayerProfileModel | null = null;
  isLoading = false;
  isFetchingCard = false;
  imageError = false;
  error = '';
  playerId: number | null = null;
  loggedInUserId: number | null = null;

  // ── Archetype overlay ───────────────────────────────────────
  @ViewChild('archetypeCardElement') archetypeCardElement?: ElementRef<HTMLElement>;
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
    'GK': { top: '50%', left: '10%' },
    'Goalkeeper': { top: '50%', left: '10%' },
    'CB': { top: '50%', left: '22%' },
    'Center Back': { top: '50%', left: '22%' },
    'Centre Back': { top: '50%', left: '22%' },
    'LB': { top: '22%', left: '22%' },
    'Left Back': { top: '22%', left: '22%' },
    'RB': { top: '78%', left: '22%' },
    'Right Back': { top: '78%', left: '22%' },
    'LWB': { top: '18%', left: '34%' },
    'Left Wing Back': { top: '18%', left: '34%' },
    'RWB': { top: '82%', left: '34%' },
    'Right Wing Back': { top: '82%', left: '34%' },
    'CDM': { top: '50%', left: '38%' },
    'Defensive Midfielder': { top: '50%', left: '38%' },
    'CM': { top: '50%', left: '50%' },
    'Central Midfielder': { top: '50%', left: '50%' },
    'Midfielder': { top: '50%', left: '50%' },
    'LM': { top: '22%', left: '50%' },
    'Left Midfielder': { top: '22%', left: '50%' },
    'RM': { top: '78%', left: '50%' },
    'Right Midfielder': { top: '78%', left: '50%' },
    'CAM': { top: '50%', left: '64%' },
    'Attacking Midfielder': { top: '50%', left: '64%' },
    'LW': { top: '22%', left: '76%' },
    'Left Wing': { top: '22%', left: '76%' },
    'Left Winger': { top: '22%', left: '76%' },
    'RW': { top: '78%', left: '76%' },
    'Right Wing': { top: '78%', left: '76%' },
    'Right Winger': { top: '78%', left: '76%' },
    'ST': { top: '50%', left: '78%' },
    'Striker': { top: '50%', left: '78%' },
    'CF': { top: '50%', left: '76%' },
    'Center Forward': { top: '50%', left: '76%' },
    'Forward': { top: '50%', left: '78%' },
  };

  // ── Cached / Memoized Derived Properties ────────────────────
  tierClass = 'tier-base';
  isOwnProfile = false;
  tierNeon = '#c8ff4d';
  tierLogoUrl = 'images/logo/app-icon/logo-icon-dark.png';
  tierButtonVariant: 'accent' | 'amber' | 'gold' = 'accent';
  fullName = '';
  profileImageUrl: string | null = null;
  initials = 'P';
  primaryPosition = '';
  isGK = false;
  secondaryPositions: string[] = [];
  academyName = 'No Academy';
  preferredFoot = 'N/A';
  statusLabel = 'Available';
  statusClass = 'status-available';
  goalsPerMatch = '0.00';
  assistsPerMatch = '0.00';
  motmPercentage = '0';
  competitionBreakdown: { label: string; iconColor: string; stats: any }[] = [];
  archetypeTitle = 'AI ARCHETYPE';
  archetypeTextDescription = 'Under Evaluation...';
  daysUntilNextReveal = 0;
  nextRevealNotice = 'AI-powered playing style analysis';
  ownedPositionIds: string[] = [];
  primaryPositionId = '';
  secondaryPositionIds: string[] = [];

  // ── Lifecycle ───────────────────────────────────────────────
  ngOnInit() {
    this.translate.onLangChange
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.computeCompetitionBreakdown();
        this.cdr.markForCheck();
      });

    let userRoles: string[] = [];
    const user = this.tokenStorage.getUser();
    if (user) {
      this.loggedInUserId = user.userId;
      userRoles = user.roles || [];
    } else {
      const token = this.tokenStorage.getAccessToken();
      if (token) {
        const decoded = this.decodeTokenPayload(token);
        if (decoded) {
          this.loggedInUserId = decoded.userId;
          userRoles = decoded.roles || [];
        }
      }
    }

    const paramId = this.route.snapshot.paramMap.get('playerId');

    this.isScouter = userRoles.some(r => r.toLowerCase() === 'scouter');
    if (this.isScouter && this.loggedInUserId) {
      this.currentScouterId = this.loggedInUserId;
    }

    const isParent = userRoles.some(r => r.toLowerCase() === 'parent');
    if (isParent) {
      this.parentService.getMyChildren()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res: any) => {
            const children = res?.data || res || [];
            const hasElite = children.some((c: any) => c.isEliteTier || c.academyTier === 'Elite');
            const hasPro = children.some((c: any) => c.academyTier === 'Pro');
            if (hasElite) {
              this.tokenStorage.saveEffectiveTier('Elite');
            } else if (hasPro) {
              this.tokenStorage.saveEffectiveTier('Pro');
            }
            this.cdr.markForCheck();
          }
        });
    }

    if (paramId) {
      this.playerId = Number(paramId);
      this.loadProfile(this.playerId);
      this.logAndNotifyIfScouter(userRoles);
    } else if (this.loggedInUserId) {
      this.playerId = this.loggedInUserId;
      this.loadProfile(this.playerId);
    } else {
      this.error = 'Authentication required';
      this.cdr.markForCheck();
    }
  }

  ngAfterViewInit() { }

  ngOnDestroy() {
    this.observer?.disconnect();
    if (this.radarChart) {
      this.radarChart.destroy();
      this.radarChart = undefined;
    }
  }

  private computeDerivedState() {
    if (!this.profile) {
      this.tierClass = 'tier-base';
      this.isOwnProfile = false;
      this.tierNeon = '#c8ff4d';
      this.tierLogoUrl = 'images/logo/app-icon/logo-icon-dark.png';
      this.tierButtonVariant = 'accent';
      this.fullName = '';
      this.profileImageUrl = null;
      this.initials = 'P';
      this.primaryPosition = '';
      this.isGK = false;
      this.secondaryPositions = [];
      this.academyName = 'No Academy';
      this.preferredFoot = 'N/A';
      this.statusLabel = 'Available';
      this.statusClass = 'status-available';
      this.goalsPerMatch = '0.00';
      this.assistsPerMatch = '0.00';
      this.motmPercentage = '0';
      this.competitionBreakdown = [];
      this.archetypeTitle = 'AI ARCHETYPE';
      this.archetypeTextDescription = 'Under Evaluation...';
      this.daysUntilNextReveal = 0;
      this.nextRevealNotice = 'AI-powered playing style analysis';
      this.ownedPositionIds = [];
      this.primaryPositionId = '';
      this.secondaryPositionIds = [];
      return;
    }

    const rating = this.profile.playerCard?.overallRating ?? 0;
    if (rating >= 80) {
      this.tierClass = 'tier-elite';
      this.tierNeon = '#ff6a00';
      this.tierLogoUrl = 'images/logo/app-icon/logo-icon-orange.png';
      this.tierButtonVariant = 'amber';
    } else if (rating >= 70) {
      this.tierClass = 'tier-gold';
      this.tierNeon = '#ffd700';
      this.tierLogoUrl = 'images/logo/app-icon/logo-icon-yellow.png';
      this.tierButtonVariant = 'gold';
    } else {
      this.tierClass = 'tier-base';
      this.tierNeon = '#c8ff4d';
      this.tierLogoUrl = 'images/logo/app-icon/logo-icon-dark.png';
      this.tierButtonVariant = 'accent';
    }

    this.isOwnProfile = this.loggedInUserId !== null && this.loggedInUserId === this.playerId;
    this.fullName = `${this.profile.firstName} ${this.profile.lastName}`;
    this.profileImageUrl = this.profile.profileImageUrl || this.profile.playerCard?.profileImageUrl || null;

    const f = this.profile.firstName?.charAt(0) || '';
    const l = this.profile.lastName?.charAt(0) || '';
    this.initials = `${f}${l}`.toUpperCase() || 'P';

    const primaryObj = this.profile.positions.find(p => p.isPrimary);
    this.primaryPosition = primaryObj?.position ?? '';
    this.isGK = this.primaryPosition.toUpperCase() === 'GK';

    this.secondaryPositions = this.profile.positions.filter(p => !p.isPrimary).map(p => p.position);
    this.ownedPositionIds = this.profile.positions.map(p => p.position.toUpperCase());
    this.primaryPositionId = this.primaryPosition.toUpperCase();
    this.secondaryPositionIds = this.secondaryPositions.map(p => p.toUpperCase());

    this.academyName = this.profile.currentAcademy?.academyName ?? 'No Academy';
    this.preferredFoot = this.profile.playerCard?.preferredFoot ?? 'N/A';

    // Status Label & Class
    const status = this.profile.availabilityStatus;
    if (status === undefined || status === null) {
      this.statusLabel = 'Available';
    } else if (typeof status === 'number') {
      switch (status) {
        case 1: this.statusLabel = 'Available'; break;
        case 2: this.statusLabel = 'Injured'; break;
        case 3: this.statusLabel = 'Resting'; break;
        case 4: this.statusLabel = 'Suspended'; break;
        default: this.statusLabel = 'Available'; break;
      }
    } else {
      const strStatus = String(status).trim();
      const lower = strStatus.toLowerCase();
      if (lower === 'available' || lower === 'active' || lower === '1' || lower === '0') this.statusLabel = 'Available';
      else if (lower === 'injured' || lower === '2') this.statusLabel = 'Injured';
      else if (lower === 'resting' || lower === '3') this.statusLabel = 'Resting';
      else if (lower === 'suspended' || lower === '4') this.statusLabel = 'Suspended';
      else if (lower === 'transferred') this.statusLabel = 'Transferred';
      else this.statusLabel = strStatus.charAt(0).toUpperCase() + strStatus.slice(1);
    }

    const lowerLabel = this.statusLabel.toLowerCase();
    if (lowerLabel === 'available' || lowerLabel === 'active') this.statusClass = 'status-available';
    else if (lowerLabel === 'injured') this.statusClass = 'status-injured';
    else if (lowerLabel === 'resting') this.statusClass = 'status-resting';
    else if (lowerLabel === 'suspended') this.statusClass = 'status-suspended';
    else if (lowerLabel === 'transferred') this.statusClass = 'status-transferred';
    else this.statusClass = 'status-available';

    // Per match stats
    if (this.profile.totalMatches === 0) {
      this.goalsPerMatch = '0.00';
      this.assistsPerMatch = '0.00';
      this.motmPercentage = '0';
    } else {
      this.goalsPerMatch = (this.profile.totalGoals / this.profile.totalMatches).toFixed(2);
      this.assistsPerMatch = (this.profile.totalAssists / this.profile.totalMatches).toFixed(2);
      this.motmPercentage = Math.round((this.profile.totalMOTMs / this.profile.totalMatches) * 100).toString();
    }

    this.computeCompetitionBreakdown();

    // Archetype
    this.archetypeTitle = this.profile.playerCard?.archetypePlayerName || 'AI ARCHETYPE';
    this.archetypeTextDescription = this.profile.archetypeText || 'Under Evaluation...';

    // Days until next reveal
    const lastRevealed = this.profile.playerCard?.archetypeLastRevealedAt;
    if (lastRevealed) {
      const date = new Date(lastRevealed);
      if (!isNaN(date.getTime())) {
        const diffMs = Date.now() - date.getTime();
        const diffDays = diffMs / (1000 * 60 * 60 * 24);
        const remaining = 7 - diffDays;
        this.daysUntilNextReveal = remaining > 0 ? Math.ceil(remaining) : 0;
      } else {
        this.daysUntilNextReveal = 0;
      }
    } else {
      this.daysUntilNextReveal = 0;
    }

    if (this.daysUntilNextReveal > 0) {
      this.nextRevealNotice = `Next AI re-evaluation in ${this.daysUntilNextReveal} day${this.daysUntilNextReveal > 1 ? 's' : ''}`;
    } else {
      this.nextRevealNotice = 'AI-powered playing style analysis';
    }
  }

  private computeCompetitionBreakdown() {
    if (!this.profile) {
      this.competitionBreakdown = [];
      return;
    }
    this.competitionBreakdown = [
      { label: this.translate.instant('PLAYER.TRAINING_SESSIONS'), iconColor: '#c8ff4d', stats: this.profile.sessionStats },
      { label: this.translate.instant('PLAYER.FRIENDLY_MATCHES'), iconColor: '#ffd700', stats: this.profile.friendlyStats },
      { label: this.translate.instant('PLAYER.TOURNAMENTS'), iconColor: '#ff6a00', stats: this.profile.tournamentStats },
    ];
  }

  private logAndNotifyIfScouter(roles: string[]) {
    if (!this.playerId || !this.loggedInUserId || this.playerId === this.loggedInUserId) {
      return;
    }

    const isScouter = roles.some(r => r.toLowerCase() === 'scouter');

    if (isScouter) {
      this.scouterService.logProfileView(this.loggedInUserId, this.playerId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          error: (err) => console.error('Failed to log scouter profile view in backend:', err)
        });
    }
  }

  goToTimeline() {
    if (this.playerId) {
      this.router.navigate(['/player/timeline', this.playerId]);
    } else {
      this.router.navigate(['/player/timeline']);
    }
  }

  goToDrillTimeline() {
    if (this.playerId) {
      this.router.navigate(['/player/drill-timeline', this.playerId]);
    } else {
      this.router.navigate(['/player/drill-timeline']);
    }
  }

  goToAcademyComparison() {
    if (this.playerId) {
      this.router.navigate(['/player/academy-comparison', this.playerId]);
    } else {
      this.router.navigate(['/player/academy-comparison']);
    }
  }

  goToHighlights() {
    if (this.playerId) {
      this.router.navigate(['/player/highlights', this.playerId]);
    } else {
      this.router.navigate(['/player/highlights']);
    }
  }

  // ── Archetype overlay actions ───────────────────────────────
  openArchetypeOverlay() {
    this.isCardFlipped = false;
    this.revealError = '';
    this.showArchetypeOverlay = true;
    this.cdr.markForCheck();
  }

  flipArchetypeCard() {
    if (this.isCardFlipped || this.isRevealingArchetype || !this.playerId) return;

    this.isRevealingArchetype = true;
    this.revealError = '';
    this.cdr.markForCheck();

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
        this.computeDerivedState();
        this.isCardFlipped = true;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isRevealingArchetype = false;
        this.revealError = err?.error?.message || this.translate.instant('PLAYER.MESSAGES.REVEAL_ARCHETYPE_FAILED');
        this.cdr.markForCheck();
      }
    });
  }

  closeArchetypeOverlay() {
    this.showArchetypeOverlay = false;
    this.isCardFlipped = false;
    this.isRevealingArchetype = false;
    this.revealError = '';
    this.cdr.markForCheck();
  }

  async exportArchetypeToPdf(event: Event) {
    event.stopPropagation();
    if (!this.archetypeCardElement?.nativeElement) return;
    
    const card = this.archetypeCardElement.nativeElement;

    try {
      const htmlToImage = await import('html-to-image');

      // Temporarily remove 3D rotation to ensure flat rasterization
      const oldTransform = card.style.transform;
      card.style.transform = 'none';
      
      const frontFace = card.querySelector('.card-face-front') as HTMLElement;
      const backFace = card.querySelector('.card-face-back') as HTMLElement;
      
      const oldFrontDisplay = frontFace ? frontFace.style.display : '';
      const oldBackDisplay = backFace ? backFace.style.display : '';

      if (this.isCardFlipped) {
        if (backFace) backFace.style.display = 'none';
      } else {
        if (frontFace) frontFace.style.display = 'none';
      }

      // Generate the image as PNG
      const dataUrl = await htmlToImage.toPng(card, {
        quality: 1,
        pixelRatio: 2,
        backgroundColor: 'transparent'
      });

      // Restore DOM state
      card.style.transform = oldTransform;
      if (frontFace) frontFace.style.display = oldFrontDisplay;
      if (backFace) {
        backFace.style.display = oldBackDisplay;
      }

      // Download as PNG
      const link = document.createElement('a');
      const playerName = this.profile?.firstName ? `${this.profile.firstName}-Archetype` : 'Player-Archetype';
      link.download = `${playerName}.png`;
      link.href = dataUrl;
      link.click();

    } catch (e) {
      console.error('Error generating Archetype PNG', e);
    }
  }

  fetchPlayerCard() {
    if (!this.playerId || this.isFetchingCard) return;
    this.isFetchingCard = true;
    this.cdr.markForCheck();

    this.playerCardService.getPlayerCard(this.playerId).subscribe({
      next: (card) => {
        if (this.profile) {
          this.profile.playerCard = { ...card };
        }
        this.computeDerivedState();
        this.isFetchingCard = false;

        if (this.radarChart) {
          this.radarChart.destroy();
          this.radarChart = undefined;
        }
        this.chartInitialized = false;

        this.toastService.show(this.translate.instant('PLAYER.MESSAGES.REFRESH_SUCCESS'), 'success');
        this.cdr.markForCheck();

        setTimeout(() => {
          this.initRadarChart();
        }, 0);
      },
      error: (err) => {
        this.isFetchingCard = false;
        this.toastService.show(
          err?.status === 404 ? 'Unable to generate player card' : 'Failed to fetch player card',
          'error'
        );
        this.cdr.markForCheck();
      }
    });
  }

  // ── Position pin helper ─────────────────────────────────────
  getPosPinStyle(pos: string): { top: string; left: string } {
    return this.posPinMap[pos] ?? this.posPinMap[pos.toUpperCase()] ?? { top: '50%', left: '54%' };
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
    this.cdr.markForCheck();
  }

  openUpdatePrimaryModal() {
    this.positionError = '';
    this.selectedPosition = null;
    this.modalMode = 'UPDATE_PRIMARY';
    this.showPositionModal = true;
    this.cdr.markForCheck();
  }

  openRemovePositionModal() {
    this.positionError = '';
    this.selectedPosition = null;
    this.modalMode = 'REMOVE';
    this.showPositionModal = true;
    this.cdr.markForCheck();
  }

  closePositionModal() {
    this.showPositionModal = false;
    this.selectedPosition = null;
    this.positionError = '';
    this.cdr.markForCheck();
  }

  selectPositionNode(posId: string) {
    if (!this.isPositionSelectable(posId)) return;
    this.selectedPosition = posId;
    this.cdr.markForCheck();
  }

  confirmPositionSelection() {
    if (!this.selectedPosition || !this.playerId) {
      this.positionError = 'Please select a position first.';
      this.cdr.markForCheck();
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
    this.cdr.markForCheck();
  }

  onConfirmAction() {
    this.showConfirmDialog = false;

    if (!this.selectedPosition || !this.playerId || !this.pendingConfirmMode) return;

    this.isSavingPosition = true;
    this.positionError = '';
    this.cdr.markForCheck();

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
    this.cdr.markForCheck();
  }

  onCancelAction() {
    this.showConfirmDialog = false;
    this.pendingConfirmMode = null;
    this.cdr.markForCheck();
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
    this.cdr.markForCheck();

    if (this.radarChart) {
      this.radarChart.destroy();
      this.radarChart = undefined;
    }
    this.chartInitialized = false;
    this.countersAnimated = false;

    this.profileService.getPlayerProfile(id).subscribe({
      next: (profile) => {
        this.profile = profile;
        this.computeDerivedState();
        this.isLoading = false;
        if (this.isScouter && this.currentScouterId && this.playerId && !this.isOwnProfile) {
          this.checkFollowStatus();
          this.checkShortlistStatus();
        }
        this.cdr.markForCheck();
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
        this.cdr.markForCheck();
      }
    });
  }

  private checkFollowStatus() {
    if (!this.currentScouterId || !this.playerId) return;
    this.scouterService.isFollowing(this.currentScouterId, this.playerId).subscribe({
      next: (isFollowing: boolean) => {
        this.isFollowing = !!isFollowing;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Failed to check follow status:', err);
      }
    });
  }

  private checkShortlistStatus() {
    if (!this.currentScouterId || !this.playerId) return;
    this.scouterService.isShortlisted(this.currentScouterId, this.playerId).subscribe({
      next: (isShortlisted: boolean) => {
        this.isShortlisted = !!isShortlisted;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Failed to check shortlist status:', err);
      }
    });
  }

  toggleShortlist() {
    if (!this.currentScouterId || !this.playerId || this.isShortlistLoading) return;

    this.isShortlistLoading = true;
    this.cdr.markForCheck();

    if (this.isShortlisted) {
      this.scouterService.removeFromShortlist(this.currentScouterId, this.playerId).subscribe({
        next: () => {
          this.isShortlisted = false;
          this.isShortlistLoading = false;
          this.toastService.show(this.translate.instant('PLAYER.MESSAGES.SHORTLIST_REMOVE_SUCCESS'), 'info');
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.isShortlistLoading = false;
          console.error('Failed to remove from shortlist:', err);
          this.toastService.show(this.translate.instant('PLAYER.MESSAGES.SHORTLIST_REMOVE_FAILED'), 'error');
          this.cdr.markForCheck();
        }
      });
    } else {
      this.scouterService.addToShortlist(this.currentScouterId, this.playerId).subscribe({
        next: () => {
          this.isShortlisted = true;
          this.isShortlistLoading = false;
          this.toastService.show(this.translate.instant('PLAYER.MESSAGES.SHORTLIST_ADD_SUCCESS'), 'success');
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.isShortlistLoading = false;
          console.error('Failed to add to shortlist:', err);
          this.toastService.show(this.translate.instant('PLAYER.MESSAGES.SHORTLIST_ADD_FAILED'), 'error');
          this.cdr.markForCheck();
        }
      });
    }
  }

  toggleFollow() {
    if (!this.currentScouterId || !this.playerId || this.isFollowLoading) return;

    this.isFollowLoading = true;
    this.cdr.markForCheck();

    if (this.isFollowing) {
      this.scouterService.unfollowPlayer(this.currentScouterId, this.playerId).subscribe({
        next: () => {
          this.isFollowing = false;
          this.isFollowLoading = false;
          this.toastService.show(this.translate.instant('PLAYER.MESSAGES.UNFOLLOW_SUCCESS'), 'info');
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.isFollowLoading = false;
          console.error('Failed to unfollow player:', err);
          this.toastService.show(this.translate.instant('PLAYER.MESSAGES.UNFOLLOW_FAILED'), 'error');
          this.cdr.markForCheck();
        }
      });
    } else {
      this.scouterService.followPlayer(this.currentScouterId, this.playerId).subscribe({
        next: () => {
          this.isFollowing = true;
          this.isFollowLoading = false;
          this.toastService.show(this.translate.instant('PLAYER.MESSAGES.FOLLOW_SUCCESS'), 'success');
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.isFollowLoading = false;
          console.error('Failed to follow player:', err);
          this.toastService.show(this.translate.instant('PLAYER.MESSAGES.FOLLOW_FAILED'), 'error');
          this.cdr.markForCheck();
        }
      });
    }
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
        layout: {
          padding: {
            top: 20,
            bottom: 20,
            left: 25,
            right: 25
          }
        },
        plugins: { legend: { display: false } },
        scales: {
          r: {
            angleLines: { color: 'rgba(255, 255, 255, 0.1)' },
            grid: { color: 'rgba(255, 255, 255, 0.1)' },
            pointLabels: {
              color: '#808897',
              font: { size: 10, weight: 'bold' }
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
      this.cdr.markForCheck();
      return;
    }

    this.animateValue(0, this.profile.totalMatches, 800, v => this.animatedCounters.matches = v);
    this.animateValue(0, this.profile.totalGoals, 900, v => this.animatedCounters.goals = v);
    this.animateValue(0, this.profile.totalAssists, 1000, v => this.animatedCounters.assists = v);
    this.animateValue(0, this.profile.totalMOTMs, 1100, v => this.animatedCounters.motms = v);
  }

  private animateValue(start: number, end: number, duration: number, callback: (v: number) => void) {
    this.ngZone.runOutsideAngular(() => {
      const startTime = performance.now();
      const easeOutCubic = (t: number) => 1 - Math.pow(1 - t, 3);

      const tick = (now: number) => {
        const p = Math.min((now - startTime) / duration, 1);
        const eased = easeOutCubic(p);
        const val = Math.round(start + eased * (end - start));
        callback(val);
        this.cdr.markForCheck();
        if (p < 1) requestAnimationFrame(tick);
      };
      requestAnimationFrame(tick);
    });
  }
}
