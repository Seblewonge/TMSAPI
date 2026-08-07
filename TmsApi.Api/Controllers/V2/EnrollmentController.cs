using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using TmsApi.Application.Enrollments.Queries;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/enrollments")]
public class EnrollmentsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetEnrollmentsQuery(), cancellationToken);
        return Ok(result);
    }
// [HttpPost("{id:int}/approve")]
// public IActionResult Approve(int id)
// {
//     return NoContent();
// }   
[HttpPost("{id:int}/approve")]
public IActionResult Approve(int id)
{
    return Ok(new
    {
        id,
        status = "Approved"
    });
}
}