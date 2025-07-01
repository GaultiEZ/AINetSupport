using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;
using System.Security;
using System.Reflection.Metadata.Ecma335;

namespace GLAIStudio.AINetSupportCSS
{
    
    public interface IClient
    {
        
        HttpClient HttpClient { get; set; }

        OpenAIMessage OpenAIMessage { get; set; }

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

        public void AddAssistMessage(string messageContent)
        {
            OpenAIMessage.Messages.Add(new Message { Role = "assistant", Content = messageContent });
        }
    }

    public class SimClientSingleModel:IClient
    {
        public string Model { get; set; }
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
        public double Temperature { get; set; }
        public int MaxTokens { get; set; }
        public int ContextLength { get; set; }

        public HttpClient HttpClient { get; set; }

        public OpenAIMessage OpenAIMessage { get; set; }

        public SimClientSingleModel(HttpClient https, string api, string modelName, string endpoint, double temp = 0.7, int maxtoken = 2000, int contextlen = 1024, OpenAIMessage opi = null)
        {
            this.Model = modelName;
            this.ApiKey = api;
            this.Endpoint = endpoint;
            this.Temperature = temp;
            this.MaxTokens = maxtoken;
            this.ContextLength = contextlen;
            HttpClient = https;
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // 初始化 OpenAIMessage
            if (opi == null)
            {
                OpenAIMessage = new OpenAIMessage
                {
                    Model = this.Model,
                    Stream = true,
                    Temperature = this.Temperature, // 默认值
                    MaxTokens = this.MaxTokens,   // 默认值
                    ContextLength = this.ContextLength // 默认值
                };
            }

        }

        public async Task<Stream> CallApiPost(string userInput)
        {
            // 构建请求内容
            var userMessage = new Message { Role = "user", Content = userInput };
            OpenAIMessage.Messages.Add(userMessage);

            var content = new StringContent(
                JsonSerializer.Serialize(OpenAIMessage),
                Encoding.UTF8,
                "application/json");

            // 发送请求
            try
            {
                var response = await HttpClient.PostAsync(Endpoint, content);
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
            OpenAIMessage.Messages.Add(userMessage);
            var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            try
            {
                var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, default);
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


    }

    public class SimClientMultiModel:IClient
    {
        

        public HttpClient HttpClient { get; set; }
        public OpenAIMessage OpenAIMessage { get; set; }

        public SimClientMultiModel(HttpClient https,OpenAIMessage opi = null)
        {
            HttpClient = https;
            if (opi == null)
            {
                OpenAIMessage = new OpenAIMessage();
            }
            
        }
        public async Task<Stream> CallApiPost(string api, string modelName, string endpoint, string userInput, double temp = 0.7, int maxtoken = 2000, int contextlen = 1024)
        {
            OpenAIMessage.Model = modelName;
            OpenAIMessage.Temperature = temp;
            OpenAIMessage.ContextLength = contextlen;
            OpenAIMessage.MaxTokens = maxtoken;
            OpenAIMessage.Stream = true;

            var userMessage = new Message { Role = "user", Content = userInput };
            OpenAIMessage.Messages.Add(userMessage);

            var content = new StringContent(
                JsonSerializer.Serialize(OpenAIMessage),
                Encoding.UTF8,
                "application/json");

            // 发送请求
            try
            {
                var response = await HttpClient.PostAsync(endpoint, content);
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
        public async Task<Stream> CallApiPostAsync(string api, string modelName, string endpoint, string userInput, double temp = 0.7, int maxtoken = 2000, int contextlen = 1024)
        {
            OpenAIMessage.Model = modelName;
            OpenAIMessage.Temperature = temp;
            OpenAIMessage.ContextLength = contextlen;
            OpenAIMessage.MaxTokens = maxtoken;
            OpenAIMessage.Stream = true;

            var userMessage = new Message { Role = "user", Content = userInput };
            OpenAIMessage.Messages.Add(userMessage);
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("Authorization", $"Bearer {api}");
            request.Content = new StringContent(JsonSerializer.Serialize(OpenAIMessage), Encoding.UTF8, "application/json");
            try
            {
                var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, default);
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