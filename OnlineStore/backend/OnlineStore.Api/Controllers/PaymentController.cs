using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using OnlineStore.Infrastructure.Auth;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    
    public PaymentController(UserManager<AppUser> userManager, ApplicationDbContext db, IConfiguration config)
    {
        _userManager = userManager;
        _db = db;
        _config = config;
    }

    [HttpGet("config")]
    [AllowAnonymous]
    public IActionResult GetConfig()
    {
        return Ok(new { publishableKey = _config["Stripe:PublishableKey"] });
    }

    [HttpPost("cards")]
    public async Task<IActionResult> AddSavedCard([FromBody] AddCardRequest req)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (string.IsNullOrEmpty(user.StripeCustomerId))
        {
            var options = new CustomerCreateOptions { Email = user.Email, Name = user.FullName };
            var service = new CustomerService();
            var customer = await service.CreateAsync(options);
            user.StripeCustomerId = customer.Id;
            await _userManager.UpdateAsync(user);
        }

        try
        {
            var attachOptions = new PaymentMethodAttachOptions
            {
                Customer = user.StripeCustomerId,
            };
            var pmService = new PaymentMethodService();
            var attachedPaymentMethod = await pmService.AttachAsync(req.PaymentMethodId, attachOptions);

            return Ok(new { id = attachedPaymentMethod.Id, last4 = attachedPaymentMethod.Card.Last4, brand = attachedPaymentMethod.Card.Brand, expMonth = attachedPaymentMethod.Card.ExpMonth, expYear = attachedPaymentMethod.Card.ExpYear });
        }
        catch (StripeException e)
        {
            return BadRequest(new { error = e.StripeError.Message });
        }
    }

    [HttpGet("cards")]
    public async Task<IActionResult> GetSavedCards()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (string.IsNullOrEmpty(user.StripeCustomerId))
            return Ok(new List<object>());

        try 
        {
            var options = new PaymentMethodListOptions
            {
                Customer = user.StripeCustomerId,
                Type = "card",
            };
            var service = new PaymentMethodService();
            var paymentMethods = await service.ListAsync(options);

            var cards = paymentMethods.Data.Select(pm => new
            {
                id = pm.Id,
                brand = pm.Card.Brand,
                last4 = pm.Card.Last4,
                expMonth = pm.Card.ExpMonth,
                expYear = pm.Card.ExpYear
            });

            return Ok(cards);
        }
        catch (StripeException e)
        {
             return BadRequest(new { error = e.StripeError.Message });
        }
    }

    [HttpDelete("cards/{paymentMethodId}")]
    public async Task<IActionResult> RemoveSavedCard(string paymentMethodId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        try
        {
            var service = new PaymentMethodService();
            await service.DetachAsync(paymentMethodId);
            return Ok(new { success = true });
        }
        catch (StripeException e)
        {
            return BadRequest(new { error = e.StripeError.Message });
        }
    }
}

public class AddCardRequest
{
    public string PaymentMethodId { get; set; } = default!;
}
