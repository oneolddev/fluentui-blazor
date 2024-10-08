// ------------------------------------------------------------------------
// MIT License - Copyright (c) Microsoft Corporation. All rights reserved.
// ------------------------------------------------------------------------

namespace FluentUI.Demo.Generators.Tests;

public class CodeCommentsGenerator
{
    [Theory]
    [InlineData("M:Microsoft.FluentUI.AspNetCore.Components.ToastService.ShowToast``2(Microsoft.FluentUI.AspNetCore.Components.ToastParameters{``1})", "ToastService.ShowToast``2")]
    public void CleanupParamName_ShouldParse(string input, string expected)
    {
        var actual = Generators.CodeCommentsGenerator.CleanupParamName(input);

        Assert.Equal(expected, actual);
    }

}
