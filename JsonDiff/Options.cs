using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace JsonDiff
{
    internal class Options
    {
        [Value(0, MetaName = "left", HelpText = "Right JSON file name.", Required = true)]
        public string LeftFile { get; set; }

        [Value(1, MetaName = "right", HelpText = "Left JSON file name.", Required = true)]
        public string RightFile { get; set; }

        [Value(2, MetaName = "output", HelpText = "Output file name. Empty output to console.")]
        public string OutputFile { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }
    }
}
