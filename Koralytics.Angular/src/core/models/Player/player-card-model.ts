export interface PlayerCardModel {
  playerName: string;
  position: string;
  overallRating: number;
  paceRating: number;
  dribblingRating: number;
  shootingRating: number;
  defendingRating: number;
  passingRating: number;
  physicalRating: number;
  goalkeepingRating: number | null;
  overallTrainingAvg: number;
  overallTournamentAvg: number;
  transferClassification: string;
  archetypePlayerName: string | null;
  playStyleTag: string | null;
  preferredFoot: string;
  weakFootRating: number;
  profileImageUrl: string | null;
}