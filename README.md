# AssemblyInfo Editor
## Overview
AssemblyInfoEditor is a tool designed to help manage `InternalsVisibleTo` attributes in C# projects by moving them from the 'AssemblyInfo.cs' file to the project file.

## Features
* Moves the attributes from AssemblyInfo.cs into SDK-style project file (both in old .NET Framework and new .NET 5+ style).  
Removes unnecessary `AssemblyTitle` and `Guid` attirbutes from AssemblyInfo.cs.
Removes AssemblyInfo.cs if it doesn't contain any useful attributes.
* Replaces attributes in existing project files from the old style to the new style.

## Getting Started
1. Clone this repository to your local machine.
2. Build the AssemblyInfoEditor project using Visual Studio or your preferred C# IDE.

## Usage
Run the AssemblyInfoEditor executable and follow the on-screen commands.

### Arguments
* `--nowelcome` (disables the welcome message)