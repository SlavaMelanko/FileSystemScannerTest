using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ashampoo
{
    public class Log
    {
        public static void Info(string message)
        {
            Console.WriteLine(message);
        }

        public static void Info(string message, params object[] args)
        {
            Console.WriteLine(message, args);
        }
    }
}
