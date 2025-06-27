using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;
using System.Security;

namespace GLAIStudio.AINetSupportCSS
{ 

    public class SimClient
    {
        public string model;
        public string endpoint;
        public string API;
        public double temp;
        public int maxtokens;
        public int contextlen;
        HttpClient httpClient;
        OpenAIMessage openAIMessage;

        public SimClient(HttpClient https, string api, string modelName, string endpoint,double temp=0.7,int maxtoken = 20,int contextlen = 1024)
        {
            this.model = modelName;
            this.API = api;
            this.endpoint = endpoint;
            this.temp = temp;
            this.maxtokens = maxtoken;
            this.contextlen = contextlen;
            httpClient = https;
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {API}");
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // 初始化 OpenAIMessage
            openAIMessage = new OpenAIMessage
            {
                Model = model,
                Stream = true,
                Temperature = this.temp, // 默认值
                MaxTokens = this.maxtokens,   // 默认值
                ContextLength = this.contextlen // 默认值
            };
        }

        public async Task<Stream> CallApiPost(string userInput)
        {
            // 构建请求内容
            var userMessage = new Message { Role = "user", Content = userInput };
            openAIMessage.Messages.Add(userMessage);

            var content = new StringContent(
                JsonSerializer.Serialize(openAIMessage),
                Encoding.UTF8,
                "application/json");

            // 发送请求
            try
            {
                var response = await httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();

                // 返回响应流
                return await response.Content.ReadAsStreamAsync();
            }
            catch (HttpRequestException ex)
            {
                
                Console.Error.WriteLine($"HTTP 请求失败: {ex.Message}");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                Console.Error.WriteLine($"请求被取消或超时: {ex.Message}");
                throw;
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"JSON 解析失败: {ex.Message}");
                throw;
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"IO 操作失败: {ex.Message}");
                throw;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"参数无效: {ex.Message}");
                throw;
            }
            catch (AggregateException ex)
            {
                Console.Error.WriteLine($"多个异常发生: {ex.Message}");
                foreach (var innerEx in ex.InnerExceptions)
                {
                    Console.Error.WriteLine($"内层异常: {innerEx.Message}");
                }
                throw;
            }
            catch (SecurityException ex)
            {
                Console.Error.WriteLine($"安全异常: {ex.Message}");
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.Error.WriteLine($"未经授权访问: {ex.Message}");
                throw;
            }


        }

        public async Task<Stream> CallApiPostAsync(string userInput)
        {
            var userMessage = new Message { Role = "user", Content = userInput };
            openAIMessage.Messages.Add(userMessage);
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("Authorization", $"Bearer {API}");
            request.Content = new StringContent(JsonSerializer.Serialize(openAIMessage), Encoding.UTF8, "application/json");
            try
            {
                var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, default);
                response.EnsureSuccessStatusCode();
                var stream = await response.Content.ReadAsStreamAsync();
                return stream;
            }
            catch (HttpRequestException ex)
            {

                Console.Error.WriteLine($"HTTP 请求失败: {ex.Message}");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                Console.Error.WriteLine($"请求被取消或超时: {ex.Message}");
                throw;
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"JSON 解析失败: {ex.Message}");
                throw;
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"IO 操作失败: {ex.Message}");
                throw;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"参数无效: {ex.Message}");
                throw;
            }
            catch (AggregateException ex)
            {
                Console.Error.WriteLine($"多个异常发生: {ex.Message}");
                foreach (var innerEx in ex.InnerExceptions)
                {
                    Console.Error.WriteLine($"内层异常: {innerEx.Message}");
                }
                throw;
            }
            catch (SecurityException ex)
            {
                Console.Error.WriteLine($"安全异常: {ex.Message}");
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.Error.WriteLine($"未经授权访问: {ex.Message}");
                throw;
            }
        }

        public static string AnswerProcessContent(string line)
        {
            if (line == null)
            {
                return "";
            }
            if (line.StartsWith("data: ") && !line.Contains("[DONE]"))
            {
                var jsonData = line.Substring(6);
                try
                {
                    // 解析JSON并提取内容
                    using var doc = JsonDocument.Parse(jsonData);
                    if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                        choices[0].TryGetProperty("delta", out var delta) &&
                        delta.TryGetProperty("content", out var contentElement))
                    {
                        string context = contentElement.GetString();
                        return context;
                    }
                }
                catch (JsonException)
                {
                    return "E1ZA";
                }
            }
            return "";
        }
    }

    public class OpenAIMessage
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; } = new();

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = true;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("context_length")]
        public int ContextLength { get; set; }
    }
    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }
}