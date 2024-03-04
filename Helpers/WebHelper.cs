using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using Flurl.Http;
using Microsoft.Maui.Storage;
using POIViewerMap.DataClasses;
using ShimSkiaSharp;
using System.Globalization;
using System.Text;
using System.Threading;

namespace POIViewerMap.Helpers;

class WebHelper
{
    private static readonly string ServerUrl = "https://sdsdevelopment.w5.lt/poidata-server";//"http://192.168.1.182:4000/poidata-server";
    private static readonly string Download = "uploads/france.bin";
    private static readonly string FILES = "client/index.php";
    public static readonly string PARAM_ACTION = "action";
    private static readonly string ACTION_DOWNLOAD = "uploads";

    public static readonly string PARAM_FILE_NAME = "filename";

    // filenames/dates fetch
    public static readonly string ACTION_FILES = "files";

    public static string localPath = string.Empty;

    private string cookieName;
    private string cookieValue;
    private IFlurlResponse response;

    public async Task DownloadPOIFileAsync(string filename)
    {
        await FileDownloadPostForm(filename);
    }
    public async Task<List<FileFetch>> FilenamesFetchAsync(Dictionary<string, string> parameters)
    {
        return await FilenamesFetchPostForm(parameters);
    }
    private async Task<List<FileFetch>> FilenamesFetchPostForm(Dictionary<string, string> keyValuePairs)
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
                await Toast.Make($"File access Failed: - {responseCode}").Show();
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"File access Failed: - {ex.Message}").Show();
        }
        return null;
    }
    private async Task FileDownloadPostForm(string filename)
    {
        string responseCode = string.Empty;
        try
        {
            localPath = Path.Combine(FileSystem.AppDataDirectory, filename.ToLower());
            var dir = Path.GetDirectoryName(localPath);
            Directory.CreateDirectory(FileSystem.AppDataDirectory);
            if (File.Exists(Path.Combine(localPath)))
                File.Delete(Path.Combine(localPath));
            var path = await $"{ServerUrl}/{ACTION_DOWNLOAD}/{filename.ToLower()}"
            .DownloadFileAsync(dir, filename.ToLower());
        }
        catch (Exception ex)
        {
            await Toast.Make($"The file download failed: {filename.ToLower()} - {ex.Message}").Show();
        }
    }
    public List<FileFetch> ParseJsonContent(string content)
    {
        var fileFetch = new List<FileFetch>();
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
                    AddFile(item, ref fileFetch);
                }
            }
        return fileFetch;
    }
    private void AddFile(string file, ref List<FileFetch> list)
    {
        // ["gloucestershire-bicyclerepairstations.binT26\/02\/2024 11:33:27","gloucestershire-bakeries.binT26\/02\/2024 11:27:04","gloucestershire-atms.binT26\/02\/2024 11:26:07","croatia.binT27\/02\/2024 08:10:12","..T27\/02\/2024 08:27:41",".T27\/02\/2024 08:12:59"],"msg":"Success."}
        file = file.Replace("[", string.Empty);
        var fields = file.Split(',');
        //var data = new DataClasses.Device();
        var ff = new FileFetch();
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
                    ff.Name = field.Substring(Idx1 + 1, Idx2 - 1);
                    var ts = field.Substring(Idx2 + 1).Replace("\\", string.Empty).TrimEnd('"');
                    ff.LastUpdated = Convert.ToDateTime(DateTime.ParseExact(ts, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)); ;
                }
            }
            list.Add(ff);
        }
    }
}
