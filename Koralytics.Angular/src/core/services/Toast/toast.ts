import { Injectable, NgZone } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type ToastType = 'success' | 'error' | 'info' | 'warning';

export interface Toast {
  id: number;
  type: ToastType;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private toastsSubject = new BehaviorSubject<Toast[]>([]);
  toasts$ = this.toastsSubject.asObservable();
  private counter = 0;

  
  constructor(private ngZone: NgZone) {} 

  show(message: string, type: ToastType = 'info', duration: number = 4000) {
   
    this.ngZone.run(() => {
      const id = this.counter++;
      const currentToasts = this.toastsSubject.value;
      
      this.toastsSubject.next([...currentToasts, { id, type, message }]);

     
      setTimeout(() => {
        this.clear(id);
      }, duration);
    });
  }

  clear(id: number) {
    this.ngZone.run(() => {
      const currentToasts = this.toastsSubject.value;
      this.toastsSubject.next(currentToasts.filter(t => t.id !== id));
    });
  }
}