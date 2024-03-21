using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;  // collection of types that support interoperation and platform invoke services
using System.Text; // classes representing ASCII and Unicode character encodings

namespace tasklistbinary
{
    internal class Program
    {
        // delegate that represents the callback function used with the EnumWindows function
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // Enumerates all top-level windows on the screen by passing the handle to each window
        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        // Retrieves the text of the specified window's title
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        // Retrieves the identifier of the thread that created the specified window
        [DllImport("user32.dll")]
        static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        static void Main(string[] args)
        {


            // Set up WMI query to retrieve process information
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Process");

            // Retrieve process information
            ManagementObjectCollection processCollection = searcher.Get();

            // Get the length of the longest process name for formatting
            int maxProcessNameLength = maxLength(processCollection);

            // Write headers based on arguments
            if (args.Length == 0)
            {

                Console.WriteLine("Image Name".PadRight(maxProcessNameLength + 3) + "PID".PadRight(8) + "Session Name".PadRight(15) + "Session#".PadRight(10) + "Mem Usage");
                Console.WriteLine($"{new string('=', maxProcessNameLength + 2)} {new string('=', 7)} {new string('=', 14)} {new string('=', 9)} {new string('=', 9)}");
                
            }
            else if (args[0] == "/V")
            {
                Console.WriteLine("Image Name".PadRight(maxProcessNameLength + 3) + "PID".PadRight(8) + "Session Name".PadRight(15) + "Session#".PadRight(10) + "Mem Usage".PadRight(13) + "Status".PadRight(15) + "User Name".PadRight(30) + "CPU Time".PadRight(15) + "Window Title");
                Console.WriteLine($"{new string('=', maxProcessNameLength + 2)} {new string('=', 7)} {new string('=', 14)} {new string('=', 9)} {new string('=', 12)} {new string('=', 14)} {new string('=', 29)} {new string('=', 14)} {new string('=', 12)}");
            }
            else if (args[0] == "/SVC")
            {
                Console.WriteLine("Image Name".PadRight(maxProcessNameLength + 3) + "PID".PadRight(8) + "Services");
                Console.WriteLine($"{new string('=', maxProcessNameLength + 2)} {new string('=', 7)} {new string('=', 8)}");
            }
            else
            {
                Console.WriteLine("Invalid Command.");
                Console.ReadLine();
            }


            // Iterate through each process
            foreach (ManagementObject process in processCollection)
            {
                // get basic process information
                // Get Session Name and session Id
                string sessionName;
                int sessId = Convert.ToInt32(process["SessionId"]);
                string sessionId = sessId.ToString().PadRight(10);
                if (sessId == 1)
                {
                    sessionName = "Console".PadRight(15); // Assuming session ID 1 corresponds to "Console"
                }
                else 
                {
                    sessionName = "Services".PadRight(15); // Assuming session IDs 0 corresponds to "Services"
                }

                long memsize = (Convert.ToInt64(process["WorkingSetSize"]))/1024;
                string processName = process["Name"].ToString().PadRight(maxProcessNameLength + 3);
                int procId = Convert.ToInt32(process["ProcessId"]);
                string PID = procId.ToString().PadRight(8);

                // Print process information based on the /switch

                if (args.Length == 0)
                {

                    Console.WriteLine(processName + PID + sessionName + sessionId + $"{memsize} K");

                }
                else if (args[0] == "/V")
                {   
                    // get additional information: CPU time, status, username, window title  
                    // these variables are populated below through methods defined later in this file

                    // username
                    string username = GetProcessUserName(process).PadRight(30);
                   
                    // cpu time
                    long kernelModeTime = Convert.ToInt64(process["KernelModeTime"]);
                    long userModeTime = Convert.ToInt64(process["UserModeTime"]);
                    TimeSpan cpu = TimeSpan.FromTicks((long)(kernelModeTime + userModeTime));
                    string CPUtime = cpu.ToString(@"h\:mm\:ss").PadRight(15);
                  
                   // window title
                    string windowTitle = getWindowTitle(procId);

                    // status - uses cpu time and window title
                    string status = ProcessStatus(Convert.ToInt32(process["ProcessId"]), (int)cpu.TotalSeconds, windowTitle).PadRight(15);

     
                    Console.WriteLine(processName + PID + sessionName + sessionId + $"{memsize} K".PadRight(13) + status + username + CPUtime + windowTitle);


                }
                else if (args[0] == "/SVC")
                {

                    // Create a dictionary to store process IDs and the services hosted within each process (services stored in a list)
                    var processServices = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<string>>();

                    // get process ID
                    int processId = Convert.ToInt32(process["ProcessId"]);

                    // check if the process ID is not already in the dictionary
                    if (!processServices.ContainsKey(processId))
                    {
                        // add the process ID to the dictionary with an empty list of services
                        processServices[processId] = new System.Collections.Generic.List<string>();
                    }

                    // set up WMI query to retrieve services hosted within the current process
                    string serviceQuery = $"SELECT * FROM Win32_Service WHERE ProcessId = {processId}";

                    ManagementObjectSearcher serviceSearcher = new ManagementObjectSearcher(serviceQuery);

                    // retrieve services hosted within the current process
                    ManagementObjectCollection serviceCollection = serviceSearcher.Get();

                    // iterate through each service hosted in the current process
                    foreach (ManagementObject service in serviceCollection)
                    {
                        // get the service name
                        string serviceName = service["Name"].ToString();

                        // add the service name to the list of services hosted in the current process
                        processServices[processId].Add(serviceName);
                    }
                    

                    // put services for the current process into a formatted string
                    string hostedServices = "N/A";
                    if (processServices[processId].Count > 0 && processId != 0) 
                    {
                        hostedServices = string.Join(", ", processServices[processId]);
                    }

                    Console.WriteLine(processName + PID + hostedServices);


                }
                else
                {
                    Console.WriteLine("Invalid Command.");
                    Console.ReadLine();
                }

            }


        }

