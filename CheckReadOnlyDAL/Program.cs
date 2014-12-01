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
            int N = projToFilesDict.Count;
            ManualResetEvent[] doneEvents = new ManualResetEvent[N];
            CodeAnalyser[] codeAnalisers = new CodeAnalyser[N];
            CheckReadOnlyDALResultMessage[] messsages = new CheckReadOnlyDALResultMessage[N];

            var myEnum2 = projToFilesDict.Keys.GetEnumerator();
            int i = 0;
            while (myEnum2.MoveNext())
            {

                string key = myEnum2.Current;

                Console.WriteLine("Scanning project <{0}> ...", key);

                List<string> srcFnList = projToFilesDict[key];

                CodeAnalyser codeAnaliser = new CodeAnalyser(key, srcFnList);
                codeAnalisers[i] = codeAnaliser;
                ManualResetEvent doneEvent = new ManualResetEvent(false);
                doneEvents[i] = doneEvent;
                CheckReadOnlyDALResultMessage message = new CheckReadOnlyDALResultMessage(doneEvent);
                messsages[i] = message;

                ThreadPool.QueueUserWorkItem(codeAnaliser.Analyze, message);

                i++;
            }

            WaitHandle.WaitAll(doneEvents);

            for (i = 0; i < N; i++)
            {
                messsages[i].print();
            }
            //---------------------------------------------------------------------------------------

            return 0;
        }
    }
}
