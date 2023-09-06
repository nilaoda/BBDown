using System.CommandLine;
using System.CommandLine.Parsing;

namespace BBDown
{
    public static class CommandLineExtension
    {
        public static T? GetValueForOption<T>(this ParseResult parseResult, Option<T> option) where T : struct
            => parseResult.HasOption(option) ? parseResult.GetValueForOption<T>(option) : null;
    }
}
