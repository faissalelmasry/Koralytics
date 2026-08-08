import { DifficultyLevel, DrillMode } from '../enums/koralytics.enums';

export interface DrillTemplateDto {
  id: number;
  name: string;
  categoryId: number;
  categoryName: string;
  academyId: number | null;
  academyName: string | null;
  difficultyLevel: DifficultyLevel;
  drillMode: DrillMode;
  isShared: boolean;
  videoUrl?: string | null;
  createdById: number;
}

export interface DrillCategoryDto {
  id: number;
  name: string;
}

export interface CreateDrillTemplateDto {
  name: string;
  categoryId: number;
  difficultyLevel: DifficultyLevel;
  drillMode: DrillMode;
  videoUrl?: string | null;
}

export interface UpdateDrillTemplateDto {
  name: string;
  categoryId: number;
  difficultyLevel: DifficultyLevel;
  drillMode: DrillMode;
  videoUrl?: string | null;
}

export interface TemplateFilterDto {
  pageNumber: number;
  pageSize: number;
  searchTerm?: string | null;
}