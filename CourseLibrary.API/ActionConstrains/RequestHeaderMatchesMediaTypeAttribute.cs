using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace CourseLibrary.API.ActionConstrains;

[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
public class RequestHeaderMatchesMediaTypeAttribute : Attribute, IActionConstraint
{
    private readonly string _requestHeaderToMatch;
    private readonly MediaTypeCollection _mediaTypeCollection = new();

    public RequestHeaderMatchesMediaTypeAttribute(
        string requestHeaderToMatch, 
        string mediaType, 
        params string[] otherMediaTypes)
    {
        _requestHeaderToMatch = requestHeaderToMatch ?? throw new ArgumentNullException(nameof(requestHeaderToMatch));

        if (!MediaTypeHeaderValue.TryParse(mediaType, out var parsedMediaType))
        {
            throw new ArgumentException(nameof(mediaType));
        }
        
        _mediaTypeCollection.Add(parsedMediaType);

        foreach (var otherMediaType in otherMediaTypes)
        {
            if (!MediaTypeHeaderValue.TryParse(otherMediaType, out var parsedOtherMediaType))
            {
                throw new ArgumentException(nameof(otherMediaTypes));
            }
            
            _mediaTypeCollection.Add(parsedOtherMediaType);
        }
    }

    public int Order { get; }

    public bool Accept(ActionConstraintContext context)
    {
        var requestHeaders = context.RouteContext.HttpContext.Request.Headers;
        if (!requestHeaders.ContainsKey(_requestHeaderToMatch))
        {
            return false;
        }

        var parsedRequestMediaType = new MediaType(requestHeaders[_requestHeaderToMatch]);
        foreach (var mediaType in _mediaTypeCollection)
        {
            var parsedMediaType = new MediaType(mediaType);
            if (parsedMediaType.Equals(parsedRequestMediaType))
            {
                return true;
            }
        }
        return false;
    }
}