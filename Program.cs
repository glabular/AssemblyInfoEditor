using AssemblyInfoEditor;
using Microsoft.Extensions.Logging;
using System.Text;

internal class Program
{
    private static string _mainFolder;
    private static ToolOptions _toolOption;
    private static string[] _csprojFilesPaths;
    private static ILogger<Program> _logger;

    private static void Main(string[] args)
    {
        ConfigureLogger();
        ConsoleOutput("Application started.");

        if (args.Length == 0 || !args.Contains("--nowelcome"))
        {
            DisplayWelcomeMessage();
        }

        PromptForMainFolder();
        ConsoleOutput($"Main folder is set to: {_mainFolder}\n");
        PromptForAction();
        Console.WriteLine();
        Console.Write("All set. Press Enter to begin...");
        Console.ReadLine();
        ConsoleOutput("Searching for project files.");

        _csprojFilesPaths = FindCsprojFiles(_mainFolder);
        var foundFilesNumber = _csprojFilesPaths.Length;

        ConsoleOutput($"{foundFilesNumber} project files found");

        switch (_toolOption)
        {
            case ToolOptions.TransferOldStyle:
            case ToolOptions.TransferNewStyle:
                TransferAttributes();
                break;
            case ToolOptions.ReplaceOldWithNew:
                ReplaceOldStyleWithNewStyle();
                break;
            default:
                ConsoleOutput("Unknown option selected.");
                break;
        }
    }

    private static void ReplaceOldStyleWithNewStyle()
    {
        foreach (var filePath in _csprojFilesPaths)
        {
            Console.WriteLine();
            ConsoleOutput($"Processing {Path.GetFileNameWithoutExtension(filePath)}");

            var fileContent = File.ReadAllText(filePath);

            fileContent = ProcessReplacement(fileContent);

            File.WriteAllText(filePath, fileContent);
        }
    }

    private static string ProcessReplacement(string fileContent)
    {
        const string startTag = "<ItemGroup>";
        const string endTag = "</ItemGroup>";
        const string assemblyAttributeTag = "<AssemblyAttribute Include=\"System.Runtime.CompilerServices.InternalsVisibleToAttribute\">";
        const string parameterTag = "<_Parameter1>";
        const string parameterEndTag = "</_Parameter1>";

        var startIndex = 0;
        while ((startIndex = fileContent.IndexOf(startTag, startIndex)) != -1)
        {
            var endIndex = fileContent.IndexOf(endTag, startIndex) + endTag.Length;
            if (endIndex == -1)
            {
                break;
            }

            var itemGroupContent = fileContent[startIndex..endIndex];

            if (itemGroupContent.Contains(assemblyAttributeTag) && itemGroupContent.Contains(parameterTag))
            {
                var newItemGroup = BuildNewItemGroup(itemGroupContent, parameterTag, parameterEndTag);
                fileContent = string.Concat(fileContent.AsSpan(0, startIndex), newItemGroup, fileContent.AsSpan(endIndex));
            }
            else
            {
                startIndex = endIndex;
            }
        }

        return fileContent;
    }

    private static string BuildNewItemGroup(string itemGroupContent, string parameterTag, string parameterEndTag)
    {
        var newItemGroup = new StringBuilder();
        newItemGroup.AppendLine("<ItemGroup>");

        var currentIndex = 0;
        while ((currentIndex = itemGroupContent.IndexOf(parameterTag, currentIndex)) != -1)
        {
            var parameterStartIndex = currentIndex + parameterTag.Length;
            var parameterEndIndex = itemGroupContent.IndexOf(parameterEndTag, parameterStartIndex);

            if (parameterEndIndex == -1)
            {
                break;
            }

            var projectName = itemGroupContent[parameterStartIndex..parameterEndIndex];
            newItemGroup.AppendLine($"    <InternalsVisibleTo Include=\"{projectName}\" />");

            currentIndex = parameterEndIndex + parameterEndTag.Length;
        }

        newItemGroup.Append("  </ItemGroup>");

        return newItemGroup.ToString();
    }

