using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace runtests
{
    class Program
    {
        enum TestResult
        {
            Unknown,
            Succeeded,
            Failed,
            Crashed,
        }

        static void Main(string[] args)
        {
            // parse args
            string cwd = Environment.CurrentDirectory;
            var testdirs = new List<string>();
            string phpexepath = Path.Combine(cwd, "php.exe");

            foreach (var arg in args)
            {
                if (arg.EndsWith("php.exe", StringComparison.OrdinalIgnoreCase))
                {
                    phpexepath = arg;
                }
                else
                {
                    testdirs.Add(Path.GetFullPath(Path.Combine(cwd, arg)));
                }
            }

            // run tests lazily
            var tests = testdirs
                .SelectMany(dir => ExpandTestDir(dir));

            // output results
            foreach (var test in tests)
            {
                Console.Write($"{test} ... ");
                Console.WriteLine(TestCore(test, phpexepath));
            }
        }

        static IEnumerable<string> ExpandTestDir(string testdir)
        {
            return System.IO.Directory.EnumerateFiles(testdir, "*.php", SearchOption.AllDirectories);
        }

        static TestResult TestCore(string testpath, string phpexepath)
        {
            var testfname = Path.GetFileName(testpath);
            var outputexe = Path.Combine(testfname + ".exe");   // current dir so it finds pchpcor.dll etc.

            File.Delete(outputexe);

            // php.exe 'testpath' >> OUTPUT1
            var phpoutput = RunProcess(phpexepath, testpath);

            // peach.exe /target:exe '/out:testpath.exe' 'testpath'
            var compileroutput = RunProcess("peach.exe", $"/target:exe \"/out:{outputexe}\" \"{testpath}\"");

            // TODO: check compiler crashed

            // testpath.exe >> OUTPUT2
            var output = RunProcess(outputexe, string.Empty);

            File.Delete(outputexe);

            if (output == phpoutput)
            {
                return TestResult.Succeeded;
            }

            // TODO: log details
            Console.WriteLine(output);
            Console.WriteLine(phpoutput);
            //
            return TestResult.Failed;
        }

        static string RunProcess(string exe, string args)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo(exe, args)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false,
            };

            //
            process.Start();

            // To avoid deadlocks, always read the output stream first and then wait.
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            //
            if (!string.IsNullOrEmpty(error))
                return error;

            //
            return output;
        }
    }
}
