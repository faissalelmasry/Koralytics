import { Component, Input, OnChanges, inject } from '@angular/core';
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
export class MatchTimelineEventsComponent implements OnChanges {
  private sanitizer = inject(DomSanitizer);

  @Input({ required: true }) events!: TimelineEvent[];

  eventsWithIcons: (TimelineEvent & { iconSvg: SafeHtml })[] = [];

  ngOnChanges(): void {
    this.eventsWithIcons = this.events.map((event, idx) => ({
      ...event,
      iconSvg: this.sanitizer.bypassSecurityTrustHtml(this.buildSvg(event.rawType, idx))
    }));
  }

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
        return `<svg width="30" height="30" viewBox="0 0 40 40" fill="none">
          <defs>
            <radialGradient id="gBall${idx}" cx="35%" cy="25%" r="65%">
              <stop offset="0%" stop-color="#ecfdf5"/>
              <stop offset="40%" stop-color="#6ee7b7"/>
              <stop offset="75%" stop-color="#22c55e"/>
              <stop offset="100%" stop-color="#166534"/>
            </radialGradient>
            <radialGradient id="gPent${idx}" cx="30%" cy="30%" r="70%">
              <stop offset="0%" stop-color="#14532d"/>
              <stop offset="100%" stop-color="#052e16"/>
            </radialGradient>
            <filter id="gShadow${idx}" x="-20%" y="-20%" width="140%" height="140%">
              <feDropShadow dx="0" dy="4" stdDeviation="3" flood-color="#000" flood-opacity="0.6"/>
              <feDropShadow dx="0" dy="0" stdDeviation="4" flood-color="#22c55e" flood-opacity="0.3"/>
            </filter>
          </defs>
          <circle cx="20" cy="20" r="16" fill="url(#gBall${idx})" filter="url(#gShadow${idx})"/>
          <path d="M20 12 L24.5 15.5 L23 20.5 L17 20.5 L15.5 15.5 Z" fill="url(#gPent${idx})" stroke="#052e16" stroke-width="0.5"/>
          <path d="M20 12 L15.5 15.5 L11 14 L10.5 8.5 L15.5 7 Z" fill="url(#gPent${idx})" opacity="0.9" stroke="#052e16" stroke-width="0.5"/>
          <path d="M20 12 L24.5 15.5 L29 14 L29.5 8.5 L24.5 7 Z" fill="url(#gPent${idx})" opacity="0.9" stroke="#052e16" stroke-width="0.5"/>
          <path d="M17 20.5 L23 20.5 L24.5 26 L20 29.5 L15.5 26 Z" fill="url(#gPent${idx})" opacity="0.95" stroke="#052e16" stroke-width="0.5"/>
          <path d="M15.5 15.5 L17 20.5 L12.5 24 L8 20.5 L9.5 15.5 Z" fill="url(#gPent${idx})" opacity="0.85" stroke="#052e16" stroke-width="0.5"/>
          <path d="M24.5 15.5 L23 20.5 L27.5 24 L32 20.5 L30.5 15.5 Z" fill="url(#gPent${idx})" opacity="0.85" stroke="#052e16" stroke-width="0.5"/>
          <path d="M15.5 7 L20 12 M24.5 7 L20 12 M10.5 8.5 L15.5 15.5 M29.5 8.5 L24.5 15.5 M17 20.5 L15.5 26 M23 20.5 L24.5 26" stroke="#15803d" stroke-width="0.6" opacity="0.7"/>
          <path d="M8 15 A13 13 0 0 1 25 6 A15 15 0 0 0 8 15 Z" fill="#fff" opacity="0.4"/>
        </svg>`;

