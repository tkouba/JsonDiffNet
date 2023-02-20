using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonDiff
{
    internal class Program
    {
        // ! https://stackoverflow.com/questions/24876082/find-and-return-json-differences-using-newtonsoft-in-c

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<Options>(args).MapResult(
                (Options opts) => RunOptions(opts),
                errs => HandleParseError(errs));
        }
        static int RunOptions(Options opts)
        {
            if (opts.Verbose)
                Console.WriteLine("Comparing '{0}' and '{1}'.", opts.LeftFile, opts.RightFile);
            string leftString = File.ReadAllText(opts.LeftFile);
            JToken left = leftString.TrimStart().StartsWith('[') ? JArray.Parse(leftString) : JObject.Parse(leftString);
            string rightString = File.ReadAllText(opts.RightFile);
            JToken right = rightString.TrimStart().StartsWith('[') ? JArray.Parse(rightString) : JObject.Parse(rightString);
            JToken diff = FindDiff(left, right);
            if (String.IsNullOrEmpty(opts.OutputFile))
            {
                if (opts.NoColor)
                {
                    Console.WriteLine(diff.ToString());
                }
                else
                {
                    using (StringWriter sw = new StringWriter())
                    using (AnsiColoredDiffJsonWriter writer = new AnsiColoredDiffJsonWriter(sw))
                    {
                        writer.Formatting = Formatting.Indented;
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Serialize(writer, diff);
                        Console.WriteLine(sw.ToString());
                    }
                }
            }
            else
            {
                if (opts.Verbose)
                    Console.WriteLine("Differences are saved to '{0}'", opts.OutputFile);
                File.WriteAllText(opts.OutputFile, diff.ToString());
            }
            if (diff.HasValues == false)
            {
                if (opts.Verbose)
                    Console.WriteLine("Files are same.");
                return 0;
            }
            return 1;
        }
        static int HandleParseError(IEnumerable<Error> errs)
        {
            return 255;
        }


        /// <summary>
        /// Compare two JSON and create diff object
        /// </summary>
        /// <param name="leftJson">Left JSON to compare</param>
        /// <param name="rightJson">Right JSON to compare</param>
        /// <returns>Diff object with the result.</returns>
        /// <remarks>
        /// Original version https://stackoverflow.com/a/65222961/1498252 by Rohith Daruri
        /// based on https://stackoverflow.com/a/53654737/1498252 by Dzmitry Paliakou
        /// </remarks>
        public static JToken FindDiff(JToken leftJson, JToken rightJson)
        {
            var difference = new JObject();
            if (JToken.DeepEquals(leftJson, rightJson)) return difference;

            switch (leftJson.Type)
            {
                case JTokenType.Object:
                    {
                        var LeftJSON = leftJson as JObject;
                        var RightJSON = rightJson as JObject;
                        var RemovedTags = LeftJSON.Properties().Select(c => c.Name).Except(RightJSON.Properties().Select(c => c.Name));
                        var AddedTags = RightJSON.Properties().Select(c => c.Name).Except(LeftJSON.Properties().Select(c => c.Name));
                        var UnchangedTags = LeftJSON.Properties().Where(c => JToken.DeepEquals(c.Value, RightJSON[c.Name])).Select(c => c.Name);
                        foreach (var tag in RemovedTags)
                        {
                            difference[tag] = new JObject
                            {
                                ["-"] = LeftJSON[tag]
                            };
                        }
                        foreach (var tag in AddedTags)
                        {
                            difference[tag] = new JObject
                            {
                                ["+"] = RightJSON[tag]
                            };
                        }
                        var ModifiedTags = LeftJSON.Properties().Select(c => c.Name).Except(AddedTags).Except(UnchangedTags).Except(RemovedTags);
                        foreach (var tag in ModifiedTags)
                        {
                            var foundDifference = FindDiff(LeftJSON[tag], RightJSON[tag]);
                            if (foundDifference.HasValues)
                            {
                                difference[tag] = foundDifference;
                            }
                        }
                    }
                    break;
                case JTokenType.Array:
                    {
                        var LeftArray = leftJson as JArray;
                        var RightArray = rightJson as JArray;

                        if (LeftArray != null && RightArray != null)
                        {
                            if (LeftArray.Count() == RightArray.Count())
                            {
                                for (int index = 0; index < LeftArray.Count(); index++)
                                {
                                    var foundDifference = FindDiff(LeftArray[index], RightArray[index]);
                                    if (foundDifference.HasValues)
                                    {
                                        difference[$"{index}"] = foundDifference;
                                    }
                                }
                            }
                            else
                            {
                                var left = new JArray(LeftArray.Except(RightArray, new JTokenEqualityComparer()));
                                var right = new JArray(RightArray.Except(LeftArray, new JTokenEqualityComparer()));
                                if (left.HasValues)
                                {
                                    difference["-"] = left;
                                }
                                if (right.HasValues)
                                {
                                    difference["+"] = right;
                                }
                            }
                        }
                    }
                    break;
                default:
                    difference["-"] = leftJson;
                    difference["+"] = rightJson;
                    break;
            }

            return difference;
        }
    }
}