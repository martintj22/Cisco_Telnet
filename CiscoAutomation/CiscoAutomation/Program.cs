﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CiscoAutomation
{
    class Program
    {
        public static Dictionary<String, String> CiscoDevice = new Dictionary<String, String>();
        public static List<String> confList = new List<String>();
        public static Telnet.TelnetConnection T1;

        public static string ip;
        public static string pw;

        static void Main(string[] args)
        {
            Connect();
        }

        /// <summary>
        /// Function to establish connection, with ip&pw from user-input
        /// </summary>
        static void Connect()
        {
            bool connecting = true;

            while (connecting)
            {
                Console.Clear();
                Print("Please enter the ip of the device you wish to configure", ConsoleColor.Red);
                ip = Console.ReadLine();
                Print("Enter the password", ConsoleColor.Red);
                pw = Console.ReadLine();

                // If both strings aren't empty - try to logon
                if ((!(string.IsNullOrWhiteSpace(ip))) && (!(string.IsNullOrWhiteSpace(pw))))
                {
                    CiscoDevice.Add("IP", ip); // Add ip to dict
                    T1 = new Telnet.TelnetConnection(CiscoDevice["IP"], 23);
                    // If connection succeeds
                    if (T1.IsConnected == true)
                    {
                        string s = T1.CiscoLogin(pw); // Login with given password
                        T1.CiscoEnable(pw); // After logging in, enter enable-mode with password
                        Print("Connection established!", ConsoleColor.Green);
                        Print("\n" + "Press any key to go to the configuration-menu", ConsoleColor.Yellow);
                        Console.ReadKey();

                        connecting = false; // Exit loop if connection succeeds
                        ChooseConf(); // Enter menu if connection succeeds
                    }
                }
                else if (string.IsNullOrEmpty(ip) || (string.IsNullOrEmpty(pw)))
                {
                    Print("Please enter username and password!", ConsoleColor.Red);
                    Print("\n" + "Press any key to retry logging in", ConsoleColor.Yellow);
                    Console.ReadKey();
                }
            }
            
        }

        /// <summary>
        /// Menu for choosing configurations
        /// </summary>
        static void ChooseConf()
        {
            Console.Clear(); // Clear console; to prevent clutter from multiple configurations
            string choice;
            bool decide = true;
            while (decide) {            
                Print("Welcome. Press the desired button to do the configuration you wish", ConsoleColor.Yellow);
                Print("1) Set/change password", ConsoleColor.Yellow);
                Print("2) Set/change MOTD", ConsoleColor.Yellow);
                Print("3) Change hostname", ConsoleColor.Yellow);
                Print("4) Configure vty-lines", ConsoleColor.Yellow);
                Print("5) Save running-config to text file", ConsoleColor.Yellow);
                Print("\n" + "Type 'exit' to close the program", ConsoleColor.Yellow);
                choice = Console.ReadLine();

                if (choice == "1")
                {
                    decide = false;
                    SetPass();
                }

                else if (choice == "2")
                {
                    decide = false;
                    SetMotd();
                }

                else if (choice == "3")
                {
                    decide = false;
                    SetHostname();
                }

                else if (choice == "4")
                {
                    decide = false;
                    LineVty();
                }

                else if (choice == "5")
                {
                    decide = false;
                    GetCfg();
                }

                else if (choice == "exit")
                {
                    Environment.Exit(0);
                }

                else
                {
                    Print("Please enter correct input!", ConsoleColor.Red);
                }
            }
        }

        /// <summary>
        /// To quickly use Console.Writeline, with text-color
        /// </summary>
        /// <param name="msg">The output string</param>
        /// <param name="color">Color to use for text</param>
        static void Print(string msg, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ResetColor();
        }
        
        /// <summary>
        /// Changes password, with a given string from user
        /// </summary>
        static void SetPass()
        {
            string pass;
            Print("Enter the password you wish to use", ConsoleColor.Green);
            pass = Console.ReadLine();
            T1.CiscoCommand("enable");
            T1.CiscoCommand("enable secret " + pass);
            Print("Password was successfully changed", ConsoleColor.Green);
            Print("\n" + "Press any key to return to menu..", ConsoleColor.Yellow);
            Console.ReadKey();
            ChooseConf(); // Back to menu
        }

        /// <summary>
        /// Changes MOTD, with a given string from user
        /// </summary>
        static void SetMotd()
        {
            string motd;

            Print("Enter the MOTD text:", ConsoleColor.Green);
            motd = Console.ReadLine();
            T1.CiscoCommand("configure terminal");
            T1.CiscoCommand("banner motd !" + motd + "!");
            T1.CiscoCommand("end"); // Back to enable mode
            Print("MOTD changed to " + motd, ConsoleColor.Green);
            Print("\n" + "Press any key to return to menu..", ConsoleColor.Yellow);
            Console.ReadKey();
            ChooseConf(); // Back to menu
        }

        /// <summary>
        /// Configure vty-lines from range given from user; can enter multiple commands
        /// </summary>
        static void LineVty()
        {
            string lineStart, lineEnd;
            bool configure = true;

            Print("Enter the first line in the range", ConsoleColor.Green);
            lineStart = Console.ReadLine();
            Print("Enter the last line in the range", ConsoleColor.Green);
            lineEnd = Console.ReadLine();
            T1.CiscoCommand("configure terminal");
            T1.CiscoCommand("line vty " + lineStart + " " + lineEnd);
            Print("Configuring vty lines from range " + lineStart + " to " + lineEnd + " ...", ConsoleColor.Yellow);
            Print("Press any key to continue..", ConsoleColor.Yellow);
            Console.ReadKey();

            while (configure)
            {
                string command;
                Print("Enter the commands you wish to use, type 'done' when you wish to exit. \n", ConsoleColor.Green);
                Console.Write("Command: ");
                command = Console.ReadLine();
                T1.CiscoCommand(command);

                // Exit loop and go back to menu if command string is 'done'
                if (command == "done")
                {
                    T1.CiscoCommand("end"); // Go back to enable-mode when loop is exit
                    configure = false;
                    ChooseConf();
                }                
            }
        }

        /// <summary>
        /// Changes hostname, with a given string from user
        /// </summary>
        static void SetHostname()
        {
            string newhost;
            Print("Enter hostname: ", ConsoleColor.Green);
            newhost = Console.ReadLine();
            T1.CiscoCommand("configure terminal");
            T1.CiscoHostname(newhost); // Seperate function specifically for changing hostname
            T1.CiscoCommand("end");
            Print("Hostname successfully changed to " + newhost, ConsoleColor.Green);
            Print("\n" + "Press any key to return to menu..", ConsoleColor.Yellow);
            Console.ReadKey();
            ChooseConf(); // Back to menu
        }

        static void GetCfg()
        {
            try
            {
                string filename;
                Print("Please enter a name for the file", ConsoleColor.Yellow);
                filename = Console.ReadLine();
                string path = String.Format(@"{0}\" + filename, Environment.CurrentDirectory + ".txt");

                StreamWriter sw = new StreamWriter(path);

                confList = T1.CiscoCommand("Show running");

                confList.ForEach(delegate (string line)
                {
                    sw.WriteLine(line);
                });

                sw.Close();
                Print("Config file saved to \n" + path, ConsoleColor.Green);
                Print("\n" + "Press any key to return to menu..", ConsoleColor.Yellow);
                Console.ReadKey();
                ChooseConf();
            }
            catch (Exception e)
            {
                Print("Exception: " + e, ConsoleColor.Red);
                Console.ReadKey();
            }
        }  
    }
}
