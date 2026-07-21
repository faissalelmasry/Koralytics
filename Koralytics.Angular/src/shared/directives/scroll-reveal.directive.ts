import { Directive, ElementRef, Input, AfterViewInit, OnDestroy } from '@angular/core';

@Directive({
  selector: '[scrollReveal]',
  standalone: true,
})
export class ScrollRevealDirective implements AfterViewInit, OnDestroy {
  @Input() direction: 'bottom' | 'left' | 'right' = 'bottom';
  @Input() delay: number = 0;
  @Input() threshold: number = 0.08;
  @Input() once: boolean = false;

  private observer?: IntersectionObserver;
  private host: HTMLElement;

  constructor(private el: ElementRef<HTMLElement>) {
    this.host = el.nativeElement;
  }

  ngAfterViewInit() {
    this.host.setAttribute('data-reveal-direction', this.direction);
    this.host.style.transitionDelay = `${this.delay}ms`;

    this.observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            this.reveal();
          } else {
            this.host.classList.remove('revealed');
          }
        });
      },
      { threshold: this.threshold, rootMargin: '0px 0px -20px 0px' }
    );

    this.observer.observe(this.host);
  }

  private reveal() {
    this.host.classList.add('revealed');
    if (this.once) {
      this.observer?.unobserve(this.host);
    }
  }

  ngOnDestroy() {
    this.observer?.disconnect();
  }
}
