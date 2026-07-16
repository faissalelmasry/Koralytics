import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './empty-state.html',
  styleUrls: ['./empty-state.css']
})
export class EmptyStateComponent {
  @Input() title: string = 'no data found';
  @Input() description: string = 'there are no records matching your radar tracking parameters.';
  @Input() actionText: string = ''; // سيبها فاضية لو مش عايز تظهر زرار
  
  @Output() actionClick = new EventEmitter<void>();

  onActionClick() {
    this.actionClick.emit();
  }
}