import { Component, Input, Output, EventEmitter, HostListener, ElementRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-date-picker',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-date-picker.html',
  styleUrls: ['./custom-date-picker.css']
})
export class CustomDatePicker implements OnInit {
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
  monthNames = ['january', 'february', 'march', 'april', 'may', 'june', 'july', 'august', 'september', 'october', 'november', 'december'];
  dayNames = ['su', 'mo', 'tu', 'we', 'th', 'fr', 'sa'];

  constructor(private elementRef: ElementRef) {}

  ngOnInit() {
    if (this.value) {
      this.viewDate = new Date(this.value);
    }
    this.renderCalendar();
  }

  toggleCalendar() {
    if (!this.disabled) {
      this.isOpen = !this.isOpen;
      if (this.isOpen && this.value) {
        this.viewDate = new Date(this.value);
      }
      this.renderCalendar();
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

  selectDay(day: number, event: Event) {
    event.stopPropagation();
    const selected = new Date(this.viewDate.getFullYear(), this.viewDate.getMonth(), day);
    
    const offset = selected.getTimezoneOffset();
    const localSelected = new Date(selected.getTime() - (offset * 60 * 1000));
    this.value = localSelected.toISOString().split('T')[0];
    
    this.valueChange.emit(this.value);
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