namespace FitCore.Application.Setup;

/// <summary>
/// Wizard step numbers. 0 = fresh, 1..3 = wizard steps.
/// The wizard asks only what it can't default. Everything else is defaulted
/// from fitcore.defaults.json and editable in Settings later.
/// </summary>
public enum WizardStep
{
    Fresh = 0,
    YourGym = 1,
    Account = 2,
    Confirm = 3
}
