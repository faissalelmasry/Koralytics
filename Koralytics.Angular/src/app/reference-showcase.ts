import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { LoadingSpinnerComponent } from '../shared/components/loading-spinner/loading-spinner';
import { NavbarComponent } from '../shared/components/navbar/navbar';
import { Footer } from '../shared/components/footer/footer';
import { CustomButtonComponent } from "../shared/components/custom-button/custom-button";
import { SearchBarComponent } from "../shared/components/search-bar/search-bar";
import { CustomInputComponent } from "../shared/components/custom-input-component/custom-input-component";
import { CustomSelect } from "../shared/components/custom-select/custom-select";
import { CustomDatePicker } from "../shared/components/custom-date-picker/custom-date-picker";
import { CustomToggle } from "../shared/components/custom-toggle/custom-toggle";
import { ToastContainerComponent } from "../shared/components/toast/toast";
import { ToastService } from '../core/services/Toast/toast';
import { ModalContainerComponent } from "../shared/components/modal-container/modal-container";
import { ModalService } from '../core/services/Modal/modal';
import { EmptyStateComponent } from "../shared/components/empty-state/empty-state";
import { DataTable,TableColumn } from '../shared/components/data-table/data-table';
import { StatusChipComponent } from "../shared/components/status-chip/status-chip";
import { Pagination } from "../shared/components/pagination/pagination";
import { FileUpload } from "../shared/components/file-upload/file-upload";
import { ImageUpload } from "../shared/components/image-upload/image-upload";
import { FilterState, FilterPanel } from '../shared/components/filter-panel/filter-panel';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    LoadingSpinnerComponent,
    NavbarComponent,
    Footer,
    CustomButtonComponent,
    SearchBarComponent,
    CustomInputComponent,
    CustomSelect,
    CustomDatePicker,
    CustomToggle,
    ToastContainerComponent,
    ModalContainerComponent,
    EmptyStateComponent,
    DataTable,
    StatusChipComponent,
    Pagination,
    FileUpload,
    ImageUpload,
    FilterPanel
],
  templateUrl: './reference-showcase.html',
  styleUrls: ['./reference-showcase.css']
})
export class App implements OnInit {
  isAppLoading = true;
  mySearch: string = '';
  playerName: string = '';
  selectedPosition: string = '';
  playerBirthDate: string = '';
  isAiAnalysisEnabled: boolean = true;

  tableColumns: TableColumn[] = [
    { key: 'name', label: 'player name', type: 'text' },
    { key: 'position', label: 'position', type: 'text' },
    { key: 'status', label: 'squad status', type: 'badge' },
    { key: 'actions', label: 'tracking hub', type: 'action' }
  ];

  playersData = [
    { id: 1, name: 'mohamed salah', position: 'right winger', status: 'active' },
    { id: 2, name: 'virgil van dijk', position: 'center back', status: 'active' },
    { id: 3, name: 'alisson becker', position: 'goalkeeper', status: 'injured' },
    { id: 4, name: 'harvey elliott', position: 'midfielder', status: 'pending' }
  ];

  // حقن الخدمات بشكل نظيف
  private modal = inject(ModalService);
  private toast = inject(ToastService);

  constructor(private cdr: ChangeDetectorRef) { }

  ngOnInit() {
    setTimeout(() => {
      this.isAppLoading = false;
      this.cdr.detectChanges();
    }, 1500);
  }

  // 1. تيست الـ Toasts ورا بعض
  triggerFakeTest() {
    this.toast.show('player video tracking started successfully', 'success', 4000);

    setTimeout(() => {
      this.toast.show('failed to upload match analysis footage', 'error', 5000);
    }, 800);

    setTimeout(() => {
      this.toast.show('tactical board configuration updated', 'info', 3000);
    }, 1500);

    setTimeout(() => {
      this.toast.show('squad stamina level is critically low', 'warning', 6000);
    }, 2200);
  }

