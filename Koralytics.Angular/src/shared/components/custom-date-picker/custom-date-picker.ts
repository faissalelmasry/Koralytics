import { Component, Input, Output, EventEmitter, HostListener, ElementRef, OnInit, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-date-picker',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-date-picker.html',
  styleUrls: ['./custom-date-picker.css'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CustomDatePicker),
      multi: true
    }
  ]
})
export class CustomDatePicker implements OnInit, ControlValueAccessor {
  @Input() label: string = 'select date';
  @Input() errorMessage: string = '';
  @Input() disabled: boolean = false;

  // Two-way data binding (String format: YYYY-MM-DD)
  @Input() value: string = '';
  @Output() valueChange = new EventEmitter<string>();

  isOpen: boolean = false;
  
  currentDate: Date = new Date();
  viewDate: Date = new Date();
  daysInMonth: number[] = [];
  blankDays: number[] = [];
  monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
  dayNames = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'];
  years: string[] = (() => {
    const cy = new Date().getFullYear();
    const start = cy - 80;
    const end = cy + 10;
    const result: string[] = [];
    for (let y = start; y <= end; y++) {
      result.push(y.toString());
    }
    return result;
  })();

  onChange: any = () => {};
  onTouch: any = () => {};

  constructor(private elementRef: ElementRef) {}

  ngOnInit() {
    if (this.value) {
      this.viewDate = new Date(this.value);
    }
    this.renderCalendar();
  }

  writeValue(value: any): void {
    if (value !== undefined && value !== null) {
      this.value = value;
      const d = new Date(this.value);
      if (!isNaN(d.getTime())) {
        this.viewDate = d;
      }
      this.renderCalendar();
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

  toggleCalendar() {
    if (!this.disabled) {
      this.isOpen = !this.isOpen;
      if (this.isOpen && this.value) {
        const d = new Date(this.value);
        if (!isNaN(d.getTime())) {
          this.viewDate = d;
        }
      }
      this.renderCalendar();
      this.onTouch();
    }
  }

  renderCalendar() {
    const year = this.viewDate.getFullYear();
    const month = this.viewDate.getMonth();

    const firstDayIndex = new Date(year, month, 1).getDay();
    const totalDays = new Date(year, month + 1, 0).getDate();

    this.blankDays = Array(firstDayIndex).fill(0);
    this.daysInMonth = Array.from({ length: totalDays }, (_, i) => i + 1);
  }

  prevMonth(event: Event) {
    event.stopPropagation();
    this.viewDate = new Date(this.viewDate.getFullYear(), this.viewDate.getMonth() - 1, 1);
    this.renderCalendar();
  }

  nextMonth(event: Event) {
    event.stopPropagation();
    this.viewDate = new Date(this.viewDate.getFullYear(), this.viewDate.getMonth() + 1, 1);
    this.renderCalendar();
  }

  onMonthChange(event: any) {
    this.viewDate = new Date(this.viewDate.getFullYear(), parseInt(event.target.value), 1);
    this.renderCalendar();
  }

  onYearChange(event: any) {
    this.viewDate = new Date(parseInt(event.target.value), this.viewDate.getMonth(), 1);
    this.renderCalendar();
  }

  onManualInput(event: any) {
    const val = event.target.value;
    this.value = val;
    this.valueChange.emit(this.value);
    this.onChange(this.value);
    this.onTouch();
    
    // Auto-update calendar if typing is a valid date (YYYY-MM-DD)
    if (/^\d{4}-\d{2}-\d{2}$/.test(val)) {
      const d = new Date(val);
      if (!isNaN(d.getTime())) {
        this.viewDate = d;
        this.renderCalendar();
      }
    }
  }

  selectDay(day: number, event: Event) {
    event.stopPropagation();
    const selected = new Date(this.viewDate.getFullYear(), this.viewDate.getMonth(), day);
    
    const offset = selected.getTimezoneOffset();
    const localSelected = new Date(selected.getTime() - (offset * 60 * 1000));
    this.value = localSelected.toISOString().split('T')[0];
    
    this.valueChange.emit(this.value);
    this.onChange(this.value);
    this.onTouch();
    this.isOpen = false;
  }

  isToday(day: number): boolean {
    const today = new Date();
    return today.getDate() === day &&
           today.getMonth() === this.viewDate.getMonth() &&
           today.getFullYear() === this.viewDate.getFullYear();
  }

  isSelected(day: number): boolean {
    if (!this.value) return false;
    const selected = new Date(this.value);
    if (isNaN(selected.getTime())) return false;
    return selected.getDate() === day &&
           selected.getMonth() === this.viewDate.getMonth() &&
           selected.getFullYear() === this.viewDate.getFullYear();
  }

  @HostListener('document:click', ['$event'])
  closeOnClickOutside(event: Event) {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen = false;
    }
  }
}