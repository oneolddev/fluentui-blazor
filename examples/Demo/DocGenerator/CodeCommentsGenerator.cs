using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace FluentUI.Demo.Generators;

[Generator]
public class CodeCommentsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //IncrementalValuesProvider<AdditionalText> additionalTexts = context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith(".xml"));

        IncrementalValueProvider<ImmutableArray<AdditionalText>> files = context.AdditionalTextsProvider.Where(at => at.Path.EndsWith(".xml")).Collect();
        context.RegisterSourceOutput(files, GenerateSource);
    }

    public void GenerateSource(SourceProductionContext context, ImmutableArray<AdditionalText> files)
    {
        List<XElement> members = [];

        foreach (AdditionalText file in files)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }
            SourceText f = file.GetText(context.CancellationToken);
            var xml = XDocument.Parse(f.ToString(), LoadOptions.None);

            members.AddRange(xml.Descendants("member"));
        }

        StringBuilder sb = new();

        sb.AppendLine("#pragma warning disable CS1591");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("");
        sb.AppendLine("namespace FluentUI.Demo.Shared;");
        sb.AppendLine("public static partial class CodeComments");
        sb.AppendLine("{");
        sb.AppendLine("\tpublic static string GetSummary(string name)");
        sb.AppendLine("\t{");
        sb.AppendLine("\t\tDictionary<string, string> summarydata = new Dictionary<string, string>() {");
        foreach (var m in members)
        {
            var paramName = CleanupParamName(m.Attribute("name").Value.ToString());
            var summary = CleanupSummary(m.Descendants().First().ToString());

            sb.AppendLine("\t\t\t[\"" + paramName + "\"] = \"" + summary + "\", ");
        }
        var lastComma = sb.ToString().LastIndexOf(',');

        sb.Remove(lastComma, 1);
        sb.AppendLine("\t\t};");
        sb.Append("\t\t");
        sb.AppendLine("KeyValuePair<string, string> foundPair = summarydata.FirstOrDefault(x => x.Key.Equals(name));");

        sb.AppendLine("\t\treturn foundPair.Value;");
        sb.AppendLine("\t\t}");
        sb.AppendLine("}");

        context.AddSource($"CodeComments.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    internal static string CleanupParamName(string value)
    {
        Regex regex = new("[P,T,M,F]:Microsoft\\.FluentUI\\.AspNetCore\\.Components\\.");
        value = regex.Replace(value, "");
        regex = new("[P,T,M,F]:FluentUI\\.Demo\\.Shared\\.Components\\.");
        value = regex.Replace(value, "");
        regex = new("[P,T,M,F]:FluentUI\\.Demo\\.Shared\\.");
        value = regex.Replace(value, "");
        regex = new("\\(.*\\)$");
        value = regex.Replace(value, "");

        return value;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers", Justification = "Need to use Envrionment to ensure the right newline is generated for platform it is being builtand run on")]
    internal static string CleanupSummary(string value)
    {
        Regex regex = new(@"[ ]{2,}");
        value = regex.Replace(value, "");

        regex = new("<see(?:also)? cref=[\"|'][!,P,T,M,F]+:+Microsoft\\.Fast\\.Components\\.FluentUI\\.([\\w|\\.]*)(?<generic>`\\d)?[\"|']\\s*/>");
        value = regex.Replace(value, m => m.Groups["generic"].Success ? $"<code>{m.Groups[1].Value}&lt;T&gt;</code>" : $"<code>{m.Groups[1].Value}</code>");

        regex = new("<see(?:also)? cref=[\"|'][!,P,T,M,F]+:+FluentUI\\.Demo\\.Shared\\.([\\w|\\.]*)(?<generic>`\\d)?[\"|']\\s*/>");
        value = regex.Replace(value, m => m.Groups["generic"].Success ? $"<code>{m.Groups[1].Value}&lt;T&gt;</code>" : $"<code>{m.Groups[1].Value}</code>");

        regex = new("<see(?:also)? cref=[\"|'][!,P,T,M,F]+:+([\\w|\\.|`|\\(|\\)|\\{|\\}|\\,]*)[\"|']\\s*/>");
        value = regex.Replace(value, m => $"<code>{m.Groups[1].Value}</code>");

        regex = new("<see langword=[\"|']([\\w|\\.|`|(|)|{|}|,]*)[\"|']\\s*/>");
        value = regex.Replace(value, m => $"<code>{m.Groups[1].Value}</code>");

        regex = new("<see href=\"(.*?)\">(.*?)</see>");
        value = regex.Replace(value, "<a href=\"$1\">$2</a>");

        // <param name="source">An <see cref="T:System.Linq.IQueryable`1"/>.</param>
        //<returns> A new <see cref = "T:System.Linq.IQueryable`1" /> representing the <paramref name = "source" /> with sorting rules applied.</returns>

        regex = new("<param(?:ref)? name=[\"|'](.*)[\"|']\\s*/>");
        value = regex.Replace(value, m => $"<code>{m.Groups[0].Value}</code>{m.Groups[1].Value}");

        return value.Trim()
                    .Replace("<summary>" + Environment.NewLine, "")
                    .Replace(Environment.NewLine + "</summary>", "")
                    .Replace(Environment.NewLine, "<br />")
                    .Replace("\"", "'")
                    .Replace("Microsoft.FluentUI.AspNetCore.Components.", "")
                    .Replace("FluentDataGrid`1.", "")
                    .Replace("System.Linq.", "")
                    .Replace("System.Linq.Queryable.", "")
                    .Replace("System.Collections.", "")
                    .Replace("`1", "<T>")
                    .Replace("`0", "<U>");

    }

    #region Additional methods for parsing ParamName
    internal static string ReduceArgument(string input)
    {
        var l = ParseString(input);
        if (l.Count > 1)
        {
            List<string> nl = new();
            foreach (var i in l)
            {
                nl.Add(ReduceArgument(i));
            }
            input = string.Join(",", nl);
        }
        else
        {
            // text enclosed by outermost curly braces
            Match match = Regex.Match(input, @"{(.*)}");
            if (match.Success)
            {
                var a = match.Groups[1].Value;
                var b = ReduceArgument(a);
                input = input.Replace(a, b);
            }
        }

        // removes all text before last period including period
        return Regex.Replace(input, @".*\.", ""); ;
    }
    internal static string Transform(string input)
    {
        // text enclosed by outermost curly braces
        Match match = Regex.Match(input, @"{(.*)}");
        if (match.Success)
        {
            var a = match.Groups[1].Value;
            var b = Transform(a);
            input = input.Replace(a, b);
            // replace {} with <>
            input = Regex.Replace(input, @"\{([^}]*)\}", "<$1>");
            // transform ValueTuple
            input = Regex.Replace(input, @"ValueTuple<(.+)>", "($1)");
            // transform Nullable
            input = Regex.Replace(input, @"Nullable<(.+)>", "$1?");
        }
        if (input.Equals("String"))
        {
            input = "string";
        }
        else if (input.Equals("Int32"))
        {
            input = "int";
        }
        else if (input.Equals("Boolean"))
        {
            input = "bool";
        }
        return input;
    }
    internal static List<string> ParseString(string input)
    {
        var result = new List<string>();
        var pattern = @"(?>[^,{]+|\{(?:[^{}]+|(?<open>\{)|(?<-open>\}))*\}(?(open)(?!)))+";
        var matches = Regex.Matches(input, pattern);

        foreach (Match match in matches)
        {
            result.Add(match.Value);
        }

        return result;
    }
    internal static string ProcessParamName(string input)
    {
        string pattern = @"\((.*)\)$";
        Match match = Regex.Match(input, pattern);
        if (match.Success)
        {
            string arguments = match.Groups[1].Value;
            var listArguments = ParseString(arguments);
            List<string> reducedArguments = new();
            foreach (var arg in listArguments)
            {
                //        Console.WriteLine(arg);
                var ra = ReduceArgument(arg);
                //        Console.WriteLine(ra);
                var ta = Transform(ra);
                //        Console.WriteLine(ta);
                reducedArguments.Add(ta);
            }
            var newArguments = string.Join(",", reducedArguments);
            input = input.Replace(arguments, newArguments);
        }

        return input;
    }
    #endregion
}
