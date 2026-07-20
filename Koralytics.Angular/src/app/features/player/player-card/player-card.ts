import { Component, Input, OnInit, AfterViewInit, ElementRef, ViewChild, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PlayerCardModel } from '../../../../core/models/Player/player-card-model';
import { PlayerCardService } from '../../../../core/services/player/player-card.service';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';

@Component({
  selector: 'app-player-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './player-card.html',
  styleUrls: ['./player-card.css']
})
export class PlayerCardComponent implements OnInit, AfterViewInit {
  private playerCardService = inject(PlayerCardService);
  private tokenStorage = inject(TokenStorageService);
  private cdr = inject(ChangeDetectorRef);

  @Input() player?: PlayerCardModel;

  @ViewChild('cardElement') cardElement!: ElementRef<HTMLDivElement>;

  isLoading = false;
  error = '';
  animatedRating = 0;
  isFlipped = false;
  stars = [1, 2, 3, 4, 5];

  get tierClass(): string {
    const rating = this.player?.overallRating ?? 0;
    if (rating >= 80) return 'tier-elite';
    if (rating >= 70) return 'tier-gold';
    return 'tier-base';
  }

  get isGK(): boolean {
    return this.player?.position?.toUpperCase() === 'GK';
  }

  get statsList() {
    if (!this.player) return [];
    return [
      { label: 'PAC', value: Math.round(this.player.paceRating) },
      { label: 'DRI', value: Math.round(this.player.dribblingRating) },
      { label: 'SHO', value: Math.round(this.player.shootingRating) },
      { label: 'DEF', value: Math.round(this.player.defendingRating) },
      { label: 'PAS', value: Math.round(this.player.passingRating) },
      { label: 'PHY', value: Math.round(this.player.physicalRating) }
    ];
  }

  ngOnInit() {
    if (this.player) return;

    const token = this.tokenStorage.getAccessToken();
    if (!token) return;

    const decoded = this.decodeTokenPayload(token);
    if (!decoded) return;

    const { userId, roles } = decoded;
    if (!roles.includes('Player')) return;

    this.isLoading = true;
    this.playerCardService.getPlayerCard(userId).subscribe({
      next: (card) => {
        this.player = card;
        this.isLoading = false;
        this.cdr.detectChanges();
        setTimeout(() => this.runStatsIntro(), 50);
      },
      error: (err) => {
        this.isLoading = false;
        this.error = err?.status === 404
          ? 'Player card not found'
          : 'Failed to load player card';
      }
    });
  }

  ngAfterViewInit() {
    if (this.player) {
      this.runStatsIntro();
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

  toggleFlip() {
    this.cardElement.nativeElement.style.transform = '';
    this.isFlipped = !this.isFlipped;
    if (!this.isFlipped) {
      this.runStatsIntro();
    }
  }

  getInitials(name: string): string {
    if (!name) return '';
    const parts = name.trim().split(' ');
    if (parts.length > 1) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return parts[0][0].toUpperCase();
  }

  private runStatsIntro() {
    if (!this.player) return;
    const targetRating = Math.round(this.player.overallRating);
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    if (reduceMotion) {
      this.animatedRating = targetRating;
      return;
    }

    this.animateValue(0, targetRating, 1100, (v) => this.animatedRating = v);

    setTimeout(() => {
      const card = this.cardElement.nativeElement;

      if (this.isGK) {
        const gkFill = card.querySelector('.gk-stat-bar .fill') as HTMLElement;
        const gkNum = card.querySelector('.gk-stat-block .num') as HTMLElement;
        const gkValue = Math.round(this.player!.goalkeepingRating || 0);

        if (gkFill) {
          gkFill.style.width = '0%';
          gkFill.style.transition = 'none';
          if (gkNum) gkNum.textContent = '0';
          void gkFill.offsetWidth;
          gkFill.style.transition = '';
          gkFill.style.width = `${gkValue}%`;
        }
        if (gkNum) this.animateValue(0, gkValue, 900, (v) => gkNum.textContent = String(v));

      } else {
        const fills = card.querySelectorAll('.stats-grid .stat-bar .fill');

        fills.forEach(f => {
          (f as HTMLElement).style.transition = 'none';
          (f as HTMLElement).style.width = '0%';
        });

        const nums = card.querySelectorAll('.stats-grid .stat .num');
        nums.forEach(n => n.textContent = '0');

        void (fills[0] as HTMLElement)?.offsetWidth;

        fills.forEach(f => (f as HTMLElement).style.transition = '');

        this.statsList.forEach((stat, i) => {
          if (fills[i]) {
            (fills[i] as HTMLElement).style.width = `${stat.value}%`;
            const numEl = fills[i].parentElement?.parentElement?.querySelector('.num');
            if (numEl) {
              this.animateValue(0, stat.value, 900, (v) => numEl.textContent = String(v));
            }
          }
        });
      }
    }, 250);
  }

  private animateValue(start: number, end: number, duration: number, callback: (v: number) => void) {
    const startTime = performance.now();
    const easeOutCubic = (t: number) => 1 - Math.pow(1 - t, 3);

    const tick = (now: number) => {
      const p = Math.min((now - startTime) / duration, 1);
      const eased = easeOutCubic(p);
      callback(Math.round(start + eased * (end - start)));
      if (p < 1) requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  }

  onMouseMove(e: MouseEvent) {
    if (this.isFlipped || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
    const card = this.cardElement.nativeElement;
    const rect = card.getBoundingClientRect();
    const px = (e.clientX - rect.left) / rect.width;
    const py = (e.clientY - rect.top) / rect.height;
    const rotateY = (px - 0.5) * 14;
    const rotateX = (0.5 - py) * 14;
    card.style.transform = `rotateX(${rotateX}deg) rotateY(${rotateY}deg) scale(1.02)`;
  }

  onMouseLeave() {
    if (!this.isFlipped) {
      this.cardElement.nativeElement.style.transform = 'rotateX(0deg) rotateY(0deg)';
    }
  }
}
