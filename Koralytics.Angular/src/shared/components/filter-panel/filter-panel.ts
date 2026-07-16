import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CustomInputComponent } from '../custom-input-component/custom-input-component';
import { CustomSelect } from '../custom-select/custom-select';
import { CustomToggle } from '../custom-toggle/custom-toggle';
import { CustomButtonComponent } from '../custom-button/custom-button';

export interface FilterState {
  search: string;
  position: string;
  status: string;
  aiOnly: boolean;
}

@Component({
  selector: 'app-filter-panel',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule,
  ],
  templateUrl: './filter-panel.html',
  styleUrls: ['./filter-panel.css']
})
export class FilterPanel implements OnInit {
  @Input() isExpanded: boolean = true;

  @Output() filterChange = new EventEmitter<FilterState>();

  filters: FilterState = {
    search: '',
    position: '',
    status: '',
    aiOnly: false
  };

  positionOptions = [
    { value: 'goalkeeper', label: 'goalkeeper' },
    { value: 'center back', label: 'center back' },
    { value: 'left back', label: 'left back' },
    { value: 'right back', label: 'right back' },
    { value: 'midfielder', label: 'midfielder' },
    { value: 'right winger', label: 'right winger' },
    { value: 'left winger', label: 'left winger' },
    { value: 'striker', label: 'striker' }
  ];

  statusOptions = [
    { value: 'active', label: 'active roster' },
    { value: 'injured', label: 'injured list' },
    { value: 'pending', label: 'pending review' }
  ];

  ngOnInit() {
    this.emitFilters();
  }

  togglePanel() {
    this.isExpanded = !this.isExpanded;
  }

  emitFilters() {
    this.filterChange.emit({ ...this.filters });
  }

  resetFilters() {
    this.filters = {
      search: '',
      position: '',
      status: '',
      aiOnly: false
    };
    this.emitFilters();
  }
}