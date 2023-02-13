namespace C_Sharp;

[AttributeUsage(AttributeTargets.Property)]
public class ColumnNameAttribute : Attribute
{
    private string name;

    public ColumnNameAttribute(string name)
    {
        this.name = name;
    }

    public string GetName()
    {
        return this.name;
    }
}