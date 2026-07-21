import { Component, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar';
import { LoadingSpinnerComponent } from "../../../shared/components/loading-spinner/loading-spinner";

@Component({
  selector: 'app-auth-layout',
  imports: [CommonModule, RouterModule, NavbarComponent, LoadingSpinnerComponent],
  templateUrl: './auth-layout.component.html',
  styleUrls: ['./auth-layout.component.css']
})
export class AuthLayoutComponent {
  isLoaded = false;
  showLoader = true;

  @ViewChild('tiltCard', { static: false }) tiltCard!: ElementRef;

  ngOnInit() {
    // Show loader once per session
    const alreadyLoaded = sessionStorage.getItem('koralyticsLoaded') === '1';

    if (alreadyLoaded) {
      this.showLoader = false;
      this.isLoaded = true;
    } else {
      setTimeout(() => {
        this.isLoaded = true;
        sessionStorage.setItem('koralyticsLoaded', '1');
        setTimeout(() => this.showLoader = false, 700);
      }, 1400); // MIN_LOAD_MS
    }
  }

  ngAfterViewInit() {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches === false) {
      const card = this.tiltCard?.nativeElement;
      if (card) {
        card.addEventListener('mousemove', (e: MouseEvent) => {
          const rect = card.getBoundingClientRect();
          const px = (e.clientX - rect.left) / rect.width;
          const py = (e.clientY - rect.top) / rect.height;
          const rotateY = (px - 0.5) * 10;
          const rotateX = (0.5 - py) * 10;
          card.style.transform = `rotateX(${rotateX}deg) rotateY(${rotateY}deg)`;
          card.style.setProperty('--mx', (px * 100) + '%');
          card.style.setProperty('--my', (py * 100) + '%');
        });
        card.addEventListener('mouseleave', () => {
          card.style.transform = 'rotateX(0deg) rotateY(0deg)';
        });
      }
    }
  }
}
