using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using System.Collections.Generic;
using System.IO;

namespace Adeptus.Views;

public class DataTemplateSelector : IDataTemplate
{
    [Content]
    public Dictionary<string, IDataTemplate> Templates { get; } = [];

    /// <summary>
    /// Select a template by view model type name
    /// </summary>
    public Control? Build(object? param)
    {
        if (param == null) return null;

        var typeName = param.GetType().Name;
        if (Templates.TryGetValue(typeName, out var template))
        {
            return template.Build(param);
        }

        throw new InvalidDataException($"Data template not found for {typeName}");
    }

    public bool Match(object? data) => true;
}
