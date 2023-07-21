# AssemblyInfo Editor
## Overview
AssemblyInfoEditor is a tool designed to help manage 'InternalsVisibleTo' attributes in C# projects by moving them from the 'AssemblyInfo.cs' file to the project file. This tool simplifies the management of 'InternalsVisibleTo' attributes.

## Features
Moves InternalsVisibleTo attribute from AssemblyInfo.cs into SDK-style project file.  
Removes unnecessary AssemblyTitle and Guid attirbutes from AssemblyInfo.cs.
Removes AssemblyInfo.cs if it doesn't contain any useful attributes.

## Getting Started
Clone this repository to your local machine.
Build the AssemblyInfoEditor project using Visual Studio or your preferred C# IDE.

## Usage
Open a console or terminal window.
Navigate to the folder containing the AssemblyInfoEditor executable.
Run the AssemblyInfoEditor executable with the following command:

```AssemblyInfoEditor.exe "C:\Users\YourUserName\Documents\YourProjectFolder"```

The tool will automatically move 'InternalsVisibleTo' attributes to the project file and perform the required clean-up.
