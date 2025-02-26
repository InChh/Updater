using System;
using System.Threading.Tasks;
using Aliyun.OSS;
using Aliyun.OSS.Common;
using Aliyun.OSS.Common.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Updater.Apis;
using Updater.Consts;
using Updater.Models;

namespace Updater.Helpers;

public static class CacheHelper
{
    public static async Task<OssClient> GetOssClient(this IMemoryCache cache, Config config)
    {
       return (await cache.GetOrCreateAsync(CacheKeyConsts.OssClient, async entry =>
       {
           var stsApi = App.Services.GetRequiredService<IStsApi>();
           var sts = await stsApi.GetStsAsync();
           var credentialsProvider =
               new DefaultCredentialsProvider(new DefaultCredentials(sts.AccessKeyId, sts.AccessKeySecret,
                   sts.SecurityToken));

           var conf = new ClientConfiguration();

           var client = new OssClient(config.OssEndpoint, credentialsProvider, conf);
           entry.Value = client;
           entry.SlidingExpiration = TimeSpan.FromMinutes(30);
           return client;
       }))!;
    }
}