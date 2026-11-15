namespace FitCore.Application.Settings;

public sealed record MessageTemplate
{
    public string Name { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
}

public sealed record MessageTemplatesSettings : ISettingsGroup
{
    public static string GroupKey => "messageTemplates";

    public List<MessageTemplate> RenewalReminder { get; init; } = new();
    public List<MessageTemplate> Expiry { get; init; } = new();
    public List<MessageTemplate> Welcome { get; init; } = new();
    public List<MessageTemplate> FollowUp { get; init; } = new();
}
