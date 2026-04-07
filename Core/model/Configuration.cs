namespace HeadlessHub.Core.Model;

/// <summary>
/// A configuration that is usable by AppBase
/// </summary>
public class Configuration
{
    public string Prefix { get; private set; }
    public string Delimiter { get; private set; }
    public List<Argument> Arguments { get; private set; }
    public bool IsRaw { get; private set; }

    public static readonly string ArgumentErrorKey = "ArgumentValidateParse-Error";

    public Configuration(string prefix, string delimiter, List<Argument> arguments, bool isRaw = false)
    {
        Prefix = prefix;
        Delimiter = delimiter;
        Arguments = arguments;
        IsRaw = isRaw;
    }

    public bool IsChanged()
    {
        var changed = Arguments.Any(a => a.IsValueChanged);
        if (changed)
        {
            foreach (var arg in Arguments)
                arg.IsValueChanged = false;
        }
        return changed;
    }

    public string? GenerateArgumentString(AppBase app, Dictionary<string, string>? runtimeArguments = null)
    {
        if (runtimeArguments != null)
        {
            foreach (var ra in runtimeArguments)
            {
                var arg = Arguments.FirstOrDefault(a => a.Name == ra.Key);
                if (arg != null) arg.Value = ra.Value;
            }
        }

        if (IsRaw)
        {
            return Arguments.Count == 2 ? Arguments[1].Value : string.Empty;
        }

        // Validate required-on-argument dependencies
        foreach (var a in Arguments)
        {
            ValidateRequiredOnArgument(a);
        }

        var arguments = Arguments.FindAll(a => a.Required || (!a.Required && !string.IsNullOrEmpty(a.Value)));
        
        foreach (var a in arguments)
        {
            try { a.Validate(); }
            catch (Exception ex)
            {
                var ex2 = new ArgumentException(ArgumentErrorKey + a.NameHuman + ": " + ex.Message);
                ex2.Data["argument"] = a;
                throw ex2;
            }
        }

        var result = string.Empty;
        foreach (var a in arguments)
        {
            var mappedValue = a.MappedValue();

            if (string.IsNullOrEmpty(a.Value))
            {
                result += " " + a.Name;
            }
            else if (!a.IsMulti || string.IsNullOrEmpty(mappedValue))
            {
                result += $" {Prefix}{a.Name}{Delimiter}\"{mappedValue}\"";
            }
            else
            {
                var splitted = mappedValue!.Split(' ');
                var multiPart = string.Join("", splitted.Select(s => $" \"{s}\""));
                result += $" {Prefix}{a.Name}{Delimiter}{multiPart}";
            }
        }

        return result;
    }

    private void ValidateRequiredOnArgument(Argument a)
    {
        if (!string.IsNullOrEmpty(a.RequiredOnArgument))
        {
            var parts = a.RequiredOnArgument.Split("=");
            if (parts.Length == 2)
            {
                var depArg = Arguments.FirstOrDefault(arg => arg.Name == parts[0]);
                if (depArg != null)
                {
                    a.Required = depArg.Value == parts[1];
                }
            }
        }
    }
}
