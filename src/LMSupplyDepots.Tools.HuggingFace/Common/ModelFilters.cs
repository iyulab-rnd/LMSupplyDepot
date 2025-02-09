﻿using System.Collections.ObjectModel;

namespace LMSupplyDepots.Tools.HuggingFace.Common;

/// <summary>
/// Defines the filter constants for different model types.
/// </summary>
public static class ModelFilters
{
    /// <summary>
    /// Required filters for text generation models.
    /// </summary>
    public static readonly ReadOnlyCollection<string> TextGenerationFilters = new(new[]
    {
        "text-generation",
        "gguf"
    });

    /// <summary>
    /// Required filters for embedding models.
    /// </summary>
    public static readonly ReadOnlyCollection<string> EmbeddingFilters = new(new[]
    {
        "sentence-similarity",
        "gguf"
    });
}