// ------------------------------------------------------------------------
// MIT License - Copyright (c) Microsoft Corporation. All rights reserved.
// ------------------------------------------------------------------------

using System.ComponentModel.Design;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FluentUI.Demo.Shared.Tests;

public class ApiDocumentation
{
    [Fact]
    public void ToastService_ClearAll()
    {
        Type component = typeof(Microsoft.FluentUI.AspNetCore.Components.ToastService);
        MethodInfo? methodInfo = component.GetMethod("ClearAll");

        Assert.NotNull(methodInfo);
        var actual = Components.ApiDocumentation.GetDescription(component, methodInfo);
        var expected = "Removes all toasts";

        Assert.Equal(expected, actual);
    }
    [Fact]
    public void ToastService_ShowCustom()
    {
        Type component = typeof(Microsoft.FluentUI.AspNetCore.Components.ToastService);
        MethodInfo? methodInfo = component.GetMethod("ShowCustom");

        Assert.NotNull(methodInfo);

        var parameters = methodInfo.GetParameters();

        Assert.Equal("System.String", parameters[0].ParameterType.FullName);
        Assert.Equal("System.Nullable{System.Int32}", parameters[1].ParameterType.FullName);

    }
    [Fact]
    public void ToastService_ShowError()
    {
        var input = "System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]";
        //var expected = "System.Nullable{System.Int32}";
        var expected = "System.Nullable`1[[System.Int32]]";

        var actual = ProcessParameter(input);

        Assert.Equal(expected, actual);
    }
    internal static string ProcessParameter(string input)
    {
        var l = ExtractComponents(input);
        if (l.Count > 1)
        {
            List<string> result = new List<string>();
            foreach (var component in l)
            {
                result.Add(ProcessParameter(component));
                input = input.Replace(component, ProcessParameter(component));
            }
            /* fix here next */
        }
        else
        {
            var match = Regex.Match(input, @"^\[(.*)\]$");
            if (match.Success)
            {
                var a = match.Groups[1].Value;
                var b = ProcessParameter(a);
                input = input.Replace(a, b);
            }

            MatchCollection matches = Regex.Matches(input, @"\[(?:[^[\]]+|(?<open>\[)|(?<-open>\])*)\](?(open)(?!))");
            if (matches.Count < 2)
            {
                input = Regex.Match(input, @"^([^,]+)").Groups[1].Value;
            }
        }

        return input;
    }
    [Theory]
    [InlineData("", "")]
    [InlineData("System.String", "System.String")]
    [InlineData("System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]", "System.Nullable`1[[System.Int32]]")]
    [InlineData("System.Nullable`1[[Microsoft.AspNetCore.Components.EventCallback`1[[Microsoft.FluentUI.AspNetCore.Components.ToastResult, Microsoft.FluentUI.AspNetCore.Components, Version=4.10.2.0, Culture=neutral, PublicKeyToken=null]], Microsoft.AspNetCore.Components, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60]]",
            "System.Nullable`1[[Microsoft.AspNetCore.Components.EventCallback`1[[Microsoft.FluentUI.AspNetCore.Components.ToastResult]]]]")]
    [InlineData("System.Nullable`1[[System.ValueTuple`2[[Microsoft.FluentUI.AspNetCore.Components.Icon, Microsoft.FluentUI.AspNetCore.Components, Version=4.10.2.0, Culture=neutral, PublicKeyToken=null],[Microsoft.FluentUI.AspNetCore.Components.Color, Microsoft.FluentUI.AspNetCore.Components, Version=4.10.2.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
            "")]
    public void ProcessParameter_Check(string input, string expected)
    {
        var actual = ProcessParameter2(input);

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Text surrounded by []
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static List<string> ExtractComponents(string input)
    {
        List<string> components = new List<string>();
        string pattern = @"\[(?:[^[\]]+|(?<open>\[)|(?<-open>\])*)\](?(open)(?!))";
        MatchCollection matches = Regex.Matches(input, pattern);

        foreach (Match match in matches)
        {
            components.Add(match.Value.Trim());
        }

        return components;
    }

    /* next try */
    public static string ProcessParameter2(string input)
    {
        if (input.Equals(string.Empty))
        {
            return input;
        }
        /*
        string pattern = @"(?<=\[)\[*(.*)\]*(?=\])";

        MatchCollection matches = Regex.Matches(input, pattern);

        foreach (Match match in matches)
        {
            Console.WriteLine("Match: " + match.Value);

            // Recursively extract nested brackets content
            var l = ParseBracketedItems(match.Value);
            var b = ProcessParameter2(match.Value);
            input = input.Replace(match.Value, b);
        }
        */
        string pattern = @"(?<=\[)\[*(.*)\]*(?=\])";

        if (Components.ApiDocumentation.IsTuple(input))
        {
            var typesInTuple = Components.ApiDocumentation.TupleParameterTypeList(input);
            foreach (var t in typesInTuple)
            {
                var match = Regex.Match(t, pattern);
                var b = ProcessParameter2(match.Value);
                input = input.Replace(match.Value, b);
            }
        }
        else
        {

            var match = Regex.Match(input, pattern);
            if (match.Success)
            {
                var b = ProcessParameter2(match.Value);
                input = input.Replace(match.Value, b);
            }
            else
            {
                var c = Extract(input)[0];
                input = c;
            }
        }

        return input;
    }

    private static List<string> Extract(string input)
    {
        // Regex pattern to check for square brackets and split by commas if no brackets are found
        var enclosedInBrackets = @"^\[.*\]$";
        if (Regex.IsMatch(input, enclosedInBrackets))
        {
            // Return the entire input as a single item if brackets are present
            return new List<string> { input };
        }

        //
        var splitPattern = @",\s*";
        var matches = Regex.Split(input, splitPattern);
        var result = matches.Select(match => match.Trim()).ToList();

        return result;
    }

    public static List<string> ParseBracketedItems(string input)
    {
        //       var pattern = @"\[(?<content>[^\[\]]+|\[(?<c>[^\[\]]*)\])*]";
        //        var pattern = @"\[([^][]*(?:\[[^][]*\][^][]*)*)\]";
        //        var pattern = @"([^,\[\]]+|\[[^\[\]]*(?:\[[^\[\]]*\][^\[\]]*)*\])";
        var pattern = @"\[([^\[\]]*(?:\[[^\[\]]*\][^\[\]]*)*)\](?=,|\s*$)";
        var matches = Regex.Matches(input, pattern);

        List<string> result = new List<string>();
        foreach (Match match in matches)
        {
            result.Add(match.Value.Trim());
        }

        return result;
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("System.String", "System.String")]
    [InlineData("[System.String]", "[System.String]")]
    [InlineData("[System.String], a, b, c, d", "[System.String]")]
    [InlineData("stuff[System.String], a, b, c, d", "stuff[System.String]")]
    [InlineData("stuff[System.String]", "stuff[System.String]")]
    [InlineData("System.String, a, b, c, d", "System.String")]
    [InlineData("stuff[[System.String]], a, b, c, d", "stuff[[System.String]]")]
    public void TestMatch(string input, string expected)
    {
        var pattern = @"\[+.*?\]+|[^,\s]+";

        var match = Regex.Match(input, pattern);

        var actual = string.Empty;
        if (match.Success)
        {
            actual = match.Value;
        }
        Assert.Equal(expected, actual);

    }

    [Theory]
    [InlineData("[abc]", 1)]
    [InlineData("[abc],[def]", 2)]
    [InlineData("[abc],def", 0)]
    [InlineData("[abc,a=3],[def]", 2)]
    [InlineData("[abc, a=3],[def]", 2)]
    [InlineData("[[abc],[def]]", 0)]
    [InlineData("[Microsoft.FluentUI.AspNetCore.Components.Icon, Microsoft.FluentUI.AspNetCore.Components, Version=4.10.2.0, Culture=neutral, PublicKeyToken=null],[Microsoft.FluentUI.AspNetCore.Components.Color, Microsoft.FluentUI.AspNetCore.Components, Version=4.10.2.0, Culture=neutral, PublicKeyToken=null]", 2)]
    public void TupleTest(string input, int expected)
    {
        int elementsInListCount = 0;
        string typePattern = @"\[[a-zA-Z0-9.,= ]+\]";
        string tuplePattern = $"^{typePattern}(,{typePattern})*$";

        var matchList = Regex.Match(input, tuplePattern);
        if (matchList.Success)
        {
            var matches = Regex.Matches(input, typePattern);
            elementsInListCount = matches.Count;
            List<string> list = matches.Cast<Match>().Select(m => m.Value).ToList();
        }

        Assert.Equal(expected, elementsInListCount);
    }
    [Theory]
    [InlineData("[abc]", 0)]
    [InlineData("[abc],[def]", 2)]
    [InlineData("[abc],def", 0)]
    [InlineData("[abc,a=3],[def]", 2)]
    [InlineData("[abc, a=3],[def]", 2)]
    [InlineData("[[abc],[def]]", 0)]
    [InlineData("[Microsoft.FluentUI.AspNetCore.Components.Icon, Microsoft.FluentUI.AspNetCore.Components, Version=4.10.2.0, Culture=neutral, PublicKeyToken=null],[Microsoft.FluentUI.AspNetCore.Components.Color, Microsoft.FluentUI.AspNetCore.Components, Version=4.10.2.0, Culture=neutral, PublicKeyToken=null]", 2)]
    public void TupleTestForApiDocumentation(string input, int expected)
    {
        int elementsInListCount = 0;

        if (Components.ApiDocumentation.IsTuple(input))
        {
            var list = Components.ApiDocumentation.TupleParameterTypeList(input);
            elementsInListCount = list.Count;
        }

        Assert.Equal(expected, elementsInListCount);
    }

    [Theory]
    [InlineData("Microsoft.FluentUI.AspNetCore.Components.ToastParameters{``1}",
                1, "ToastParameters<``1>")]
    [InlineData("System.Type,Microsoft.FluentUI.AspNetCore.Components.ToastParameters,``0",
                3, "Type,ToastParameters,``0")]
    [InlineData("Microsoft.FluentUI.AspNetCore.Components.ToastIntent,System.String,System.Nullable{System.Int32},System.String,System.Nullable{Microsoft.AspNetCore.Components.EventCallback{Microsoft.FluentUI.AspNetCore.Components.ToastResult}}",
                5, "ToastIntent,string,int?,string,EventCallback<ToastResult>?")]
    [InlineData("System.String,System.Nullable{Microsoft.AspNetCore.Components.EventCallback{Microsoft.FluentUI.AspNetCore.Components.ToastResult}}",
                2, "string,EventCallback<ToastResult>?")]
    [InlineData("System.String,System.Nullable{System.Int32},System.String,System.Nullable{Microsoft.AspNetCore.Components.EventCallback{Microsoft.FluentUI.AspNetCore.Components.ToastResult}},System.Nullable{System.ValueTuple{Microsoft.FluentUI.AspNetCore.Components.Icon,Microsoft.FluentUI.AspNetCore.Components.Color}}",
                5, "string,int?,string,EventCallback<ToastResult>?,(Icon,Color)?")]
    [InlineData("System.Collections.Generic.Dictionary{System.String,System.Collections.Generic.List{System.Int32}}",
                1, "Dictionary<string,List<int>>")]
    [InlineData("System.Tuple{System.String,System.Tuple{System.Int32,System.String}}",
                1, "(string,(int,string))")]
    [InlineData("System.Collections.Generic.IReadOnlyDictionary{System.String,System.Object},System.Collections.Generic.Dictionary{System.String,System.Object}@",
                2, "IReadOnlyDictionary<string,object>,out Dictionary<string,object>")]
    public void ParseArgumentsTest(string input, int expectedCount, string expectedOutput)
    {
        List<string> arguments = ParseArguments(input);

        Assert.Equal(expectedCount, arguments.Count);
        //Assert.Equal(expectedOutput, matches[0].Value);
        List<string> strippedArguments = arguments.Select(arg => StripNamespace(arg)).ToList();
        List<string> nominalizedArguments = strippedArguments.Select(arg => NominalizeArgument(arg)).ToList();

        var actual = string.Join(",", nominalizedArguments);

        Assert.Equal(expectedOutput, actual);
    }

    private string ProcessInput(string input)
    {
        List<string> arguments = ParseArguments(input);
        List<string> strippedArguments = arguments.Select(arg => StripNamespace(arg)).ToList();
        return string.Join(",", strippedArguments);
    }

    private List<string> ParseArguments(string input)
    {
        List<string> result = new List<string>();
        string pattern = @"([^,{}@]+(?:{(?:[^{}]*|(?<open>{)|(?<-open>}))*(?(open)(?!))})*@*)";
        Regex regex = new Regex(pattern);
        MatchCollection matches = regex.Matches(input);
        foreach (Match match in matches)
        {
            result.Add(match.Value);
        }
        return result;
    }

    [Theory]
    [InlineData("Microsoft.FluentUI.AspNetCore.Components.ToastParameters{``1}", "ToastParameters{``1}")]
    [InlineData("System.Collections.Generic.List{System.String}", "List{String}")]
    [InlineData("System.String", "String")]
    [InlineData("Microsoft.FluentUI.AspNetCore.Components.ToastService", "ToastService")]
    [InlineData("System.Collections.Generic.Dictionary{System.String,System.Collections.Generic.List{System.Int32}}", "Dictionary{String,List{Int32}}")]
    [InlineData("System.Collections.Generic.Dictionary{System.String,System.Object}@", "Dictionary{String,Object}@")]
    public void StripeNamespaceTest(string input, string expected)
    {
        string actual = StripNamespace(input);
        Assert.Equal(expected, actual);
    }

    private string StripNamespace(string input)
    {
        string pattern = @"(?:[a-zA-Z0-9]+\.)+([a-zA-Z0-9]+)";
        return Regex.Replace(input, pattern, "$1");
    }

    [Theory]
    [InlineData("Dictionary{String,Object}@", "out Dictionary<string,object>")]
    [InlineData("Dictionary{String,Object}", "Dictionary<string,object>")]
    [InlineData("List{String}", "List<string>")]
    [InlineData("String", "string")]
    [InlineData("``1", "``1")]
    [InlineData("ToastService", "ToastService")]
    [InlineData("ToastParameters{``1}", "ToastParameters<``1>")]
    [InlineData("Tuple{String,Tuple{Int32,String}}", "(string,(int,string))")]
    [InlineData("Tuple{String,Tuple{Int32,String}}@", "out (string,(int,string))")]
    [InlineData("Nullable{Tuple{String,Tuple{Int32,String}}}", "(string,(int,string))?")]
    [InlineData("Nullable{EventCallback{ToastResult}}", "EventCallback<ToastResult>?")]
    public void NominalizeArgumentTest(string input, string expected)
    {
        string actual = NominalizeArgument(input);
        Assert.Equal(expected, actual);
    }

    private string NominalizeArgument(string input)
    {
        if (input.EndsWith("@"))
        {
            input = "out " + input.TrimEnd('@');
        }

        input = input.Replace('{', '<').Replace('}', '>');
        input = input.Replace("String", "string");
        input = input.Replace("Int32", "int");
        input = input.Replace("Boolean", "bool");
        input = input.Replace("Object", "object");

        // Replace ValueTuple<...> with (...)
        string valueTuplePattern = @"ValueTuple<((?:[^<>]+|<(?<Depth>)|>(?<-Depth>))*(?(Depth)(?!)))>";
        while (Regex.IsMatch(input, valueTuplePattern))
        {
            input = Regex.Replace(input, valueTuplePattern, "($1)");
        }

        // Replace Tuple<...> with (...)
        string tuplePattern = @"Tuple<((?:[^<>]+|<(?<Depth>)|>(?<-Depth>))*(?(Depth)(?!)))>";
        while (Regex.IsMatch(input, tuplePattern))
        {
            input = Regex.Replace(input, tuplePattern, "($1)");
        }

        // Replace Nullable<...> with ...?
        string nullablePattern = @"Nullable<((?:[^<>]+|<(?<Depth>)|>(?<-Depth>))*(?(Depth)(?!)))>";
        while (Regex.IsMatch(input, nullablePattern))
        {
            input = Regex.Replace(input, nullablePattern, "$1?");
        }

        return input;
    }

    [Theory]
    [InlineData("System.String",
        "System.String")]
    [InlineData("System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
        "System.Nullable{System.Int32}")]
    [InlineData("System.Nullable`1[[Microsoft.AspNetCore.Components.EventCallback`1[[Microsoft.FluentUI.AspNetCore.Components.ToastResult, Microsoft.FluentUI.AspNetCore.Components, Version=4.11.1.0, Culture=neutral, PublicKeyToken=null]], Microsoft.AspNetCore.Components, Version=9.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60]]\r\n",
        "System.Nullable{Microsoft.AspNetCore.Components.EventCallback{Microsoft.FluentUI.AspNetCore.Components.ToastResult}}")]
    [InlineData("System.Nullable`1[[System.ValueTuple`2[[Microsoft.FluentUI.AspNetCore.Components.Icon, Microsoft.FluentUI.AspNetCore.Components, Version=4.11.1.0, Culture=neutral, PublicKeyToken=null],[Microsoft.FluentUI.AspNetCore.Components.Color, Microsoft.FluentUI.AspNetCore.Components, Version=4.11.1.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]\r\n",
        "System.Nullable{System.ValueTuple{Microsoft.FluentUI.AspNetCore.Components.Icon,Microsoft.FluentUI.AspNetCore.Components.Color}}")]
    public void NormalizeParameterTest(string input, string expected)
    {
        var actual = NormalizeParameter(input);

        Assert.Equal(expected, actual);
    }

    private static string NormalizeParameter(string input)
    {
        if (Components.ApiDocumentation.IsTuple(input))
        {
            var typesInTuple = Components.ApiDocumentation.TupleParameterTypeList(input);
            foreach (var type in typesInTuple)
            {
                var match = Regex.Match(type, input);
                var b = NormalizeParameter(match.Value);
                input = input.Replace(match.Value, b);
            }
        }
        else
        {
            string pattern = @"\[([^][]*)\]";
            var match = Regex.Match(input, pattern);
            if (match.Success)
            {
                var b = NormalizeParameter(match.Groups[1].Value);
                input = input.Replace(match.Groups[0].Value, b);
            }
            input = StripAssemblyInformation(input);
        }

        /*
        var matches = Regex.Matches(input, pattern);

        foreach (Match match in matches)
        {
            var b = NormalizeParameter(match.Groups[1].Value);
            input = input.Replace(match.Groups[0].Value, b);
        }
        */


        return input;
    }

    private static string StripAssemblyInformation(string input)
    {
        var pattern = @"([^,]+)";
        var match = Regex.Match(input, pattern);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return input;
    }
}
