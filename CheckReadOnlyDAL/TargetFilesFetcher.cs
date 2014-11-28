using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;

namespace CheckReadOnlyDAL
{
    public class TargetFilesFetcher
    {
        private Regex _rx;
        private string _currentSrcFileName;
        private List<string> _currentSrcLines;
        private List<string>.Enumerator _currentSrcLinesEnumerator;
        private int _totalParsedChars;
        public int CurrentSourceLineNumber{get; private set;}

        private Regex Rx
        {
            get
            {
                if(_rx == null)
                {
                    string GetReadOnlyInstanceMethod = ConfigurationManager.AppSettings["GetReadOnlyInstanceMethod"];
                    string pattern = string.Format(@"\b\w+\.{0}\(\)\.(\w+)\(.*\)", GetReadOnlyInstanceMethod);
                    //string pattern = string.Format(@"\b{0}\b", GetReadOnlyInstanceMethod);
                    _rx = new Regex(pattern);
                }
                return _rx;
            }
        }

        public bool MatchSrc(string srcLine)
        {
            /*Match match = Rx.Match(srcLine);
            return match.Length > 0;*/

            return Rx.IsMatch(srcLine);
        }

        public int getNextMatchPosition(string srcFileName = null)
        {
            if (!string.IsNullOrEmpty(srcFileName))
            {
                _currentSrcFileName = srcFileName;
                _currentSrcLines = new List<string>(File.ReadLines(_currentSrcFileName));
                _currentSrcLinesEnumerator = _currentSrcLines.GetEnumerator();
                _totalParsedChars = 0;
                CurrentSourceLineNumber = 0;
            }

            if (_currentSrcLines == null)
                throw new Exception("Invalid first call");

            int result = -1;

            while (_currentSrcLinesEnumerator.MoveNext())
            {
                CurrentSourceLineNumber++;
                string srcLine = _currentSrcLinesEnumerator.Current;
                Match match = Rx.Match(srcLine);

                if (match.Success)
                {
                    result = _totalParsedChars + match.Groups[1].Captures[0].Index;
                    _totalParsedChars += srcLine.Length + 2;
                    break;
                }

                _totalParsedChars += srcLine.Length + 2;
            }

            return result;
        }

        public IEnumerable<string> getFileNames(string curDir)
        {
            string[] files = Directory.GetFiles(curDir, "*.cs", SearchOption.AllDirectories);

            List<string> result = new List<string>();

            foreach(string fileName in files)
            {
                IEnumerable<string> lines = File.ReadLines(fileName);//.Where(line => MatchSrc(line));

                /*if (lines.Count(line => MatchSrc(line)) > 0)
                    result.Add(fileName);*/

                foreach(var line in lines)
                {
                    if(MatchSrc(line))
                    {
                        result.Add(fileName);
                        break;
                    }
                }
            }

            return result;
        }

        public string getProjectForSrcFile(string srcFile)
        {
            string path = Path.GetDirectoryName(srcFile);
            string proFile = Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).SingleOrDefault();

            if (string.IsNullOrEmpty(proFile))
                throw new Exception("Project file not found");

            return proFile;
        }

        public Dictionary<string, List<string>> getProjToSrcFilesDict(string curDir)
        {
            IEnumerable<string> srcFilesList = getFileNames(curDir);

            Dictionary<string, List<string>> result = new Dictionary<string,List<string>>();

            foreach(var fileName in srcFilesList)
            {
                string projFileName = getProjectForSrcFile(fileName);

                if (result.ContainsKey(projFileName))
                    result[projFileName].Add(fileName);
                else
                    result.Add(projFileName, new List<string>{fileName});
            }

            return result;
        }
    }
}
