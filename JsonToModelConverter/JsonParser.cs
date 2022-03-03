using System.Text;
using System.Text.RegularExpressions;

namespace JsonToModelConverter
{
    public static class JsonParser
    {
        private static Stack<List<string>> splitArrayPool;
        private static StringBuilder stringBuilder;
        private static Dictionary<int, Dictionary<string, string>> pairs;
        private static int depth = 0;

        public static string FromJson(this string json, string className)
        {
            if (stringBuilder == null) stringBuilder = new StringBuilder();
            if (splitArrayPool == null) splitArrayPool = new Stack<List<string>>();

            stringBuilder.Length = 0;
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '"')
                {
                    i = AppendUntilStringEnd(true, i, json);
                    continue;
                }
                if (char.IsWhiteSpace(c))
                    continue;

                stringBuilder.Append(c);
            }
            var list = Split(stringBuilder.ToString());
            return GenerateModel(className);
        }

        private static int AppendUntilStringEnd(bool appendEscapeCharacter, int startIdx, string json)
        {
            stringBuilder.Append(json[startIdx]);
            for (int i = startIdx + 1; i < json.Length; i++)
            {
                if (json[i] == '\\')
                {
                    if (appendEscapeCharacter)
                        stringBuilder.Append(json[i]);
                    stringBuilder.Append(json[i + 1]);
                    i++;
                }
                else if (json[i] == '"')
                {
                    stringBuilder.Append(json[i]);
                    return i;
                }
                else
                    stringBuilder.Append(json[i]);
            }
            return json.Length - 1;
        }

        private static List<string> Split(string json, bool isArray = false)
        {
            if (pairs == null) pairs = new Dictionary<int, Dictionary<string, string>>();
            if (!pairs.ContainsKey(depth)) pairs[depth] = new Dictionary<string, string>();

            var splitArray = splitArrayPool.Count > 0 ? splitArrayPool.Pop() : new List<string>();
            splitArray.Clear();
            if (json.Length == 2)
                return splitArray;
            int parseDepth = 0;
            stringBuilder.Length = 0;
            for (int i = 1; i < json.Length - 1; i++)
            {
                switch (json[i])
                {
                    case '[':
                    case '{':
                        parseDepth++;
                        break;
                    case ']':
                    case '}':
                        parseDepth--;
                        break;
                    case '"':
                        i = AppendUntilStringEnd(true, i, json);
                        continue;
                    case ',':
                    case ':':
                        if (parseDepth == 0)
                        {
                            splitArray.Add(stringBuilder.ToString());
                            stringBuilder.Length = 0;
                            continue;
                        }
                        break;
                }

                stringBuilder.Append(json[i]);
            }

            splitArray.Add(stringBuilder.ToString());
            PutTypesIntoPairs(splitArray, isArray);

            return splitArray;
        }

        private static void PutTypesIntoPairs(List<string> splitArray, bool isArray = false)
        {
            if (isArray) return;
            string key = default;
            string value = default;
            for (var i = 0; i < splitArray.Count; i++)
            {
                if (i % 2 == 0)
                {
                    key = splitArray[i].Trim('\"');
                }
                else
                {
                    value = GetTypeOf(splitArray[i], key);
                    if (!pairs[depth].TryAdd(key, value))
                        break;
                }
            }
        }

        private static string GetTypeOf(string value, string key)
        {
            if (value.StartsWith('[') && value.EndsWith(']'))
            {
                var keyPascal = key.ToPascalCase();

                var list = Split(value, true);

                if (!value.Contains('{'))
                {
                    value = value.Split('[',']',',').First();
                    return $"IEnumerable<{GetTypeOf(value, keyPascal)}>";
                }

                depth++;
                list.First().FromJson(keyPascal);
                depth--;
                return $"IEnumerable<{keyPascal}>";
            }
            else if (value.StartsWith("{") && value.EndsWith("}"))
            {
                var keyPascal = key.ToPascalCase();

                depth++;
                value.FromJson(keyPascal);
                depth--;

                return keyPascal;
            }
            else if (value == "\"null\"")
            {
                return "string";
            }
            else if (value.Contains("\""))
            {
                return "string";
            }
            else if (value.Contains("true") || value.Contains("false"))
            {
                return "bool";
            }
            else if (value.Contains("."))
            {
                return "double";
            }
            else
            {
                return "int";
            }
        }

        private static string GenerateModel(string className, bool JsonProperty = true)
        {
            var sb = new StringBuilder();
            var classDecl = $"public class {className.ToPascalCase()}\n{{\n";
            sb.Append(classDecl);
            foreach (var variable in pairs[depth])
            {
                var field = $"\tpublic {variable.Value} {variable.Key.ToPascalCase()} {{ get; set; }}\n\n";

                if (JsonProperty)
                {
                    var prop = $"\t[JsonProperty(\"{variable.Key}\")]\n";
                    sb.Append(prop);
                }

                sb.Append(field);
            }
            pairs[depth] = new Dictionary<string, string>();

            sb.Append("}");

            string classText = string.Empty;

            Directory.CreateDirectory("../../../../GeneratedModels/");

            var path = Path.GetFullPath($"../../../../GeneratedModels/{className.ToPascalCase()}.cs");
            var append = File.Exists(path);

            if (append)
            {
                using (var sw = new StreamReader(path))
                {
                    classText = sw.ReadToEnd();
                    classText = classText.Remove(classText.Length - 1, 1);
                }
            }

            classText += sb.ToString();

            using (var sw = new StreamWriter(path))
            {
                
                sw.WriteLine(classText);
            }

            return sb.ToString();
        }

        private static string ToPascalCase(this string original)
        {
            Regex invalidCharsRgx = new Regex("[^_a-zA-Z0-9]");
            Regex whiteSpace = new Regex(@"(?<=\s)");
            Regex startsWithLowerCaseChar = new Regex("^[a-z]");
            Regex firstCharFollowedByUpperCasesOnly = new Regex("(?<=[A-Z])[A-Z0-9]+$");
            Regex lowerCaseNextToNumber = new Regex("(?<=[0-9])[a-z]");
            Regex upperCaseInside = new Regex("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))");

            var pascalCase = invalidCharsRgx.Replace(whiteSpace.Replace(original, "_"), string.Empty)
                // split by underscores
                .Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => startsWithLowerCaseChar.Replace(w, m => m.Value.ToUpper()))
                .Select(w => firstCharFollowedByUpperCasesOnly.Replace(w, m => m.Value.ToLower()))
                .Select(w => lowerCaseNextToNumber.Replace(w, m => m.Value.ToUpper()))
                .Select(w => upperCaseInside.Replace(w, m => m.Value.ToLower()));

            return string.Concat(pascalCase);
        }
    }
}
