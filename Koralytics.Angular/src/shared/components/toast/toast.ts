import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService, Toast } from '../../../core/services/Toast/toast';

import { Observable } from 'rxjs';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast.html',
  styleUrls: ['./toast.css']
})
export class ToastContainerComponent implements OnInit {
  toasts$!: Observable<Toast[]>;

  constructor(private toastService: ToastService) { }

  ngOnInit() {
    this.toasts$ = this.toastService.toasts$;
  }

  removeToast(id: number) {
    this.toastService.clear(id);
  }
}