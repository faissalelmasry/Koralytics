import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './confirm-dialog.html',
  styleUrls: ['./confirm-dialog.css']
})
export class ConfirmDialogComponent {
  @Input() isOpen: boolean = false;
  @Input() title: string = '';
  @Input() message: string = '';
  @Input() details: string[] = [];
  @Input() level: 'info' | 'warning' | 'danger' = 'info';
  @Input() confirmText: string = 'Confirm';
  @Input() cancelText: string = 'Cancel';
  @Input() requiresConfirmation: boolean = false;
  @Input() confirmationText: string = '';

  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  confirmationInput: string = '';

  onCancel(): void {
    this.isOpen = false;
    this.confirmationInput = '';
    this.cancel.emit();
  }

  onConfirm(): void {
    if (this.requiresConfirmation && this.confirmationInput !== this.confirmationText) {
      return;
    }
    this.isOpen = false;
    this.confirmationInput = '';
    this.confirm.emit();
  }

  get isConfirmDisabled(): boolean {
    if (!this.requiresConfirmation) return false;
    return this.confirmationInput !== this.confirmationText;
  }
}
