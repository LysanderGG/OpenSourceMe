using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HeaderBatcher;

namespace OpenSourceMe
{
    /// <summary>
    /// Main class of the program.
    /// It is in charge of coopying the project file from a Source Folder
    /// to a Destination Folder and had or not a header to these files.
    /// The files to copy, the destination and the order to add or not a header
    /// is notified in a CSV file.
    /// </summary>
    class OpenSourcer
    {
        //
        // Constantes
        //

        // True/False Text Values
        private static readonly String[]   TRUE_VALUES         = { "true", "yes", "1" };
        private static readonly String[]   FALSE_VALUES        = { "false", "no", "0", "" };

        // CSV Header Values
        private const String     CSV_VALUE_COPY      = "Copy";
        private const String     CSV_VALUE_HEADER    = "Header";
        private const String     CSV_VALUE_SRC_PATH  = "Path";

        // Default Paths for HeaderBatcher
        private const String     DEFAULT_NEW_HEADER_PATH         = "newHeader.txt";
        private static readonly String[]   DEFAULT_OLD_HEADERS_PATHS    = { 
                                                                              DEFAULT_NEW_HEADER_PATH
                                                                          };
        private static readonly String[]   DEFAULT_HEADERS_TO_IGNORE       = null;
        private const String     DEFAULT_BLACKLIST_PATH          = "blackList.txt";
        private const String     DEFAULT_WHITELIST_PATH          = "whiteList.txt";

        //
        // Members
        //
        private CSVFileManager  m_CSVFileManager;
        private String          m_rootPathFrom;
        private String          m_rootPathTo;
        private String          m_rootPathHBFiles;
        private int             m_copyIndex;
        private int             m_headerIndex;
        private int             m_srcPathIndex;
        private Dictionary<String, HeaderBatcher.FileBatcher> m_headerBatchersDic;

        /// <summary>
        /// OpenSourcer Constructor
        /// </summary>
        /// <param name="_CSVFilePath">Path to the CSV File that contains the Project information.</param>
        /// <param name="_rootPathFrom">Root Path of the Source Project.</param>
        /// <param name="_rootPathTo">Root Path of the Destination Project.</param>
        /// <param name="_rootPathHBFiles">Root Path of the HeaderBatcher configuration files.</param>
        public OpenSourcer(String _CSVFilePath, String _rootPathFrom, String _rootPathTo, String _rootPathHBFiles) {
            m_CSVFileManager    = new CSVFileManager(_CSVFilePath);
            m_headerBatchersDic = new Dictionary<string,FileBatcher>();

            m_rootPathFrom      = _rootPathFrom;
            m_rootPathTo        = _rootPathTo;
            m_rootPathHBFiles   = _rootPathHBFiles;

            AddToHBDictionary(DEFAULT_NEW_HEADER_PATH, DEFAULT_OLD_HEADERS_PATHS, DEFAULT_HEADERS_TO_IGNORE, DEFAULT_BLACKLIST_PATH, DEFAULT_WHITELIST_PATH);

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
                errorMessage += "The CSV header (first line) must contain the following values : \"" + CSV_VALUE_COPY + "\", \"" + CSV_VALUE_HEADER + "\", \"" + CSV_VALUE_SRC_PATH + "\" in any order.\n";
                throw new Exception(errorMessage);
            }
        }

        /// <summary>
        /// Processes the next item.
        /// </summary>
        /// <returns>True if an item has been processed. False if no more items.</returns>
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

