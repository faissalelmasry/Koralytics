import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface TableColumn {
  key: string;       
  label: string;     
  type?: 'text' | 'badge' | 'action'; 
}

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './data-table.html',
  styleUrls: ['./data-table.css']
})
export class DataTable {
  @Input() columns: TableColumn[] = [];
  @Input() data: any[] = [];
  
  @Output() actionClick = new EventEmitter<{ row: any, action: string }>();

  onAction(row: any, action: string) {
    this.actionClick.emit({ row, action });
  }
}