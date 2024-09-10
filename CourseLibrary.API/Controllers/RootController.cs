﻿using CourseLibrary.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api")]
public class RootController : ControllerBase
{
    [HttpGet(Name = "GetRoot")]
    public IActionResult GetRoot()
    {
        var links = new List<LinkDto>
        {
            new(
                Url.Link("GetRoot", new { }),
                "self",
                "GET"
            ),
            new(
                Url.Link("GetAuthors", new { }),
                "authors",
                "GET"
            ),
            new(
                Url.Link("CreateAuthor", new { }),
                "create_author",
                "POST"
            )
        };

        return Ok(links);
    }
}