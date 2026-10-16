namespace CodeGenerator.Utils;

public class CommandParameter
{
    public string Type { get; set; }
    public string BoundValue { get; set; }

    public CommandParameter(string type, string boundValue)
    {
        Type = type;
        BoundValue = boundValue;
    }
}