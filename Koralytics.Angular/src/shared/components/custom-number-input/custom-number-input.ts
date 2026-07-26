import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-number-input',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-number-input.html',
  styleUrls: ['./custom-number-input.css'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CustomNumberInputComponent),
      multi: true
    }
  ]
})
export class CustomNumberInputComponent implements ControlValueAccessor {
  @Input() label: string = '';
  @Input() placeholder: string = '';
  @Input() min: number | null = 0;
  @Input() max: number | null = 100;
  @Input() step: number = 1;
  @Input() disabled: boolean = false;

  @Input() value: number | null = null;
  @Output() valueChange = new EventEmitter<number | null>();

  isFocused: boolean = false;

  onChange = (_: any) => {};
  onTouched = () => {};

  writeValue(val: any): void {
    this.value = val !== null && val !== undefined && val !== '' ? Number(val) : null;
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  onInput(event: Event): void {
    const inputEl = event.target as HTMLInputElement;
    const rawVal = inputEl.value;
    if (rawVal === '' || rawVal === null || rawVal === undefined) {
      this.setValue(null);
    } else {
      let num = Number(rawVal);
      if (isNaN(num)) {
        this.setValue(null);
      } else {
        if (this.max !== null && this.max !== undefined && num > this.max) {
          num = this.max;
          inputEl.value = String(this.max);
        }
        if (this.min !== null && this.min !== undefined && num < this.min) {
          num = this.min;
          inputEl.value = String(this.min);
        }
        this.setValue(num);
      }
    }
  }

  increment(): void {
    if (this.disabled) return;
    const current = this.value ?? 0;
    const stepVal = Number(this.step) || 1;
    let nextVal = Math.round((current + stepVal) * 100) / 100;
    if (this.max !== null && this.max !== undefined && nextVal > this.max) {
      nextVal = this.max;
    }
    this.setValue(nextVal);
  }

  decrement(): void {
    if (this.disabled) return;
    const current = this.value ?? 0;
    const stepVal = Number(this.step) || 1;
    let nextVal = Math.round((current - stepVal) * 100) / 100;
    if (this.min !== null && this.min !== undefined && nextVal < this.min) {
      nextVal = this.min;
    }
    this.setValue(nextVal);
  }

  private setValue(val: number | null): void {
    if (val !== null && val !== undefined) {
      if (this.max !== null && this.max !== undefined && val > this.max) {
        val = this.max;
      }
      if (this.min !== null && this.min !== undefined && val < this.min) {
        val = this.min;
      }
    }
    this.value = val;
    this.valueChange.emit(val);
    this.onChange(val);
  }

  onFocus(): void {
    this.isFocused = true;
  }

  onBlur(): void {
    this.isFocused = false;
    if (this.value !== null && this.value !== undefined) {
      if (this.max !== null && this.max !== undefined && this.value > this.max) {
        this.setValue(this.max);
      } else if (this.min !== null && this.min !== undefined && this.value < this.min) {
        this.setValue(this.min);
      }
    }
    this.onTouched();
  }
}
