using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using Flurl.Http;
using Microsoft.Maui.Storage;
using POIViewerMap.DataClasses;
using POIViewerMap.Resources.Strings;
using ShimSkiaSharp;
using System.Globalization;
using System.Text;
using System.Threading;

namespace POIViewerMap.Helpers;

class WebHelper
{
    private static readonly string ServerUrl = "https://sdsdevelopment.w5.lt/poidata-server";// "http://192.168.1.182:4000/poidata-server";
    private static readonly string FILES = "client/index.php";
    public static readonly string PARAM_ACTION = "action";
    private static readonly string ACTION_DOWNLOAD = "uploads";

    public static readonly string PARAM_FILE_NAME = "filename";

    // filenames/dates fetch
    public static readonly string ACTION_FILES = "files";

    public static string localPath = string.Empty;

    private IFlurlResponse response;

    public async Task DownloadPOIFileAsync(string filename)
    {
        await FileDownloadPostForm(filename);
    }
    public async Task<FileFetch> FilenamesFetchAsync(Dictionary<string, string> parameters)
    {
        return await FilenamesFetchPostForm(parameters);
    }
    private async Task<FileFetch> FilenamesFetchPostForm(Dictionary<string, string> keyValuePairs)
    {
        string responseCode = string.Empty;
        try
        {
            response = await $"{ServerUrl}/{FILES}".WithTimeout(30).PostMultipartAsync(mp => mp
            .AddString("action", keyValuePairs[PARAM_ACTION])
            .AddString(PARAM_FILE_NAME, keyValuePairs[PARAM_FILE_NAME])
            );
            if (response.StatusCode == 200)
            {
                var respContent = await response.ResponseMessage.Content.ReadAsStringAsync();
                return ParseJsonContent(respContent);
            }
            else
            {
                // handle errors
                responseCode = await response.ResponseMessage.Content.ReadAsStringAsync();
                return new FileFetch()
                {
                    ErrorMsg = $"{AppResource.ServerAccessFailedMsg} {responseCode}",
                    Error = true
                };
            }
        }
        catch (Exception ex)
        {
            return new FileFetch()
            {
                ErrorMsg = $"{AppResource.ServerAccessFailedMsg} {responseCode}",
                Error = true
            };
        }
    }
    private async Task FileDownloadPostForm(string filename)
    {
        string responseCode = string.Empty;
        try
        {
            var name = $"{FilenameHelper.GetCountryCodeFromTranslatedCountry(Path.GetFileNameWithoutExtension(filename))}.bin";
            localPath = Path.Combine(FileSystem.AppDataDirectory, name);
            var dir = Path.GetDirectoryName(localPath);
            Directory.CreateDirectory(FileSystem.AppDataDirectory);
            if (File.Exists(Path.Combine(localPath)))
                File.Delete(Path.Combine(localPath));
            var path = await $"{ServerUrl}/{ACTION_DOWNLOAD}/{name}"
             .DownloadFileAsync(dir, $"{name}");
        }
        catch (Exception ex)
        {
            await Toast.Make($"The file download failed: {filename} - {ex.Message}").Show();
        }
    }
    //private string HandleError(string error) TODO
    //{
    //    var result = string.Empty;
    //    if (error.ToLower().Contains("timed out"))
    //    {
    //        var url = this.appSettings.ServerUrl.ToLower().Replace("http://", string.Empty);
    //        result = $"{AppResource.WebConnectionErrorMsg} {AppResource.WebConnectionErrorInformMsg}{url}";
    //    }
    //    else if (error.ToLower().Contains("unauthorized") || error.Contains("401"))
    //    {
    //        result = $"{AppResource.WebAuthorizationErrorMsg} 401";
    //    }
    //    else if (error.Equals("Server error"))
    //    {
    //        result = AppResource.WebConnectionServerResponseErrorMsg;
    //    }
    //    else if (error.ToLower().Contains("an invalid request uri was provided"))
    //    {
    //        result = $"{AppResource.WebConnectionErrorMsg} {error}";
    //    }
    //    else if (error.ToLower().Contains("http error code: 503"))
    //    {
    //        result = $"{AppResource.WebConnectionErrorMsg} {AppResource.WebConnectionErrorInformMsg} {error}";
    //    }
    //    else
    //    {
    //        var url = string.Empty;
    //        if (!string.IsNullOrEmpty(this.appSettings.ServerUrl))
    //            url = this.appSettings.ServerUrl.ToLower().Replace("http://", string.Empty);
    //        result = $"{AppResource.WebConnectionErrorMsg} {AppResource.WebConnectionErrorInformMsg}{url}";
    //    }

    //    return result;
    //}
    public FileFetch ParseJsonContent(string content)
    {
        var ff = new FileFetch();
        byte[] byteArray = Encoding.ASCII.GetBytes(content);
        var stream = new MemoryStream(byteArray);

        // convert stream to string
        using (var sr = new StreamReader(stream))
            while (!sr.EndOfStream)
            {
                var line = sr.ReadToEnd();
                var Idx = line.IndexOf("\"data\":");
                if (Idx < 0)
                    continue;
                line = line.Replace("\"data\":", string.Empty);
                line = line[Idx..];
                var files = line.Split(",");
                foreach (var item in files)
                {
                    AddFile(item, ref ff);
                }
            }
        return ff;
    }
    private void AddFile(string file, ref FileFetch ff)
    {
        file = file.Replace("[", string.Empty);
        var fields = file.Split(',');
        foreach (var field in fields)
        {
            if (field.ToLower().Contains("msg") || field.StartsWith('.') || field.StartsWith(".."))
                continue;
            if (field.ToLower().Contains(".bin"))
            {
                var Idx1 = field.IndexOf("\"");
                if (Idx1 > -1)
                {
                    var Idx2 = field.IndexOf("T");
                    ff.Names.Add(field.Substring(Idx1 + 1, Idx2 - 1));
                    var ts = field.Substring(Idx2 + 1).Replace("\\", string.Empty).TrimEnd(']').TrimEnd('"');
                    ff.LastUpdated = Convert.ToDateTime(DateTime.ParseExact(ts, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture));
                }
            }
        }
    }
}
