using System.Security.Claims;
using Koralytics.API.Filters;
using Koralytics.Domain.Enums;
using System.Threading.Tasks;
using Koralytics.Application.DTOs.Subscription;
using Koralytics.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koralytics.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        /// <summary>
        /// Gets all subscription records for children linked to the currently logged-in parent.
        /// </summary>
        [HttpGet("my-children")]
        [Authorize(Roles = "Parent")]
        public async Task<IActionResult> GetMyChildrenSubscriptions()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int parentUserId))
            {
                return Unauthorized("Invalid user token.");
            }

            var result = await _subscriptionService.GetMyChildrenSubscriptionsAsync(parentUserId);
            return Ok(result);
        }

        /// <summary>
        /// Gets all subscription records for a specific academy.
        /// </summary>
        [HttpGet("academy/{academyId}")]
        [Authorize(Roles = "AcademyAdmin")]
        public async Task<IActionResult> GetAcademySubscriptions(int academyId)
        {
            var result = await _subscriptionService.GetAcademySubscriptionsAsync(academyId);
            return Ok(result);
        }

        /// <summary>
        /// Academy Admin issues a custom subscription plan or overrides an existing auto-created unpaid plan.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "AcademyAdmin")]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 🟢 Uses Step 2 logic: Updates existing Unpaid sub or creates a new one
            var createdSubscription = await _subscriptionService.CreateCustomSubscriptionAsync(dto);
            return Ok(createdSubscription);
        }

        /// <summary>
        /// Settles an unpaid subscription record (online/card path).
        /// </summary>
        [HttpPost("{id}/pay")]
        [Authorize(Roles = "Parent,AcademyAdmin")]
        [RequiresPlanFeature(TierFeature.StripePayments)]
        public async Task<IActionResult> PaySubscription(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized("Invalid user token.");
            }

            var success = await _subscriptionService.PaySubscriptionAsync(id, currentUserId);
            if (!success)
            {
                return NotFound($"Subscription with ID {id} was not found or access was denied.");
            }

            return Ok(new { message = "Subscription successfully settled." });
        }

        /// <summary>
        /// Academy Admin confirms cash payment received at the desk for a player subscription.
        /// </summary>
        [HttpPost("{id}/mark-paid-cash")]
        [Authorize(Roles = "AcademyAdmin")]
        public async Task<IActionResult> MarkAsPaidByCash(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int adminUserId))
            {
                return Unauthorized("Invalid user token.");
            }

            var success = await _subscriptionService.MarkAsPaidByCashAsync(id, adminUserId);
            if (!success)
            {
                return NotFound($"Subscription with ID {id} was not found or could not be updated.");
            }

            return Ok(new { message = "Cash payment confirmed. Subscription marked as Paid." });
        }

        /// <summary>
        /// Generates a Stripe Payment Intent ClientSecret for credit/debit card checkout in Angular.
        /// </summary>
        [HttpPost("{id}/create-payment-intent")]
        [Authorize(Roles = "Parent")]
        public async Task<IActionResult> CreatePaymentIntent(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int parentUserId))
            {
                return Unauthorized("Invalid user token.");
            }

            var response = await _subscriptionService.CreatePaymentIntentAsync(id, parentUserId);
            if (response == null)
            {
                return NotFound($"Subscription #{id} not found or access denied.");
            }

            return Ok(response);
        }

        /// <summary>
        /// Retrieves the complete subscription billing history for a specific child/player profile.
        /// </summary>
        [HttpGet("children/{playerId}/history")]
        [Authorize(Roles = "Parent,AcademyAdmin")]
        public async Task<IActionResult> GetPlayerSubscriptionHistory(int playerId)
        {
            var history = await _subscriptionService.GetPlayerSubscriptionHistoryAsync(playerId);
            return Ok(history);
        }
    }
}