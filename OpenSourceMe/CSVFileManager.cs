using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace OpenSourceMe
{
    class CSVFileManager
    {
        private static char[]   s_separators = { ',' , ';' };

        private String[][]  m_values;
        private String[]    m_header;
        private int         m_currentLine = 0;


        public CSVFileManager(String _path) {
            String[] lines = File.ReadAllLines(_path, Encoding.UTF8);
            m_values = new String[lines.Length - 1][];
            m_header = lines[0].Split(s_separators);

            for(int i = 1; i < lines.Length; ++i) {
                m_values[i-1] = lines[i].Split(s_separators);
            }
        }

        public String[] GetHeader() {
            return m_header;
        }

        public String[] ReadLine() {
            if(m_currentLine >= m_values.Length) {
                return null;
            }

            return m_values[m_currentLine++];
        }

    }
}
