using System;

namespace Updater.Exceptions;

public class UpdateException(string message, Exception innerException) : ApplicationException(message, innerException)
{
    
}