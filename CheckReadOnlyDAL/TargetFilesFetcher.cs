using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
        private string _pattern;
        private int threadCount = 0;
        private CountdownEvent _countDownEvent;
        private ConcurrentQueue<string> _targetFiles;

        class ContainsReadOnlyCallChecker
        {
            private TargetFilesFetcher parent;
            public bool Result { get; set; }
            public string FileName { get; private set; }
            private Regex localRx;

            public ContainsReadOnlyCallChecker(TargetFilesFetcher parent)
            { 
                this.parent = parent;

                localRx = new Regex(parent._pattern);
            }

            public void containsReadOnlyCall(object threadContext)
            {
                FileName = (string)threadContext;
                IEnumerable<string> lines = File.ReadLines(FileName);

                foreach (var line in lines)
                {
                    if (localRx.IsMatch(line))
                    {
                        Result = true;
                        parent._countDownEvent.Signal();
                        Interlocked.Decrement(ref parent.threadCount);
                        return;
                    }
                }

                Result = false;
                parent._countDownEvent.Signal();
                Interlocked.Decrement(ref parent.threadCount);
            }
        }

        private Regex Rx
        {
            get
            {
                if(_rx == null)
                {
                    _rx = new Regex(_pattern);
                }
                return _rx;
            }
        }

        public TargetFilesFetcher()
        { 
            string GetReadOnlyInstanceMethod = ConfigurationManager.AppSettings["GetReadOnlyInstanceMethod"];
            _pattern = string.Format(@"\b\w+\.{0}\(\)\.(\w+)\(.*\)", GetReadOnlyInstanceMethod);
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
            int N = files.Length;

            Console.WriteLine("{0} source code files in target folder", N);

            //ContainsReadOnlyCallChecker[] checkers = new ContainsReadOnlyCallChecker[N];

            _countDownEvent = new CountdownEvent(N);
            _targetFiles = new ConcurrentQueue<string>();

            for (int i = 0, n = N; i < n; )
            {
                if(threadCount < 64)
                { 
                    //ContainsReadOnlyCallChecker checker = new ContainsReadOnlyCallChecker(this);

                    //checkers[i] = checker;

                    if (ThreadPool.QueueUserWorkItem( (Object threadContext) => {
                        ContainsReadOnlyCallChecker checker = new ContainsReadOnlyCallChecker(this);
                        checker.containsReadOnlyCall(threadContext);
                        if (checker.Result)
                            _targetFiles.Enqueue((string)threadContext);
                    }, files[i]))
                    {
                        Interlocked.Increment(ref threadCount);
                        i++;
                    }
                }
            }

            _countDownEvent.Wait();

            /*List<string> result = new List<string>();

            for (int i = 0, n = checkers.Length; i < n; i++ )
            {
                if(checkers[i].Result)
                    result.Add(checkers[i].FileName);
            }*/

            Console.WriteLine("{0} source code files to analyze", _targetFiles.Count);

            return _targetFiles;
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
