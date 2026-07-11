using System.Threading.Channels;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Helpers;

public class SecurityEventMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityEventChannel _channel;
    private readonly ILogger<SecurityEventMiddleware> _logger;

    public SecurityEventMiddleware(
        RequestDelegate next,
        SecurityEventChannel channel,
        ILogger<SecurityEventMiddleware> logger)
    {
        _next = next;
        _channel = channel;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode is 401 or 403 or 429)
        {
            var eventType = context.Response.StatusCode switch
            {
                401 => "unauthorized_access",
                403 => "unauthorized_access",
                429 => "rate_limited",
                _ => "unknown"
            };

            var severity = context.Response.StatusCode switch
            {
                401 => "low",
                403 => "medium",
                429 => "low",
                _ => "low"
            };

            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            _channel.Enqueue(new SecurityEvent
            {
                EventType = eventType,
                UserId = userId,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                RequestPath = context.Request.Path,
                Severity = severity,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }
}

public class SecurityEventChannel
{
    private readonly Channel<SecurityEvent> _channel = Channel.CreateUnbounded<SecurityEvent>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public void Enqueue(SecurityEvent evt) => _channel.Writer.TryWrite(evt);

    public ChannelReader<SecurityEvent> Reader => _channel.Reader;
}

public class SecurityEventBackgroundService : BackgroundService
{
    private readonly SecurityEventChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SecurityEventBackgroundService> _logger;

    public SecurityEventBackgroundService(
        SecurityEventChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<SecurityEventBackgroundService> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<SecurityEvent>(100);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var evt = await _channel.Reader.ReadAsync(stoppingToken);
                batch.Add(evt);

                // Drain channel for up to 5 seconds or until batch is full
                while (batch.Count < 100)
                {
                    if (_channel.Reader.TryRead(out var next))
                        batch.Add(next);
                    else
                        break;
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.SecurityEvents.AddRange(batch);
                await db.SaveChangesAsync(stoppingToken);

                _logger.LogDebug("Persisted {Count} security events", batch.Count);
                batch.Clear();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist security events");
                batch.Clear();
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
