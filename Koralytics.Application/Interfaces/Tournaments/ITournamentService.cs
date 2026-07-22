using Koralytics.Application.DTOs.Tournament;
using Koralytics.Application.DTOs.Tournaments;
using Koralytics.Domain.Enums;

namespace Koralytics.Application.Interfaces.Tournament
{
    public interface ITournamentService
    {
        Task<IEnumerable<TournamentDto>> GetAllAsync();
        Task<TournamentDto?> GetByIdAsync(int id);
        Task<TournamentDto> CreateTournamentAsync(CreateTournamentDto dto, int requestingUserId);
        Task InviteAcademyAsync(int tournamentId, int academyId);
        Task AcceptInvitationAsync(int tournamentId, int academyId);
        Task RegisterSquadAsync(int tournamentId, int teamId, List<int> playerIds);
        Task UpdateStatusAsync(int tournamentId, TournamentStatus status);
        Task<List<TournamentTeamDto>> GetTeamsAsync(int tournamentId);
        Task<List<int>> GetRegisteredPlayerIdsAsync(int tournamentId, int teamId);
    }
}