    private static void TransferAttributes()
    {
        foreach (var csprojFilePath in _csprojFilesPaths)
        {
            var project = CreateProject(csprojFilePath);
            
            if (!IsProjectValid(project))
            {
                ConsoleOutput($"{project.Name} project is not valid.");
                continue;
            }

            Console.WriteLine();
            ConsoleOutput($"Processing {project.Name}");

            var linesToNewProjectFile = new List<string>();
            var linesToNewAssemblyInfoFile = new List<string>();

            CategorizeAssemblyInfoLines(project.AssemblyInfoFileContent.ToList(), linesToNewProjectFile, linesToNewAssemblyInfoFile);

            if (linesToNewProjectFile.Count == 0)
            {
                ConsoleOutput($"There is no data to add to the project file.");
            }
            else
            {
                CreateNewProjectFile(project.ProjectFileContent, csprojFilePath, linesToNewProjectFile);
            }

            var canDeleteAssemblyInfo = !linesToNewAssemblyInfoFile.Any(line => !string.IsNullOrEmpty(line) && !line.StartsWith("using "));

            if (canDeleteAssemblyInfo)
            {
                File.Delete(project.AssemblyInfoFilePath);
                ConsoleOutput($"File deleted: {project.AssemblyInfoFilePath}");
            }
            else
            {
                var isFileEmpty = linesToNewAssemblyInfoFile.All(string.IsNullOrEmpty);

                if (!isFileEmpty)
                {
                    CreateNewAssemblyinfoFile(project.AssemblyInfoFilePath, linesToNewAssemblyInfoFile);
                    ConsoleOutput($"AssemblyInfo.cs file updated: {project.AssemblyInfoFilePath}");
                }
            }

            if (CanPropertiesFolderBeDeleted(project.PropertiesFolderPath))
            {
                Directory.Delete(project.PropertiesFolderPath);

                ConsoleOutput($"Folder deleted: {project.PropertiesFolderPath}");
            }
        }
    }

    private static void CategorizeAssemblyInfoLines(List<string> assemblyInfoFileContent,
                                        List<string> newProjectFileLines,
                                        List<string> newAssemblyInfoLines)
    {
        foreach (var line in assemblyInfoFileContent)
        {
            switch (GoesToProjectFileOrAssemblyinfoFile(line))
            {
                case FileDestination.ProjectFile:
                    newProjectFileLines.Add(line);
                    break;
                case FileDestination.AssemblyInfoFile:
                    newAssemblyInfoLines.Add(line);
                    break;
                default:
                    break;
            }
        }
    }

    private static Project? CreateProject(string csprojFilePath)
    {
        var projectName = Path.GetFileNameWithoutExtension(csprojFilePath);
        var projectFolderPath = Path.GetDirectoryName(csprojFilePath);
        var projectPropertiesFolderPath = Path.Combine(projectFolderPath, "properties");
        var assemblyInfoFilePath = Path.Combine(projectPropertiesFolderPath, "AssemblyInfo.cs");

        if (IsAssemblyInfoExist(projectPropertiesFolderPath, assemblyInfoFilePath))
        {
            return new Project()
            {
                Name = projectName,
                FolderPath = projectFolderPath,
                PropertiesFolderPath = projectPropertiesFolderPath,
                AssemblyInfoFileContent = File.ReadAllLines(assemblyInfoFilePath),
                AssemblyInfoFilePath = assemblyInfoFilePath,
                ProjectFileContent = File.ReadAllLines(csprojFilePath)
            };
        }

        return null;
    }

    private static bool IsProjectValid(Project project)
    {
        return project is not null && IsSdkStyleProject(project.ProjectFileContent);
    }

    private static bool IsSdkStyleProject(string[] projectFileText)
    {
        return projectFileText.Length > 0 && projectFileText[0].StartsWith("<Project Sdk=");
    }

    private static void PromptForAction()
    {
        while (true)
        {
            Console.WriteLine("Please select what you would like this tool to do:");
            Console.WriteLine("1: Transfer attributes in old (.NET Framework) style.");
            Console.WriteLine("2: Transfer attributes in new (.NET 5 +) style.");
            Console.WriteLine("3: Replace attributes in projects from the old style to the new style.");

            var input = Console.ReadLine();

            if (int.TryParse(input, out int syntaxIndex) &&
                Enum.IsDefined(typeof(ToolOptions), syntaxIndex))
            {
                _toolOption = (ToolOptions)syntaxIndex;
                break;
            }
            else
            {
                ConsoleOutput("Invalid input! Use a valid number.");
            }
        }
    }

    private static void PromptForMainFolder()
    {
        while (true)
        {
            Console.WriteLine("Please, provide the root folder:");
            _mainFolder = Console.ReadLine();

            if (string.IsNullOrEmpty(_mainFolder))
            {
                Console.WriteLine("The input was empty! You should specify the root folder.");
            }
            else if (!Directory.Exists(_mainFolder))
            {
                Console.WriteLine("The folder does not exist! Try again.");
            }
            else
            {
                break;
            }
        }
    }

    private static FileDestination GoesToProjectFileOrAssemblyinfoFile(string lineToCheck)
    {
        if (lineToCheck.Contains("[assembly: InternalsVisibleTo("))
        {
            return FileDestination.ProjectFile;
        }

        if (lineToCheck.Contains("[assembly: AssemblyTitle("))
        {
            return FileDestination.Other;
        }

        if (lineToCheck.Contains("[assembly: Guid("))
        {
            return FileDestination.Other;
        }

        return FileDestination.AssemblyInfoFile;
    }

