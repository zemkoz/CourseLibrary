using System.Text.Json;
using AutoMapper;
using CourseLibrary.API.ActionConstrains;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.RespourceParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Net.Http.Headers;
using Guid = System.Guid;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/authors")]
public class AuthorsController : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;
    private readonly IPropertyMappingService _propertyMappingService;
    private readonly IPropertyCheckerService _propertyCheckerService;
    private readonly ProblemDetailsFactory _problemDetailsFactory;

    public AuthorsController(
        ICourseLibraryRepository courseLibraryRepository,
        IMapper mapper,
        IPropertyMappingService propertyMappingService,
        IPropertyCheckerService propertyCheckerService,
        ProblemDetailsFactory problemDetailsFactory)
    {
        _courseLibraryRepository = courseLibraryRepository ??
                                   throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _propertyMappingService = propertyMappingService
                                  ?? throw new ArgumentNullException(nameof(propertyMappingService));
        _propertyCheckerService = propertyCheckerService
                                  ?? throw new ArgumentNullException(nameof(propertyCheckerService));
        _problemDetailsFactory = problemDetailsFactory
                                 ?? throw new ArgumentNullException(nameof(problemDetailsFactory));
    }

    [HttpGet(Name = "GetAuthors")]
    public async Task<IActionResult> GetAuthors(
        [FromQuery] AuthorsResourceParameters authorsResourceParameters)
    {
        if (!_propertyMappingService
                .ValidMappingExistsFor<AuthorDto, Author>(authorsResourceParameters.OrderBy))
        {
            return BadRequest();
        }

        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(HttpContext,
                    400,
                    detail: $"Not all requested data shaping fields exists " +
                            $"on the resource: {authorsResourceParameters.Fields}.")
            );
        }

        var authorsFromRepo = await _courseLibraryRepository
            .GetAuthorsAsync(authorsResourceParameters);

        var paginationMetadata = new
        {
            totalCount = authorsFromRepo.TotalCount,
            pageSize = authorsFromRepo.PageSize,
            currentPage = authorsFromRepo.CurrentPage,
            totalPages = authorsFromRepo.TotalPages
        };

        Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));

        var links = CreateLinksForAuthors(
            authorsResourceParameters, authorsFromRepo.HasPrevious, authorsFromRepo.HasNext);

        var shapedAuthors = _mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo)
            .ShapeData(authorsResourceParameters.Fields);

        var shapedAuthorsWithLinks = shapedAuthors.Select(author =>
        {
            var authorAsDictionary = author as IDictionary<string, object>;
            var authorLinks = CreateLinksForAuthor((Guid)authorAsDictionary["Id"], null);
            authorAsDictionary.Add("links", authorLinks);
            return authorAsDictionary;
        });

        var linkedCollectionResource = new
        {
            value = shapedAuthorsWithLinks,
            links
        };

        return Ok(linkedCollectionResource);
    }

    private IEnumerable<LinkDto> CreateLinksForAuthors(
        AuthorsResourceParameters authorsResourceParameters,
        bool hasPrevioursPage,
        bool hasNextPage)
    {
        var links = new List<LinkDto>();

        links.Add(new LinkDto(CreateAuthorsResourceUri(
            authorsResourceParameters, ResourceUriType.Current), "self", "GET"));

        if (hasPrevioursPage)
        {
            links.Add(new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage),
                "previous_page",
                "GET"));
        }

        if (hasNextPage)
        {
            links.Add(new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage),
                "next_page",
                "GET"));
        }

        return links;
    }

    private string? CreateAuthorsResourceUri(
        AuthorsResourceParameters authorsResourceParameters,
        ResourceUriType type)
    {
        switch (type)
        {
            case ResourceUriType.PreviousPage:
                return Url.Link("GetAuthors",
                    new
                    {
                        fields = authorsResourceParameters.Fields,
                        orderBy = authorsResourceParameters.OrderBy,
                        pageNumber = authorsResourceParameters.PageNumber - 1,
                        pageSize = authorsResourceParameters.PageSize,
                        mainCategory = authorsResourceParameters.MainCategory,
                        searchQuery = authorsResourceParameters.SearchQuery
                    });
            case ResourceUriType.NextPage:
                return Url.Link("GetAuthors",
                    new
                    {
                        fields = authorsResourceParameters.Fields,
                        orderBy = authorsResourceParameters.OrderBy,
                        pageNumber = authorsResourceParameters.PageNumber + 1,
                        pageSize = authorsResourceParameters.PageSize,
                        mainCategory = authorsResourceParameters.MainCategory,
                        searchQuery = authorsResourceParameters.SearchQuery
                    });
            case ResourceUriType.Current:
            default:
                return Url.Link("GetAuthors",
                    new
                    {
                        fields = authorsResourceParameters.Fields,
                        orderBy = authorsResourceParameters.OrderBy,
                        pageNumber = authorsResourceParameters.PageNumber,
                        pageSize = authorsResourceParameters.PageSize,
                        mainCategory = authorsResourceParameters.MainCategory,
                        searchQuery = authorsResourceParameters.SearchQuery
                    });
        }
    }

    [HttpGet("{authorId}")]
    [RequestHeaderMatchesMediaType("Accept", "application/json",
        "application/vnd.marvin.author.friendly+json")]
    [Produces("application/json",
        "application/vnd.marvin.author.friendly+json")]
    public async Task<ActionResult<AuthorDto>> GetAuthorWithoutLinks(
        Guid authorId,
        [FromQuery] string? fields,
        [FromHeader(Name = "Accept")] string? mediaType)
    {
        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(fields))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(HttpContext,
                    400,
                    detail: $"Not all requested data shaping fields exists " +
                            $"on the resource: {fields}.")
            );
        }

        var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);
        if (authorFromRepo == null)
        {
            return NotFound();
        }

        var friendlyResourceToReturn = _mapper.Map<AuthorDto>(authorFromRepo)
            .ShapeData(fields) as IDictionary<string, object>;

        return Ok(friendlyResourceToReturn);
    }

    [HttpGet("{authorId}")]
    [RequestHeaderMatchesMediaType("Accept", "application/vnd.marvin.hateoas+json",
        "application/vnd.marvin.author.friendly.hateoas+json")]
    [Produces("application/vnd.marvin.hateoas+json",
        "application/vnd.marvin.author.friendly.hateoas+json")]
    public async Task<ActionResult<AuthorDto>> GetAuthorWithLinks(
        Guid authorId,
        [FromQuery] string? fields)
    {
        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(fields))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(HttpContext,
                    400,
                    detail: $"Not all requested data shaping fields exists " +
                            $"on the resource: {fields}.")
            );
        }

        var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);
        if (authorFromRepo == null)
        {
            return NotFound();
        }
        
        var friendlyResourceToReturn = _mapper.Map<AuthorDto>(authorFromRepo)
            .ShapeData(fields) as IDictionary<string, object>;
        friendlyResourceToReturn.Add("links", CreateLinksForAuthor(authorId, fields));

        return Ok(friendlyResourceToReturn);
    }


    [HttpGet("{authorId}", Name = "GetAuthor")]
    [RequestHeaderMatchesMediaType("Accept", "application/vnd.marvin.author.full+json")]
    [Produces("application/vnd.marvin.author.full+json")]
    public async Task<ActionResult<AuthorFullDto>> GetFullAuthorWithoutLinks(
        Guid authorId,
        [FromQuery] string? fields)
    {
        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(fields))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(HttpContext,
                    400,
                    detail: $"Not all requested data shaping fields exists " +
                            $"on the resource: {fields}.")
            );
        }

        var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);
        if (authorFromRepo == null)
        {
            return NotFound();
        }

        var fullResourceToReturn = _mapper.Map<AuthorFullDto>(authorFromRepo)
            .ShapeData(fields) as IDictionary<string, object>;
        return Ok(fullResourceToReturn);
    }
    
    [HttpGet("{authorId}", Name = "GetAuthor")]
    [RequestHeaderMatchesMediaType("Accept", "application/vnd.marvin.author.full.hateoas+json")]
    [Produces("application/json",
        "application/vnd.marvin.author.full.hateoas+json")]
    public async Task<ActionResult> GetFullAuthorWithLinks(
        Guid authorId,
        [FromQuery] string? fields,
        [FromHeader(Name = "Accept")] string? mediaType)
    {
        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(fields))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(HttpContext,
                    400,
                    detail: $"Not all requested data shaping fields exists " +
                            $"on the resource: {fields}.")
            );
        }

        var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);
        if (authorFromRepo == null)
        {
            return NotFound();
        }

        var fullResourceToReturn = _mapper.Map<AuthorFullDto>(authorFromRepo)
            .ShapeData(fields) as IDictionary<string, object>;
        fullResourceToReturn.Add("links", CreateLinksForAuthor(authorId, fields));
        return Ok(fullResourceToReturn);
    }

    private IEnumerable<LinkDto> CreateLinksForAuthor(Guid authorId, string? fields)
    {
        var links = new List<LinkDto>();
        if (string.IsNullOrWhiteSpace(fields))
        {
            links.Add(new LinkDto(Url.Link("GetAuthor", new { authorId }), "self", "GET"));
        }
        else
        {
            links.Add(new LinkDto(Url.Link("GetAuthor", new { authorId, fields }), "self", "GET"));
        }

        links.Add(new LinkDto(
            Url.Link("CreateCourseForAuthor", new { authorId }),
            "create_course_for_author",
            "POST"));

        links.Add(new LinkDto(
            Url.Link("GetCoursesForAuthor", new { authorId }),
            "get_courses_for_author",
            "GET"));


        return links;
    }

    [HttpPost(Name = "CreateAuthorWithDateOfDeath")]
    [RequestHeaderMatchesMediaType("Content-Type",
        "application/vnd.marvin.authorforcreationwithdateofdeath+json")]
    [Consumes("application/vnd.marvin.authorforcreationwithdateofdeath+json")]
    public async Task<ActionResult<AuthorDto>> CreateAuthorWithDateOfDeath(AuthorForCreationWithDateOfDeathDto author)
    {
        var authorEntity = _mapper.Map<Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        var links = CreateLinksForAuthor(authorToReturn.Id, null);
        var linkedResourceToReturn = authorToReturn.ShapeData(null) as IDictionary<string, object?>;

        linkedResourceToReturn?.Add("links", links);

        return CreatedAtRoute("GetAuthor",
            new { authorId = authorToReturn.Id },
            linkedResourceToReturn);
    }

    [HttpPost(Name = "CreateAuthor")]
    [RequestHeaderMatchesMediaType("Content-Type",
        "application/json",
        "application/vnd.marvin.authorforcreation+json")]
    [Consumes(
        "application/json",
        "application/vnd.marvin.authorforcreation+json")]
    public async Task<ActionResult<AuthorDto>> CreateAuthor(AuthorForCreationDto author)
    {
        var authorEntity = _mapper.Map<Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        var links = CreateLinksForAuthor(authorToReturn.Id, null);
        var linkedResourceToReturn = authorToReturn.ShapeData(null) as IDictionary<string, object?>;

        linkedResourceToReturn?.Add("links", links);

        return CreatedAtRoute("GetAuthor",
            new { authorId = authorToReturn.Id },
            linkedResourceToReturn);
    }

    [HttpOptions]
    public IActionResult GetAuthorsOptions()
    {
        Response.Headers.Append("Allow", "GET,HEAD,POST,OPTIONS");
        return Ok();
    }
}