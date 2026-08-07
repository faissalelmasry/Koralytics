using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<IEnumerable<PlayerSubscriptionDto>> GetMyChildrenSubscriptionsAsync(int parentUserId)
        {
            // 1. Fetch player IDs linked to this parent
            var parentPlayerRepo = _unitOfWork.Repository<ParentPlayer>();
            var childPlayerIds = await parentPlayerRepo.GetQueryableAsNoTracking()
                .Where(pp => pp.ParentId == parentUserId)
                .Select(pp => pp.PlayerId)
                .ToListAsync();

            if (!childPlayerIds.Any())
                return Enumerable.Empty<PlayerSubscriptionDto>();

            // 2. Query subscriptions with clean projection
            var subRepo = _unitOfWork.Repository<PlayerSubscription>();

            return await subRepo.GetQueryableAsNoTracking()
                .Where(s => childPlayerIds.Contains(s.PlayerId))
                .OrderByDescending(s => s.DueDate)
                .Select(s => new PlayerSubscriptionDto
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
                })
                .ToListAsync();
        }

        public async Task<bool> PaySubscriptionAsync(int subscriptionId, int paidByUserId)
        {
            var subRepo = _unitOfWork.Repository<PlayerSubscription>();
            var subscription = await subRepo.GetByIdAsync(subscriptionId);

            if (subscription == null)
                return false;

            // SECURITY FIX: Validate that this subscription belongs to one of the parent's children
            var parentPlayerRepo = _unitOfWork.Repository<ParentPlayer>();
            var isChildOfParent = await parentPlayerRepo.GetQueryableAsNoTracking()
                .AnyAsync(pp => pp.ParentId == paidByUserId && pp.PlayerId == subscription.PlayerId);

            if (!isChildOfParent)
                return false;

            subscription.Status = SubscriptionStatus.Paid;
            subscription.PaidAt = DateTime.UtcNow;
            subscription.PaidByUserId = paidByUserId;

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> MarkAsPaidByCashAsync(int subscriptionId, int adminUserId)
        {
            var subRepo = _unitOfWork.Repository<PlayerSubscription>();
            var subscription = await subRepo.GetByIdAsync(subscriptionId);

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

        /// <summary>
        /// Admin custom plan creation or override logic for auto-created unpaid subscriptions.
        /// </summary>
        public async Task<PlayerSubscriptionDto> CreateCustomSubscriptionAsync(CreateSubscriptionDto dto)
        {
            var subRepo = _unitOfWork.Repository<PlayerSubscription>();

            var startDate = dto.StartDate ?? DateTime.UtcNow;
            var dueDate = CalculateDueDate(startDate, dto.Duration);
            var graceUntil = dueDate.AddDays(7);

            // 1. Check if an UNPAID subscription already exists for this child in this academy
            var existingUnpaidSub = await subRepo.GetQueryable()
                .FirstOrDefaultAsync(s =>
                    s.PlayerId == dto.PlayerId &&
                    s.AcademyId == dto.AcademyId &&
                    s.Status == SubscriptionStatus.Unpaid
                );

            int targetSubId;

            if (existingUnpaidSub != null)
            {
                // 🟢 OVERRIDE: Update the existing auto-generated unpaid subscription
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
                // 🟢 CREATE NEW: Create a fresh custom unpaid subscription
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
        /// <summary>
        /// Retrieves the complete subscription and payment history for a specific player profile.
        /// </summary>
        public async Task<IEnumerable<PlayerSubscriptionDto>> GetPlayerSubscriptionHistoryAsync(int playerId)
        {
            var subRepo = _unitOfWork.Repository<PlayerSubscription>();

            return await subRepo.GetQueryableAsNoTracking()
                .Where(s => s.PlayerId == playerId)
                .OrderByDescending(s => s.StartDate)
                .Select(s => new PlayerSubscriptionDto
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
                })
                .ToListAsync();
        }

        private async Task<PlayerSubscriptionDto?> GetSubscriptionByIdAsync(int subId)
        {
            var subRepo = _unitOfWork.Repository<PlayerSubscription>();

            return await subRepo.GetQueryableAsNoTracking()
                .Where(s => s.Id == subId)
                .Select(s => new PlayerSubscriptionDto
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
                })
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<PlayerSubscriptionDto>> GetAcademySubscriptionsAsync(int academyId)
        {
            var subRepo = _unitOfWork.Repository<PlayerSubscription>();

            return await subRepo.GetQueryableAsNoTracking()
                .Where(s => s.AcademyId == academyId)
                .OrderByDescending(s => s.DueDate)
                .Select(s => new PlayerSubscriptionDto
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
                })
                .ToListAsync();
        }

        public async Task<PaymentIntentResponseDto?> CreatePaymentIntentAsync(int subscriptionId, int parentUserId)
        {
            var subRepo = _unitOfWork.Repository<PlayerSubscription>();
            var subscription = await subRepo.GetByIdAsync(subscriptionId);

            if (subscription == null)
                return null;

            var parentPlayerRepo = _unitOfWork.Repository<ParentPlayer>();
            var isChildOfParent = await parentPlayerRepo.GetQueryableAsNoTracking()
                .AnyAsync(pp => pp.ParentId == parentUserId && pp.PlayerId == subscription.PlayerId);

            if (!isChildOfParent)
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
                Amount = (long)(subscription.Amount * 100),
                Currency = "egp",
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = new Dictionary<string, string>
                {
                    { "SubscriptionId", subscription.Id.ToString() },
                    { "PlayerId", subscription.PlayerId.ToString() }
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