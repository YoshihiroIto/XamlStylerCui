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
        private static string DefaultOptionFileName { get; } = "XamlStylerCui.yml";

        private static int Main(string[] args)
        {
            var inputFilepath = string.Empty;
            var outputFilepath = string.Empty;
            var optionsFilepath = string.Empty;
            var isCheckStyling = false;
            var isGenerateDefaultOptionsFile = false;
            var isShowHelp = false;

            var optionSet = new OptionSet
            {
                {"i=|input=", "Input file.", v => inputFilepath = v},
                {"o=|output=", "Output file.", v => outputFilepath = v},
                {"options=", "Options file.", v => optionsFilepath = v},
                {"check", "Check Styling.", v => isCheckStyling = v != null},
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
                GenerateDefaultOptionsFile();
                return 0;
            }

            try
            {
                if (isCheckStyling)
                {
                    var isClean = CheckStyling(inputFilepath, optionsFilepath);

                    Console.WriteLine("{0} is {1}.", inputFilepath, isClean ? "clean" : "dirty");
                    return isClean ? 0 : 1;
                }

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

        private static void GenerateDefaultOptionsFile()
        {
            using (var tw = new StringWriter())
            {
                var serializer = new Serializer(SerializationOptions.EmitDefaults);
                serializer.Serialize(tw, new StylerOptions());


                File.WriteAllText(DefaultOptionFileName, tw.ToString());
            }
        }

        private static bool CheckStyling(string inputFilepath, string optionsFilepath)
        {
            if (File.Exists(inputFilepath) == false)
                throw new FileNotFoundException("Input file is not fount.", inputFilepath);

            var options = MakeOptions(optionsFilepath);
            var styler = StylerService.CreateInstance(options);

            var inputText = File.ReadAllText(inputFilepath);
            var outputText = styler.ManipulateTreeAndFormatInput(inputText);

            return inputText == outputText;
        }

        private static void ExecuteStyler(string inputFilepath, string outputFilepath, string optionsFilepath)
        {
            if (File.Exists(inputFilepath) == false)
                throw new FileNotFoundException("Input file is not fount.", inputFilepath);

            var options = MakeOptions(optionsFilepath);
            var styler = StylerService.CreateInstance(options);

            var inputText = File.ReadAllText(inputFilepath);
            var outputText = styler.ManipulateTreeAndFormatInput(inputText);

            // todo:Because there is a blank character is left at the end of the line
            outputText = styler.ManipulateTreeAndFormatInput(outputText);

            if (string.IsNullOrEmpty(outputFilepath))
                Console.WriteLine(outputText);
            else
                File.WriteAllText(outputFilepath, outputText);
        }

        private static StylerOptions MakeOptions(string optionsFilepath)
        {
            StylerOptions options;
            {
                var actualOptionsFilepath = FindOptionsFilePath(optionsFilepath);

                if (string.IsNullOrEmpty(actualOptionsFilepath) == false)
                {
                    var optionsText = File.ReadAllText(actualOptionsFilepath);
                    options = (new Deserializer()).Deserialize<StylerOptions>(new StringReader(optionsText));
                }
                else
                {
                    options = new StylerOptions();
                }
            }

            return options;
        }

        private static string FindOptionsFilePath(string inputOptionsFilepath)
        {
            // current to root
            if (string.IsNullOrEmpty(inputOptionsFilepath) == false)
            {
                string optionsFilepath = inputOptionsFilepath;
                {
                    var file = Path.GetFileName(inputOptionsFilepath);
                    var dir = Path.GetDirectoryName(Path.GetFullPath(inputOptionsFilepath));

                    while (dir != null)
                    {
                        optionsFilepath = Path.Combine(dir, file);

                        if (File.Exists(optionsFilepath))
                            break;

                        var parent = Directory.GetParent(dir);
                        if (parent == null)
                            break;

                        dir = parent.FullName;
                    }
                }

                if (File.Exists(optionsFilepath) == false)
                    throw new FileNotFoundException("Options file is not fount.", inputOptionsFilepath);

                return optionsFilepath;
            }

            // home directory
            {
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                var optionsFilePath = Path.Combine(homeDir, DefaultOptionFileName);

                if (File.Exists(optionsFilePath))
                    return optionsFilePath;
            }

            // no options file
            return string.Empty;
        }
    }
}