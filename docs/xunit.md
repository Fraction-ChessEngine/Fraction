How to use xunit?
------------------------

## Usage
Write a new testing method in an existing or new public testing class and mark it with the
`[Fact]` attribute. If you want to supply the function with parameters, use the
`[Theory]` in conjunction with the `[InlineData()]` attribute. Do not forget to `Assert`
at the end!

## Additional Resources
- [Getting Started](https://xunit.net/docs/getting-started/v3/cmdline)
- [Unit testing in C#](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test)
