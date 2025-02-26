using System.Collections.Generic;
using Updater.Extensions;

namespace Updater.Helpers;

public class ApplicationVersionComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x is null || y is null)
        {
            return 0;
        }
        
        x.CheckVersionNumber();
        y.CheckVersionNumber();

        var xParts = x.Split('.');
        var yParts = y.Split('.');

        if (xParts.Length != 3 || yParts.Length != 3)
        {
            return 0;
        }

        if (xParts[0] != yParts[0])
        {
            return int.Parse(xParts[0]) - int.Parse(yParts[0]);
        }

        if (xParts[1] != yParts[1])
        {
            return int.Parse(xParts[1]) - int.Parse(yParts[1]);
        }

        return int.Parse(xParts[2]) - int.Parse(yParts[2]);
    }
}