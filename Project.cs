using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyInfoEditor;

public class Project
{
    public string? Name { get; set; }

    public string[] ProjectFileContent { get; set; } = Array.Empty<string>();

    public string FolderPath { get; set; } = string.Empty;

    public string PropertiesFolderPath { get; set; } = string.Empty;

    public string AssemblyInfoFilePath { get; set; } = string.Empty;

    public string[] AssemblyInfoFileContent { get; set; } = Array.Empty<string>();
}
