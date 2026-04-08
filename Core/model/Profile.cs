using Newtonsoft.Json;

namespace HeadlessHub.Core.Model;

/// <summary>
/// A profile contains a collection of apps and its constraints.
/// </summary>
public class Profile
{
    [JsonProperty("Name")]
    public string Name { get; private set; }
    
    [JsonProperty("Apps")]
    public Dictionary<string, ProfileState> Apps { get; private set; }
    
    [JsonProperty("IsTaggedForStart")]
    public bool IsTaggedForStart { get; set; }

    public Profile(string name, Dictionary<string, ProfileState> apps, bool isTaggedForStart = false)
    {
        Name = name;
        Apps = apps;
        IsTaggedForStart = isTaggedForStart;
    }
}

/// <summary>
/// Defines the constraints of an app in a profile.
/// </summary>
public class ProfileState
{
    [JsonProperty("IsRequired")]
    public bool IsRequired { get; private set; }
    
    private bool _taggedForStart;
    
    [JsonProperty("TaggedForStart")]
    public bool TaggedForStart
    {
        get => _taggedForStart;
        set
        {
            if (IsRequired) return;
            _taggedForStart = value;
        }
    }

    [JsonProperty("RuntimeArguments")]
    public Dictionary<string, string>? RuntimeArguments { get; private set; }

    [JsonIgnore]
    public AppBase? App { get; private set; }

    public ProfileState(bool isRequired = false, bool taggedForStart = false, Dictionary<string, string>? runtimeArguments = null)
    {
        IsRequired = isRequired;
        _taggedForStart = IsRequired ? IsRequired : taggedForStart;
        RuntimeArguments = runtimeArguments;
    }

    public void SetApp(AppBase app)
    {
        App = app;
    }
}
