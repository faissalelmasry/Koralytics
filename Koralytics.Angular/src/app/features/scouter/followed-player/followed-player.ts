import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

import { ScouterService } from '../../../../core/services/Scouter/scouter.service';
import { PlayerCardDto } from '../../../../core/interfaces/Scouter.interfaces';
import { ToastService } from '../../../../core/services/Toast/toast';
import { SearchBarComponent } from '../../../../shared/components/search-bar/search-bar';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../shared/components/footer/footer';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { PlayerCardComponent } from '../../player/player-card/player-card';

@Component({
  selector: 'app-followed-players',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    SearchBarComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    CustomButtonComponent,
    Pagination,
    NavbarComponent,
    Footer,
    ScrollRevealDirective,
    PlayerCardComponent
  ],
  templateUrl: './followed-player.html',
  styleUrls: ['./followed-player.css']
})
export class FollowedPlayersComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private scouterService = inject(ScouterService);
  private tokenStorage = inject(TokenStorageService);
  private toastService = inject(ToastService);
  private destroyRef = inject(DestroyRef);
  public players = signal<PlayerCardDto[]>([]);
  public totalCount = signal<number>(0);
  public isLoading = signal<boolean>(true);
  public isUnfollowing = signal<number | null>(null);
  public pageNumber = signal<number>(1);
  public searchTerm = signal<string>('');

  public readonly PAGE_SIZE = 10;
  private currentScouterId = 0;
  private searchSubject = new Subject<string>();

  ngOnInit(): void {
    const paramId = this.route.snapshot.paramMap.get('scouterId');

    if (paramId) {
      this.currentScouterId = Number(paramId);
    } else {
      const token = this.tokenStorage.getAccessToken();
      const decoded = token ? this.decodeTokenPayload(token) : null;
      if (!decoded) {
        this.toastService.show('Unable to determine current scouter. Please log in again.', 'error');
        this.isLoading.set(false);
        return;
      }
      this.currentScouterId = decoded.userId;
    }

    this.loadPlayers();

    this.searchSubject.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((term) => {
      this.searchTerm.set(term);
      this.resetAndLoad();
    });
  }

  public onSearch(event: string | Event): void {
    const value = typeof event === 'string' ? event : ((event.target as HTMLInputElement)?.value ?? '');
    this.searchSubject.next(value);
  }

  public loadPlayers(): void {
    this.isLoading.set(true);

    this.scouterService.getFollowedPlayers(
      this.currentScouterId,
      this.pageNumber(),
      this.PAGE_SIZE,
      this.searchTerm()
    )
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe({
      next: (result) => {
        if (result.items.length === 0 && this.pageNumber() > 1) {
          this.pageNumber.update(p => p - 1);
          this.loadPlayers();
          return;
        }

        this.players.set(result.items);
        this.totalCount.set(result.totalCount);
        this.isLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        console.error('Failed to load followed players', err);
        this.toastService.show('Failed to load players.', 'error');
        this.isLoading.set(false);
      }
    });
  }

  public goToPage(page: number): void {
    if (page < 1 || page === this.pageNumber() || this.isLoading()) return;
    this.pageNumber.set(page);
    this.loadPlayers();
  }

  public resetAndLoad(): void {
    this.pageNumber.set(1);
    this.loadPlayers();
  }

  public unfollow(playerId: number): void {
    if (this.isUnfollowing() === playerId) return;
    this.isUnfollowing.set(playerId);

    this.scouterService.unfollowPlayer(this.currentScouterId, playerId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toastService.show('Player unfollowed successfully.', 'success');
          this.isUnfollowing.set(null);
          
          this.loadPlayers();
        },
        error: (err: HttpErrorResponse) => {
          console.error('Failed to unfollow player', err);
          this.toastService.show('Failed to unfollow player. Please try again.', 'error');
          this.isUnfollowing.set(null);
        }
      });
  }

  public viewProfile(playerId: number): void {
    this.router.navigate(['/player/profile/', playerId]);
  }

  private decodeTokenPayload(token: string): { userId: number; roles: string[] } | null {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;

      let payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      while (payload.length % 4) payload += '=';
      const decoded = JSON.parse(atob(payload));

      const userId = parseInt(
        decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? '0',
        10
      );
      if (!userId) return null;

      const rawRoles = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      const roles: string[] = Array.isArray(rawRoles) ? rawRoles : rawRoles ? [rawRoles] : [];

      return { userId, roles };
    } catch {
      return null;
    }
  }
}