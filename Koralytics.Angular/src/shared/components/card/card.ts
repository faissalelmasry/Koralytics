import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoadingSpinnerComponent } from '../loading-spinner/loading-spinner';

@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule, LoadingSpinnerComponent],
  templateUrl: './card.html',
  styleUrls: ['./card.css']
})
export class CardComponent {
  @Input() title: string = '';
  @Input() subtitle: string = '';
  @Input() variant: 'elevated' | 'outlined' | 'flat' = 'elevated';
  @Input() clickable: boolean = false;
  @Input() hoverable: boolean = true;
  @Input() loading: boolean = false;
  
  @Output() cardClicked = new EventEmitter<void>();

  onCardClick() {
    if (this.clickable && !this.loading) {
      this.cardClicked.emit();
    }
  }

  hasFooter(): boolean {
    // Simplest way to check for footer or allow it to be rendered
    return true;
  }
}
