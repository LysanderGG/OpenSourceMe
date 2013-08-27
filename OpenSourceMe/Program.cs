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
        static String s_helpText = "OpenSourceMe [CSV File Path] [Root Path From] [Root Path To] [Root Path HeaderBatcher Files] [--force]";

        static void Main(string[] args) {
            if(args.Length < 4 || args.Length > 5) {
                Console.WriteLine("Incorrect Command Line.");
                Console.WriteLine(s_helpText);
                return;
            }

            try {
                String CSVFilePath      = args[0];
                String rootPathFrom     = args[1];
                String rootPathTo       = args[2];
                String rootPathHBFiles  = args[3];

                // Optionnal args
                bool   force            = false;

                if(args.Length > 4) {
                    foreach(string arg in args) {
                        switch(arg) {
                            case "--force":
                                force = true; break;
                            default: 
                                break;
                        }
                    }
                }
                
                OpenSourcer os = new OpenSourcer(CSVFilePath, rootPathFrom, rootPathTo, rootPathHBFiles);
            
                bool res        = true;
                int  nbErrors   = 0;
                while(res) {
                        try {
                            res = os.ProcessNext();
                        }
                        catch(OSMException e) {
                            nbErrors++;
                            Console.WriteLine();
                            Console.WriteLine("# Error : " + e.Message);
                            Console.WriteLine();
                            if(force) {
                                res |= false;
                            } else {
                                res = false;
                            }
                        }
                }

                Console.WriteLine();
                Console.WriteLine("Script finished.");
                Console.WriteLine("Errors : " + nbErrors);
            }
            catch(OSMCriticalException e) {
                Console.WriteLine();
                Console.WriteLine("# Critical Error : " + e.Message);
                Console.WriteLine();
            }
            catch(Exception e) {
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
