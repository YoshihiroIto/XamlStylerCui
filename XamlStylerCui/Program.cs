using System;
using System.IO;
using Mono.Options;
using XamlStyler.Core;
using XamlStyler.Core.Options;
using YamlDotNet.Serialization;

namespace XamlStylerCui
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var inputFilepath = string.Empty;
            var outputFilepath = string.Empty;
            var optionsFilepath = string.Empty;
            var isGenerateDefaultOptionsFile = false;
            var isShowHelp = false;

            var optionSet = new OptionSet
            {
                {"i=|input=", "Input file.", v => inputFilepath = v},
                {"o=|output=", "Output file.", v => outputFilepath = v},
                {"options=", "Options file.", v => optionsFilepath = v},
                {"gen_default_options", "Generate Default Options file.", v => isGenerateDefaultOptionsFile = v != null},
                {"h|help", "Show help.", v => isShowHelp = v != null}
            };

            try
            {
                optionSet.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine("error:");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `CommandLineOption --help' for more information.");
                return 1;
            }

            if (isShowHelp)
            {
                ShowUsage(optionSet);
                return 0;
            }

            if (isGenerateDefaultOptionsFile)
            {
                GenerateDefaultOptionsFile(outputFilepath);
                return 0;
            }

            try
            {
                ExecuteStyler(inputFilepath, outputFilepath, optionsFilepath);
            }
            catch (Exception e)
            {
                Console.WriteLine("error:");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `CommandLineOption --help' for more information.");
                return 1;
            }

            return 0;
        }

        private static void ShowUsage(OptionSet optionSet)
        {
            Console.Error.WriteLine("Usage:XamlStylerCui [OPTIONS]");
            Console.Error.WriteLine();
            optionSet.WriteOptionDescriptions(Console.Error);
        }

        private static void GenerateDefaultOptionsFile(string outputFilepath)
        {
            using (var tw = new StringWriter())
            {
                var serializer = new Serializer(SerializationOptions.EmitDefaults);
                serializer.Serialize(tw, new StylerOptions());

                if (string.IsNullOrEmpty(outputFilepath))
                    Console.WriteLine(tw.ToString());
                else
                    File.WriteAllText(outputFilepath, tw.ToString());
            }
        }

        private static void ExecuteStyler(string inputFilepath, string outputFilepath, string optionsFilepath)
        {
            if (File.Exists(inputFilepath) == false)
                throw new FileNotFoundException("Input file is not fount.", inputFilepath);

            StylerOptions options;
            {
                if (string.IsNullOrEmpty(optionsFilepath) == false)
                {
                    string actualOptionsFilepath = optionsFilepath;
                    {
                        var file = Path.GetFileName(optionsFilepath);
                        var dir = Path.GetDirectoryName(Path.GetFullPath(optionsFilepath));

                        while (dir != null)
                        {
                            actualOptionsFilepath = Path.Combine(dir, file);

                            if (File.Exists(actualOptionsFilepath))
                                break;

                            var parent = Directory.GetParent(dir);
                            if (parent == null)
                                break;

                            dir = parent.FullName;
                        }
                    }

                    if (File.Exists(actualOptionsFilepath) == false)
                        throw new FileNotFoundException("Options file is not fount.", optionsFilepath);

                    var optionsText = File.ReadAllText(actualOptionsFilepath);
                    options = (new Deserializer()).Deserialize<StylerOptions>(new StringReader(optionsText));
                }
                else
                {
                    options = new StylerOptions();
                }
            }

            var inputText = File.ReadAllText(inputFilepath);

            var styler = StylerService.CreateInstance(options);
            var outputText = styler.ManipulateTreeAndFormatInput(inputText);

            if (string.IsNullOrEmpty(outputFilepath))
                Console.WriteLine(outputText);
            else
                File.WriteAllText(outputFilepath, outputText);
        }
    }
}