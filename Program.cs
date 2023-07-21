using AssemblyInfoEditor;
using System.IO;
using System.Net.Http.Headers;
using System.Text;

internal class Program
{
    private static string _mainFolder;

    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("No arguments provided! You should specify the root folder as an argument.");
            return;
        }

        _mainFolder = args[0];

        if (string.IsNullOrEmpty(_mainFolder))
        {
            Console.WriteLine("The argument was empty! You should specify the root folder as an argument.");
            return;
        }

        if (!Directory.Exists(_mainFolder))
        {
            Console.WriteLine("The folder does not exist! Please, try again.");
            return;
        }

        Console.WriteLine($"Main folder is set to: {_mainFolder}");
        Console.WriteLine("Press Enter...");
        Console.ReadLine();

        ConsoleOutput("Searching for project files");

        var csprojFilesPaths = FindCsprojFiles(_mainFolder);
        var foundFilesAmount = csprojFilesPaths.Length;

        ConsoleOutput($"{foundFilesAmount} project files found");
        Console.WriteLine();

        var globalCounter = 0;

        foreach (var csprojFilePath in csprojFilesPaths)
        {
            globalCounter++;
            var projectFolderPath = Path.GetDirectoryName(csprojFilePath);
            var propertiesFolder = Path.Combine(projectFolderPath, "properties");
            var assemblyInfoFilePath = Path.Combine(propertiesFolder, "AssemblyInfo.cs");
            var projectName = Path.GetFileNameWithoutExtension(csprojFilePath);
            ConsoleOutput($"Processing project #{globalCounter}: {projectName}");

            var projectFileText = File.ReadAllLines(csprojFilePath);

            if (!projectFileText[0].StartsWith("<Project Sdk="))
            {
                ConsoleOutput($"The project is non-SDK.");
                Console.WriteLine();
                continue;
            }

            var linesToNewProjectFile = new List<string>();
            var linesToNewAssemblyinfoFile = new List<string>();

            if (IsAssemblyinfoExist(propertiesFolder, assemblyInfoFilePath))
            {
                ConsoleOutput($"AssemblyInfo.cs does exist in {projectName}");

                var assemblyInfoFileContent = File.ReadAllLines(assemblyInfoFilePath).ToList();                

                for (int i = 0; i < assemblyInfoFileContent.Count; i++)
                {
                    switch (GoesToProjectFileOrAssemblyinfoFile(assemblyInfoFileContent[i]))
                    {
                        case FileDestination.ProjectFile:
                            linesToNewProjectFile.Add(assemblyInfoFileContent[i]);
                            break;
                        case FileDestination.AssemblyInfoFile:
                            linesToNewAssemblyinfoFile.Add(assemblyInfoFileContent[i]);
                            break;
                        default:
                            break;
                    }
                }

                if (linesToNewProjectFile.Count != 0)
                {
                    CreateNewProjectFile(projectFileText, csprojFilePath, linesToNewProjectFile);                    
                }

                var canDeleteAssemblyInfo = !linesToNewAssemblyinfoFile.Any(line => !string.IsNullOrEmpty(line) && !line.StartsWith("using "));

                if (canDeleteAssemblyInfo)
                {
                    File.Delete(assemblyInfoFilePath);
                    ConsoleOutput($"File deleted: {assemblyInfoFilePath}");
                }
                else
                {
                    var isFileEmpty = linesToNewAssemblyinfoFile.All(string.IsNullOrEmpty);

                    if (!isFileEmpty)
                    {
                        CreateNewAssemblyinfoFile(assemblyInfoFilePath, linesToNewAssemblyinfoFile);
                        ConsoleOutput($"AssemblyInfo.cs file updated: {assemblyInfoFilePath}");
                    }
                }

                if (CanPropertiesFolderBeDeleted(propertiesFolder))
                {
                    Directory.Delete(propertiesFolder);

                    ConsoleOutput($"Folder deleted: {propertiesFolder}");
                }
            }
            else
            {
                ConsoleOutput($"AssemblyInfo.cs doen't exist in {projectName}");
            }

            Console.WriteLine();
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

    private static bool IsAssemblyinfoExist(string propertiesFolder, string assemblyInfoFilePath)
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
                        editedProjectFile.AddRange(GetAssemblyAtribute(extractedString));
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
        Console.WriteLine($"Project file updated: {csprojFilePath}");

        static List<string> GetAssemblyAtribute(string anotherProjectName)
        {
            return new List<string>
            {
                "    <AssemblyAttribute Include=\"System.Runtime.CompilerServices.InternalsVisibleToAttribute\">",
                $"      <_Parameter1>{anotherProjectName.Trim()}</_Parameter1>",
                "    </AssemblyAttribute>"
            };
        }
    }

    private static void ConsoleOutput(string message)
    {
        if (!Console.IsOutputRedirected)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] - {message}");
        }
    }    
}