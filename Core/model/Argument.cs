using Newtonsoft.Json;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HeadlessHub.Core.Model;

/// <summary>
/// An argument, usable by AppBase
/// </summary>
public class Argument
{
    public string Name { get; set; }
    public bool Required { get; set; }
    public string Type { get; set; }
    public string? Section { get; set; }
    public string? Description { get; set; }
    public string? NameHuman { get; set; }
    public string? RequiredOnArgument { get; set; }
    public bool EmptyAllowedOnRequired { get; set; }
    public bool IsRuntimeArgument { get; set; }
    public bool IsMulti { get; set; }

    private string? _value;
    public string? Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                if (_value != null && value != null)
                {
                    IsValueChanged = true;
                }
                _value = value;
            }
        }
    }

    public Dictionary<string, string>? ValueMapping { get; set; }

    [JsonIgnore]
    public bool IsValueChanged { get; set; }

    [JsonIgnore]
    public string RangeBy { get; private set; } = string.Empty;

    [JsonIgnore]
    public string RangeTo { get; private set; } = string.Empty;

    public const string TypeString = "string";
    public const string TypeFloat = "float";
    public const string TypeInt = "int";
    public const string TypeBool = "bool";
    public const string TypeFile = "file";
    public const string TypePath = "path";
    public const string TypePassword = "password";
    public const string TypeSelection = "selection";
    public const string RangeDelimitter = "..";

    public Argument(string name, string type, bool required, string? section = null,
        string? description = null, string? nameHuman = null, string? requiredOnArgument = null,
        bool emptyAllowedOnRequired = false, bool isRuntimeArgument = false, bool isMulti = false,
        string? value = null, Dictionary<string, string>? valueMapping = null)
    {
        Name = name;
        Required = required;
        Type = type.ToLower();
        Section = section;
        Description = description;
        NameHuman = string.IsNullOrEmpty(nameHuman) ? Name : nameHuman;
        RequiredOnArgument = requiredOnArgument;
        EmptyAllowedOnRequired = emptyAllowedOnRequired;
        IsRuntimeArgument = isRuntimeArgument;
        IsMulti = isMulti;
        Value = value;
        ValueMapping = valueMapping;
        ValidateType();
        IsValueChanged = false;
    }

    public string? MappedValue()
    {
        if (ValueMapping != null && Value != null)
        {
            foreach (var item in ValueMapping)
            {
                if (Value == item.Key) return item.Value;
            }
        }
        return Value;
    }

    public bool ShouldSerializeValue() => !IsRuntimeArgument;

    public void ValidateType()
    {
        if (Type.StartsWith(TypeString) || Type.StartsWith(TypeFloat) || Type.StartsWith(TypeInt))
        {
            if (Type.Length > TypeString.Length && Type.StartsWith(TypeString))
                ValidateRange(TypeString);
            else if (Type.Length > TypeFloat.Length && Type.StartsWith(TypeFloat))
                ValidateRange(TypeFloat);
            else if (Type.Length > TypeInt.Length && Type.StartsWith(TypeInt))
                ValidateRange(TypeInt);
        }
        else if (Type != TypeBool && Type != TypeFile && Type != TypePath && 
                 Type != TypePassword && !Type.StartsWith(TypeSelection))
        {
            throw new Exception($"Argument-Type '{Type}' is invalid: {NameHuman}");
        }
    }

    public void Validate()
    {
        if (string.IsNullOrEmpty(Value) && !EmptyAllowedOnRequired && Required)
            throw new ArgumentException($"is required: {NameHuman}");

        if (Type.StartsWith(TypeFloat))
            ValidateFloat();
        else if (Type.StartsWith(TypeInt))
            ValidateInt();
        else if (Type == TypeBool)
            ValidateBool();
        else if (Type == TypeFile || Type == TypePath)
            ValidatePath();
    }

    private void ValidateRange(string type)
    {
        var range = Type.Substring(type.Length);
        var rangeSplitted = range.Split(RangeDelimitter);
        if (rangeSplitted.Length == 2)
        {
            RangeBy = rangeSplitted[0].TrimStart('[');
            RangeTo = rangeSplitted[1].TrimEnd(']');
        }
    }

    private void ValidateFloat()
    {
        if (float.TryParse(Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
        {
            if (!string.IsNullOrEmpty(RangeBy) && !string.IsNullOrEmpty(RangeTo))
            {
                var min = float.Parse(RangeBy, CultureInfo.InvariantCulture);
                var max = float.Parse(RangeTo, CultureInfo.InvariantCulture);
                if (val < min || val > max)
                    throw new ArgumentException($"Out of range ({RangeBy} to {RangeTo}): {NameHuman}");
            }
        }
        else if (!string.IsNullOrEmpty(Value))
        {
            throw new ArgumentException($"Invalid float: {Value}");
        }
    }

    private void ValidateInt()
    {
        if (int.TryParse(Value, out var val))
        {
            if (!string.IsNullOrEmpty(RangeBy) && !string.IsNullOrEmpty(RangeTo))
            {
                var min = int.Parse(RangeBy);
                var max = int.Parse(RangeTo);
                if (val < min || val > max)
                    throw new ArgumentException($"Out of range ({RangeBy} to {RangeTo}): {NameHuman}");
            }
        }
        else if (!string.IsNullOrEmpty(Value))
        {
            throw new ArgumentException($"Invalid integer: {Value}");
        }
    }

    private void ValidateBool()
    {
        if (string.IsNullOrEmpty(Value)) return;
        var v = Value.ToLowerInvariant();
        if (v != "true" && v != "false" && v != "1" && v != "0" && v != "yes" && v != "no")
            throw new ArgumentException($"Invalid boolean: {Value}");
    }

    private void ValidatePath()
    {
        if (!string.IsNullOrEmpty(Value))
        {
            try { Path.GetFullPath(Value); }
            catch { throw new ArgumentException($"Invalid path: {Value}"); }
        }
    }
}
