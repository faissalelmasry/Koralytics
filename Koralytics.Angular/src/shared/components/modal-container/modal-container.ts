import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ModalService,ModalOptions } from '../../../core/services/Modal/modal';
@Component({
  selector: 'app-modal-container',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './modal-container.html',
  styleUrls: ['./modal-container.css']
})
export class ModalContainerComponent implements OnInit {
  modalService = inject(ModalService);
  options: ModalOptions | null = null;

  ngOnInit() {
    this.modalService.modalState$.subscribe(state => {
      this.options = state;
    });
  }
}