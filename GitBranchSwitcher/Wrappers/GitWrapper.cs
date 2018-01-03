using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitBranchSwitcher.Wrappers
{
    internal class GitWrapper
    {
        private string folder;
        private string executable;
        private string _outputString { get; set; } = string.Empty;

        /// <summary>
        /// Creates a new GitWrapper instance.
        /// </summary>
        /// <param name="folder">The folder for the local repository.</param>
        public GitWrapper(string folder)
        {
            this.folder = folder;
            this.executable = this.FindExecutable();
        }

        /// <summary>
        /// Executes Git with the given command
        /// </summary>
        /// <param name="gitCommand">The command to execute, e.g. "init"</param>
        /// <returns>Git's command line output</returns>
        public string Execute(string gitCommand)
        {
            try
            {
                _outputString = string.Empty;
                var info = new ProcessStartInfo();
                info.FileName = this.executable;
                info.Arguments = gitCommand;
                info.CreateNoWindow = false;//true
                info.WorkingDirectory = this.folder;
                info.UseShellExecute = false;//false
                info.RedirectStandardError = true;//true
                info.RedirectStandardOutput = true;//true
                var git = new Process();
                git.StartInfo = info;
                git.OutputDataReceived += proc_OutputDataReceived;

                git.Start();

                string error = git.StandardError.ReadToEnd();

                git.WaitForExit();

                git.Close();
                string output = _outputString;

                return !string.IsNullOrEmpty(error) ? error : output;

            }
            catch (Exception ex)
            {

                throw;
            }
        }
        void proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            _outputString = _outputString + Environment.NewLine + e.Data;
        }
        /// <summary>
        /// Finds out the path to git.exe
        /// </summary>
        /// <returns>The complete path to git.exe</returns>
        private string FindExecutable()
        {
            string pathVariable = Environment.GetEnvironmentVariable("path");
            var invalidChars = Path.GetInvalidPathChars();

            foreach (var value in pathVariable.Split(';'))
            {
                if (value.IndexOfAny(invalidChars) == -1)
                {
                    if (File.Exists(Path.Combine(value, "git.exe")))
                    {
                        return Path.Combine(value, "git.exe");
                    }

                    if (File.Exists(Path.Combine(value, "git.cmd")))
                    {
                        // Calling git.cmd with Process.Start doesn't work, so we always need to use git.exe.
                        // git.cmd is in a folder called "cmd", and git.exe is in a folder called "bin", which is on the same level as "cmd".
                        var exePath = Path.Combine(Directory.GetParent(value).ToString(), "bin");

                        if (File.Exists(Path.Combine(exePath, "git.exe")))
                        {
                            return Path.Combine(exePath, "git.exe");
                        }
                    }
                }
            }

            throw new FileNotFoundException("No Git executable found!");
        }





    }
}
