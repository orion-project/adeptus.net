using System;

namespace Adeptus.Models;

public class AppError : Exception
{
    public AppError(string message) : base(message)
    {
    }
}
