using Koralytics.Application.DTOs.Tournament;
using Koralytics.Domain.Enums;

namespace Koralytics.Application.Interfaces.Tournament
{
    public interface ITournamentService
    {
        Task<TournamentDto> CreateTournamentAsync(CreateTournamentDto dto, int requestingUserId);
        Task InviteAcademyAsync(int tournamentId, int academyId);
        Task AcceptInvitationAsync(int tournamentId, int academyId);
        Task RegisterSquadAsync(int tournamentId, int teamId, List<int> playerIds);
        Task UpdateStatusAsync(int tournamentId, TournamentStatus status);
    }
}