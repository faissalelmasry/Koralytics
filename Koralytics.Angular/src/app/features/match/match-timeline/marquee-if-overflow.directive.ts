import {
  Directive, ElementRef, HostListener, OnInit, OnChanges,
  OnDestroy, Renderer2, Input, NgZone
} from '@angular/core';

/**
 * Detects if the host element's text overflows its container.
 * When it does, it:
 *  1. Sets CSS custom property `--marquee-scroll` to the exact overflow amount (negative px).
 *  2. Adds the `is-scrolling` CSS class to trigger the marquee animation.
 *
 * IMPORTANT — CSS contract:
 *   • The HOST element must NOT have overflow:hidden itself.
 *     It should have: display:block; white-space:nowrap; and no overflow clipping.
 *   • The PARENT element must have overflow:hidden to clip the scrolling span.
 *   • The animation (translateX) is on the host element and works because the
 *     parent clips it — the text scrolls inside the parent's visible area.
 *
 * Usage: <span appMarqueeIfOverflow [marqueeText]="text">{{ text }}</span>
 */
@Directive({
  selector: '[appMarqueeIfOverflow]',
  standalone: true
})
export class MarqueeIfOverflowDirective implements OnInit, OnChanges, OnDestroy {
  /** Pass the bound text so the directive re-checks when the text changes. */
  @Input() marqueeText: string | null | undefined = '';

  private resizeObserver?: ResizeObserver;
  private timers: ReturnType<typeof setTimeout>[] = [];

  constructor(
    private el: ElementRef<HTMLElement>,
    private renderer: Renderer2,
    private zone: NgZone
  ) {}

  ngOnInit(): void {
    this.zone.runOutsideAngular(() => {
      if (typeof ResizeObserver !== 'undefined') {
        this.resizeObserver = new ResizeObserver(() => this.check());
        this.resizeObserver.observe(this.el.nativeElement);
        const parent = this.el.nativeElement.parentElement;
        if (parent) this.resizeObserver.observe(parent);
      }
      // Multiple staggered checks so fonts/layout can settle at different times.
      this.addTimer(50);
      this.addTimer(200);
      this.addTimer(600);
    });
  }

  ngOnChanges(): void {
    this.addTimer(80);
    this.addTimer(350);
  }

  @HostListener('window:resize')
  onResize(): void {
    this.check();
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
    this.timers.forEach(t => clearTimeout(t));
    this.timers = [];
  }

  private addTimer(ms: number): void {
    const id = setTimeout(() => {
      this.check();
      this.timers = this.timers.filter(t => t !== id);
    }, ms);
    this.timers.push(id);
  }

  private check(): void {
    const el = this.el.nativeElement;
    if (!el.isConnected) return;

    // ── MEASURE THE REAL TEXT WIDTH ──────────────────────────────────────────
    // The host element has white-space:nowrap but no overflow:hidden, so
    // scrollWidth already gives the true text width vs offsetWidth (visible).
    // If the component CSS adds overflow:hidden to the host, we measure via
    // a hidden clone to bypass that clamping.
    const containerWidth = el.offsetWidth;
    if (containerWidth === 0) return;

    // Use scrollWidth directly — works when parent clips (not the element itself)
    let trueTextWidth = el.scrollWidth;

    // If scrollWidth === offsetWidth but text is clearly clipped (text-overflow),
    // fall back to the clone measurement to get the real text width.
    if (trueTextWidth <= containerWidth) {
      const clone = el.cloneNode(true) as HTMLElement;
      clone.style.cssText = [
        'position:fixed',
        'top:-9999px',
        'left:-9999px',
        'visibility:hidden',
        'pointer-events:none',
        'white-space:nowrap',
        'overflow:visible',
        'text-overflow:clip',
        'width:auto',
        'max-width:none',
        `font-size:${getComputedStyle(el).fontSize}`,
        `font-family:${getComputedStyle(el).fontFamily}`,
        `font-weight:${getComputedStyle(el).fontWeight}`,
        `letter-spacing:${getComputedStyle(el).letterSpacing}`,
      ].join(';');
      document.body.appendChild(clone);
      trueTextWidth = clone.scrollWidth;
      document.body.removeChild(clone);
    }
    // ─────────────────────────────────────────────────────────────────────────

    const overflow = trueTextWidth - containerWidth;

    if (overflow > 2) {
      // Add a small extra buffer (16px) so the very last character isn't clipped
      el.style.setProperty('--marquee-scroll', `-${overflow + 16}px`);
      this.renderer.addClass(el, 'is-scrolling');
    } else {
      el.style.removeProperty('--marquee-scroll');
      this.renderer.removeClass(el, 'is-scrolling');
    }
  }
}
