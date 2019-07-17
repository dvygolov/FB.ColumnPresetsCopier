using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;

namespace FB.ColumnPresetsCopier
{
    public class ColumnPresetsCopier
    {
        private string _accessToken;
        private RestClient _restClient;
        private const string _fileName = "clmnpresets.json";

        public ColumnPresetsCopier(string apiAddress, string accessToken)
        {
            _accessToken = accessToken;
            _restClient = new RestClient(apiAddress);
        }

        public void Download(string acc)
        {
            var request = new RestRequest($"act_{acc}", Method.GET);
            request.AddQueryParameter("access_token", _accessToken);
            request.AddQueryParameter("fields", "user_settings{column_presets{columns,id,name}}");
            var response = _restClient.Execute(request);
            var json = (JObject)JsonConvert.DeserializeObject(response.Content);
            if (!string.IsNullOrEmpty(json["error"]?["message"].ToString()))
            {
                Console.WriteLine(
                    $"Ошибка при попытке выполнить запрос:{json["error"]["message"]}");
                return;
            }
            foreach (var clmnPreset in json["user_settings"]["column_presets"]["data"])
            {
                Console.WriteLine($"Найден шаблон отображения колонок: {clmnPreset["name"]}");
            }
            System.IO.File.WriteAllText(_fileName, response.Content);
            Console.WriteLine("Скачивание шаблонов отображения закончено.");
        }

        public void Upload(string acc)
        {
            if (!System.IO.File.Exists(_fileName))
            {
                Console.WriteLine("Файл с шаблонами отображения не существует! Сначала скачайте их!");
                return;
            }

            var jsonTxt = System.IO.File.ReadAllText(_fileName);
            var json = (JObject)JsonConvert.DeserializeObject(jsonTxt);
            var accSplit = acc.Split(',');
            foreach (var a in accSplit)
            {
                var request = new RestRequest($"act_{a}", Method.GET);
                request.AddQueryParameter("access_token", _accessToken);
                request.AddQueryParameter("fields", "user_settings");
                var response = _restClient.Execute(request);
                var usjson = (JObject)JsonConvert.DeserializeObject(response.Content);
                var usid=usjson["user_settings"]["id"];
                if (usid== null) //в этом акке еще не было пользовательских настроек  
                {
                    request = new RestRequest($"act_{a}/user_settings", Method.POST);
                    request.AddQueryParameter("access_token", _accessToken);
                    response = _restClient.Execute(request);
                    usjson = (JObject)JsonConvert.DeserializeObject(response.Content);
                    usid=usjson["id"];
                }
                foreach (var clmnPreset in json["user_settings"]["column_presets"]["data"])
                {
                    var req = new RestRequest($"{usid}/column_presets", Method.POST);
                    req.AddParameter("access_token", _accessToken);
                    req.AddParameter("name", clmnPreset["name"]);
                    req.AddParameter("columns", clmnPreset["columns"]);
                    var resp = _restClient.Execute(req);
                    if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                        Console.WriteLine($"Загрузили шаблон {clmnPreset["name"]} в аккаунт {a}");
                    else
                        Console.WriteLine($"Не смогли загрузить шаблон {clmnPreset["name"]} в аккаунт {a}");
                }
            }
            Console.WriteLine("Загрузка шаблонов закончена.");
        }

        /*public void Clear(string acc)
        {
            Console.Write($"Вы действительно хотите удалить все автоправила в аккаунте {acc}?(Y/N)");
            var answer = Console.ReadKey();
            Console.WriteLine();
            if (answer.KeyChar != 'y' && answer.KeyChar != 'Y') return;
            var request = new RestRequest($"act_{acc}/adrules_library", Method.GET);
            request.AddQueryParameter("access_token", _accessToken);
            request.AddQueryParameter("fields", "entity_type,evaluation_spec,execution_spec,name,schedule_spec");
            var response = _restClient.Execute(request);
            var json = (JObject)JsonConvert.DeserializeObject(response.Content);
            foreach (var rule in json["data"])
            {
                Console.WriteLine($"Удаляем правило: {rule["name"]}");
                request = new RestRequest($"{rule["id"]}", Method.DELETE);
                request.AddQueryParameter("access_token", _accessToken);
                var resp = _restClient.Execute(request);
                if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                    Console.WriteLine("Возникла проблема при удалении этого правила :-(");

            }
            Console.WriteLine("Удаление правил закончено.");
        }*/
    }
}
