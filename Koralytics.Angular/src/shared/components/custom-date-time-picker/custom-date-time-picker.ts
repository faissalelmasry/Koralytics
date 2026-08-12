import { Component, Input, Output, EventEmitter, HostListener, HostBinding, ElementRef, OnInit, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-date-time-picker',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-date-time-picker.html',
  styleUrls: ['./custom-date-time-picker.css'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CustomDateTimePicker),
      multi: true
    }
  ]
})
export class CustomDateTimePicker implements OnInit, ControlValueAccessor {
  @Input() label: string = 'select date & time';
  @Input() errorMessage: string = '';
  @Input() disabled: boolean = false;

  @Input() value: string = '';
  @Output() valueChange = new EventEmitter<string>();

  isOpen: boolean = false;

  @HostBinding('class.is-open')
  get isOpenHostClass(): boolean {
    return this.isOpen;
  }
  panelTop: number = 0;
  panelLeft: number = 0;
  panelWidth: number = 0;

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

  selectedHour: number = 12;
  selectedMinute: number = 0;
  hours: number[] = Array.from({ length: 24 }, (_, i) => i);
  minutes: number[] = Array.from({ length: 12 }, (_, i) => i * 5);

  onChange: any = () => {};
  onTouch: any = () => {};

  constructor(private elementRef: ElementRef) {}

  ngOnInit() {
    this.parseValue();
    this.renderCalendar();
  }

  writeValue(value: any): void {
    if (value !== undefined && value !== null) {
      this.value = value;
      this.parseValue();
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

  private parseValue(): void {
    if (this.value) {
      const d = new Date(this.value);
      if (!isNaN(d.getTime())) {
        this.viewDate = d;
        const localDate = new Date(d.getTime() - (d.getTimezoneOffset() * 60 * 1000));
        this.selectedHour = localDate.getUTCHours();
        this.selectedMinute = Math.round(localDate.getUTCMinutes() / 5) * 5;
        if (this.selectedMinute === 60) {
          this.selectedMinute = 0;
          this.selectedHour = (this.selectedHour + 1) % 24;
        }
      }
    }
  }

  toggleCalendar() {
    if (!this.disabled) {
      this.isOpen = !this.isOpen;
      if (this.isOpen) {
        this.parseValue();
        this.renderCalendar();
        this.calculatePanelPosition();
      }
      this.onTouch();
    }
  }

  private calculatePanelPosition(): void {
    const wrapper = this.elementRef.nativeElement.querySelector('.date-control-wrapper');
    if (wrapper) {
      const rect = wrapper.getBoundingClientRect();
      this.panelTop = rect.bottom + 4;
      this.panelLeft = rect.left;
      this.panelWidth = rect.width;
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

    const datePart = val.split('T')[0];
    if (/^\d{4}-\d{2}-\d{2}$/.test(datePart)) {
      const d = new Date(datePart);
      if (!isNaN(d.getTime())) {
        this.viewDate = d;
        this.renderCalendar();
      }
    }
  }

  selectDay(day: number, event: Event) {
    event.stopPropagation();
    this.viewDate = new Date(this.viewDate.getFullYear(), this.viewDate.getMonth(), day);
    this.renderCalendar();
    this.emitValue();
  }

  onHourChange(event: any) {
    this.selectedHour = parseInt(event.target.value);
    this.emitValue();
  }

  onMinuteChange(event: any) {
    this.selectedMinute = parseInt(event.target.value);
    this.emitValue();
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

  get displayValue(): string {
    if (!this.value) return '';
    return this.value.replace('T', ' ');
  }

  get hasDateSelected(): boolean {
    if (!this.value) return false;
    const d = new Date(this.value);
    return !isNaN(d.getTime());
  }

  private emitValue(): void {
    const pad = (n: number) => n.toString().padStart(2, '0');
    const year = this.viewDate.getFullYear();
    const month = pad(this.viewDate.getMonth() + 1);
    const day = pad(this.viewDate.getDate());
    const hour = pad(this.selectedHour);
    const minute = pad(this.selectedMinute);
    this.value = `${year}-${month}-${day}T${hour}:${minute}`;
    this.valueChange.emit(this.value);
    this.onChange(this.value);
    this.onTouch();
  }

  @HostListener('document:click', ['$event'])
  closeOnClickOutside(event: Event) {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen = false;
    }
  }
}
