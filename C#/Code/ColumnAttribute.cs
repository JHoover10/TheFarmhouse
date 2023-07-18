namespace C_Sharp;

[AttributeUsage(AttributeTargets.Property)]
public class ColumnAttribute : Attribute
{
    private string name;

    public ColumnAttribute(string name)
    {
        this.name = name;
    }

    public string GetName()
    {
        return this.name;
    }
}