// ------------------------------------------------------------------------
// MIT License - Copyright (c) Microsoft Corporation. All rights reserved.
// ------------------------------------------------------------------------

namespace FluentUI.Demo.Generators.Tests;

public class CodeCommentsGenerator
{
    [Theory]
    [InlineData("M:Microsoft.FluentUI.AspNetCore.Components.ToastService.ShowToast``2(Microsoft.FluentUI.AspNetCore.Components.ToastParameters{``1})", "ToastService.ShowToast``2(ToastParameters<``1>)")]
    public void CleanupParamName_ShouldParse(string input, string expected)
    {
        var actual = Generators.CodeCommentsGenerator.CleanupParamName(input);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("ToastService.ClearQueueMentionToasts", "ToastService.ClearQueueMentionToasts")]
    [InlineData("ToastService.ClearIntent(Microsoft.FluentUI.AspNetCore.Components.ToastIntent,System.Boolean)", "ToastService.ClearIntent(ToastIntent,bool)")]
    [InlineData("ToastService.ShowToast``2(Microsoft.FluentUI.AspNetCore.Components.ToastParameters{``1})", "ToastService.ShowToast``2(ToastParameters<``1>)")]
    [InlineData("ToastService.ShowToast``1(System.Type,Microsoft.FluentUI.AspNetCore.Components.ToastParameters,``0)", "ToastService.ShowToast``1(Type,ToastParameters,``0)")]
    [InlineData("ToastService.ShowToast(Microsoft.FluentUI.AspNetCore.Components.ToastIntent,System.String,System.Nullable{System.Int32},System.String,System.Nullable{Microsoft.AspNetCore.Components.EventCallback{Microsoft.FluentUI.AspNetCore.Components.ToastResult}})", "ToastService.ShowToast(ToastIntent,string,int?,string,EventCallback<ToastResult>?)")]
    [InlineData("ToastService.ShowCustom(System.String,System.Nullable{System.Int32},System.String,System.Nullable{Microsoft.AspNetCore.Components.EventCallback{Microsoft.FluentUI.AspNetCore.Components.ToastResult}},System.Nullable{System.ValueTuple{Microsoft.FluentUI.AspNetCore.Components.Icon,Microsoft.FluentUI.AspNetCore.Components.Color}})", "ToastService.ShowCustom(string,int?,string,EventCallback<ToastResult>?,(Icon,Color)?)")]
    public void ProcessParamName_ShouldReturnExpected(string input, string expected)
    {
        var actual = Generators.CodeCommentsGenerator.ProcessParamName(input);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReduceArgument_ForTuple()
    {
        var arg = "System.Nullable{System.ValueTuple{Microsoft.FluentUI.AspNetCore.Components.Icon,Microsoft.FluentUI.AspNetCore.Components.Color}}";
        string expected = "Nullable{ValueTuple{Icon,Color}}";
        var actual = Generators.CodeCommentsGenerator.ReduceArgument(arg);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Transform_ForTuple()
    {
        var arg = "Nullable{ValueTuple{Icon,Color}}";
        var expected = "(Icon,Color)?";
        var actual = Generators.CodeCommentsGenerator.Transform(arg);

        Assert.Equal(expected, actual);
    }
}
