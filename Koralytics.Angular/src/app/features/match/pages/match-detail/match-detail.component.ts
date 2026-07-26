import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { MatchService } from '../../../../../core/services/match/match.service';
import { MatchTimelineComponent } from '../../match-timeline/match-timeline.component';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../../shared/components/footer/footer';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';

@Component({
  selector: 'app-match-detail',
  standalone: true,
  imports: [CommonModule, MatchTimelineComponent, NavbarComponent, Footer, LoadingSpinnerComponent],
  templateUrl: './match-detail.component.html',
  styleUrls: ['./match-detail.component.css']
})
export class MatchDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private matchService = inject(MatchService);
  private cdr = inject(ChangeDetectorRef);

  matchId!: number;
  isLoading = true;
  error = '';

  matchInfo: any = {
    homeTeam: '',
    awayTeam: '',
    homeScore: 0,
    awayScore: 0,
    status: '',
    homeTeamId: 0,
    awayTeamId: 0
  };

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.matchId = Number(id);
      this.loadMatch();
    }
  }

  loadMatch(): void {
    this.isLoading = true;
    this.matchService.getMatch(this.matchId).subscribe({
      next: (res) => {
        const m = res.data ?? res;
        this.matchInfo = {
          homeTeam: m.homeTeamName ?? m.HomeTeamName ?? '',
          awayTeam: m.awayTeamName ?? m.AwayTeamName ?? '',
          homeAcademy: m.homeTeamAcademyName ?? m.HomeTeamAcademyName ?? '',
          awayAcademy: m.awayTeamAcademyName ?? m.AwayTeamAcademyName ?? '',
          homeScore: m.homeScore ?? m.HomeScore ?? 0,
          awayScore: m.awayScore ?? m.AwayScore ?? 0,
          status: m.status ?? m.Status ?? '',
          homeTeamId: m.homeTeamId ?? m.HomeTeamId ?? 0,
          awayTeamId: m.awayTeamId ?? m.AwayTeamId ?? 0
        };
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.error = 'Failed to load match details.';
        this.cdr.detectChanges();
      }
    });
  }
}
