import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MiniPlayerCardComponent } from '../mini-player-card/mini-player-card.component';
import { MiniPlayerCardModel } from '../../../../core/models/Player/mini-player-card-model';

@Component({
  selector: 'app-match-lineups',
  standalone: true,
  imports: [CommonModule, MiniPlayerCardComponent],
  templateUrl: './match-lineups.component.html',
  styleUrls: ['./match-lineups.component.css']
})
export class MatchLineupsComponent {
  @Input({ required: true }) homeName!: string;
  @Input() homeFormation: string = '4-3-3';
  @Input({ required: true }) homeStarters!: MiniPlayerCardModel[][];
  @Input({ required: true }) homeBench!: MiniPlayerCardModel[];

  @Input({ required: true }) awayName!: string;
  @Input() awayFormation: string = '3-4-3';
  @Input({ required: true }) awayStarters!: MiniPlayerCardModel[][];
  @Input({ required: true }) awayBench!: MiniPlayerCardModel[];

  selectedTeam: 'home' | 'away' = 'home';

  selectTeam(team: 'home' | 'away'): void {
    this.selectedTeam = team;
  }
}
