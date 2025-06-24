using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Diagnostics.CodeAnalysis;
using static ApiProvider;

public class ApiProvider : IDisposable
{
    private static ApiProvider? instance = null;
    private HttpClientHandler handler = new HttpClientHandler();
    private HttpListener server = new HttpListener();
    private HttpClient client;
    private bool isServerRun = false;
    private Uri baseAddr = new Uri("http://26.152.224.100:8000/");
    private string fileIdName = "Id.txt";
    private string id = "";
    private string reqId = "localhost:55080";
    private string verCode = "";
    public string decryptKey { get; private set; } = "";

    private string servBaseAddr = "http://*:55080/";
    private static string redirectUri = "http://localhost:55080/";

    public static ApiProvider Instance
    {
        get
        {
            if(instance == null)
                instance = new ApiProvider();
            return instance;
        }
    }

    public ApiProvider()
    {
        initSettings();
        handler.AllowAutoRedirect = true;
        client = new HttpClient(handler);
        client.BaseAddress = baseAddr;
        client.Timeout = TimeSpan.FromSeconds(30);
        server.Prefixes.Add(servBaseAddr);
        server.Start();
    }

    private void initSettings()
    {
        string[] lines = File.ReadAllLines("Settings.txt", Encoding.UTF8);
        foreach (string line in lines)
        {
            string[] parts = line.Split("=");
            if (parts[0] == "localServerAddr")
            {
                this.servBaseAddr = parts[1];
            }
            else if (parts[0] == "redirectUri")
            {
                ApiProvider.redirectUri = parts[1];
            }
            else if (parts[0] == "reqId")
            {
                this.reqId = parts[1];
            }
            else if (parts[0] == "authServerAddr")
            {
                this.baseAddr = new Uri(parts[1]);
            }
        }
    }

    public async Task Listen()
    {
        isServerRun = true;
        await Task.Run(() => {
            HttpListenerContext ctx = server.GetContext();
            HttpListenerRequest req = ctx.Request;
            string? hostaddr = req.Headers["Host"];

            if (req.Headers["Host"] == reqId)
            {
                string? code = req.QueryString["code"];
                if (code == null)
                    goto down;
                string body = $"grant_type=authorization_code" +
                $"&code={code}" +
                $"&redirect_uri={redirectUri}" +
                $"&code_vierfier={verCode}" +
                $"&client_id={this.id}";
                var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
                HttpResponseMessage response = client.PostAsync("token", content).Result;
                response.EnsureSuccessStatusCode();
                string responseStr = response.Content.ReadAsStringAsync().Result;
                TokenRespForJson? accesTokenResp = JsonSerializer.Deserialize<TokenRespForJson>(responseStr, AppJsonContext.Default.TokenRespForJson);
                if (accesTokenResp == null)
                    throw new Exception("Token is null");

                HttpRequestMessage reqGetKey = new HttpRequestMessage(HttpMethod.Get, client.BaseAddress + $"get_key?nonce={makeRandomString()}");
                reqGetKey.Headers.Add("Authorization", " Bearer " + accesTokenResp.access_token);
                response = client.SendAsync(reqGetKey).Result;
                response.EnsureSuccessStatusCode();
                responseStr = response.Content.ReadAsStringAsync().Result;
                JsonObject? jobj = JsonObject.Parse(responseStr) as JsonObject;
                if (jobj == null)
                    throw new Exception("Maleformed response");
                string? tempKey = jobj["key"]?.GetValue<string>();
                if (tempKey == null)
                    throw new Exception("Maleformed response");
                this.decryptKey = tempKey;
            }
            down:
                HttpListenerResponse resp = ctx.Response;
                string data = "something...";
                resp.ContentLength64 = data.Length;
                resp.StatusCode = (int)HttpStatusCode.OK;
                resp.StatusDescription = "Status OK";
                resp.OutputStream.Write(Encoding.UTF8.GetBytes(data));
        });
        server.Stop();
    }

    private static string ReadFormData(HttpListenerRequest request)
    {
        using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
        {
            return reader.ReadToEnd();
        }
    }

    public void fillID()
    {
        this.id = File.ReadAllText(fileIdName);
    }

    public void register()
    {
        try
        {
            fillID();
        } 
        catch(FileNotFoundException ex)
        {
            registerClassForJson jsonObj = new registerClassForJson();

            string json = JsonSerializer.Serialize(jsonObj, AppJsonContext.Default.registerClassForJson);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = client.PostAsync("register", content).Result;

            response.EnsureSuccessStatusCode();
            string responseStr = response.Content.ReadAsStringAsync().Result;
            JsonNode? node = JsonNode.Parse(responseStr);
            if (node is null)
                throw new ApplicationException("id is null that is bad");
            this.id = node["id"].GetValue<string>();
            File.Create(fileIdName).Dispose();
            File.WriteAllText(fileIdName, this.id);
        }
    }
    public string auth()
    {
        (string r, string c) = makeCodeChallange();
        this.verCode = r;
        HttpResponseMessage response = client.GetAsync($"authorize" +
            $"?response_type=code" +
            $"&client_id={this.id}" +
            $"&code_challange={HttpUtility.UrlEncode(c)}" +
            $"&state={makeRandomString()}" +
            $"&app_token={makeRandomString()}" +
            $"&code_challange_method=S256" +
            $"&redirect_uri={redirectUri}" +
            $"&scope=test_scope other_test_scope").Result;
        string responseStr = response.Content.ReadAsStringAsync().Result;
        response.EnsureSuccessStatusCode();
        return responseStr;
    }

    private static (string randomStr, string challange) makeCodeChallange()
    {
        var randomStr = makeRandomString();

        byte[] sha;

        using (SHA256 hash = SHA256Managed.Create())
        {
            sha = hash.ComputeHash(Encoding.UTF8.GetBytes(randomStr));
        }

        return (randomStr, System.Convert.ToBase64String(sha));

    }

    private static string makeRandomString()
    {
        Random random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, 45)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public void Dispose()
    {
        isServerRun = false;
        server.Stop();
        server.Close();
    }
    public class registerClassForJson
    {
        public string[] redirect_uris { get; set; } = new List<string>() { redirectUri }.ToArray();
        public string token_endpoint_auth_method { get; set; } = "client_secret_post";
        public string[] grant_types { get; set; } = new List<string>() { "urn:ietf:params:oauth:grant-type:jwt-bearer" }.ToArray();
        public string[] response_types { get; set; } = new List<string>() { "code" }.ToArray();
        public string state { get; set; } = makeRandomString();
        public string scope { get; set; } = "test_scope other_test_scope";
        public registerClassForJson() { }
    }
    public class TokenRespForJson
    {
        public string access_token { get; set; } = "";
        public string refresh_token { get; set; } = "";
        public string app_token { get; set; } = "";
        public string token_type { get; set; } = "";
        public int expires_in { get; set; } = 0;

        public TokenRespForJson() { }
    }

}

[JsonSerializable(typeof(ApiProvider.TokenRespForJson))]
[JsonSerializable(typeof(ApiProvider.registerClassForJson))]
partial class AppJsonContext : JsonSerializerContext
{

}
