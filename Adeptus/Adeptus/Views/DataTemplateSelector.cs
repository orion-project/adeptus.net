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
        if (!Templates.TryGetValue(typeName, out IDataTemplate? template))
        {
            // Find template for a base type for cases
            // DesignSomeViewModel --> SomeViewModel
            typeName = param.GetType().BaseType?.Name ?? "Default";
            if (!Templates.TryGetValue(typeName, out template))
            {
                throw new InvalidDataException($"Data template not found for {typeName}");
            }
        }
        return template.Build(param);
    }

    public bool Match(object? data) => true;
}
