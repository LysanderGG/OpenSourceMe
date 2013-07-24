using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeaderBatcher;

namespace OpenSourceMe
{
    class Program
    {
        static String s_helpText = "OpenSourceMe [CSV File Path] [Root Path From] [Root Path To] [Root Path HeaderBatcher Files]";

        static void Main(string[] args) {
            if(args.Length != 4) {
                Console.WriteLine("Incorrect Command Line.");
                Console.WriteLine(s_helpText);
                return;
            }

            try {
                String CSVFilePath      = args[0];
                String rootPathFrom     = args[1];
                String rootPathTo       = args[2];
                String rootPathHBFiles  = args[3];
                OpenSourcer os = new OpenSourcer(CSVFilePath, rootPathFrom, rootPathTo, rootPathHBFiles);
            
                while(os.ProcessNext());
            } catch(Exception e) {
                Console.WriteLine("## Exception ##");
                Console.WriteLine("#");
                Console.WriteLine("# Message     : " + e.Message);
                Console.WriteLine("#");
                Console.WriteLine("# Stack Trace : " + e.StackTrace);
                Console.WriteLine("#");
                Console.WriteLine("## End Of Exception ##");
            }
        }
    }
}
