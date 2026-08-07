import { Component, Input, Output, EventEmitter, ContentChild, TemplateRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../pipes/localized-date.pipe';

export interface TableColumn {
  key: string;       
  label: string;     
  type?: 'text' | 'badge' | 'action' | 'user' | 'date'; 
  translate?: boolean;
}

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [CommonModule, TranslatePipe, LocalizedDatePipe],
  templateUrl: './data-table.html',
  styleUrls: ['./data-table.css']
})
export class DataTable {
  @Input() columns: TableColumn[] = [];
  @Input() data: any[] = [];
  @Input() expandable = false;
  
  @ContentChild('expandedRowTemplate') expandedRowTemplate!: TemplateRef<any>;
  
  @Output() actionClick = new EventEmitter<{ row: any, action: string }>();

  expandedRows = new Set<any>();

  onAction(row: any, action: string) {
    this.actionClick.emit({ row, action });
  }

  toggleRow(row: any) {
    const id = row.id !== undefined ? row.id : row;
    if (this.expandedRows.has(id)) {
      this.expandedRows.delete(id);
    } else {
      this.expandedRows.add(id);
    }
  }

  isRowExpanded(row: any): boolean {
    const id = row.id !== undefined ? row.id : row;
    return this.expandedRows.has(id);
  }
}