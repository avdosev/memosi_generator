namespace MemesApi
{
    public static class ConfigurationConsts
    {
        public const string ApiUrl = "API_URL";
        public const string ConnectionString = "CONNECTION_STRING";
        public const string ModelUrl = "MODEL_CONNECTION_STRING";
        
        public static readonly string OutputTemplate =
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{TraceId}-{SpanId}] {Scope} [{SourceContext}] {Message} {NewLine}{Exception}";
    }
}
