using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Web.Website.Controllers;

namespace oslavuje.sk.Controllers.Surface;

// ~/umbraco/surface/MojeHesloFormSurface/MojeHesloFormApiKey
public class MojeHesloFormSurfaceController : SurfaceController
{
    public MojeHesloFormSurfaceController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider) : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult MojeHesloFormApiKey()
    {
        var keyPair = new ApiKeyValidator().GetNewKeyPair();
        return Json(keyPair);
    }
}

public class ApiKeyValidator
{
    private readonly string key1 = "ajHsy478$!jds7^hskasdiu&42b";
    private readonly string key2 = "dHGteu4^*@jdskjdUJ738jas)ah";
    private readonly int ticksOffset = 12345;

    public ApiKeyPair GetNewKeyPair()
    {
        DateTime now = DateTime.Now;
        DateTime later = now.AddMilliseconds(ticksOffset);

        ApiKeyPair ret = new ApiKeyPair()
        {
            MainKey = Encrypt(now.Ticks.ToString(), key1),
            SubKey = Encrypt(later.Ticks.ToString(), key2),
        };

        return ret;
    }

    public bool IsValid(string apiKey1, string apiKey2)
    {
        if (string.IsNullOrEmpty(apiKey1) || string.IsNullOrEmpty(apiKey2))
        {
            return false;
        }
        try
        {
            return IsValid(new ApiKeyPair() { MainKey = apiKey1, SubKey = apiKey2 });
        }
        catch
        {
            return false;
        }
    }

    public bool IsValid(ApiKeyPair keyPair)
    {
        string now = Decrypt(keyPair.MainKey ?? string.Empty, key1);
        string later = Decrypt(keyPair.SubKey ?? string.Empty, key2);

        long ticksNow, ticksLater;
        if (!long.TryParse(now, out ticksNow) || !long.TryParse(later, out ticksLater))
        {
            return false;
        }

        DateTime dtNow = new DateTime(ticksNow).AddMilliseconds(ticksOffset);
        DateTime dtLater = new DateTime(ticksLater);
        if (dtNow != dtLater)
        {
            return false;
        }

        if (dtNow < DateTime.Now.AddHours(-5))
        {
            return false;
        }

        return true;
    }

    public string Encrypt(string text, string key)
    {
        using (var md5 = MD5.Create())
        {
            using (var tdes = TripleDES.Create())
            {
                tdes.Key = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
                tdes.Mode = CipherMode.ECB;
                tdes.Padding = PaddingMode.PKCS7;

                using (var transform = tdes.CreateEncryptor())
                {
                    byte[] textBytes = Encoding.UTF8.GetBytes(text);
                    byte[] bytes = transform.TransformFinalBlock(textBytes, 0, textBytes.Length);
                    return Convert.ToBase64String(bytes, 0, bytes.Length);
                }
            }
        }
    }

    public string Decrypt(string cipher, string key)
    {
        using (var md5 = MD5.Create())
        {
            using (var tdes = TripleDES.Create())
            {
                tdes.Key = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
                tdes.Mode = CipherMode.ECB;
                tdes.Padding = PaddingMode.PKCS7;

                using (var transform = tdes.CreateDecryptor())
                {
                    byte[] cipherBytes = Convert.FromBase64String(cipher);
                    byte[] bytes = transform.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(bytes);
                }
            }
        }
    }
}

public class ApiKeyPair
{
    public string? MainKey { get; set; }
    public string? SubKey { get; set; }
}