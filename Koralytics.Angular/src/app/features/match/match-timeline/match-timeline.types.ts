import { MiniPlayerCardModel } from '../../../../core/models/Player/mini-player-card-model';

export interface TimelineEvent {
  minute: number;
  eventType: string;
  eventSubtext: string;
  rawType: string;
  side: 'home' | 'away';
  player: MiniPlayerCardModel;
  assistPlayerId?: number;
  accentColor: string;
}
