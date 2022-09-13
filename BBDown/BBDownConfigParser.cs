using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.CommandLine;
using System.IO;
using System.Linq;
using static BBDown.Core.Logger;
using System.Text;
using System.Threading.Tasks;

namespace BBDown
{
    internal class BBDownConfigParser
    {
        public static void HandleConfig(List<string> newArgsList, RootCommand rootCommand)
        {
            try
            {
                var configPath = newArgsList.Contains("--config-file")
                    ? newArgsList.ElementAt(newArgsList.IndexOf("--config-file") + 1)
                    : Path.Combine(Program.APP_DIR, "BBDown.config");
                if (File.Exists(configPath))
                {
                    Log($"加载配置文件: {configPath}");
                    var configArgs = File
                        .ReadAllLines(configPath)
                        .Where(s => !string.IsNullOrEmpty(s) && !s.StartsWith("#"))
                        .SelectMany(s =>
                        {
                            var trimLine = s.Trim();
                            if (trimLine.IndexOf('-') == 0 && trimLine.IndexOf(' ') != -1)
                            {
                                var spaceIndex = trimLine.IndexOf(' ');
                                var paramsGroup = new string[] { trimLine[..spaceIndex], trimLine[spaceIndex..] };
                                return paramsGroup.Where(s => !string.IsNullOrEmpty(s)).Select(s => s.Trim(' ').Trim('\"'));
                            }
                            else
                            {
                                return new string[] { trimLine.Trim('\"') };
                            }
                        }
                        );
                    var configArgsResult = rootCommand.Parse(configArgs.ToArray());
                    foreach (var item in configArgsResult.CommandResult.Children)
                    {
                        if (item is OptionResult o)
                        {
                            if (!newArgsList.Contains("--" + o.Option.Name))
                            {
                                newArgsList.Add("--" + o.Option.Name);
                                newArgsList.AddRange(o.Tokens.Select(t => t.Value));
                            }
                        }
                    }

                    //命令行的优先级>配置文件优先级
                    LogDebug("新的命令行参数: " + string.Join(" ", newArgsList));
                }
            }
            catch (Exception)
            {
                LogError("配置文件读取异常，忽略");
            }
        }
    }
}
