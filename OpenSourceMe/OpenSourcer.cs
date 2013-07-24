using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HeaderBatcher;

namespace OpenSourceMe
{
    class OpenSourcer
    {
        private static readonly String[]   TRUE_VALUES         = { "true", "yes", "1" };
        private static readonly String[]   FALSE_VALUES        = { "false", "no", "0", null };

        private const String     CSV_VALUE_COPY      = "Copy";
        private const String     CSV_VALUE_HEADER    = "Header";
        private const String     CSV_VALUE_SRC_PATH  = "Path";

        private const String     DEFAULT_HEADERBATCHER_ROOTPATH  = "D:/KLab/Tests/HeaderBatcherFiles/";
        private const String     DEFAULT_NEW_HEADER_PATH         = DEFAULT_HEADERBATCHER_ROOTPATH + "newHeader.txt";
        private static readonly String[]   DEFAULT_OLD_HEADERS_PATHS       = { 
                                                                              DEFAULT_NEW_HEADER_PATH
                                                                          };
        private static readonly String[]   DEFAULT_HEADERS_TO_IGNORE       = null;
        private const String     DEFAULT_WHITELIST_PATH          = DEFAULT_HEADERBATCHER_ROOTPATH + "whiteList.txt";
        private const String     DEFAULT_BLACKLIST_PATH          = DEFAULT_HEADERBATCHER_ROOTPATH + "blackList.txt";

        
        private CSVFileManager  m_CSVFileManager;
        private String          m_rootPathFrom;
        private String          m_rootPathTo;
        private int             m_copyIndex;
        private int             m_headerIndex;
        private int             m_srcPathIndex;
        private Dictionary<String, HeaderBatcher.FileBatcher> m_headerBatchersDic;


        
        public OpenSourcer(String _CSVFilePath, String _rootPathFrom, String _rootPathTo) {
            m_CSVFileManager    = new CSVFileManager(_CSVFilePath);
            m_headerBatchersDic = new Dictionary<string,FileBatcher>();
            m_headerBatchersDic.Add(DEFAULT_NEW_HEADER_PATH, 
                                    new HeaderBatcher.FileBatcher( DEFAULT_NEW_HEADER_PATH, DEFAULT_OLD_HEADERS_PATHS, DEFAULT_HEADERS_TO_IGNORE, 
                                                                   DEFAULT_BLACKLIST_PATH, DEFAULT_WHITELIST_PATH)
                                   );
            m_rootPathFrom = _rootPathFrom;
            m_rootPathTo   = _rootPathTo;

            // Indexes setting
            String[] header = m_CSVFileManager.GetHeader();
            m_copyIndex     = Array.IndexOf(header, CSV_VALUE_COPY);
            m_headerIndex   = Array.IndexOf(header, CSV_VALUE_HEADER);
            m_srcPathIndex  = Array.IndexOf(header, CSV_VALUE_SRC_PATH);

            // If missing at least 1 item in the header of the CSV file,
            // throws an Exception.
            if(m_copyIndex == -1 || m_headerIndex == -1 || m_srcPathIndex == -1) {
                String errorMessage = "";
                if(m_copyIndex == -1) {
                    errorMessage += "\"" + CSV_VALUE_COPY + "\" is missing in the CSV header.\n";
                } 
                if(m_headerIndex == -1) {
                    errorMessage += "\"" + CSV_VALUE_HEADER + "\" is missing in the CSV header.\n";
                }
                if(m_srcPathIndex == -1) {
                    errorMessage += "\"" + CSV_VALUE_SRC_PATH + "\" is missing in the CSV header.\n";
                }
                throw new Exception(errorMessage);
            }
        }

        /// <summary>
        /// Processes the next item.
        /// </summary>
        /// <returns>true if an item has been processed. Flase if no more items.</returns>
        public bool ProcessNext() {
            // Is there a next line ?
            String[] line = m_CSVFileManager.ReadLine();
            if(line == null) {
                return false;
            }

            if(CopyFile(line)) {
                Console.WriteLine(line[m_srcPathIndex] + " has been copied to " + GetDstPath(line));
            }

            if(ApplyHeader(line)) {
                Console.WriteLine(GetDstPath(line) + " : header applied.");
            }

            return true;
        }

