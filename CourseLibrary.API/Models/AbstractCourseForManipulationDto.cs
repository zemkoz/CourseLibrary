using System.ComponentModel.DataAnnotations;
using CourseLibrary.API.ValidationAttributes;

namespace CourseLibrary.API.Models;

[CourseTitleMustBeDifferentFromDescription]
public abstract class AbstractCourseForManipulationDto // : IValidatableObject
{
    [Required(ErrorMessage = "You should fill out a title.")]
    [MaxLength(100, ErrorMessage = "The title shouldn't have more than 100 characters.")]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(1500, ErrorMessage = "The description shouldn't have more than 1500 characters.")]
    public virtual string Description { get; set; } = string.Empty;

    /*
     * Implements class level validation by implementing interface IValidatableObject 
     * I left this code here as an example but,
     * we use here validation attribute CourseTitleMustBeDifferentFromDescriptionAttribute
     * 
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Title == Description)
        {
            yield return new ValidationResult(
                "The provided description should be different from the title.",
                ["Course"]);
        }
    }*/
}