        /// <summary>
        /// Translate a String value into boolean.
        /// Text values corresponding to True/False are defined by TRUE_VALUES and FALSE_VALUES.
        /// </summary>
        /// <param name="_str">String to convert.</param>
        /// <returns>The associated boolean value.</returns>
        private static bool GetBoolValueOfString(string _str) {
            if(_str == null || FALSE_VALUES.Contains(_str.ToLower())) {
                return false;
            }
            if(TRUE_VALUES.Contains(_str.ToLower())) {
                return true;
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

        /// <summary>
        /// Returns the header relative path to apply on a file.
        /// </summary>
        /// <param name="_line">CSV Line corresponding to the file.</param>
        /// <returns>The header relative path.</returns>
        private String GetHeaderToApply(String[] _line) {
            String headerValue   = _line[m_headerIndex];
            String headerToApply = null;

            if(!HasToCopyFile(_line)) {
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
        /// Returns the Absolute Destination Path.
        /// </summary>
        /// <param name="_line">CSV Line corresponding to the file.</param>
        /// <returns>Absolute Destination Path</returns>
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

        /// <summary>
        /// Returns the Absolute Source Path.
        /// </summary>
        /// <param name="_line">CSV Line corresponding to the file.</param>
        /// <returns>Absolute Source Path</returns>
        private String GetSrcPath(String[] _line) {
            String srcPath;
            if(_line[m_srcPathIndex].StartsWith("./")) {
                srcPath = _line[m_srcPathIndex].Remove(0, 2);
            } else {
                srcPath = _line[m_srcPathIndex];
            }

            if(!File.Exists(m_rootPathFrom + srcPath)) {
                throw new Exception("The Path \""+ srcPath + "\" does not refer to an existing file.\nIf the file does not exist anymore or if it had been moved, please update the CSV File.\n");
            }

            return m_rootPathFrom + srcPath;
        }

        /// <summary>
        /// Applies the required header on the destination file.
        /// The destination file must have been created previously.
        /// </summary>
        /// <param name="_line">CSV Line corresponding to the file.</param>
        /// <returns>True if a header had been added to the file. Flase otherwise.</returns>
        private bool ApplyHeader(String[] _line) {
            String header = GetHeaderToApply(_line);

            if(header == null) {
                return false;
            }
            
            // Is there already a HeaderBatcher for this specific header ?
            if(!m_headerBatchersDic.ContainsKey(header)) {
                AddToHBDictionary(header, DEFAULT_OLD_HEADERS_PATHS, DEFAULT_HEADERS_TO_IGNORE,
                                  DEFAULT_BLACKLIST_PATH, DEFAULT_WHITELIST_PATH);
            }

            return m_headerBatchersDic[header].BatchOne(GetDstPath(_line));
        }

        /// <summary>
        /// Copies the file from the source folder to the destination one,
        /// if the CSV file requires it to.
        /// </summary>
        /// <param name="_line">CSV Line corresponding to the file.</param>
        /// <returns>True if the file had been copied. False otherwise.</returns>
        private bool CopyFile(String[] _line) {
            if(!HasToCopyFile(_line)) {
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

        /// <summary>
        /// Tells if a file has to be copied or not.
        /// </summary>
        /// <param name="_line">CSV Line corresponding to the file.</param>
        /// <returns>True if the file has to be copied. False otherwise.</returns>
        private bool HasToCopyFile(String[] _line) {
            bool res = true;
            try {
                res = GetBoolValueOfString(_line[m_copyIndex]);
            } catch(ArgumentException) {
                // We assume it is a FilePath.
                // means res = true;
            }
            return res;
        }

        /// <summary>
        /// Converts relative paths to absolute ones.
        /// </summary>
        /// <param name="_newHeaderPath"></param>
        /// <param name="_oldHeadersPaths"></param>
        /// <param name="_headersToIgnorePaths"></param>
        /// <param name="_blackListPath"></param>
        /// <param name="_whiteListPath"></param>
        private void AddToHBDictionary(String _newHeaderPath, String[] _oldHeadersPaths, String[] _headersToIgnorePaths, String _blackListPath, String _whiteListPath) {
            String[] oldHeaders = null;
            String[] headersToIgnore = null;

            if(_oldHeadersPaths != null) {
                oldHeaders = new String[_oldHeadersPaths.Length];
                for(int i = 0; i < _oldHeadersPaths.Length; ++i) {
                    if(_oldHeadersPaths[i] != null) {
                        oldHeaders[i] = m_rootPathHBFiles + _oldHeadersPaths[i];
                    }
                }
            }

            if(_headersToIgnorePaths != null) {
                headersToIgnore = new String[_headersToIgnorePaths.Length];
                for(int i = 0; i < _headersToIgnorePaths.Length; ++i) {
                    if(_headersToIgnorePaths[i] != null) {
                        headersToIgnore[i] = m_rootPathHBFiles + _headersToIgnorePaths[i];
                    }
                }
            }

            m_headerBatchersDic.Add(_newHeaderPath, 
                                    new HeaderBatcher.FileBatcher(  m_rootPathHBFiles + _newHeaderPath, 
                                                                    oldHeaders, 
                                                                    headersToIgnore, 
                                                                    m_rootPathHBFiles + _blackListPath, 
                                                                    m_rootPathHBFiles + _whiteListPath)
                                   );
        }
    }
}
