using Scriban;
using Scriban.Runtime;

namespace MicroGen.Generator.Templates;

/// <summary>
/// Wraps Scriban template rendering with common helpers available to all templates.
/// </summary>
public sealed class TemplateEngine
{
    private readonly ScriptObject _globals;

    public TemplateEngine()
    {
        _globals = [];
        _globals.Import(typeof(TemplateFunctions));
    }

    /// <summary>
    /// Renders a Scriban template string with the given model.
    /// </summary>
    public string Render(string templateText, object model, string? modelName = null)
    {
        var template = Template.Parse(templateText);
        if (template.HasErrors)
        {
            var errors = string.Join("\n", template.Messages.Select(m => m.ToString()));
            throw new InvalidOperationException($"Template parse error:\n{errors}");
        }

        var context = new TemplateContext { MemberRenamer = member => member.Name };
        var scriptObject = new ScriptObject();
        scriptObject.Import(_globals);

        if (modelName is not null)
        {
            scriptObject.Add(modelName, model);
        }
        else
        {
            // Import properties individually to preserve PascalCase naming
            foreach (var prop in model.GetType().GetProperties())
            {
                scriptObject.Add(prop.Name, prop.GetValue(model));
            }
        }

        context.PushGlobal(scriptObject);
        return template.Render(context).Trim();
    }
}

/// <summary>
/// Template helper functions available as {{ func_name param }}.
/// </summary>
public static class TemplateFunctions
{
    public static string PascalCase(string input) =>
        MicroGen.Core.Helpers.NamingHelper.ToPascalCase(input);

    public static string CamelCase(string input) =>
        MicroGen.Core.Helpers.NamingHelper.ToCamelCase(input);

    public static string KebabCase(string input) =>
        MicroGen.Core.Helpers.NamingHelper.ToKebabCase(input);

    public static string Indent(string text, int spaces)
    {
        var indent = new string(' ', spaces);
        return string.Join("\n",
            text.Split('\n').Select(line => string.IsNullOrWhiteSpace(line) ? line : indent + line));
    }
}
