# AssemblyInfoEditor
## Overview
AssemblyInfoManager is a tool designed to help manage 'InternalsVisibleTo' attributes in C# projects by moving them from the 'AssemblyInfo.cs' file to the project file. This tool simplifies the management of 'InternalsVisibleTo' attributes and ensures consistency across projects.

## Features
Moves 'InternalsVisibleTo' attributes from 'AssemblyInfo.cs' to the project file.
Places 'InternalsVisibleTo' attributes right after '<Import Project=...' nodes.
Removes 'AssemblyInfo.cs' if it contains only 'AssemblyTitle' and 'Guid' attributes after moving 'InternalsVisibleTo' attributes.
Removes the 'Properties' directory if it's empty after the above operations.

## Getting Started
Clone this repository to your local machine.
Build the AssemblyInfoManager project using Visual Studio or your preferred C# IDE.

## Usage
Open a console or terminal window.
Navigate to the folder containing the AssemblyInfoManager executable.
Run the AssemblyInfoManager executable with the following command:
```  AssemblyInfoManager.exe "C:\Users\YourUserName\Documents\YourProjectFolder"```

The tool will automatically move 'InternalsVisibleTo' attributes to the project file and perform the required clean-up.
