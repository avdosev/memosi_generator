namespace MemesApi
{
    public class AppSettings
    {
        /// <summary>
        /// Макс. размер изображения в байтах.
        /// </summary>
        public long MaxImageSize { get; set; }
        public string? ModelServiceUrl { get; set; }
    }
}
