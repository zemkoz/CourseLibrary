﻿using CourseLibrary.API.DbContexts;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.RespourceParameters;
using Microsoft.EntityFrameworkCore;

namespace CourseLibrary.API.Services;

public class CourseLibraryRepository : ICourseLibraryRepository 
{
    private readonly CourseLibraryContext _context;
    private readonly IPropertyMappingService _propertyMappingService;

    public CourseLibraryRepository(CourseLibraryContext context, 
        IPropertyMappingService propertyMappingService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _propertyMappingService = propertyMappingService 
                                  ?? throw new ArgumentException(nameof(propertyMappingService));
    }

    public void AddCourse(Guid authorId, Course course)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(authorId));
        }

        if (course == null)
        {
            throw new ArgumentNullException(nameof(course));
        }

        // always set the AuthorId to the passed-in authorId
        course.AuthorId = authorId;
        _context.Courses.Add(course);
    }

    public void DeleteCourse(Course course)
    {
        _context.Courses.Remove(course);
    }

    public async Task<Course> GetCourseAsync(Guid authorId, Guid courseId)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(authorId));
        }

        if (courseId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(courseId));
        }

#pragma warning disable CS8603 // Possible null reference return.
        return await _context.Courses
          .Where(c => c.AuthorId == authorId && c.Id == courseId).FirstOrDefaultAsync();
#pragma warning restore CS8603 // Possible null reference return.
    }

    public async Task<IEnumerable<Course>> GetCoursesAsync(Guid authorId)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(authorId));
        }

        return await _context.Courses
                    .Where(c => c.AuthorId == authorId)
                    .OrderBy(c => c.Title).ToListAsync();
    }

    public void UpdateCourse(Course course)
    {
        // no code in this implementation
    }

    public void AddAuthor(Author author)
    {
        if (author == null)
        {
            throw new ArgumentNullException(nameof(author));
        }

        // the repository fills the id (instead of using identity columns)
        author.Id = Guid.NewGuid();

        foreach (var course in author.Courses)
        {
            course.Id = Guid.NewGuid();
        }

        _context.Authors.Add(author);
    }

    public async Task<bool> AuthorExistsAsync(Guid authorId)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(authorId));
        }

        return await _context.Authors.AnyAsync(a => a.Id == authorId);
    }

    public void DeleteAuthor(Author author)
    {
        if (author == null)
        {
            throw new ArgumentNullException(nameof(author));
        }

        _context.Authors.Remove(author);
    }

    public async Task<Author> GetAuthorAsync(Guid authorId)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(authorId));
        }

#pragma warning disable CS8603 // Possible null reference return.
        return await _context.Authors.FirstOrDefaultAsync(a => a.Id == authorId);
#pragma warning restore CS8603 // Possible null reference return.
    }

   
    public async Task<PagedList<Author>> GetAuthorsAsync(AuthorsResourceParameters authorsResourceParameters)
    {
        var query = _context.Authors as IQueryable<Author>;

        var mainCategory = authorsResourceParameters.MainCategory;
        if (!string.IsNullOrWhiteSpace(mainCategory))
        {
            query = query.Where(a => a.MainCategory == mainCategory.Trim());
        }

        var searchQuery = authorsResourceParameters.SearchQuery;
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            searchQuery = searchQuery.Trim();
            query = query.Where(a => a.MainCategory.Contains(searchQuery)
                || a.FirstName.Contains(searchQuery)
                || a.LastName.Contains(searchQuery));
        }

        if (!string.IsNullOrWhiteSpace(authorsResourceParameters.OrderBy))
        {
            var authorPropertyMappingDictionary = _propertyMappingService.GetPropertyMapping<AuthorDto, Author>();
            query = query.ApplySort(authorsResourceParameters.OrderBy, authorPropertyMappingDictionary);            
        }

        return await PagedList<Author>.CreateAsync(query, 
            authorsResourceParameters.PageNumber, authorsResourceParameters.PageSize);
    }

    public async Task<IEnumerable<Author>> GetAuthorsAsync(IEnumerable<Guid> authorIds)
    {
        if (authorIds == null)
        {
            throw new ArgumentNullException(nameof(authorIds));
        }

        return await _context.Authors.Where(a => authorIds.Contains(a.Id))
            .OrderBy(a => a.FirstName)
            .ThenBy(a => a.LastName)
            .ToListAsync();
    }

    public void UpdateAuthor(Author author)
    {
        // no code in this implementation
    }

    public async Task<bool> SaveAsync()
    {
        return (await _context.SaveChangesAsync() >= 0);
    }
}

