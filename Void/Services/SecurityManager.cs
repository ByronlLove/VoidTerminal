using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using VoidTerminal.Models;

namespace VoidTerminal.Services;

public static class SecurityManager
{
    private static readonly byte[] Salt = System.Text.Encoding.UTF8.GetBytes("void_genome_salt_secure");
    private static string DataPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.void");

    public static bool DatabaseExists() => File.Exists(DataPath);

    public static void ImportExternalDictionary(VoidData data, string filePath)
    {
        if (!File.Exists(filePath)) return;
        var lines = File.ReadAllLines(filePath);
        foreach (var word in lines)
        {
            string clean = word.Trim().ToLower();
            if (!string.IsNullOrEmpty(clean)) data.Dictionary.Add(clean);
        }
    }

    public static bool SaveSecureData(VoidData data, string password)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            var encrypted = Encrypt(json, password);
            File.WriteAllBytes(DataPath, encrypted);
            return true;
        }
        catch { return false; }
    }

    public static (bool success, VoidData? data) LoadSecureData(string password)
    {
        if (!File.Exists(DataPath)) return (false, null);
        try
        {
            var encrypted = File.ReadAllBytes(DataPath);
            var decryptedJson = Decrypt(encrypted, password);
            var data = JsonSerializer.Deserialize<VoidData>(decryptedJson);
            return (true, data);
        }
        catch { return (false, null); }
    }

    private static byte[] GetKey(string password)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, Salt, 100000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32);
    }

    private static byte[] Encrypt(string plainText, string password)
    {
        using var aes = Aes.Create();
        aes.Key = GetKey(password);
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs)) { sw.Write(plainText); }
        return ms.ToArray();
    }

    private static string Decrypt(byte[] cipherText, string password)
    {
        using var aes = Aes.Create();
        aes.Key = GetKey(password);
        byte[] iv = new byte[16];
        Array.Copy(cipherText, 0, iv, 0, 16);
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(cipherText, 16, cipherText.Length - 16);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}