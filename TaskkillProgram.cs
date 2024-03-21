using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace taskkillbinary
{
    internal class Program
    {
        // boolean variables to track with switches are given
        static Boolean PIDpresent = false;
        static Boolean IMpresent = false;
        static Boolean Fpresent = false;
        static Boolean Tpresent = false;
        static string PID = null;
        static string processname = null;

        static void Main(string[] args)
        {
            // track which switches were given by the user
            populateVariables(args);

            // killing process by the pid or process name based on arguments from user
            // all taskkill commands need either /PID or /IM switch
            // if both /PID and /IM are present, /PID can be used as it is unique
            if (PIDpresent)
            {
                killProcessbyId(Convert.ToInt32(PID));
            }
            else if (IMpresent)
            {
                killProcessbyName(processname);
            }

        }

        static void populateVariables(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {

                if (args[i].StartsWith("/"))
                {
                    // process each command line argument and populate their boolean variables
                    switch (args[i].ToLower())
                    {
                        case "/pid":
                            PIDpresent = true;
                            PID = args[++i];
                            break;
                        case "/im":
                            IMpresent = true;
                            processname = args[++i].Trim();
                            break;
                        case "/f":
                            Fpresent = true;
                            break;
                        case "/t":
                            Tpresent = true;
                            break;
                        default:
                            // Handle unknown arguments or no arguments
                            Console.WriteLine($"No switches were provide or the switches were invalid.");
                            break;
                    }

                }
            }
        }

        static void killProcessbyId(int processId)
        {
            try
            {
                // Find the process by its PID
                Process process = Process.GetProcessById(processId);

                // Kill the process based on the /T switch.
                // if /T switch is present, the tool needs to kill all child processes
                // if /T is not present, the tool simply kills the process
                if (!Tpresent)
                {
                    /* 
                    Switches: /PID
                              /PID /IM
                              /PID /F
                              /PID /IM /F
                    */
                    killProcess(process);
                }
                if (Tpresent)
                {
                    /* 
                    Switches: / PID / T
                              / PID / IM / T
                              / PID / F / T
                              / PID / IM / F / T
                    */
                    killChildProcesses(process);
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while terminating process: {ex.Message}");
            }
        }

        static void killProcessbyName(string procName)
        {
            // Get all processes with the specified name
            Process[] processes = Process.GetProcessesByName(procName);


            if (processes.Length > 0)
            {

                // Iterate over the processes and terminate each one
                foreach (Process process in processes)
                {
                    try
                    {
                        // Kill the process based presence of /T switch
                        if (!Tpresent)
                        {
                            /*
                            Switches: /IM
                                      /IM /F
                           */
                            killProcess(process);
                        }
                        if (Tpresent)
                        {
                            // /IM /T and /IM /F /T
                            killChildProcesses(process);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error occurred while terminating process: {ex.Message}");
                    }

                }
            }
            else
            {
                Console.WriteLine($"No processes with the name '{procName}' are running.");
            }
        }

        static void killProcess(Process process)
        {
            if (Fpresent)
            {
                // if /F switch is present, the tool will forcebly close the process with .Kill()
                process.Kill();
                if (!Tpresent)
                    Console.WriteLine($"SUCCESS: The process with PID {process.Id} has been terminated.");
            }
            else
            {
                // if /F is not present, the tool will close the process more gracefully with .CloseMainWindow()
                process.CloseMainWindow();
                if (!Tpresent)
                    Console.WriteLine($"SUCCESS: Sent termination signal to the process with PID {process.Id}.");
            }
        }

        static void killChildProcesses(Process root)
        {

            if (root != null)
            {


                var list = new List<Process>();

                // stores the parent process and all it's child processes in a list
                GetProcessAndChildren(Process.GetProcesses().ToList(), root, list);


                // kill each process in the list
                foreach (Process p in list)
                {
                    if (!p.HasExited)
                    {
                        try
                        {

                            killProcess(p);
                            if (!Fpresent)
                            {
                                if (p.Id == root.Id)
                                    Console.WriteLine($"SUCCESS: Sent termination signal to process with PID {p.Id}");
                                else
                                    Console.WriteLine($"SUCCESS: Sent termination signal to process with PID {p.Id}, child of PID {root.Id}");
                            }
                            if (Fpresent)
                            {
                                if (p.Id == root.Id)
                                    Console.WriteLine($"SUCCESS: The process with PID {p.Id} has been terminated.");
                                else
                                    Console.WriteLine($"SUCCESS: The process with PID {p.Id}, child of PID {root.Id}, has been terminated.");
                            }
                        }

                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Unknown process id: " + root);
            }
        }


        static void GetProcessAndChildren(List<Process> allProcesses, Process parent, List<Process> output)
        {
            //recursively gets all child processes of the parent process
            foreach (Process p in allProcesses)
            {
                // if the current process in not the parent process and the parent of the current process matches the parent process given
                if (p.Id != parent.Id && ParentId(p) == parent.Id)
                {
                    // calls itself and passes current process
                    GetProcessAndChildren(allProcesses, p, output);
                }
            }
            // adds process to list
            output.Add(parent);
        }
        static int ParentId(Process process)
        {
            // this method gets the parent Id of a given process

            try
            {
                using (ManagementObject managementObject = new ManagementObject($"win32_process.handle='{process.Id}'"))
                {
                    managementObject.Get();
                    return Convert.ToInt32(managementObject["ParentProcessId"]);
                }
            }
            catch
            {
                // send -1 if the parent id can't be retrieved
                return -1;
            }
        }
    }

}

