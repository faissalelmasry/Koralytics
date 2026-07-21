import { Component, OnInit, AfterViewInit, OnDestroy, inject, ElementRef, ViewChild, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { Chart, registerables } from 'chart.js';
import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../shared/components/footer/footer';
import { PlayerCardComponent } from '../player-card/player-card';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { PlayerProfileService } from '../../../../core/services/player/player-profile.service';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { PlayerProfileModel } from '../../../../core/models/Player/player-profile-model';

Chart.register(...registerables);

@Component({
  selector: 'app-player-profile',
  standalone: true,
  imports: [CommonModule, NavbarComponent, Footer, PlayerCardComponent, LoadingSpinnerComponent, ScrollRevealDirective],
  templateUrl: './player-profile.component.html',
  styleUrls: ['./player-profile.component.css']
})
export class PlayerProfileComponent implements OnInit, AfterViewInit, OnDestroy {

  // ── Dependency injection ────────────────────────────────────
  private route = inject(ActivatedRoute);
  private profileService = inject(PlayerProfileService);
  private tokenStorage = inject(TokenStorageService);
  private cdr = inject(ChangeDetectorRef);

  // ── View children ───────────────────────────────────────────
  @ViewChild('countersSection') countersSection!: ElementRef<HTMLElement>;
  @ViewChild('radarCanvas') radarCanvas!: ElementRef<HTMLCanvasElement>;

  // ── State ───────────────────────────────────────────────────
  profile: PlayerProfileModel | null = null;
  isLoading = false;
  error = '';
  playerId: number | null = null;

  // ── Archetype overlay ───────────────────────────────────────
  showArchetypeOverlay = false;
  isCardFlipped = false;

  // ── Counter animation ───────────────────────────────────────
  animatedCounters = { matches: 0, goals: 0, assists: 0, motms: 0 };
  private countersAnimated = false;
  private observer?: IntersectionObserver;

  // ── Radar chart ─────────────────────────────────────────────
  private radarChart?: Chart<'radar'>;
  private chartInitialized = false;

  // ── Position pin mapping ────────────────────────────────────
  private readonly posPinMap: Record<string, { top: string; left: string }> = {
    'GK': { top: '85%', left: '8%' },
    'Goalkeeper': { top: '85%', left: '8%' },
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
    const paramId = this.route.snapshot.paramMap.get('playerId');

    if (paramId) {
      this.playerId = Number(paramId);
      this.loadProfile(this.playerId);
    } else {
      const token = this.tokenStorage.getAccessToken();
      if (!token) {
        this.error = 'Authentication required';
        return;
      }
      const decoded = this.decodeTokenPayload(token);
      if (!decoded) {
        this.error = 'Invalid session';
        return;
      }
      this.playerId = decoded.userId;
      this.loadProfile(this.playerId);
    }
  }

  ngAfterViewInit() {}

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
    this.showArchetypeOverlay = true;
  }

  flipArchetypeCard() {
    this.isCardFlipped = true;
  }

  closeArchetypeOverlay() {
    this.showArchetypeOverlay = false;
    this.isCardFlipped = false;
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

  get tierNeon(): string {
    const rating = this.profile?.playerCard?.overallRating ?? 0;
    if (rating >= 80) return '#ff6a00';
    if (rating >= 70) return '#ffd700';
    return '#c8ff4d';
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

  get archetypeLabel(): string {
    return this.profile?.archetypeText ?? 'Under Evaluation...';
  }

  // ── Data loading ────────────────────────────────────────────
  private loadProfile(id: number) {
    this.isLoading = true;
    this.error = '';
    this.profile = null;

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
