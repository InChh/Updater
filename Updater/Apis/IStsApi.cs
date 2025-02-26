using System.Threading.Tasks;
using Updater.Models;
using WebApiClientCore.Attributes;

namespace Updater.Apis;

[LoggingFilter]
public interface IStsApi
{
    [HttpGet("sts/download")]
    Task<Sts> GetStsAsync();
}