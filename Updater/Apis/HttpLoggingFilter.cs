using System.Threading.Tasks;
using Serilog;
using WebApiClientCore;

namespace Updater.Apis;

public class HttpLoggingFilter : IApiFilter
{
    public Task OnRequestAsync(ApiRequestContext context)
    {
        Log.Debug("Request: {Method} {Uri}", context.HttpContext.RequestMessage.Method,
            context.HttpContext.RequestMessage.RequestUri);
        return Task.CompletedTask;
    }

    public Task OnResponseAsync(ApiResponseContext context)
    {
        Log.Debug("Response: {StatusCode} {ReasonPhrase} Content: {Content}",
            context.HttpContext.ResponseMessage?.StatusCode,
            context.HttpContext.ResponseMessage?.ReasonPhrase,
            context.HttpContext.ResponseMessage?.Content);
        return Task.CompletedTask;
    }
}