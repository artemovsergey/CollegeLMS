using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Services;

public class StvccHealthService(IHttpClientFactory httpClientFactory, IConfiguration config)
    : IStvccHealthService
{
    public async Task<Result<StvccHealthDto>> CheckAsync(CancellationToken ct)
    {
        var baseUrl = config.GetValue<string>("WordPress:BaseUrl") ?? "https://stvcc.ru";
        var http = httpClientFactory.CreateClient("stvcc");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl);
            using var response = await http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct
            );
            return Result<StvccHealthDto>.Ok(new StvccHealthDto { Available = true });
        }
        catch (OperationCanceledException)
        {
            return Result<StvccHealthDto>.Ok(new StvccHealthDto { Available = false });
        }
        catch (HttpRequestException)
        {
            return Result<StvccHealthDto>.Ok(new StvccHealthDto { Available = false });
        }
    }
}