    private static void CreateNewAssemblyinfoFile(string assemblyInfoFilePath, List<string> assemblyInfoFileContent)
    {
        //If the target file already exists, it is overwritten. (msdn)
        File.WriteAllLines(assemblyInfoFilePath, assemblyInfoFileContent);
    }

    private static bool IsAssemblyInfoExist(string propertiesFolder, string assemblyInfoFilePath)
    {
        return Directory.Exists(propertiesFolder) && File.Exists(assemblyInfoFilePath);
    }

    private static string[] FindCsprojFiles(string searchDirectory)
    {
        return Directory.GetFiles(searchDirectory, $"*.csproj", SearchOption.AllDirectories);
    }

    private static bool CanPropertiesFolderBeDeleted(string assemblyInfoFolder)
    {
        return !Directory.GetFiles(assemblyInfoFolder).Any();
    }

    private static void CreateNewProjectFile(string[] projectFileText, string csprojFilePath, List<string> linesToAddToProjectFile)
    {
        var editedProjectFile = new List<string>();

        for (int i = 0; i < projectFileText.Length; i++)
        {
            editedProjectFile.Add(projectFileText[i]);

            if (linesToAddToProjectFile.Count != 0)
            {
                if (projectFileText[i] == string.Empty)
                {
                    editedProjectFile.Add("  <ItemGroup>");

                    for (int j = 0; j < linesToAddToProjectFile.Count;)
                    {
                        var item = linesToAddToProjectFile[j];
                        var startIndex = item.IndexOf('"') + 1;
                        var endIndex = item.LastIndexOf('"');
                        var extractedString = item[startIndex..endIndex];
                        editedProjectFile.AddRange(GetAttribute(extractedString));
                        linesToAddToProjectFile.RemoveAt(j);
                    }

                    editedProjectFile.Add("  </ItemGroup>");
                    editedProjectFile.Add(string.Empty);
                }
            }
        }

        var result = editedProjectFile.ToArray();

        //File.WriteAllLines method will overwrite an existing file if it already exists.
        File.WriteAllLines(csprojFilePath, result);
        ConsoleOutput($"Project file updated: {csprojFilePath}");
    }

    private static List<string> GetInternalsVisibleToAttribute(string projectName)
    {
        return new List<string>
        {
            $"      <InternalsVisibleTo Include=\"{projectName.Trim()}\" />"
        };
    }

    private static List<string> GetAssemblyAtribute(string projectName)
    {
        return new List<string>
        {
            "    <AssemblyAttribute Include=\"System.Runtime.CompilerServices.InternalsVisibleToAttribute\">",
            $"      <_Parameter1>{projectName.Trim()}</_Parameter1>",
            "    </AssemblyAttribute>"
        };
    }

    private static List<string> GetAttribute(string anotherProjectName)
    {
        if (_toolOption == ToolOptions.TransferNewStyle)
        {
            return GetInternalsVisibleToAttribute(anotherProjectName);
        }
        else if (_toolOption == ToolOptions.TransferOldStyle)
        {
            return GetAssemblyAtribute(anotherProjectName);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private static void ConfigureLogger()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                //.AddConsole()
                .AddDebug()
                .SetMinimumLevel(LogLevel.Debug);
        });

        _logger = loggerFactory.CreateLogger<Program>();
    }

    private static void ConsoleOutput(string message)
    {
        if (!Console.IsOutputRedirected)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] - {message}");
        }
    }

    private static void DisplayWelcomeMessage()
    {
        Console.WriteLine("***************************************************");
        Console.WriteLine("*           AssemblyInfoEditor v1.1               *");
        Console.WriteLine("*           © 2024 Maksim Glushchenko             *");
        Console.WriteLine("*                                                 *");
        Console.WriteLine("*  AssemblyInfoEditor is a tool to                *");
        Console.WriteLine("*  automatically migrate 'InternalsVisibleTo'     *");
        Console.WriteLine("*  attributes from AssemblyInfo file into         *");
        Console.WriteLine("*  SDK-style project file.                        *");
        Console.WriteLine("*                                                 *");
        Console.WriteLine("*  GitHub:                                        *");
        Console.WriteLine("*  https://github.com/glabular/AssemblyInfoEditor *");
        Console.WriteLine("*  License: MIT License                           *");
        Console.WriteLine("***************************************************");
        Console.WriteLine();
        Console.WriteLine("Welcome to AssemblyInfoEditor!");
        Console.WriteLine("Please follow the instructions to use this tool.");
        Console.WriteLine();
    }
}