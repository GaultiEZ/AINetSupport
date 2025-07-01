using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;
using System.Security;
using System.Reflection.Metadata.Ecma335;
///该包主打操作简单 性能好 自定义性强 提供一些简单的包装操作
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
                catch (JsonException je)
                {
                    throw je;
                }
            }
            return "";
        }

        public static void AddAssistMessage(string messageContent,OpenAIMessage oai)
        {
            oai.Messages.Add(new Message { Role = "assistant", Content = messageContent });
        }
    }

    public class SimClient:IClient
    {
        public string Model { get; set; }
        public string Endpoint { get; set; }
        private string ApiKey { get; set; }
        public double Temperature { get; set; }
        public int MaxTokens { get; set; }
        public int ContextLength { get; set; }

        public HttpClient HttpClient { get; set; }
        public OpenAIMessage OpenAIMessage { get; set; }

        public SimClient(HttpClient https,string model,string endpoint,string api,double temp=0.7,int maxtoken=2000,int contentlen = 8000,OpenAIMessage oai = null)
        {
            Model = model;
            ApiKey = api;
            Endpoint = endpoint;
            Temperature = temp;
            MaxTokens = maxtoken;
            ContextLength = contentlen;
            HttpClient = https;
            if (oai == null)
            {
                OpenAIMessage = new OpenAIMessage();
            }           
        }
        private void UpdateOpenAIMessage()
        {
            OpenAIMessage.Model = Model;
            OpenAIMessage.Stream = true;
            OpenAIMessage.Temperature = this.Temperature;
            OpenAIMessage.MaxTokens = this.MaxTokens;
            OpenAIMessage.ContextLength = this.ContextLength;
        }
        public async Task<Stream> CallApiPost(string api, string modelName, string endpoint, string userInput)
        {
            UpdateOpenAIMessage();
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
                Console.Error.WriteLine($"HTTP request failed: {ex.Message}");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                Console.Error.WriteLine($"Request was canceled or timed out: {ex.Message}");
                throw;
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"JSON parsing failed: {ex.Message}");
                throw;
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"IO operation failed: {ex.Message}");
                throw;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"Invalid argument: {ex.Message}");
                throw;
            }
            catch (AggregateException ex)
            {
                Console.Error.WriteLine($"Multiple exceptions occurred: {ex.Message}");
                foreach (var innerEx in ex.InnerExceptions)
                {
                    Console.Error.WriteLine($"Inner exception: {innerEx.Message}");
                }
                throw;
            }
            catch (SecurityException ex)
            {
                Console.Error.WriteLine($"Security exception: {ex.Message}");
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.Error.WriteLine($"Unauthorized access: {ex.Message}");
                throw;
            }
        }
        public async Task<Stream> CallApiPostAsync(string api, string modelName, string endpoint,string userInput)
        {
            UpdateOpenAIMessage();

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
                Console.Error.WriteLine($"HTTP request failed: {ex.Message}");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                Console.Error.WriteLine($"Request was canceled or timed out: {ex.Message}");
                throw;
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"JSON parsing failed: {ex.Message}");
                throw;
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"IO operation failed: {ex.Message}");
                throw;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"Invalid argument: {ex.Message}");
                throw;
            }
            catch (AggregateException ex)
            {
                Console.Error.WriteLine($"Multiple exceptions occurred: {ex.Message}");
                foreach (var innerEx in ex.InnerExceptions)
                {
                    Console.Error.WriteLine($"Inner exception: {innerEx.Message}");
                }
                throw;
            }
            catch (SecurityException ex)
            {
                Console.Error.WriteLine($"Security exception: {ex.Message}");
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.Error.WriteLine($"Unauthorized access: {ex.Message}");
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