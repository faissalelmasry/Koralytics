import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-toggle',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-toggle.html',
  styleUrls: ['./custom-toggle.css']
})
export class CustomToggle {
  @Input() label: string = '';
  @Input() disabled: boolean = false;

  // Two-way data binding
  @Input() checked: boolean = false;
  @Output() checkedChange = new EventEmitter<boolean>();

  toggleState() {
    if (!this.disabled) {
      this.checked = !this.checked;
      this.checkedChange.emit(this.checked);
    }
  }
}