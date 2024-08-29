using AutoMapper;

namespace CourseLibrary.API.Profiles;
public class CoursesProfile : Profile
{
    public CoursesProfile()
    {
        CreateMap<Entities.Course, Models.CourseDto>();
        CreateMap<Entities.Course, Models.CourseForUpdateDto>();
        CreateMap<Models.CourseForCreationDto, Entities.Course>();
        CreateMap<Models.CourseForUpdateDto, Entities.Course>();
    }
}