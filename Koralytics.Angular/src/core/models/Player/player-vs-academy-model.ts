export interface PlayerVsAcademyModel {
  playerId: number;
  playerName: string;
  academyId: number;
  academyName: string;
  ageGroupName: string | null;
  categories: CategoryComparisonModel[];
}

export interface CategoryComparisonModel {
  categoryId: number;
  categoryName: string;
  playerAverage: number;
  academyAverage: number;
  difference: number;
}