  async quickModalTest() {
    const confirmed = await this.modal.open({
      title: 'quick test',
      message: 'is the modal service working perfectly?',
      variant: 'success'
    });

    if (confirmed) {
      this.toast.show('you clicked confirm!', 'success');
    } else {
      this.toast.show('you clicked cancel!', 'error');
    }
  }

  async handleTableAction(event: { row: any, action: string }) {
    const player = event.row;

    if (event.action === 'view') {
      this.toast.show(`initiating ai video tracking for ${player.name}...`, 'info', 3000);
    } else if (event.action === 'delete') {
      const confirmed = await this.modal.open({
        title: 'terminate player profile',
        message: `are you sure you want to remove ${player.name} from the active tactical radar database?`,
        confirmText: 'yes, delete profile',
        cancelText: 'abort',
        variant: 'danger'
      });

      if (confirmed) {
        this.playersData = this.playersData.filter(p => p.id !== player.id);
        this.toast.show(`${player.name} removed successfully`, 'success', 4000);
      } else {
        this.toast.show('operation aborted safely', 'info', 2000);
      }
    }
  }
  currentPage: number = 1;
pageSize: number = 2;
totalItems: number = 4;

onPageChanged(page: number) {
  this.currentPage = page;
  this.toast.show(`switched to tactical page ${page}`, 'info', 1500);
}

get paginatedPlayers() {
  const startIndex = (this.currentPage - 1) * this.pageSize;
  return this.playersData.slice(startIndex, startIndex + this.pageSize);
}

isFootageUploading = false;
uploadProgressValue = 0;

onMatchFootageSelected(file: File) {
  this.toast.show(`selected footage: ${file.name}`, 'info', 2000);
  
  this.isFootageUploading = true;
  this.uploadProgressValue = 0;

  const interval = setInterval(() => {
    if (this.uploadProgressValue < 100) {
      this.uploadProgressValue += 10; 
    } else {
      clearInterval(interval);
      this.isFootageUploading = false;
      this.toast.show('tactical match video analyzed via ai models successfully!', 'success', 4000);
    }
  }, 300);
}
isAvatarUploading = false;

onPlayerAvatarSelected(file: File) {
  this.isAvatarUploading = true;
  this.toast.show(`uploading radar scan identity for ${file.name}...`, 'info', 1500);

  setTimeout(() => {
    this.isAvatarUploading = false;
    this.toast.show('player identity visual profile synched successfully', 'success', 3000);
  }, 2000);
}
currentFilters!: FilterState;

onFilterChanged(updatedFilters: FilterState) {
  this.currentFilters = updatedFilters;
  this.currentPage = 1; 
}

get filteredAndPaginatedPlayers() {
  if (!this.currentFilters) return this.playersData;

  const filtered = this.playersData.filter(player => {
    const matchesSearch = player.name.toLowerCase().includes(this.currentFilters.search.toLowerCase());
    const matchesPosition = !this.currentFilters.position || player.position === this.currentFilters.position;
    const matchesStatus = !this.currentFilters.status || player.status === this.currentFilters.status;
    
    const matchesAi = !this.currentFilters.aiOnly || (player.position.includes('winger') || player.position.includes('back'));

    return matchesSearch && matchesPosition && matchesStatus && matchesAi;
  });

  const startIndex = (this.currentPage - 1) * this.pageSize;
  return filtered.slice(startIndex, startIndex + this.pageSize);
}

get totalFilteredItems(): number {
  if (!this.currentFilters) return this.playersData.length;
  return this.playersData.filter(player => {
    const matchesSearch = player.name.toLowerCase().includes(this.currentFilters.search.toLowerCase());
    const matchesPosition = !this.currentFilters.position || player.position === this.currentFilters.position;
    const matchesStatus = !this.currentFilters.status || player.status === this.currentFilters.status;
    const matchesAi = !this.currentFilters.aiOnly || (player.position.includes('winger') || player.position.includes('back'));
    return matchesSearch && matchesPosition && matchesStatus && matchesAi;
  }).length;
}
}