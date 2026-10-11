using Microsoft.AspNetCore.Mvc;
using Shortr.Api.Contracts;
using Shortr.Application;
using Shortr.Domain;

namespace Shortr.Api.Controllers;

// This controller is the HTTP boundary. The controller calls LinkService.
// The controller maps the outcomes to HTTP responses. The controller holds
// no business logic.
[ApiController]
[Route("api/links")]
public sealed class LinksController : ControllerBase
{
    private readonly LinkService _linkService;

    public LinksController(LinkService linkService) => _linkService = linkService;

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateLinkRequest request,
        CancellationToken cancellationToken)
    {
        Link link;
        try
        {
            link = await _linkService.CreateAsync(request.Url, cancellationToken);
        }
        catch (ArgumentException)
        {
            // The service rejected the input. Return 400 with a stable message.
            // The exception text is a developer detail. The API contract does
            // not expose it.
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Invalid URL",
                Detail = "The URL must be an absolute http(s) URL.",
            });
        }

        // Return 201. The Location header points to the resolve endpoint.
        return CreatedAtAction(
            nameof(Resolve),
            new { code = link.Code },
            new CreateLinkResponse(
                link.Code,
                $"{Request.Scheme}://{Request.Host}/{link.Code}",
                link.OriginalUrl));
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> Resolve(string code, CancellationToken cancellationToken)
    {
        var link = await _linkService.ResolveAsync(code, cancellationToken);

        if (link is null)
        {
            // The code does not exist. Return 404.
            return NotFound();
        }

        // Return 302. A 301 response makes browsers cache the destination.
        // A cached destination bypasses this API. The API then loses the
        // ability to track clicks and to expire the link.
        return Redirect(link.OriginalUrl);
    }
}