using Microsoft.Extensions.VectorData;

namespace TaskManager.BLL.Search
{
    public sealed class VectorStoreTasks
    {
        [VectorStoreKey]
        public int Id { get; set; }

        [VectorStoreData]
        public string Title { get; set; } = string.Empty;

        [VectorStoreData]
        public string Description { get; set; } = string.Empty;

        [VectorStoreData]
        public bool IsCompleted { get; set; }

        [VectorStoreData]
        public DateTime DueDate { get; set; }

        [VectorStoreVector(1536)]
        public ReadOnlyMemory<float> DescriptionEmbedding { get; set; }
    }
}