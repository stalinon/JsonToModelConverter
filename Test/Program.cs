using JsonToModelConverter;

var path = Path.GetFullPath("./../../../obj.json");
var json = File.ReadAllText(path);
var elem = json.FromJson("Example");
Console.WriteLine(elem);
