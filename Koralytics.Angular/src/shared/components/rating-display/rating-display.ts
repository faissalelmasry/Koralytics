import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-rating-display',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './rating-display.html',
  styleUrls: ['./rating-display.css']
})
export class RatingDisplayComponent {
  @Input() value: number = 0;           // 0-10
  @Input() max: number = 10;
  @Input() type: 'star' | 'bar' | 'circular' | 'gauge' = 'star';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() showLabel: boolean = true;
  @Input() label: string = '';
  
  get percentage(): number {
    return Math.min(Math.max((this.value / this.max) * 100, 0), 100);
  }

  get color(): string {
    if (this.percentage >= 80) return 'success';
    if (this.percentage >= 65) return 'warning';
    return 'danger';
  }

  get stars(): number[] {
    const fullStars = Math.floor((this.value / this.max) * 5);
    return Array(5).fill(0).map((_, i) => i < fullStars ? 1 : 0);
  }

  getCircularGradient(): string {
    const colorCode = this.color === 'success' ? '#c8ff4d' : this.color === 'warning' ? '#00f0ff' : '#ff6a5c';
    return `conic-gradient(${colorCode} ${this.percentage}%, #111418 0%)`;
  }
}
