using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Api.Services;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/ab-testing")]
[Authorize(Roles = "Admin")]
public class AbTestingController : ControllerBase
{
    private readonly IAbTestingService _abTestingService;
    private readonly ILogger<AbTestingController> _logger;

    public AbTestingController(IAbTestingService abTestingService, ILogger<AbTestingController> logger)
    {
        _abTestingService = abTestingService;
        _logger = logger;
    }

    [HttpGet("experiments")]
    public async Task<ActionResult> GetActiveExperiments()
    {
        var result = await _abTestingService.GetActiveExperimentsAsync();
        return Ok(result);
    }

    [HttpGet("experiments/{name}")]
    public async Task<ActionResult> GetExperimentResults(string name)
    {
        var result = await _abTestingService.GetExperimentResultsAsync(name);
        return Ok(result);
    }

    [HttpPost("experiments")]
    public async Task<ActionResult> CreateExperiment([FromBody] CreateExperimentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ExperimentName) || 
            string.IsNullOrWhiteSpace(request.AgentName) || 
            string.IsNullOrWhiteSpace(request.VariantPrompt))
        {
            return BadRequest("Invalid experiment parameters.");
        }

        await _abTestingService.CreateExperimentAsync(
            request.ExperimentName, 
            request.AgentName, 
            request.VariantPrompt, 
            request.TrafficPercentage);

        return Ok(new { Success = true, Message = $"Experiment '{request.ExperimentName}' created successfully." });
    }

    [HttpPost("experiments/{name}/end")]
    public async Task<ActionResult> EndExperiment(string name, [FromBody] EndExperimentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.WinnerVariantName))
        {
            return BadRequest("Winner variant name is required.");
        }

        await _abTestingService.EndExperimentAsync(name, request.WinnerVariantName);
        return Ok(new { Success = true, Message = $"Experiment '{name}' ended. Winner declared: '{request.WinnerVariantName}'." });
    }
}

public class CreateExperimentRequest
{
    public string ExperimentName { get; set; } = default!;
    public string AgentName { get; set; } = default!;
    public string VariantPrompt { get; set; } = default!;
    public double TrafficPercentage { get; set; } = 0.2; // default to 20% test traffic, 80% default
}

public class EndExperimentRequest
{
    public string WinnerVariantName { get; set; } = default!;
}
