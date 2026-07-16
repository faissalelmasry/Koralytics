import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-text-input',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './custom-input-component.html',
  styleUrls: ['./custom-input-component.css']
})
export class CustomInputComponent {
  @Input() label: string = 'label';
  @Input() type: string = 'text';
  @Input() placeholder: string = '';
  @Input() errorMessage: string = '';
  @Input() disabled: boolean = false;
  
  @Input() value: string = '';
  @Output() valueChange = new EventEmitter<string>();

  isFocused: boolean = false;
  showPasswordState: boolean = false;

  get currentInputType(): string {
    if (this.type === 'password') {
      return this.showPasswordState ? 'text' : 'password';
    }
    return this.type;
  }

  onInput() {
    this.valueChange.emit(this.value);
  }

  onFocus() {
    this.isFocused = true;
  }

  onBlur() {
    this.isFocused = false;
  }

  togglePasswordVisibility() {
    this.showPasswordState = !this.showPasswordState;
  }
}