        // returns the length (int) of the longest process name -- used for formatting output
        static int maxLength(ManagementObjectCollection processes)
        {
            int maxPNLength = 0;

            foreach (ManagementObject process in processes)
            {
                string processName = Convert.ToString(process["Name"]);
                maxPNLength = Math.Max(maxPNLength, processName.Length);
            }

            return maxPNLength;
        }


        // determine the status of the process
        static string ProcessStatus(int processId, int CPUTime, string window)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                if (!process.Responding)
                {
                    return "Not Responding";
                }
                else if ((!process.HasExited && CPUTime > 0) || (!process.HasExited && window != "N/A" && CPUTime == 0 )) 
                {
                    // process is using system resources (determined by the CPU time) or it has a window open 
                    return "Running";
                }
                else
                {
                    return "Unknown";
                }
            }
            catch (Exception)
            {
                return "Unknown"; 
            }
        }

        // gets the user name
        static string GetProcessUserName(ManagementObject process)
        {
            try
            {
                // Get the handle of the process
                string processHandle = process["Handle"].ToString();

                // Create a new management object with the process handle
                ManagementObject managementObject = new ManagementObject($"Win32_Process.Handle='{processHandle}'");

                // Use GetOwner method to get the session user name
                string[] owner = new string[2];
                managementObject.InvokeMethod("GetOwner", (object[])owner);
                string userName = owner[0];
                string domain = owner[1];

                // format and return user name
                return $"{domain}\\{userName}";
            }
            catch (Exception)
            {
                // Error occurred while retrieving the user name
                return "Unknown";
            }
        }
      

        // gets the title of the window
        static string getWindowTitle(int processId)
        {
            // variable to store window handle
            IntPtr foundWindowHandle = IntPtr.Zero;


            // enumerates all top-level windows, and for each window, it checks if the process ID matches the given process ID.
            EnumWindows((hWnd, lParam) =>
            {
                int windowProcessId;
                GetWindowThreadProcessId(hWnd, out windowProcessId);

                if (windowProcessId == processId)
                {
                    // get the text of the window's title bar
                    StringBuilder sb = new StringBuilder(256);
                    GetWindowText(hWnd, sb, sb.Capacity);
                    string windowTitle = sb.ToString();

                    // if windowTitle was found
                    if (!string.IsNullOrEmpty(windowTitle))
                    {
                        foundWindowHandle = hWnd; // assign handle of the window to a variable
                        return false; // Stop enumeration once the window is found
                    }
                }
                return true; // Continue enumeration
            }, IntPtr.Zero);


            // if handle isn't empty, then return the window title
            if (foundWindowHandle != IntPtr.Zero)
            {
                StringBuilder sb = new StringBuilder(256);
                GetWindowText(foundWindowHandle, sb, sb.Capacity);
                return sb.ToString();
            }
            else
            {
                return "N/A"; // return "N/A" if no window is found
            }
        }



    }

}