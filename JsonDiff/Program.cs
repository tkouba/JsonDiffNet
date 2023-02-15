using CommandLine;
using Newtonsoft.Json.Linq;

namespace JsonDiff
{
    internal class Program
    {
        // https://github.com/andreyvit/json-diff
        // https://github.com/wbish/jsondiffpatch.net
        // https://github.com/aminm-net/JsonDiffer.Netstandard

        // ! https://stackoverflow.com/questions/24876082/find-and-return-json-differences-using-newtonsoft-in-c


        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<Options>(args).MapResult(
                (Options opts) => RunOptions(opts),
                errs => HandleParseError(errs));
        }
        static int RunOptions(Options opts)
        {
            JObject left = JObject.Parse(File.ReadAllText(opts.LeftFile));
            JObject right = JObject.Parse(File.ReadAllText(opts.RightFile));
            JObject diff = FindDiff(left, right);
            if (String.IsNullOrEmpty(opts.OutputFile))
                Console.WriteLine(diff.ToString());
            else
                File.WriteAllText(opts.OutputFile, diff.ToString());
            if (diff.HasValues == false)
                return 0;
            return 1;
        }
        static int HandleParseError(IEnumerable<Error> errs)
        {
            return 255;
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="leftJson"></param>
        /// <param name="rightJson"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <remarks>
        /// Original version https://stackoverflow.com/a/65222961/1498252 by Rohith Daruri
        /// based on https://stackoverflow.com/a/53654737/1498252 by Dzmitry Paliakou
        /// </remarks>
        public static JObject FindDiff(JToken leftJson, JToken rightJson)
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
                                ["-"] = RightJSON[tag]
                            };
                        }
                        var ModifiedTags = LeftJSON.Properties().Select(c => c.Name).Except(AddedTags).Except(UnchangedTags);
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