import { DifficultyLevel, DrillMode, SessionStatus, SessionType } from '../enums/koralytics.enums';

export interface SessionFilterDto {
    pageNumber: number;
    pageSize: number;
    teamId?: number | null;
    fromDate?: string | null;
    toDate?: string | null;
    status?: SessionStatus | null;
    searchTerm?: string | null;
}

// ==========================================
// READ DTOS (Responses)
// ==========================================

export interface DrillSessionDto {
    id: number;
    academyId: number;
    teamId: number;
    coachId: number;
    sessionDate: string;
    coachName?: string | null;
    teamName?: string | null;
    type: SessionType;
    status: SessionStatus;
    notes?: string | null;
    location?: string | null;
}

export interface DrillDto {
    id: number;
    sessionId: number;
    drillTemplateId: number;
    mode: DrillMode;
    difficultyLevel: DifficultyLevel;
    notes?: string | null;
}

export interface DrillSessionDetailsDto {
    id: number;
    academyId: number;
    teamId: number;
    coachId: number;
    sessionDate: string;
    type: SessionType;
    status: SessionStatus;
    notes?: string | null;
    sessionDrills: DrillDto[];
    location?: string | null;
}

// ==========================================
// WRITE DTOS (Requests)
// ==========================================

export interface CreateDrillSessionDto {
    teamId: number;
    sessionDate: string;
    type: SessionType;
    status: SessionStatus;
    notes?: string | null;
    playerIds: number[];
    location?: string | null;
}

export interface UpdateDrillSessionDto {
    sessionDate: string;
    type: SessionType;
    status: SessionStatus;
    notes?: string | null;
    location?: string | null;
}

export interface AddSessionDrillDto {
    drillTemplateId: number;
    mode: DrillMode;
    difficultyLevel: DifficultyLevel;
    notes?: string | null;
}