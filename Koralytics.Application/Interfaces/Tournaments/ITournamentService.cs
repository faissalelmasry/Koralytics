using Koralytics.Application.DTOs.Tournament;

namespace Koralytics.Application.Interfaces.Tournament
{
    public interface ITournamentService
    {
        Task<TournamentDto> CreateTournamentAsync(CreateTournamentDto dto, int requestingUserId);
        Task InviteAcademyAsync(int tournamentId, int academyId);
        Task AcceptInvitationAsync(int tournamentId, int academyId);
        Task RegisterSquadAsync(int tournamentId, int teamId, List<int> playerIds);
    }
}