        private static bool GetBoolValueOfString(string _str) {
            if(TRUE_VALUES.Contains(_str)) {
                return true;
            } 
            if(FALSE_VALUES.Contains(_str)) {
                return false;
            }
            String errorMessage = "Invalid value : \"" + _str + "\" given.\n";
            errorMessage += "For a True value, use one of the following word : ";
            foreach(string s in TRUE_VALUES) {
                errorMessage += s + ", ";
            }
            errorMessage.Remove(errorMessage.Length - 2);
            errorMessage += "For a False value, use one of the following word : ";
            foreach(string s in FALSE_VALUES) {
                errorMessage += s + ", ";
            }
            errorMessage.Remove(errorMessage.Length - 2);
            throw new ArgumentException(errorMessage);
        }

        private String GetHeaderToApply(String[] _line) {
            String headerValue   = _line[m_headerIndex];
            String headerToApply = null;

            if(!hasToCopyFile(_line)) {
                return null;
            }
            // The Header Value can also be a path to a specific header file.
            try {
                if(GetBoolValueOfString(headerValue)) {
                    headerToApply = DEFAULT_NEW_HEADER_PATH;
                }
            } catch(Exception e) {
                if(File.Exists(headerValue)) {
                    headerToApply = headerValue;
                } else {
                    throw new Exception(e.Message + "If you wanted to specify a Path to a Header, the given path is incorrect.\n");
                }
            }

            return headerToApply;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_line"></param>
        /// <returns>Absolute Dst Path</returns>
        private String GetDstPath(String[] _line) {
            String CopyValue    = _line[m_copyIndex];
            String dstPath      = null;

            // The Copy Value can also be a path to a specific location to copy the file.
            try {
                if(GetBoolValueOfString(CopyValue)) {
                    dstPath = _line[m_srcPathIndex];
                } else {
                    throw new Exception("GetDstPath shall not be called if the file must NOT be copied.");
                }
            } catch(ArgumentException) {
                // We assume the value is a path.
                dstPath = CopyValue;
            }

            if(dstPath.StartsWith("./")) {
                dstPath = dstPath.Remove(0,2);
            }

            return m_rootPathTo + dstPath;
        }

        private String GetSrcPath(String[] _line) {
            if(_line[m_srcPathIndex].StartsWith("./")) {
                return m_rootPathFrom + _line[m_srcPathIndex].Remove(0, 2);
            }

            return m_rootPathFrom + _line[m_srcPathIndex];
        }

        /// <summary>
        /// The destination file must have been created previously.
        /// </summary>
        /// <param name="_path"></param>
        /// <param name="_headerToApply"></param>
        /// <param name="_header"></param>
        /// <returns>True if a header had been added to the file. Flase otherwise.</returns>
        private bool ApplyHeader(String[] _line) {
            String header = GetHeaderToApply(_line);

            if(header == null) {
                return false;
            }
            
            // Is there already a HeaderBatcher for this specific header ?
            if(!m_headerBatchersDic.ContainsKey(header)) {
                m_headerBatchersDic.Add(header, 
                                        new FileBatcher(header, DEFAULT_OLD_HEADERS_PATHS, DEFAULT_HEADERS_TO_IGNORE,
                                                        DEFAULT_BLACKLIST_PATH, DEFAULT_WHITELIST_PATH)
                                        );
            }

            return m_headerBatchersDic[header].BatchOne(GetDstPath(_line));
        }

        private bool CopyFile(String[] _line) {
            if(!hasToCopyFile(_line)) {
                return false;
            }
            
            String src = GetSrcPath(_line);
            String dst = GetDstPath(_line);

            if(!File.Exists(dst)) {
                Directory.CreateDirectory(Path.GetDirectoryName(dst));
            }
            File.Copy(src, dst, true);

            return true;
        }

        private bool hasToCopyFile(String[] _line) {
            bool res = true;
            try {
                res = GetBoolValueOfString(_line[m_copyIndex]);
            } catch(ArgumentException) {
                // We assume it is a FilePath.
                // means res = true;
            }
            return res;
        }

    }
}
