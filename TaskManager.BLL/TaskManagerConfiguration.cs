using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;


namespace TaskManager.BLL;

public sealed class TaskManagerConfiguration
{
    private readonly IConfigurationRoot _configRoot;
    private static TaskManagerConfiguration? s_instance;

    private TaskManagerConfiguration(IConfigurationRoot configRoot)
    {
        this._configRoot = configRoot;
    }

    public static void Initialize(IConfigurationRoot configRoot)
    {
        s_instance = new TaskManagerConfiguration(configRoot);
    }

    public static IConfigurationRoot? ConfigurationRoot => s_instance?._configRoot;


    public static AzureOpenAIConfig AzureOpenAI => LoadSection<AzureOpenAIConfig>();
    public static AzureOpenAIEmbeddingsConfig AzureOpenAIEmbeddings => LoadSection<AzureOpenAIEmbeddingsConfig>();

    
    #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

    public class AzureOpenAIConfig
    {
        public string DeploymentName { get; set; }
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
    }    

    public class AzureOpenAIEmbeddingsConfig
    {
        public string DeploymentName { get; set; }
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
    }

    private static T LoadSection<T>([CallerMemberName] string? caller = null)
    {
        if (s_instance is null)
        {
            throw new InvalidOperationException(
                "TaskManagerConfiguration must be initialized with a call to Initialize(IConfigurationRoot) before accessing configuration values.");
        }

        if (string.IsNullOrEmpty(caller))
        {
            throw new ArgumentNullException(nameof(caller));
        }

        return s_instance._configRoot.GetSection(caller).Get<T>() ??
               throw new ApplicationException($"Configuration section not found: {caller}");
    }
}
