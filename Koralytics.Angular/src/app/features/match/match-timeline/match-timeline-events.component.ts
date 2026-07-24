import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MiniPlayerCardComponent } from '../mini-player-card/mini-player-card.component';
import { TimelineEvent } from '../match-timeline/match-timeline.types';

@Component({
  selector: 'app-match-timeline-events',
  standalone: true,
  imports: [CommonModule, MiniPlayerCardComponent],
  templateUrl: './match-timeline-events.component.html',
  styleUrls: ['./match-timeline-events.component.css']
})
export class MatchTimelineEventsComponent {
  @Input({ required: true }) events!: TimelineEvent[];
}
