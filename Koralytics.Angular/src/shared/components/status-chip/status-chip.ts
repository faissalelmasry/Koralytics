import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-status-chip',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './status-chip.html',
  styleUrls: ['./status-chip.css']
})
export class StatusChipComponent {
  @Input() label: string = 'active';
  
  @Input() type: 'success' | 'danger' | 'warning' | 'info' | string = 'success';
  
  @Input() pulse: boolean = false; 
}