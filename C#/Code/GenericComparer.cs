using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace C_Sharp;

public class GenericComparer<T> : EqualityComparer<T>
{
    private readonly Func<T, object> propertySelection = null;

    public GenericComparer()
    {
    }

    public GenericComparer(Func<T, object> propertySelection)
    {
        this.propertySelection = propertySelection;
    }

    public override bool Equals(T? x, T? y)
    {
        if (x == null && y == null)
        {
            return true;
        }
        else if (x == null || y == null)
        {
            return false;
        }

        var originalObject = propertySelection == null ? x : propertySelection(x);
        var comparisonObject = propertySelection == null ? y : propertySelection(y);

        var original = JsonConvert.SerializeObject(originalObject);
        var comparison = JsonConvert.SerializeObject(comparisonObject);

        return original.Equals(comparison);
    }

    public override int GetHashCode([DisallowNull] T obj)
    {
        throw new NotImplementedException();
    }
}