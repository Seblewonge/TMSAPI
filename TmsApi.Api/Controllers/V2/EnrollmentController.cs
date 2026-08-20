// using Asp.Versioning;
// using Microsoft.AspNetCore.Mvc;
// using MediatR;
// using Microsoft.AspNetCore.SignalR;
// using TmsApi.Application.Enrollments.Queries;
// using TmsApi.Application.Hubs;
// using TmsApi.Api.Hubs;

// namespace TmsApi.Api.Controllers.V2;

// [ApiController]
// [ApiVersion(2.0)]
// [Route("api/v{version:apiVersion}/enrollments")]
// public class EnrollmentsController(IMediator mediator, IHubContext<TmsHub, ITmsHubClient> hubContext) : ControllerBase
// {
//     [HttpGet]
//     public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
//     {
//         var result = await mediator.Send(new GetEnrollmentsQuery(), cancellationToken);
//         return Ok(result);
//     }
//     [HttpPost("{id}/approve")]
//     public async Task<IActionResult> Approve(string id, CancellationToken ct)
//     {
//         // Your existing approval logic ...
//         // After the database commit succeeds, broadcast to all connected Angular clients
//             await hubContext.Clients.All.
//         ReceiveEnrollmentStatusUpdated(id, "Approved");
//         return NoContent();
//     }  
// [HttpPost("{id:int}/approve")]
// public IActionResult Approve(int id)
// {
//     return Ok(new
//     {
//         id,
//         status = "Approved"
//     });
// }
// }
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Enrollments.Queries;
using TmsApi.Application.Hubs;
using TmsApi.Api.Hubs;
using MediatR;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/enrollments")]
public class EnrollmentsController(
    TmsDbContext dbContext,
    IMediator mediator,
    IHubContext<TmsHub, ITmsHubClient> hubContext)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetEnrollmentsQuery(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(
        int id,
        CancellationToken ct)
    {
        // 1. Find the enrollment
        var enrollment = await dbContext.Enrollments
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (enrollment is null)
        {
            return NotFound(new
            {
                message = $"Enrollment {id} was not found."
            });
        }

        // 2. Change status
        enrollment.Status = "Approved";

        // 3. Save to database
        await dbContext.SaveChangesAsync(ct);

        // 4. Tell all connected Angular clients
        await hubContext.Clients.All
            .ReceiveEnrollmentStatusUpdated(
                id.ToString(),
                "Approved");

        return NoContent();
    }
}