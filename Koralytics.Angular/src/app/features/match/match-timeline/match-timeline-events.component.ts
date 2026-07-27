import { Component, Input, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { MiniPlayerCardComponent } from '../mini-player-card/mini-player-card.component';
import { TimelineEvent } from '../match-timeline/match-timeline.types';

@Component({
  selector: 'app-match-timeline-events',
  standalone: true,
  imports: [CommonModule, MiniPlayerCardComponent],
  templateUrl: './match-timeline-events.component.html',
  styleUrls: ['./match-timeline-events.component.css']
})
export class MatchTimelineEventsComponent {
  private sanitizer = inject(DomSanitizer);
  private cdr = inject(ChangeDetectorRef);

  private _events: TimelineEvent[] = [];
  @Input({ required: true }) 
  set events(val: TimelineEvent[]) {
    this._events = val || [];
    this.eventsWithIcons = this._events.map((event, idx) => ({
      ...event,
      iconSvg: this.sanitizer.bypassSecurityTrustHtml(this.buildSvg(event.rawType, idx))
    }));
    this.cdr.detectChanges();
  }
  get events(): TimelineEvent[] {
    return this._events;
  }

  eventsWithIcons: (TimelineEvent & { iconSvg: SafeHtml })[] = [];

  getLabelColor(rawType: string): string {
    switch (rawType) {
      case 'Goal':
      case 'PenaltyScored': return '#22c55e';
      case 'YellowCard': return '#facc15';
      case 'RedCard':
      case 'OwnGoal':
      case 'PenaltyMissed': return '#f43f5e';
      case 'Substitution': return '#82f768';
      case 'CleanSheet': return '#3b82f6';
      default: return '#ffffff';
    }
  }

  private buildSvg(type: string, idx: number): string {
    switch (type) {
      case 'Goal':
        return `<svg width="28" height="28" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <radialGradient id="ball_shade_${idx}" cx="30%" cy="30%" r="70%">
              <stop offset="0%" stop-color="#FFFFFF"/>
              <stop offset="70%" stop-color="#F1F5F9"/>
              <stop offset="100%" stop-color="#CBD5E1"/>
            </radialGradient>
          </defs>
          <path d="M2 20.5H22" stroke="#10B981" stroke-width="2" stroke-linecap="round"/>
          <circle cx="12" cy="11" r="8.5" fill="url(#ball_shade_${idx})" stroke="#0F172A" stroke-width="1"/>
          <polygon points="12,8 14.85,10.07 13.76,13.43 10.24,13.43 9.15,10.07" fill="#0F172A"/>
          <path d="M12,8 L12,2.5" stroke="#0F172A" stroke-width="0.9" stroke-linecap="round"/>
          <path d="M14.85,10.07 L20.08,8.37" stroke="#0F172A" stroke-width="0.9" stroke-linecap="round"/>
          <path d="M13.76,13.43 L17,17.88" stroke="#0F172A" stroke-width="0.9" stroke-linecap="round"/>
          <path d="M10.24,13.43 L7,17.88" stroke="#0F172A" stroke-width="0.9" stroke-linecap="round"/>
          <path d="M9.15,10.07 L3.92,8.37" stroke="#0F172A" stroke-width="0.9" stroke-linecap="round"/>
          <path d="M10.5,2.7 C11.5,2.5 12.5,2.5 13.5,2.7 L12,5 Z" fill="#0F172A"/>
          <path d="M19.2,6.8 C19.9,7.9 20.3,9.1 20.4,10.3 L18,9.5 Z" fill="#0F172A"/>
          <path d="M18.2,16.2 C17.3,17.3 16,18.1 14.7,18.5 L15.2,16 Z" fill="#0F172A"/>
          <path d="M5.8,16.2 C6.7,17.3 8,18.1 9.3,18.5 L8.8,16 Z" fill="#0F172A"/>
          <path d="M4.8,6.8 C4.1,7.9 3.7,9.1 3.6,10.3 L6,9.5 Z" fill="#0F172A"/>
        </svg>`;

      case 'YellowCard':
        return `<svg width="28" height="28" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <linearGradient id="yc_grad_${idx}" x1="0" y1="0" x2="24" y2="24" gradientUnits="userSpaceOnUse">
              <stop stop-color="#FDE047"/>
              <stop offset="1" stop-color="#EAB308"/>
            </linearGradient>
          </defs>
          <rect x="7" y="4" width="11" height="16" rx="2" transform="rotate(-6 12.5 12)" fill="url(#yc_grad_${idx})" stroke="#FEF08A" stroke-width="1"/>
          <path d="M8 6L16 5.2L17 11L9 12Z" fill="#FFFFFF" fill-opacity="0.2" transform="rotate(-6 12.5 12)"/>
        </svg>`;

      case 'RedCard':
        return `<svg width="28" height="28" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <linearGradient id="rc_grad_${idx}" x1="0" y1="0" x2="24" y2="24" gradientUnits="userSpaceOnUse">
              <stop stop-color="#F87171"/>
              <stop offset="1" stop-color="#DC2626"/>
            </linearGradient>
          </defs>
          <rect x="7" y="4" width="11" height="16" rx="2" transform="rotate(-6 12.5 12)" fill="url(#rc_grad_${idx})" stroke="#FCA5A5" stroke-width="1"/>
          <path d="M8 6L16 5.2L17 11L9 12Z" fill="#FFFFFF" fill-opacity="0.2" transform="rotate(-6 12.5 12)"/>
        </svg>`;

      case 'Substitution':
        return `<svg width="28" height="28" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M4 13C4 8.02944 8.02944 4 13 4C15.5 4 17.5 5 19 6.5" stroke="#10B981" stroke-width="2.2" stroke-linecap="round"/>
          <path d="M15 6.5H19.5V2" stroke="#10B981" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>
          <path d="M20 11C20 15.9706 15.9706 20 11 20C8.5 20 6.5 19 5 17.5" stroke="#EF4444" stroke-width="2.2" stroke-linecap="round"/>
          <path d="M9 17.5H4.5V22" stroke="#EF4444" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>`;

      case 'OwnGoal':
        return `<svg width="28" height="28" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M12 5H19.5C20.3 5 21 5.7 21 6.5V17.5C21 18.3 20.3 19 19.5 19H12" stroke="#E11D48" stroke-width="1.8" stroke-linecap="round"/>
          <path d="M12 5V19" stroke="#F43F5E" stroke-width="2" stroke-linecap="round"/>
          <path d="M15 5.2V18.8M18 5.2V18.8" stroke="#FB7185" stroke-width="0.8" opacity="0.4"/>
          <path d="M12 8.5H20.5M12 12H20.8M12 15.5H20.5" stroke="#FB7185" stroke-width="0.8" stroke-dasharray="1.5 1" opacity="0.5"/>
          <path d="M3 16C3 10 7 7 13 9" stroke="#FB7185" stroke-width="1.8" stroke-linecap="round"/>
          <path d="M10 7L14 9L11 12" stroke="#FB7185" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
          <circle cx="10" cy="14" r="3" fill="#FFFFFF" stroke="#E11D48" stroke-width="0.6"/>
          <polygon points="10,12.8 10.9,13.4 10.6,14.5 9.4,14.5 9.1,13.4" fill="#E11D48"/>
        </svg>`;

      case 'PenaltyScored':
        return `<svg width="28" height="28" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M4 19V7C4 5.89543 4.89543 5 6 5H18C19.1046 5 20 5.89543 20 7V19" stroke="#2DD4BF" stroke-width="2" stroke-linecap="round"/>
          <path d="M4 11H20M9 5V19M15 5V19" stroke="#134E4A" stroke-width="1" stroke-dasharray="2 2"/>
          <circle cx="16" cy="9" r="3.5" fill="#2DD4BF" stroke="#FFFFFF" stroke-width="1"/>
          <path d="M14.5 9L15.5 10L17.5 8" stroke="#042F2C" stroke-width="1.2" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>`;

      case 'PenaltyMissed':
        return `<svg width="28" height="28" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M4 19V7C4 5.89543 4.89543 5 6 5H18C19.1046 5 20 5.89543 20 7V19" stroke="#F43F5E" stroke-width="2" stroke-linecap="round"/>
          <path d="M4 11H20M9 5V19M15 5V19" stroke="#881337" stroke-width="1" stroke-dasharray="2 2"/>
          <circle cx="16" cy="9" r="3.5" fill="#EF4444" stroke="#FFFFFF" stroke-width="1"/>
          <path d="M14.3 7.3L17.7 10.7M17.7 7.3L14.3 10.7" stroke="#000000" stroke-width="1.3" stroke-linecap="round"/>
        </svg>`;

      case 'CleanSheet':
        return `<svg width="28" height="28" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" fill="#0284c7" stroke="#38bdf8" stroke-width="1.5"/>
          <path d="M9 12l2 2 4-4" stroke="#FFFFFF" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>`;

      default:
        return `<svg width="28" height="28" viewBox="0 0 24 24" fill="none">
          <circle cx="12" cy="12" r="8" stroke="currentColor" stroke-width="2"/>
        </svg>`;
    }
  }
}
