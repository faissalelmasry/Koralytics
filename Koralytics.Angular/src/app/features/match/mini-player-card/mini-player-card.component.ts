import { Component, Input, Output, EventEmitter, HostBinding } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MiniPlayerCardModel } from '../../../../core/models/Player/mini-player-card-model';

const SIZE_MAP: Record<string, { w: number; h: number }> = {
  xs: { w: 65, h: 90 },
  sm: { w: 80, h: 110 },
  md: { w: 100, h: 135 }
};

@Component({
  selector: 'app-mini-player-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mini-player-card.component.html',
  styleUrls: ['./mini-player-card.component.css']
})
export class MiniPlayerCardComponent {
  @Input({ required: true }) player!: MiniPlayerCardModel;
  @Input() size: 'xs' | 'sm' | 'md' = 'md';
  @Input() accentColor?: string;
  @Input() float: boolean = false;

  @Output() cardClick = new EventEmitter<number>();

  @HostBinding('style.width.px')
  get hostWidth(): number {
    return SIZE_MAP[this.size]?.w ?? 100;
  }

  @HostBinding('style.height.px')
  get hostHeight(): number {
    return SIZE_MAP[this.size]?.h ?? 135;
  }

  get tierClass(): string {
    const rating = this.player?.overallRating ?? 0;
    if (rating >= 80) return 'tier-elite';
    if (rating >= 70) return 'tier-gold';
    return 'tier-base';
  }

  get displayRating(): number {
    return Math.round(this.player?.overallRating ?? 0);
  }

  getInitials(name: string): string {
    if (!name) return '';
    const parts = name.trim().split(' ');
    if (parts.length > 1) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return parts[0][0].toUpperCase();
  }

  onClick(): void {
    this.cardClick.emit(this.player.playerId);
  }
}
