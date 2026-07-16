import { Component, EventEmitter, Output, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-search-bar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './search-bar.html',
  styleUrls: ['./search-bar.css']
})
export class SearchBarComponent {
  @Input() placeholderText: string = 'search players, tactics, or clubs...';
  
  // Custom event 3ashan y-emit el-text lil-parent component lma el-user ykteb
  @Output() searchChange = new EventEmitter<string>();

  searchQuery: string = '';

  onSearchInput() {
    this.searchChange.emit(this.searchQuery);
  }

  clearSearch() {
    this.searchQuery = '';
    this.searchChange.emit(this.searchQuery);
  }
}