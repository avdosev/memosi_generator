namespace MemesApi
{
    public static class ConfigurationConsts
    {
        public const string ApiUrl = "API_URL";
        public const string ConnectionString = "CONNECTION_STRING";
        public const string ModelUrl = "MODEL_CONNECTION_STRING";

        public static class Minio
        {
            public const string Url = "MINIO_URL";
            public const string AccessKey = "MINIO_ROOT_USER";
            public const string SecretKey = "MINIO_ROOT_PASSWORD";
            public const string Bucket = "MINIO_BUCKET";
        }
        
        public static readonly string OutputTemplate =
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{TraceId}-{SpanId}] {Scope} [{SourceContext}] {Message} {NewLine}{Exception}";
    }
}
