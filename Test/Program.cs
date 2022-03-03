using JsonToModelConverter;

var path = Path.GetFullPath("./../../../obj.json");
var json = File.ReadAllText(path);

json.FromJson("Example");

