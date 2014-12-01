using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.FindSymbols;

namespace CheckReadOnlyDAL
{
    class Program
    {
        [MTAThread]
        static int Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("Please, enter the solution's path");
                Console.WriteLine("Usage: CheckReadOnlyDAL <SolutionPath>");
                return 1;
            }

            Console.WriteLine("Retrieving list of projects to scan ...");

            TargetFilesFetcher getTargetFiles = new TargetFilesFetcher();

            Dictionary<string, List<string>> projToFilesDict = getTargetFiles.getProjToSrcFilesDict(args[0]);

            getTargetFiles = null;

            Console.WriteLine("{0} projects found", projToFilesDict.Count);

            //---------------------------------------------------------------------------------------
            int threadCount = 0;
            int N = projToFilesDict.Count;
            CountdownEvent countDownEvent = new CountdownEvent(N);

            var myEnum2 = projToFilesDict.Keys.GetEnumerator();
            if (!myEnum2.MoveNext())
                return 0;

            for(int i=0; i<N; )
            {
                
                string key = myEnum2.Current;

                Console.WriteLine("Scanning project <{0}> ...", key);

                List<string> srcFnList = projToFilesDict[key];

                CodeAnalyser codeAnaliser = new CodeAnalyser(key, srcFnList);

                if (ThreadPool.QueueUserWorkItem((Object threadContext) =>
                {
                    CodeAnalyser localCodeAnalyer = (CodeAnalyser)threadContext;
                    localCodeAnalyer.Analyze();
                    Interlocked.Decrement(ref threadCount);
                    countDownEvent.Signal();
                }, codeAnaliser))
                {
                    i++;
                    myEnum2.MoveNext();
                    Interlocked.Increment(ref threadCount);
                }
            }

            countDownEvent.Wait();
            //---------------------------------------------------------------------------------------

            return 0;
        }
    }
}
