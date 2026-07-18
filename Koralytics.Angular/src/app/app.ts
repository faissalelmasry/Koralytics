import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ToastContainerComponent } from '../shared/components/toast/toast';
import { ModalContainerComponent } from '../shared/components/modal-container/modal-container';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ToastContainerComponent,
    ModalContainerComponent
  ],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App { }