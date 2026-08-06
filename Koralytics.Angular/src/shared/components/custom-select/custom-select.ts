import { Component, Input, Output, EventEmitter, HostListener, HostBinding, ElementRef, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';

export interface SelectOption {
  value: any;
  label: string;
}

@Component({
  selector: 'app-select',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './custom-select.html',
  styleUrls: ['./custom-select.css'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CustomSelect),
      multi: true
    }
  ]
})
export class CustomSelect implements ControlValueAccessor {
  @Input() label: string = 'label';
  @Input() placeholder: string = '';
  @Input() options: SelectOption[] = [];
  @Input() errorMessage: string = '';
  @Input() disabled: boolean = false;

  @Input() value: any = null;
  @Output() valueChange = new EventEmitter<any>();

  isOpen: boolean = false;

  private static activeSelect: CustomSelect | null = null;

  @HostBinding('class.is-open')
  get isOpenClass(): boolean {
    return this.isOpen;
  }

  constructor(private elementRef: ElementRef) {}

  onChange: any = () => {};
  onTouch: any = () => {};

  writeValue(value: any): void {
    if (value !== undefined) {
      this.value = value;
    }
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouch = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  get selectedLabel(): string {
    const selected = this.options.find(opt => opt.value === this.value);
    return selected ? selected.label : '';
  }

  toggleDropdown(event: MouseEvent) {
    event.stopPropagation();
    if (!this.disabled) {
      if (!this.isOpen) {
        if (CustomSelect.activeSelect && CustomSelect.activeSelect !== this) {
          CustomSelect.activeSelect.isOpen = false;
        }
        CustomSelect.activeSelect = this;
        this.isOpen = true;
      } else {
        this.isOpen = false;
        if (CustomSelect.activeSelect === this) {
          CustomSelect.activeSelect = null;
        }
      }
    }
  }

  selectOption(option: SelectOption) {
    this.value = option.value;
    this.valueChange.emit(this.value);
    this.onChange(this.value);
    this.onTouch();
    this.isOpen = false;
    if (CustomSelect.activeSelect === this) {
      CustomSelect.activeSelect = null;
    }
  }

  @HostListener('document:click', ['$event'])
  closeOnClickOutside(event: Event) {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      if (this.isOpen) {
        this.isOpen = false;
        if (CustomSelect.activeSelect === this) {
          CustomSelect.activeSelect = null;
        }
      }
    }
  }
}
