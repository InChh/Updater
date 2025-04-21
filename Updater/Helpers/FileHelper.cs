using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Updater.Consts;
using Updater.Models;

namespace Updater.Helpers;

public static partial class FileHelper
{
    // 对安全性没有要求，仅用于hash文本
    private static readonly byte[] Key = Convert.FromBase64String("mxfe4ogvlykyBFKJWoDpQdv3wlYlwJ8Poubtx5l7zDU=");
    private static readonly byte[] Iv = Convert.FromBase64String("mR7tWwMqX7szeqUwlhyc2g==");

    //
    // [GeneratedRegex(".*logs[\\/].*UpdaterLog.*")]
    // private static partial Regex LogRegex();
    public static async Task<List<FileMetadata>> GetLocalFiles(string rootPath)
    {
        var files = new List<FileMetadata>();
        var allFiles = Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories);

        foreach (var file in allFiles)
        {
            if (file.Contains(FileNameConsts.TempFolderName))
            {
                continue;
            }

            var regex = new Regex(@".*logs[\\/].*UpdaterLog.*");
            if (regex.IsMatch(file))
            {
                continue;
            }

            if (file.Contains("acad.dat") || file.Contains("workdir"))
            {
                continue;
            }

            var fileMetadata = new FileMetadata
            {
                Path = MakeRelativePath(rootPath, file),
                Size = new FileInfo(file).Length,
                Hash = await ComputeHashAsync(file),
            };
            files.Add(fileMetadata);
        }

        return files;
    }

    private static string MakeRelativePath(string rootPath, string fullPath)
    {
        var uri = new Uri(rootPath + Path.DirectorySeparatorChar);
        var relativeUri = uri.MakeRelativeUri(new Uri(fullPath));
        return Uri.UnescapeDataString(relativeUri.ToString());
    }

    private static async Task<string> ComputeHashAsync(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexStringLower(hash);
    }

    public static async Task<Manifest?> ReadManifestAsync()
    {
        var basePath = Directory.GetCurrentDirectory();
        var configPath = Path.Combine(basePath, FileNameConsts.ManifestFileName);
        if (!File.Exists(configPath))
        {
            return null;
        }

        var encryptedText = await File.ReadAllTextAsync(configPath);
        var encryptedData = Convert.FromBase64String(encryptedText);
        var jsonString = await DecryptStringAsync(encryptedData);
        return JsonSerializer.Deserialize(jsonString, typeof(Manifest), new AppJsonSerializerContext()) as Manifest;
    }

    public static async Task WriteManifestAsync(Manifest manifest)
    {
        var basePath = Directory.GetCurrentDirectory();
        var configPath = Path.Combine(basePath, FileNameConsts.ManifestFileName);
        var jsonString = JsonSerializer.Serialize(manifest, typeof(Manifest), new AppJsonSerializerContext());
        var encryptedData = await EncryptStringAsync(jsonString);
        await File.WriteAllTextAsync(configPath, Convert.ToBase64String(encryptedData));
    }

    public static async Task<Config?> ReadConfigAsync()
    {
        var basePath = Directory.GetCurrentDirectory();
        var configPath = Path.Combine(basePath, FileNameConsts.ConfigFileName);
        if (!File.Exists(configPath))
        {
            return null;
        }

        var jsonString = await File.ReadAllTextAsync(configPath);
        return JsonSerializer.Deserialize(jsonString, typeof(Config), new AppJsonSerializerContext()) as Config;
    }

    private static async Task<byte[]> EncryptStringAsync(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = Iv;
        aes.Padding = PaddingMode.PKCS7;

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var msEncrypt = new MemoryStream();
        await using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        await using var swEncrypt = new StreamWriter(csEncrypt);

        await swEncrypt.WriteAsync(plainText);
        await swEncrypt.FlushAsync();
        await csEncrypt.FlushFinalBlockAsync();

        return msEncrypt.ToArray();
    }

    private static async Task<string> DecryptStringAsync(byte[] cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = Iv;
        aes.Padding = PaddingMode.PKCS7;

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var msDecrypt = new MemoryStream(cipherText);
        await using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);

        return await srDecrypt.ReadToEndAsync();
    }

    // 格式化字节大小
    public static string FormatBytes(long bytes)
    {
        string[] suf = ["B", "KB", "MB", "GB"];
        var i = 0;
        double dbl = bytes;
        while (dbl >= 1024 && i < suf.Length - 1)
        {
            dbl /= 1024;
            i++;
        }

        return $"{dbl:F2} {suf[i]}";
    }
}