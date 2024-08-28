using AutoMapper;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/authorcollections")]
public class AuthorCollectionsController : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;

    public AuthorCollectionsController(ICourseLibraryRepository courseLibraryRepository, IMapper mapper)
    {
        _courseLibraryRepository = courseLibraryRepository 
                                   ?? throw new ArgumentException(nameof(courseLibraryRepository));
        _mapper = 
            mapper ?? throw new ArgumentException(nameof(mapper));
    }

    [HttpGet("({authorIds})", Name = "GetAuthorCollection")]
    public async Task<ActionResult<IEnumerable<AuthorDto>>> GetAuthorCollection(
        [ModelBinder(BinderType = typeof(ArrayModelBinder))] [FromRoute] IEnumerable<Guid> authorIds)
    {
        var authorEntities = await _courseLibraryRepository.GetAuthorsAsync(authorIds);
        if (authorIds.Count() != authorEntities.Count())
        {
            return NotFound();
        }
        
        var authorsToReturn = _mapper.Map<IEnumerable<AuthorDto>>(authorEntities);
        return Ok(authorsToReturn);
    }

    [HttpPost]
    public async Task<ActionResult<IEnumerable<AuthorDto>>> CreateAuthorCollection(
        IEnumerable<AuthorForCreationDto> authorCollection)
    {
        var authorEntities = _mapper.Map<IEnumerable<Entities.Author>>(authorCollection);
        foreach (var author in authorEntities)
        {
            _courseLibraryRepository.AddAuthor(author);
        }
        await _courseLibraryRepository.SaveAsync();

        var authorCollectionToReturn = _mapper.Map<IEnumerable<AuthorDto>>(authorEntities);
        var authorIds = string.Join(
            ",",
            authorCollectionToReturn.Select(a => a.Id)
        );
        
        return CreatedAtAction("GetAuthorCollection", 
            new { authorIds },
            authorCollectionToReturn);
    }
}