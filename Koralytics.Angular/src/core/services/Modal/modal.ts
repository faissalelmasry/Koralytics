import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export interface ModalOptions {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  variant?: 'danger' | 'success' | 'info';
}

@Injectable({
  providedIn: 'root'
})
export class ModalService {
  private modalSequence = new Subject<ModalOptions | null>();
  modalState$ = this.modalSequence.asObservable();

  private resolver: ((value: boolean) => void) | null = null;

  open(options: ModalOptions): Promise<boolean> {
    this.modalSequence.next(options);
    
    return new Promise<boolean>((resolve) => {
      this.resolver = resolve;
    });
  }

  confirm() {
    if (this.resolver) this.resolver(true);
    this.close();
  }

  cancel() {
    if (this.resolver) this.resolver(false);
    this.close();
  }

  private close() {
    this.modalSequence.next(null);
    this.resolver = null;
  }
}