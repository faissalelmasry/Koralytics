// ── Player Highlight DTOs ──
// Matched 1:1 against C# DTO in Koralytics.Application/DTOs/Player/PlayerHighlightDto.cs

export interface PlayerHighlightDto {
  id: number;
  playerId: number;
  academyId: number;
  videoUrl: string;
  title?: string;
  isPinned: boolean;
  uploadedAt: string;
}
