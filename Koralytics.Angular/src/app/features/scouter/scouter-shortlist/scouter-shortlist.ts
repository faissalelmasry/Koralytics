import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
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
import { PlayerCardComponent } from '../../player/player-card/player-card';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../shared/components/footer/footer';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';


@Component({
  selector: 'app-scouter-shortlist',
  standalone: true,

  imports: [
    CommonModule,
    FormsModule,
    SearchBarComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    CustomButtonComponent,
    Pagination,
    PlayerCardComponent,
    NavbarComponent,
    Footer,
    ScrollRevealDirective
  ],
  templateUrl: './scouter-shortlist.html',
  styleUrls: ['./scouter-shortlist.css']
})
export class ScouterShortlistComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private scouterService = inject(ScouterService);
  private tokenStorage = inject(TokenStorageService);
  private toastService = inject(ToastService);
  private destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);

  public players = signal<PlayerCardDto[]>([]);
  public totalCount = signal<number>(0);
  public isLoading = signal<boolean>(true);
  public isRemoving = signal<number | null>(null);
  public pageNumber = signal<number>(1);
  public searchTerm = signal<string>('');

  public readonly PAGE_SIZE = 12;
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

    this.loadShortlist();

    this.searchSubject.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((term) => {
      this.searchTerm.set(term);
      this.resetAndLoad();
    });
  }

  public onSearch(term: string): void {
    this.searchSubject.next(term);
  }

  public loadShortlist(): void {
    this.isLoading.set(true);

    this.scouterService.getShortlist(
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
          this.loadShortlist();
          return;
        }

        this.players.set(result.items);
        this.totalCount.set(result.totalCount);
        this.isLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        console.error('Failed to load shortlist', err);
        this.toastService.show('Failed to load your shortlist.', 'error');
        this.isLoading.set(false);
      }
    });
  }

  public goToPage(page: number): void {
    if (page < 1 || page === this.pageNumber() || this.isLoading()) return;
    this.pageNumber.set(page);
    this.loadShortlist();
  }

  public resetAndLoad(): void {
    this.pageNumber.set(1);
    this.loadShortlist();
  }

  public removeFromShortlist(playerId: number): void {
    if (this.isRemoving() === playerId) return;
    this.isRemoving.set(playerId);

    this.scouterService.removeFromShortlist(this.currentScouterId, playerId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toastService.show('Removed from shortlist.', 'success');
          this.isRemoving.set(null);
          
          this.loadShortlist();
        },
        error: (err: HttpErrorResponse) => {
          console.error('Failed to remove player from shortlist', err);
          this.toastService.show('Failed to remove player. Please try again.', 'error');
          this.isRemoving.set(null);
        }
      });
  }
  public viewProfile(playerId: number): void {
  if (!playerId) return;
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