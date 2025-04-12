using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Log2ui.Extensions;
using Log2ui.Settings.Services;
using Log2ui.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Settings.Validations;

[AttributeUsage(AttributeTargets.Property)]
public class ValidateLoggerNameAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string name)
        {
            return ValidationResult.Success; // Ignore
        }

        var service = App.ServiceLocator.GetRequiredService<ISettingsService>();
        var settingsVm = App.ViewModelStack.OfType<ILoggerSettingsViewModel>().LastOrDefault();
        if (settingsVm is null)
        {
            return ValidationResult.Success; // Ignore
        }

        if (!settingsVm.LoggerSettings.TryGetCurrent(100, out var current))
        {
            return ValidationResult.Success; // Ignore
        }

        if (current.OriginalName != name && service.AllLoggerNames.Contains(name, StringComparer.InvariantCultureIgnoreCase))
        {
            return new ValidationResult("Already in use.");
        }

        return ValidationResult.Success;
    }
}
