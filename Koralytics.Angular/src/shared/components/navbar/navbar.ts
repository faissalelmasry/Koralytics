import { Component, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './navbar.html',
  styleUrls: ['./navbar.css']
})
export class NavbarComponent {
  variant = signal<'primary' | 'icon'>('icon'); 
  
  isSidebarOpen = false;

  toggleSidebar(status: boolean) {
    this.isSidebarOpen = status;
  }
}