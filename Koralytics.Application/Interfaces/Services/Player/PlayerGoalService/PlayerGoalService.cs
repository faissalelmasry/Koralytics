using AutoMapper;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;
using AcademyEntity = Koralytics.Domain.Entities.Academy.Academy;

namespace Koralytics.Application.Services.Player.PlayerGoalService
{
    public class PlayerGoalService : IPlayerGoalService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<PlayerGoalService> _logger;

        public PlayerGoalService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<PlayerGoalService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PlayerGoalDto> CreatePlayerGoalAsync(int playerId, CreatePlayerGoalDto dto)
        {
            _logger.LogInformation("Creating goal for player {PlayerId} in category {Category}",
                playerId, dto.Category);

            var playerExists = await _unitOfWork.Repository<PlayerEntity>()
                .ExistsAsync(p => p.Id == playerId);

            if (!playerExists)
                throw new NotFoundException($"Player with Id {playerId} not found");

            var academyExists = await _unitOfWork.Repository<AcademyEntity>()
                .ExistsAsync(a => a.Id == dto.AcademyId);

            if (!academyExists)
                throw new NotFoundException($"Academy with Id {dto.AcademyId} not found");

            var goal = _mapper.Map<PlayerGoal>(dto);
            goal.PlayerId = playerId;
            goal.Achieved = false;

            await _unitOfWork.Repository<PlayerGoal>().AddAsync(goal);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Goal {GoalId} created for player {PlayerId}", goal.Id, playerId);

            return _mapper.Map<PlayerGoalDto>(goal);
        }

        public async Task<PlayerGoalDto> UpdatePlayerGoalAsync(int goalId, UpdatePlayerGoalDto dto)
        {
            _logger.LogInformation("Updating goal {GoalId}, achieved: {Achieved}", goalId, dto.Achieved);

            var goal = await _unitOfWork.Repository<PlayerGoal>()
                .GetQueryable()
                .FirstOrDefaultAsync(g => g.Id == goalId);

            if (goal is null)
                throw new NotFoundException($"PlayerGoal with Id {goalId} not found");

            goal.Achieved = dto.Achieved;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Goal {GoalId} updated", goalId);

            return _mapper.Map<PlayerGoalDto>(goal);
        }
    }
}
