# Tasklist and Taskkill Binaries

## Description
The purpose of the tasklist binary is to retrieve information on the processes that are present on the system. The taskkill binary is intented for the termination of specified processes. These binaries were coded in C# and mainly rely on the use of System.Management and System.Diagnostics namespaces.

## How to Set Up the Tool
Download this folder from GitHub and move it to your desired location on your computer. Open up an Adminstrator Command Prompt and navigate into the midterm-git folder. It is recommended to run these binaries from the Adminstrator Command Prompt for full functionality. While it can still run from a regular command prompt, the Administrator Command Prompt can retrieve the full information for the /V switch for the tasklist binary.

From the Adminstrator command prompt, either binary can be executed with the following commands:
- tasklistbinary
- taskkillbinary

### Tasklist Binary (command: tasklistbinary)
This command doesn't need any switches or arguments to be passed; however, it has two switches available for use. The following is a list of the command (denoted in bold) and what information it will retrieve:
- **tasklistbinary** (retrives the image name, process id, session name, session number, and memory usage)
- **tasklistbinary /V** (retrives the image name, process id, session name, session number, memory usage, CPU time, username, status, and window title)
- **tasklistbinary /SVC** (retrives the image name, process id, and the services for the process if they are present)

### Taskkill Binary
This command requires either the process id or the name of the process to be specified in order to terminate it. Below is a list of the available switches:
- **/PID**: Used to specify process id
- **/IM**: Used to specify the process name
- **/T**: Tells the binary to kill the parent process and the child processes
- **/F**: Tells the binary to forceably kill the specified processes

When using the /PID or /IM switch, the process id or the process name must be provided as an argument after the switch. Example command: **taskkillbinary /PID 2313**
