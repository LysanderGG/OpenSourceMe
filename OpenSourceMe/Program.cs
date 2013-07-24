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
        static void Main(string[] args) {
            String CSVFilePath  = "D:/KLab/Tests/FilesList.csv";
            String rootFilePath = "D:/KLab/Tests/playgroundTestFrom/Engine/";
            String rootPathTo   = "D:/KLab/Tests/playgroundTestTo/Engine/";
            OpenSourcer os = new OpenSourcer(CSVFilePath, rootFilePath, rootPathTo);
            
            while(os.ProcessNext());
        }
    }
}
