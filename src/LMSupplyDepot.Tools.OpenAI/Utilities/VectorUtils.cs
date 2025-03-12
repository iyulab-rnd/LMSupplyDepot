namespace LMSupplyDepot.Tools.OpenAI.Utilities;

/// <summary>
/// Utility methods for working with embedding vectors
/// </summary>
public static class VectorUtils
{
    /// <summary>
    /// Calculates the cosine similarity between two embedding vectors
    /// </summary>
    public static double CosineSimilarity(List<float> embedding1, List<float> embedding2)
    {
        if (embedding1 == null || embedding2 == null || embedding1.Count != embedding2.Count)
        {
            throw new ArgumentException("Embeddings must be non-null and of the same length");
        }

        float dotProduct = 0;
        float norm1 = 0;
        float norm2 = 0;

        for (int i = 0; i < embedding1.Count; i++)
        {
            dotProduct += embedding1[i] * embedding2[i];
            norm1 += embedding1[i] * embedding1[i];
            norm2 += embedding2[i] * embedding2[i];
        }

        if (norm1 <= 0 || norm2 <= 0)
        {
            return 0;
        }

        return dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
    }

    /// <summary>
    /// Normalizes an embedding vector to unit length
    /// </summary>
    public static List<float> NormalizeEmbedding(List<float> embedding)
    {
        if (embedding == null || embedding.Count == 0)
        {
            return embedding;
        }

        float norm = 0;
        foreach (var value in embedding)
        {
            norm += value * value;
        }
        norm = (float)Math.Sqrt(norm);

        if (norm <= 0)
        {
            return embedding;
        }

        var normalized = new List<float>(embedding.Count);
        foreach (var value in embedding)
        {
            normalized.Add(value / norm);
        }

        return normalized;
    }

    /// <summary>
    /// Truncates an embedding vector to a specific number of dimensions
    /// </summary>
    public static List<float> TruncateEmbedding(List<float> embedding, int dimensions)
    {
        if (embedding == null || embedding.Count <= dimensions)
        {
            return embedding;
        }

        return embedding.GetRange(0, dimensions);
    }

    /// <summary>
    /// Calculates the Euclidean distance between two embedding vectors
    /// </summary>
    public static double EuclideanDistance(List<float> embedding1, List<float> embedding2)
    {
        if (embedding1 == null || embedding2 == null || embedding1.Count != embedding2.Count)
        {
            throw new ArgumentException("Embeddings must be non-null and of the same length");
        }

        float sumSquaredDiff = 0;
        for (int i = 0; i < embedding1.Count; i++)
        {
            float diff = embedding1[i] - embedding2[i];
            sumSquaredDiff += diff * diff;
        }

        return Math.Sqrt(sumSquaredDiff);
    }

    /// <summary>
    /// Calculates the dot product of two embedding vectors
    /// </summary>
    public static float DotProduct(List<float> embedding1, List<float> embedding2)
    {
        if (embedding1 == null || embedding2 == null || embedding1.Count != embedding2.Count)
        {
            throw new ArgumentException("Embeddings must be non-null and of the same length");
        }

        float dotProduct = 0;
        for (int i = 0; i < embedding1.Count; i++)
        {
            dotProduct += embedding1[i] * embedding2[i];
        }

        return dotProduct;
    }

    /// <summary>
    /// Gets the embedding vector from a single-input embeddings response
    /// </summary>
    public static List<float> GetEmbeddingVector(EmbeddingsResponse response)
    {
        if (response?.Data == null || response.Data.Count == 0)
            return new List<float>();

        return response.Data[0].Embedding;
    }
}