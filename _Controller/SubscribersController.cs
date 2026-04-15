using Microsoft.AspNetCore.Mvc;
using RefactorHeatAlertPostGre.Data.Repositories;
using RefactorHeatAlertPostGre.Models.Dto;
using RefactorHeatAlertPostGre.Models.Entities;

namespace RefactorHeatAlertPostGre.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscribersController : ControllerBase
    {
        private readonly ISubscriberRepository _subscriberRepository;

        public SubscribersController(ISubscriberRepository subscriberRepository)
        {
            _subscriberRepository = subscriberRepository;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<Subscriber>>>> GetAll()
        {
            var subscribers = await _subscriberRepository.GetAllActiveAsync();
            return Ok(ApiResponse<List<Subscriber>>.Ok(subscribers, $"{subscribers.Count} active subscribers"));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<Subscriber>>> Subscribe([FromBody] SubscriberRequest request)
        {
            var subscriber = new Subscriber
            {
                ChatId = request.ChatId,
                Username = request.Username,
                IsSubscribed = true,
                SubscribedAt = DateTime.UtcNow
            };

            var saved = await _subscriberRepository.SaveAsync(subscriber);
            return Ok(ApiResponse<Subscriber>.Ok(saved, "Subscribed successfully"));
        }

        [HttpDelete("{chatId:long}")]
        public async Task<ActionResult<ApiResponse<bool>>> Unsubscribe(long chatId)
        {
            var result = await _subscriberRepository.UnsubscribeAsync(chatId);
            if (!result)
                return NotFound(ApiResponse<bool>.Fail("Subscriber not found"));

            return Ok(ApiResponse<bool>.Ok(true, "Unsubscribed"));
        }
    }
}