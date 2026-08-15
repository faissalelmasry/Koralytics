import { Component, Input, OnInit, AfterViewInit, ElementRef, ViewChild, inject, ChangeDetectorRef, HostBinding, OnChanges, SimpleChanges, OnDestroy, NgZone, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PlayerCardModel } from '../../../../core/models/Player/player-card-model';
import { PlayerCardService } from '../../../../core/services/player/player-card.service';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { MarqueeIfOverflowDirective } from '../../match/match-timeline/marquee-if-overflow.directive';
import { TranslatePipe } from '@ngx-translate/core';

export interface PlayerStatItem {
  label: string;
  value: number;
}

@Component({
  selector: 'app-player-card',
  standalone: true,
  imports: [CommonModule, MarqueeIfOverflowDirective, TranslatePipe],
  templateUrl: './player-card.html',
  styleUrls: ['./player-card.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PlayerCardComponent implements OnInit, AfterViewInit, OnChanges, OnDestroy {
  private playerCardService = inject(PlayerCardService);
  private tokenStorage = inject(TokenStorageService);
  private cdr = inject(ChangeDetectorRef);
  private ngZone = inject(NgZone);

  @Input() player?: PlayerCardModel;
  @Input() autoLoad: boolean = true;
  @Input() compact: boolean = false;
  @Input() @HostBinding('style.width.px') cardWidth: number = 260;
  @Input() @HostBinding('style.height.px') cardHeight: number = 380;

  @ViewChild('cardElement') cardElement!: ElementRef<HTMLDivElement>;

  isLoading = false;
  error = '';
  animatedRating = 0;
  isFlipped = false;
  isExporting = false;
  stars = [1, 2, 3, 4, 5];

  statsList: PlayerStatItem[] = [];
  tierClass = 'tier-base';
  displayClassification = '';
  isGK = false;
  isCurrentUserPlayer = false;

  private unbindMouseListeners?: () => void;

  ngOnInit() {
    this.computeDerivedState();

    const token = this.tokenStorage.getAccessToken();
    if (token) {
      const decoded = this.decodeTokenPayload(token);
      if (decoded && decoded.roles.includes('Player')) {
        this.isCurrentUserPlayer = true;
      }
    }

    if (this.player || !this.autoLoad || !this.isCurrentUserPlayer) return;

    const decoded = this.decodeTokenPayload(token!);
    if (!decoded) return;
    const { userId } = decoded;

    this.isLoading = true;
    this.cdr.markForCheck();

    this.playerCardService.getPlayerCard(userId).subscribe({
      next: (card) => {
        this.player = card;
        this.isLoading = false;
        this.computeDerivedState();
        this.cdr.markForCheck();
        setTimeout(() => this.runStatsIntro(), 50);
      },
      error: (err) => {
        this.isLoading = false;
        this.error = err?.status === 404
          ? 'Player card not found'
          : 'Failed to load player card';
        this.cdr.markForCheck();
      }
    });
  }

  ngAfterViewInit() {
    this.setupMouseListeners();
    if (this.player) {
      this.runStatsIntro();
    }
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['player']) {
      this.computeDerivedState();
      if (!changes['player'].firstChange && this.player) {
        this.runStatsIntro();
      }
    }
  }

  ngOnDestroy() {
    if (this.unbindMouseListeners) {
      this.unbindMouseListeners();
    }
  }

  private computeDerivedState() {
    if (!this.player) {
      this.tierClass = 'tier-base';
      this.displayClassification = '';
      this.isGK = false;
      this.statsList = [];
      return;
    }

    const rating = this.player.overallRating ?? 0;
    if (rating >= 80) this.tierClass = 'tier-elite';
    else if (rating >= 70) this.tierClass = 'tier-gold';
    else this.tierClass = 'tier-base';

    if (this.player.transferClassification === 'Natural') {
      this.displayClassification = 'Expert';
    } else {
      this.displayClassification = this.player.transferClassification || '';
    }

    this.isGK = this.player.position?.toUpperCase() === 'GK';

    this.statsList = [
      { label: 'PAC', value: Math.round(this.player.paceRating || 0) },
      { label: 'DRI', value: Math.round(this.player.dribblingRating || 0) },
      { label: 'SHO', value: Math.round(this.player.shootingRating || 0) },
      { label: 'DEF', value: Math.round(this.player.defendingRating || 0) },
      { label: 'PAS', value: Math.round(this.player.passingRating || 0) },
      { label: 'PHY', value: Math.round(this.player.physicalRating || 0) }
    ];
  }

  private setupMouseListeners() {
    if (!this.cardElement?.nativeElement) return;
    const card = this.cardElement.nativeElement;

    this.ngZone.runOutsideAngular(() => {
      const onMove = (e: MouseEvent) => {
        if (this.isFlipped || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
        const rect = card.getBoundingClientRect();
        const px = (e.clientX - rect.left) / rect.width;
        const py = (e.clientY - rect.top) / rect.height;
        const rotateY = (px - 0.5) * 14;
        const rotateX = (0.5 - py) * 14;
        card.style.transform = `rotateX(${rotateX}deg) rotateY(${rotateY}deg) scale(1.02)`;
      };

      const onLeave = () => {
        if (!this.isFlipped) {
          card.style.transform = 'rotateX(0deg) rotateY(0deg)';
        }
      };

      card.addEventListener('mousemove', onMove);
      card.addEventListener('mouseleave', onLeave);

      this.unbindMouseListeners = () => {
        card.removeEventListener('mousemove', onMove);
        card.removeEventListener('mouseleave', onLeave);
      };
    });
  }

  toggleFlip() {
    if (this.cardElement?.nativeElement) {
      this.cardElement.nativeElement.style.transform = '';
    }
    this.isFlipped = !this.isFlipped;
    this.cdr.markForCheck();
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
      this.cdr.markForCheck();
      return;
    }

    this.animateValue(0, targetRating, 1100, (v) => {
      if (this.animatedRating !== v) {
        this.animatedRating = v;
        this.cdr.markForCheck();
      }
    });

    setTimeout(() => {
      if (!this.cardElement?.nativeElement) return;
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
    this.ngZone.runOutsideAngular(() => {
      const startTime = performance.now();
      const easeOutCubic = (t: number) => 1 - Math.pow(1 - t, 3);

      const tick = (now: number) => {
        const p = Math.min((now - startTime) / duration, 1);
        const eased = easeOutCubic(p);
        callback(Math.round(start + eased * (end - start)));
        if (p < 1) requestAnimationFrame(tick);
      };
      requestAnimationFrame(tick);
    });
  }

  async exportToPdf(event: Event) {
    event.stopPropagation();
    if (!this.cardElement?.nativeElement || this.isExporting) return;

    const card = this.cardElement.nativeElement;
    this.isExporting = true;
    this.cdr.markForCheck();

    const offscreen = document.createElement('div');
    offscreen.style.cssText = 'position:fixed;left:-9999px;top:-9999px;pointer-events:none;';
    let blobUrl: string | null = null;

    try {
      const htmlToImage = await import('html-to-image');

      // Clone the entire card3d so CSS custom properties (--tier-neon, gradients, etc.)
      // are preserved — cloning only the face loses those variables from the parent scope
      const clone = card.cloneNode(true) as HTMLElement;

      // Flatten 3D — backface-visibility:hidden inside preserve-3d causes blank output
      // in production/minified builds with html-to-image
      clone.style.transform      = 'none';
      clone.style.transformStyle = 'flat';
      clone.style.transition     = 'none';
      clone.style.cursor         = 'default';

      const rect = card.getBoundingClientRect();
      clone.style.width  = `${rect.width}px`;
      clone.style.height = `${rect.height}px`;

      // Show only the visible face; hide the other
      const cloneFront = clone.querySelector('.face.front') as HTMLElement | null;
      const cloneBack  = clone.querySelector('.face.back')  as HTMLElement | null;

      if (this.isFlipped) {
        if (cloneFront) cloneFront.style.display = 'none';
        if (cloneBack) {
          cloneBack.style.transform = 'none';
          cloneBack.style.backfaceVisibility = 'visible';
          (cloneBack.style as any).webkitBackfaceVisibility = 'visible';
        }
      } else {
        if (cloneBack) cloneBack.style.display = 'none';
        if (cloneFront) {
          cloneFront.style.backfaceVisibility = 'visible';
          (cloneFront.style as any).webkitBackfaceVisibility = 'visible';
        }
      }

      // Freeze stat bar transitions so they render at current animated width
      clone.querySelectorAll<HTMLElement>('.fill').forEach(f => {
        f.style.transition = 'none';
      });

      // ── CORS FIX ──────────────────────────────────────────────────────────
      // Azure Blob Storage images are cross-origin. html-to-image draws them
      // onto a <canvas> which gets tainted → SecurityError → blank export.
      // We try three strategies in order:
      //   1. Draw via new Image(crossOrigin=anonymous) → canvas.toDataURL()
      //      (works if Azure Blob has CORS configured / Access-Control-Allow-Origin)
      //   2. fetch() as blob → object URL  (same condition, different path)
      //   3. Fall back to initials div (always works, no photo)
      const imgEl = clone.querySelector<HTMLImageElement>('img.photo-img');

      if (imgEl && this.player?.profileImageUrl) {
        const photoUrl = this.player.profileImageUrl;
        let converted = false;

        // Strategy 1: crossOrigin canvas
        try {
          const crossImg = new Image();
          crossImg.crossOrigin = 'anonymous';
          const loaded = await new Promise<boolean>(resolve => {
            crossImg.onload  = () => resolve(true);
            crossImg.onerror = () => resolve(false);
            crossImg.src = photoUrl + (photoUrl.includes('?') ? '&' : '?') + '_cb=' + Date.now();
          });

          if (loaded) {
            const tmpCanvas = document.createElement('canvas');
            tmpCanvas.width  = crossImg.naturalWidth;
            tmpCanvas.height = crossImg.naturalHeight;
            const ctx = tmpCanvas.getContext('2d')!;
            ctx.drawImage(crossImg, 0, 0);
            const dataUrlImg = tmpCanvas.toDataURL('image/png'); // throws if tainted
            imgEl.src = dataUrlImg;
            converted = true;
          }
        } catch { /* tainted or load failed — try next strategy */ }

        // Strategy 2: fetch → blob URL
        if (!converted) {
          try {
            const res = await fetch(photoUrl, { mode: 'cors', credentials: 'omit' });
            if (!res.ok) throw new Error('fetch failed');
            const blob = await res.blob();
            blobUrl = URL.createObjectURL(blob);
            imgEl.src = blobUrl;
            await new Promise<void>(resolve => {
              imgEl.onload  = () => resolve();
              imgEl.onerror = () => resolve();
            });
            converted = true;
          } catch { /* CORS not configured on storage — fall through */ }
        }

        // Strategy 3: initials fallback (avoids canvas taint entirely)
        if (!converted) {
          const wrap = imgEl.parentElement;
          if (wrap) {
            imgEl.remove();
            const initDiv = document.createElement('div');
            initDiv.className = 'photo';
            initDiv.textContent = this.getInitials(this.player.playerName);
            initDiv.style.cssText = 'display:flex;align-items:center;justify-content:center;width:100%;height:100%;font-size:36px;font-weight:700;';
            wrap.appendChild(initDiv);
          }
        }
      }
      // ─────────────────────────────────────────────────────────────────────


      offscreen.appendChild(clone);
      document.body.appendChild(offscreen);

      const dataUrl = await htmlToImage.toPng(clone, {
        quality: 1,
        pixelRatio: 2
      });

      // Download
      const link = document.createElement('a');
      link.download = `${this.player?.playerName || 'Player'}-Card.png`;
      link.href = dataUrl;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);

    } catch (e) {
      console.error('Error generating card PNG', e);
    } finally {
      if (offscreen.parentNode) document.body.removeChild(offscreen);
      if (blobUrl) URL.revokeObjectURL(blobUrl);
      this.isExporting = false;
      this.cdr.markForCheck();
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
}
