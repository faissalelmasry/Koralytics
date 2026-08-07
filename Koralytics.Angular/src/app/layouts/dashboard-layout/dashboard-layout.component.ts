import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar';
import { Footer } from '../../../shared/components/footer/footer';
import { ScrollRevealDirective } from '../../../shared/directives/scroll-reveal.directive';
import { SignalRService } from '../../../core/services/SignalR/signalrservice';
import { TokenStorageService } from '../../../core/services/auth/token-storage.service';
import { ToastContainerComponent } from '../../../shared/components/toast/toast';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, NavbarComponent, Footer, ScrollRevealDirective],
  templateUrl: './dashboard-layout.component.html',
  styleUrls: ['./dashboard-layout.component.css']
})
export class DashboardLayoutComponent {
  private signalRService = inject(SignalRService);
  private tokenStorage = inject(TokenStorageService);

  ngOnInit(): void {
   
  }
 
  ngOnDestroy(): void {
   
  }
}
