# JsonToModelConverter
Генерация кода модели в виде класса C# из Json Schema.

## Пример работы.
Запуск проекта Test со следующим кодом.
```
using JsonToModelConverter;

var path = Path.GetFullPath("./../../../obj.json");
var json = File.ReadAllText(path);
var elem = json.FromJson("Example");
Console.WriteLine(elem);
```
Вывод в консоль:
```
public class Example
{
        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("Version")]
        public string Version { get; set; }

        [JsonProperty("PreEstimate")]
        public double PreEstimate { get; set; }

        [JsonProperty("Estimate")]
        public double Estimate { get; set; }

        [JsonProperty("HasSmf")]
        public int HasSmf { get; set; }

}
```
