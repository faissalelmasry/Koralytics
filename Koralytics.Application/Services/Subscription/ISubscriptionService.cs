using System.Collections.Generic;
using System.Threading.Tasks;
using Koralytics.Application.DTOs.Subscription;

namespace Koralytics.Application.Interfaces
{
    public interface ISubscriptionService
    {
        Task<IEnumerable<PlayerSubscriptionDto>> GetMyChildrenSubscriptionsAsync(int parentUserId);
        Task<bool> PaySubscriptionAsync(int subscriptionId, int paidByUserId);
        Task<bool> MarkAsPaidByCashAsync(int subscriptionId, int adminUserId);
        Task<PlayerSubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionDto dto);
        Task<IEnumerable<PlayerSubscriptionDto>> GetAcademySubscriptionsAsync(int academyId);
        Task<PaymentIntentResponseDto?> CreatePaymentIntentAsync(int subscriptionId, int parentUserId);
        Task<PlayerSubscriptionDto> CreateCustomSubscriptionAsync(CreateSubscriptionDto dto);
        Task<IEnumerable<PlayerSubscriptionDto>> GetPlayerSubscriptionHistoryAsync(int playerId);
    }
}