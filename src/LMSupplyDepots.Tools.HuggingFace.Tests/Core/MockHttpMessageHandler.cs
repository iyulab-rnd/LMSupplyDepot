using LMSupplyDepots.Tools.HuggingFace.Models;
using System.Net;
using System.Text.Json;

namespace LMSupplyDepots.Tools.HuggingFace.Tests.Core;

public class MockHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 기본 인증 체크 - 모든 요청에 대해 체크
        var hasAuth = request.Headers.Contains("Authorization");
        if (!hasAuth)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("Authentication required")
            });
        }

        // 특정 모델 조회
        if (request.RequestUri!.PathAndQuery.Contains("/models/"))
        {
            if (request.RequestUri.PathAndQuery.Contains("nonexistent"))
            {
                var notFoundResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("Model not found")
                };
                return Task.FromResult(notFoundResponse);
            }

            var encodedId = request.RequestUri.PathAndQuery.Split("/models/")[1];
            var decodedId = Uri.UnescapeDataString(encodedId);

            var model = CreateMockModel(decodedId, true);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(model),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
            return Task.FromResult(response);
        }

        // 모델 검색
        if (request.RequestUri.PathAndQuery.Contains("/api/models"))
        {
            var models = new[]
            {
                CreateMockModel("test-model-1", true),  // GGUF 파일 포함
                CreateMockModel("test-model-2", true),  // GGUF 파일 포함
                CreateMockModel("test-model-3", false)  // GGUF 파일 미포함
            };
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(models),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
            return Task.FromResult(response);
        }

        // 파일 다운로드
        if (request.RequestUri.PathAndQuery.Contains("resolve/main/"))
        {
            // 존재하지 않는 파일 경로에 대해 404 반환
            if (request.RequestUri.PathAndQuery.Contains("nonexistent"))
            {
                var notFoundResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("File not found")
                };
                return Task.FromResult(notFoundResponse);
            }

            if (request.Method == HttpMethod.Head)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent("");
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                response.Content.Headers.ContentLength = 1024;
                return Task.FromResult(response);
            }
            else
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "mock-file-content",
                        System.Text.Encoding.UTF8,
                        "application/octet-stream")
                };
                return Task.FromResult(response);
            }
        }

        var defaultResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
        return Task.FromResult(defaultResponse);
    }

    private static HuggingFaceModel CreateMockModel(string id, bool includeGguf = false)
    {
        var siblings = new List<ModelResource>
        {
            new() { Rfilename = "config.json" },
            new() { Rfilename = "model.bin" }
        };

        if (includeGguf)
        {
            siblings.Add(new ModelResource { Rfilename = "model.gguf" });
        }

        return new HuggingFaceModel
        {
            ID = id,
            ModelId = id,
            Author = "test-author",
            Downloads = 1000,
            Likes = 100,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            LastModified = DateTime.UtcNow,
            Siblings = siblings.ToArray()
        };
    }

}