using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Koralytics.Application.DTOs.Subscription;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Parents;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace Koralytics.Application.Services.Subscription
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public SubscriptionService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        // ==========================================
        // 🟢 OPTIMIZATION: Centralized EF Core Projection
        // Write the mapping once, reuse it everywhere.
        // ==========================================
        private static Expression<Func<PlayerSubscription, PlayerSubscriptionDto>> MapToDto()
        {
            return s => new PlayerSubscriptionDto
            {
                Id = s.Id,
                PlayerId = s.PlayerId,
                PlayerName = (s.Player.FirstName + " " + s.Player.LastName).Trim(),
                AcademyId = s.AcademyId,
                AcademyName = s.Academy.Name,
                AcademyTier = s.Academy.Subscription != null ? s.Academy.Subscription.Tier : SubscriptionTier.Starter,
                Amount = s.Amount,
                Status = s.Status,
                Duration = s.Duration,
                StartDate = s.StartDate,
                DueDate = s.DueDate,
                PaidAt = s.PaidAt,
                GraceUntil = s.GraceUntil,
                PaidByUserId = s.PaidByUserId,
                PaidByUserName = s.PaidByUser != null
                    ? (s.PaidByUser.FirstName + " " + s.PaidByUser.LastName).Trim()
                    : null
            };
        }

        public async Task<IEnumerable<PlayerSubscriptionDto>> GetMyChildrenSubscriptionsAsync(int parentUserId)
        {
            // 🟢 OPTIMIZATION: Kept as IQueryable so EF Core generates a single SQL Subquery
            var childIdsQuery = _unitOfWork.Repository<ParentPlayer>()
                .GetQueryableAsNoTracking()
                .Where(pp => pp.ParentId == parentUserId && !pp.IsDeleted)
                .Select(pp => pp.PlayerId);

            return await _unitOfWork.Repository<PlayerSubscription>()
                .GetQueryableAsNoTracking()
                .Where(s => childIdsQuery.Contains(s.PlayerId))
                .OrderByDescending(s => s.DueDate)
                .Select(MapToDto())
                .ToListAsync();
        }

        public async Task<bool> PaySubscriptionAsync(int subscriptionId, int paidByUserId)
        {
            var subRepo = _unitOfWork.Repository<PlayerSubscription>();

            // 🟢 OPTIMIZATION: Fetches and authorizes in a single database round-trip
            var subscription = await subRepo.GetQueryable()
                .FirstOrDefaultAsync(s => s.Id == subscriptionId &&
                    _unitOfWork.Repository<ParentPlayer>().GetQueryableAsNoTracking()
                    .Any(pp => pp.ParentId == paidByUserId && pp.PlayerId == s.PlayerId && !pp.IsDeleted));

            if (subscription == null)
                return false;

            subscription.Status = SubscriptionStatus.Paid;
            subscription.PaidAt = DateTime.UtcNow;
            subscription.PaidByUserId = paidByUserId;

            subRepo.Update(subscription); // FORCE UPDATE
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> MarkAsPaidByCashAsync(int subscriptionId, int adminUserId)
        {
            var subscription = await _unitOfWork.Repository<PlayerSubscription>().GetByIdAsync(subscriptionId);

            if (subscription == null)
                return false;

            if (subscription.Status == SubscriptionStatus.Paid)
                return true;

            subscription.Status = SubscriptionStatus.Paid;
            subscription.PaidAt = DateTime.UtcNow;
            subscription.PaidByUserId = adminUserId;

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<PlayerSubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionDto dto)
        {
            var subRepo = _unitOfWork.Repository<PlayerSubscription>();

            var subscription = new PlayerSubscription
            {
                PlayerId = dto.PlayerId,
                AcademyId = dto.AcademyId,
                Amount = dto.Amount,
                Status = SubscriptionStatus.Unpaid
            };

            var startDate = dto.StartDate ?? DateTime.UtcNow;
            subscription.SetBillingCycle(startDate, dto.Duration);

            await subRepo.AddAsync(subscription);
            await _unitOfWork.SaveChangesAsync();

            return (await GetSubscriptionByIdAsync(subscription.Id))!;
        }

        public async Task<PlayerSubscriptionDto> CreateCustomSubscriptionAsync(CreateSubscriptionDto dto)
        {
            var subRepo = _unitOfWork.Repository<PlayerSubscription>();

            var startDate = dto.StartDate ?? DateTime.UtcNow;
            var dueDate = CalculateDueDate(startDate, dto.Duration);
            var graceUntil = dueDate.AddDays(7);

            var existingUnpaidSub = await subRepo.GetQueryable()
                .FirstOrDefaultAsync(s =>
                    s.PlayerId == dto.PlayerId &&
                    s.AcademyId == dto.AcademyId &&
                    s.Status == SubscriptionStatus.Unpaid
                );

            int targetSubId;

            if (existingUnpaidSub != null)
            {
                existingUnpaidSub.Amount = dto.Amount;
                existingUnpaidSub.Duration = dto.Duration;
                existingUnpaidSub.StartDate = startDate;
                existingUnpaidSub.DueDate = dueDate;
                existingUnpaidSub.GraceUntil = graceUntil;

                await _unitOfWork.SaveChangesAsync();
                targetSubId = existingUnpaidSub.Id;
            }
            else
            {
                var newSub = new PlayerSubscription
                {
                    PlayerId = dto.PlayerId,
                    AcademyId = dto.AcademyId,
                    Amount = dto.Amount,
                    Duration = dto.Duration,
                    Status = SubscriptionStatus.Unpaid,
                    StartDate = startDate,
                    DueDate = dueDate,
                    GraceUntil = graceUntil
                };

                await subRepo.AddAsync(newSub);
                await _unitOfWork.SaveChangesAsync();
                targetSubId = newSub.Id;
            }

            return (await GetSubscriptionByIdAsync(targetSubId))!;
        }

        public async Task<IEnumerable<PlayerSubscriptionDto>> GetPlayerSubscriptionHistoryAsync(int playerId)
        {
            return await _unitOfWork.Repository<PlayerSubscription>()
                .GetQueryableAsNoTracking()
                .Where(s => s.PlayerId == playerId)
                .OrderByDescending(s => s.StartDate)
                .Select(MapToDto())
                .ToListAsync();
        }

        private async Task<PlayerSubscriptionDto?> GetSubscriptionByIdAsync(int subId)
        {
            return await _unitOfWork.Repository<PlayerSubscription>()
                .GetQueryableAsNoTracking()
                .Where(s => s.Id == subId)
                .Select(MapToDto())
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<PlayerSubscriptionDto>> GetAcademySubscriptionsAsync(int academyId)
        {
            return await _unitOfWork.Repository<PlayerSubscription>()
                .GetQueryableAsNoTracking()
                .Where(s => s.AcademyId == academyId)
                .OrderByDescending(s => s.DueDate)
                .Select(MapToDto())
                .ToListAsync();
        }

        public async Task<PaymentIntentResponseDto?> CreatePaymentIntentAsync(int subscriptionId, int parentUserId)
        {
            // 🟢 OPTIMIZATION: Lightweight projection combined with the security check. 
            // We only fetch the exact 3 columns needed for Stripe, not the whole row.
            var subscriptionData = await _unitOfWork.Repository<PlayerSubscription>()
                .GetQueryableAsNoTracking()
                .Where(s => s.Id == subscriptionId &&
                            _unitOfWork.Repository<ParentPlayer>().GetQueryableAsNoTracking()
                            .Any(pp => pp.ParentId == parentUserId && pp.PlayerId == s.PlayerId && !pp.IsDeleted))
                .Select(s => new { s.Id, s.Amount, s.PlayerId })
                .FirstOrDefaultAsync();

            if (subscriptionData == null)
                return null;

            var secretKey = _configuration["OAuth:Stripe:SecretKey"] ?? _configuration["Stripe:SecretKey"];
            var publishableKey = _configuration["OAuth:Stripe:PublishableKey"] ?? _configuration["Stripe:PublishableKey"];

            if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(publishableKey))
            {
                throw new InvalidOperationException("Stripe API keys are missing from configuration.");
            }

            StripeConfiguration.ApiKey = secretKey;

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(subscriptionData.Amount * 100),
                Currency = "egp",
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = new Dictionary<string, string>
                {
                    { "SubscriptionId", subscriptionData.Id.ToString() },
                    { "PlayerId", subscriptionData.PlayerId.ToString() }
                }
            };

            var service = new PaymentIntentService();
            PaymentIntent intent = await service.CreateAsync(options);

            return new PaymentIntentResponseDto
            {
                ClientSecret = intent.ClientSecret,
                PublishableKey = publishableKey
            };
        }

        private DateTime CalculateDueDate(DateTime startDate, SubscriptionDuration duration)
        {
            return duration switch
            {
                SubscriptionDuration.OneMonth => startDate.AddMonths(1),
                SubscriptionDuration.ThreeMonths => startDate.AddMonths(3),
                SubscriptionDuration.SixMonths => startDate.AddMonths(6),
                SubscriptionDuration.OneYear => startDate.AddYears(1),
                _ => startDate.AddMonths(1)
            };
        }
    }
}