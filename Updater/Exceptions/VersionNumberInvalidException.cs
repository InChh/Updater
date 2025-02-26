using System;

namespace Updater.Exceptions;

public class VersionNumberInvalidException(string value) : ApplicationException("Version number is invalid: " + value)
{
}