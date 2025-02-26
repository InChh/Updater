using System;
using Updater.Exceptions;

namespace Updater.Extensions;

public static class ApplicationVersionStringExtensions
{
    public static void CheckVersionNumber(this string value)
    {
        // Check version number is valid, it should be in a specific format(xx.xx.xx).
        var strings = value.Split('.');
        if (strings.Length != 3)
        {
            throw new VersionNumberInvalidException(value);
        }

        // The first number of version should be current year and the second number should be current month.
        var year = DateTime.Now.Year.ToString();
        var month = DateTime.Now.Month.ToString();
        if (strings[0] != year || strings[1] != month)
        {
            throw new VersionNumberInvalidException(value);
        }

        // The third number should be greater than 0.
        if (int.TryParse(strings[2], out var number) && number <= 0)
        {
            throw new VersionNumberInvalidException(value);
        }
    }
}