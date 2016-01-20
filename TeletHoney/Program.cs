using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TeletHoney.src;

namespace TeletHoney
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server("0.0.0.0", "Welcome to the National Security Agency Remote Terminal\r\n");
            Process.GetCurrentProcess().WaitForExit();
        }
    }
}
