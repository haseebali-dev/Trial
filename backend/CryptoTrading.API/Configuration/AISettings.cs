namespace CryptoTrading.API.Configuration;

public class AISettings
{
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string DefaultModel { get; set; } = "llama3.1:8b";
    public float Temperature { get; set; } = 0.3f;
    public int MaxModels { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 120;
    public bool Enabled { get; set; } = true;
}