      case 'YellowCard':
        return `<svg width="30" height="30" viewBox="0 0 40 40" fill="none">
          <defs>
            <linearGradient id="yc3D${idx}" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stop-color="#fffbeb"/>
              <stop offset="25%" stop-color="#fde047"/>
              <stop offset="70%" stop-color="#eab308"/>
              <stop offset="100%" stop-color="#854d0e"/>
            </linearGradient>
            <filter id="ycShadow${idx}">
              <feDropShadow dx="3" dy="5" stdDeviation="3" flood-color="#000" flood-opacity="0.6"/>
              <feDropShadow dx="0" dy="0" stdDeviation="4" flood-color="#eab308" flood-opacity="0.4"/>
            </filter>
          </defs>
          <rect x="12.5" y="6.5" width="18" height="27" rx="3" transform="rotate(12 20 20)" fill="#713f12"/>
          <rect x="11" y="5" width="18" height="27" rx="3" transform="rotate(12 20 20)" fill="url(#yc3D${idx})" filter="url(#ycShadow${idx})" stroke="#fff" stroke-width="0.8" stroke-opacity="0.8"/>
          <path d="M12 8 L27 28" stroke="#fff" stroke-width="3" opacity="0.35" stroke-linecap="round"/>
        </svg>`;

      case 'RedCard':
        return `<svg width="30" height="30" viewBox="0 0 40 40" fill="none">
          <defs>
            <linearGradient id="rc3D${idx}" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stop-color="#ffe4e6"/>
              <stop offset="25%" stop-color="#fb7185"/>
              <stop offset="70%" stop-color="#e11d48"/>
              <stop offset="100%" stop-color="#4c0519"/>
            </linearGradient>
            <filter id="rcShadow${idx}">
              <feDropShadow dx="-3" dy="5" stdDeviation="3" flood-color="#000" flood-opacity="0.6"/>
              <feDropShadow dx="0" dy="0" stdDeviation="5" flood-color="#f43f5e" flood-opacity="0.5"/>
            </filter>
          </defs>
          <rect x="10.5" y="6.5" width="18" height="27" rx="3" transform="rotate(-12 20 20)" fill="#4c0519"/>
          <rect x="11" y="5" width="18" height="27" rx="3" transform="rotate(-12 20 20)" fill="url(#rc3D${idx})" filter="url(#rcShadow${idx})" stroke="#fff" stroke-width="0.8" stroke-opacity="0.8"/>
          <path d="M26 8 L11 28" stroke="#fff" stroke-width="3" opacity="0.35" stroke-linecap="round"/>
        </svg>`;

      case 'Substitution':
        return `<svg width="30" height="30" viewBox="0 0 40 40" fill="none">
          <defs>
            <linearGradient id="sgGreen${idx}" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stop-color="#4ade80"/>
              <stop offset="100%" stop-color="#15803d"/>
            </linearGradient>
            <linearGradient id="sgRed${idx}" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stop-color="#f87171"/>
              <stop offset="100%" stop-color="#b91c1c"/>
            </linearGradient>
            <filter id="sgGlow${idx}">
              <feDropShadow dx="0" dy="2" stdDeviation="2" flood-color="#000" flood-opacity="0.5"/>
            </filter>
          </defs>
          <path d="M10 18 C10 10, 22 8, 28 13" stroke="url(#sgGreen${idx})" stroke-width="4.5" stroke-linecap="round" fill="none" filter="url(#sgGlow${idx})"/>
          <path d="M30 16 L28 11 L23 15" fill="url(#sgGreen${idx})" filter="url(#sgGlow${idx})"/>
          <path d="M30 22 C30 30, 18 32, 12 27" stroke="url(#sgRed${idx})" stroke-width="4.5" stroke-linecap="round" fill="none" filter="url(#sgGlow${idx})"/>
          <path d="M10 24 L12 29 L17 25" fill="url(#sgRed${idx})" filter="url(#sgGlow${idx})"/>
        </svg>`;

      case 'OwnGoal':
        return `<svg width="30" height="30" viewBox="0 0 40 40" fill="none">
          <defs>
            <radialGradient id="ogDark${idx}" cx="35%" cy="25%" r="65%">
              <stop offset="0%" stop-color="#9ca3af"/>
              <stop offset="50%" stop-color="#4b5563"/>
              <stop offset="100%" stop-color="#111827"/>
            </radialGradient>
            <filter id="ogGlow${idx}">
              <feDropShadow dx="0" dy="0" stdDeviation="4" flood-color="#ef4444" flood-opacity="0.8"/>
            </filter>
          </defs>
          <circle cx="20" cy="20" r="15" fill="url(#ogDark${idx})" filter="url(#ogGlow${idx})" stroke="#ef4444" stroke-width="1.5"/>
          <path d="M20 6 L17 14 L23 20 L16 28 L20 34" stroke="#f87171" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          <path d="M20 6 L17 14 L23 20 L16 28 L20 34" stroke="#fff" stroke-width="0.8" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>`;

