using CourseLibrary.API.Services;
using System.Linq.Dynamic.Core;

namespace CourseLibrary.API.Helpers;

public static class IQueryableExtenstion
{
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> source,
        string orderBy,
        Dictionary<string, PropertyMappingValue> mappingDictionary)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (mappingDictionary == null)
        {
            throw new ArgumentNullException(nameof(mappingDictionary));
        }

        if (string.IsNullOrWhiteSpace(orderBy))
        {
            return source;
        }
        
        var orderByAfterSplit = orderBy.Split(',');
        
        var orderByString = string.Empty;
        foreach (var orderByClause in orderByAfterSplit)
        {
            var trimmedOrderByClause = orderByClause.Trim();
            
            var orderDescending = trimmedOrderByClause.EndsWith(" desc");
            
            var indeOfFirstSpace = trimmedOrderByClause.IndexOf(" ");
            var propertyName = indeOfFirstSpace == -1 
                ? trimmedOrderByClause 
                : trimmedOrderByClause.Remove(indeOfFirstSpace);

            if (!mappingDictionary.ContainsKey(propertyName))
            {
                throw new ArgumentException($"Key mapping for {propertyName} is missing");
            }
            
            var propertyMappingValue = mappingDictionary[propertyName];
            if (propertyMappingValue == null)
            {
                throw new ArgumentNullException(nameof(propertyMappingValue));
            }

            if (propertyMappingValue.Revert)
            {
                orderDescending = !orderDescending;
            }

            foreach (var destinationProperty in propertyMappingValue.DestinationProperties)
            {
                var comma = (string.IsNullOrWhiteSpace(orderByString) ? string.Empty : ", ");
                orderByString += comma + destinationProperty + (orderDescending ? " descending" : " ascending");
            }
        }
        
        return source.OrderBy(orderByString);
    }
}