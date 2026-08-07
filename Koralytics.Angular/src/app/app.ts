import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationStart, NavigationEnd, NavigationCancel, NavigationError } from '@angular/router';
import { ToastContainerComponent } from '../shared/components/toast/toast';
import { ModalContainerComponent } from '../shared/components/modal-container/modal-container';
import { LoadingSpinnerComponent } from '../shared/components/loading-spinner/loading-spinner';
import { FootballPitch } from "../shared/components/football-pitch/football-pitch";
import { SubscriptionLockedComponent } from '../shared/components/subscription-locked/subscription-locked';
import { AuthService } from '../core/services/auth/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ToastContainerComponent,
    ModalContainerComponent,
    LoadingSpinnerComponent,
    FootballPitch,
    SubscriptionLockedComponent
],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App implements OnInit {
  private router = inject(Router);
  public authService = inject(AuthService);

  loading = true;
  isSubscriptionActive$ = this.authService.isSubscriptionActive$;

  ngOnInit() {
    this.router.events.subscribe(e => {
      if (e instanceof NavigationStart) {
        this.loading = true;
      } else if (
        e instanceof NavigationEnd ||
        e instanceof NavigationCancel ||
        e instanceof NavigationError
      ) {
        this.loading = false;
      }
    });
  }
}