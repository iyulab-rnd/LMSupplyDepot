using LMSupplyDepots.Tools.HuggingFace.Models;
using System.Text.RegularExpressions;

namespace LMSupplyDepots.Tools.HuggingFace.Tests;

public class HuggingFaceModelTests
{
    [Fact]
    public void GetFilePaths_NoPattern_ReturnsAllFiles()
    {
        // Arrange
        var model = new HuggingFaceModel
        {
            Siblings = new[]
            {
                new ModelResource { Rfilename = "config.json" },
                new ModelResource { Rfilename = "model.bin" },
                new ModelResource { Rfilename = "vocab.txt" }
            }
        };

        // Act
        var result = model.GetFilePaths();

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Contains("config.json", result);
        Assert.Contains("model.bin", result);
        Assert.Contains("vocab.txt", result);
    }

    [Fact]
    public void GetFilePaths_WithPattern_ReturnsMatchingFiles()
    {
        // Arrange
        var model = new HuggingFaceModel
        {
            Siblings = new[]
            {
                new ModelResource { Rfilename = "config.json" },
                new ModelResource { Rfilename = "model.bin" },
                new ModelResource { Rfilename = "vocab.txt" }
            }
        };
        var pattern = new Regex(@"\.json$");

        // Act
        var result = model.GetFilePaths(pattern);

        // Assert
        Assert.Single(result);
        Assert.Equal("config.json", result[0]);
    }
}