      case 'PenaltyScored':
        return `<svg width="30" height="30" viewBox="0 0 40 40" fill="none">
          <defs>
            <linearGradient id="psMetal${idx}" x1="0%" y1="0%" x2="100%" y2="0%">
              <stop offset="0%" stop-color="#9ca3af"/>
              <stop offset="50%" stop-color="#fff"/>
              <stop offset="100%" stop-color="#4b5563"/>
            </linearGradient>
          </defs>
          <path d="M6 32 V12 A2 2 0 0 1 8 10 H32 A2 2 0 0 1 34 12 V32" stroke="url(#psMetal${idx})" stroke-width="3" stroke-linecap="round"/>
          <path d="M8 16 H32 M8 21 H32 M8 26 H32 M13 10 V32 M19 10 V32 M25 10 V32 M30 10 V32" stroke="#38bdf8" stroke-width="0.7" opacity="0.35"/>
          <circle cx="28" cy="15" r="4.5" fill="#22c55e" stroke="#fff" stroke-width="0.8"/>
          <path d="M12 28 L25 17" stroke="#22c55e" stroke-width="2" stroke-dasharray="2 2" stroke-linecap="round" opacity="0.8"/>
        </svg>`;

      case 'PenaltyMissed':
        return `<svg width="30" height="30" viewBox="0 0 40 40" fill="none">
          <defs>
            <filter id="pmGlow${idx}">
              <feDropShadow dx="0" dy="0" stdDeviation="3" flood-color="#f43f5e" flood-opacity="0.9"/>
            </filter>
          </defs>
          <path d="M8 32 V13 H32 V32" stroke="#4b5563" stroke-width="2.5" stroke-dasharray="3 2" opacity="0.5"/>
          <circle cx="35" cy="9" r="4" fill="#6b7280" stroke="#9ca3af" stroke-width="0.8"/>
          <path d="M13 13 L27 27 M27 13 L13 27" stroke="#f43f5e" stroke-width="4.5" stroke-linecap="round" filter="url(#pmGlow${idx})"/>
          <path d="M13 13 L27 27 M27 13 L13 27" stroke="#fff" stroke-width="1.2" stroke-linecap="round"/>
        </svg>`;

      case 'CleanSheet':
        return `<svg width="30" height="30" viewBox="0 0 40 40" fill="none">
          <defs>
            <linearGradient id="cs3D${idx}" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stop-color="#7dd3fc"/>
              <stop offset="35%" stop-color="#0284c7"/>
              <stop offset="80%" stop-color="#0369a1"/>
              <stop offset="100%" stop-color="#0c4a6e"/>
            </linearGradient>
            <filter id="csGlow${idx}">
              <feDropShadow dx="0" dy="4" stdDeviation="4" flood-color="#0284c7" flood-opacity="0.5"/>
            </filter>
          </defs>
          <path d="M20 5 L33 9 V19 C33 27 27 33 20 35 C13 33 7 27 7 19 V9 L20 5 Z" fill="none" stroke="#e0f2fe" stroke-width="1.5" filter="url(#csGlow${idx})"/>
          <path d="M20 6 L32 9.8 V19 C32 26.2 26.5 31.8 20 33.8 C13.5 31.8 8 26.2 8 19 V9.8 L20 6 Z" fill="url(#cs3D${idx})"/>
          <path d="M20 6 L32 9.8 V19 C32 26.2 26.5 31.8 20 33.8 V6 Z" fill="#fff" opacity="0.18"/>
          <path d="M14 19 L18 23 L26 14" stroke="#fff" stroke-width="3.5" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>`;

      default:
        return `<svg width="30" height="30" viewBox="0 0 40 40" fill="none">
          <circle cx="20" cy="20" r="14" stroke="currentColor" stroke-width="2"/>
          <circle cx="20" cy="20" r="4" fill="currentColor" opacity="0.6"/>
        </svg>`;
    }
  }
}
