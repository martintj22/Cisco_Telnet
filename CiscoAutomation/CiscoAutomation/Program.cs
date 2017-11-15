using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CiscoAutomation
{
    class Program
    {
        public static Dictionary<String, String> CiscoDevice = new Dictionary<String, String>();
        public static List<string> mylist = new List<string>();
        public static List<String> Result = new List<String>();
        public static List<string> compileCommands = new List<string>();
        public static Telnet.TelnetConnection T1;

        public static string ip = "172.16.0.1";
        public static string pw = "cisco";

        static void Main(string[] args)
        {
            while (true)
            {

                Console.WriteLine("Please enter the number for the configuration mode you would like to execute:");
                Console.WriteLine("1 - line vty password cisco");
                Console.WriteLine("2 - enable secret cisco");
                Console.WriteLine("3 - set banner motd");
                Console.WriteLine("4 - ");

                for (int i = 0; i < compileCommands.Count(); i++)
                {
                    if (compileCommands[i] == "1")
                    {
                        LineVty(i);
                    }
                }

            }
            Connect();


        }

        static void Connect()
        {
            // Hvis begge ting er udfyldt prøver vi at kontakte switchen eller routeren og få adgang via telnet
            if ((!(string.IsNullOrWhiteSpace(ip))) && (!(string.IsNullOrWhiteSpace(pw))))
            {
                CiscoDevice.Add("IP", ip);
                T1 = new Telnet.TelnetConnection(CiscoDevice["IP"], 23);
                // Hvis det lykkedes at få forbindelse
                if (T1.IsConnected == true)
                {
                    string s = T1.CiscoLogin(pw);
                    // Flush ip & pw strings if connection is succesful
                    ip = "";
                    pw = "";
                    T1.CiscoEnable("cisco");
                    Console.WriteLine("Connection established!");
                }

                // Giver fejl hvis det ikke lykkedes at kontakte IPén
                else if (T1.IsConnected == false)
                {
                    Console.WriteLine("Login has failed");
                }
            }
        }
        
        public void RouterConf()
        {
            T1.CiscoCommand("configure terminal");
            T1.CiscoCommand("line vty 0 4");
            T1.CiscoCommand("password cisco");
            T1.CiscoCommand("exit");
            T1.CiscoCommand("enable secret cisco");
            T1.CiscoCommand("banner motd !authorized access only!");
            T1.CiscoCommand("Interface fa0/1");
            T1.CiscoCommand("ip address 192.168.138.230 255.255.255.0");
            T1.CiscoCommand("no shutdown");
            T1.CiscoCommand("exit");
            T1.CiscoCommand("interface fa0/1.10");
            T1.CiscoCommand("encapsulation dot1Q 5");
            T1.CiscoCommand("ip address 192.168.139.1 255.255.255.0");
            T1.CiscoCommand("ip helper-address SKOLENS NETVÆRK");
            T1.CiscoCommand("exit");
            T1.CiscoCommand("ip dhcp pool vlandhcp");
            T1.CiscoCommand("network 192.168.139.0 255.255.255.0");
            T1.CiscoCommand("default-router 192.168.139.1");
            T1.CiscoCommand("exit");
            T1.CiscoCommand("exit");
        }    
    }
}
