import { Component, Input, Output, EventEmitter, HostListener, HostBinding, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface SelectOption {
  value: any;
  label: string;
}

@Component({
  selector: 'app-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-select.html',
  styleUrls: ['./custom-select.css']
})
export class CustomSelect {
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
