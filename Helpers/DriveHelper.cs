using CommunityToolkit.Mvvm.Messaging;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Discovery.v1;
using Google.Apis.Discovery.v1.Data;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Http;
using Google.Apis.Services;
using POIViewerMap.DataClasses;
using System.Globalization;

namespace POIViewerMap.Helpers;

class DriveHelper
{
    private static IConfigurableHttpClientInitializer credential;

    public static async Task DiscoverAPIs()
    {
        // Create the service.
        var service = new DiscoveryService(new BaseClientService.Initializer
        {
            ApplicationName = "Discovery Sample",
            ApiKey = "AIzaSyBbmQPzqdEaOGIcxvak_77gnmI3WeFYU5Y",
        });

        // Run the request.
        Console.WriteLine("Executing a list request...");
        var result = await service.Apis.List().ExecuteAsync();

        // Display the results.
        if (result.Items != null)
        {
            foreach (DirectoryList.ItemsData api in result.Items)
            {
                Console.WriteLine(api.Id + " - " + api.Title);
                if (api.Id.Equals("drive:v3"))
                    break;
            }
        }
    }
    public static async Task DriveVersion()
    {
        var version = Google.Apis.Drive.v3.DriveService.Version;
        
    }
    /// <summary>
    /// Download a Document file in PDF format.
    /// </summary>
    /// <param name="filename">file ID of any workspace document format file.</param>
    /// <returns>byte array stream if successful, null otherwise.</returns>
    public static async Task<MemoryStream> DriveDownloadFile(string filename)
    {
        //string credentialsPath = @"C:\Steved\DriveAPI-Credentials\mbtilesdriveproject-10d8b21ac163.json";
        //string folderId = "1EynZUee7SM4oqZzlnIwySyov4q7Q5N-l";
        try
        {
            /* Load pre-authorized user credentials from the environment.
             TODO(developer) - See https://developers.google.com/identity for 
             guides on implementing OAuth2 for your application. */
            //GoogleCredential credential = GoogleCredential
            //    .GetApplicationDefault()
            //    .CreateScoped(DriveService.Scope.Drive);
            GoogleCredential credential = GoogleCredential.FromJson("{\r\n  \"type\": \"service_account\",\r\n  \"project_id\": \"mbtilesdriveproject\",\r\n  \"private_key_id\": \"10d8b21ac16370329e11ad9e2c936571a3c2d516\",\r\n  \"private_key\": \"-----BEGIN PRIVATE KEY-----\\nMIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQDErRwPKUMe6c4c\\nTdFm7LMuM5qFHwDNFUDnIEWuTw8eKJ7K6VWktkMOky/X5WYDEJeqhnUAsg5pt4qA\\nnzbHcgUUGbu3LboA33NA8m82vAvSU1FiZo6vl0R3p0EnOoyXkHPv0e3vufeGYxNr\\nC/mpsFidq5L6BegNZ82COxVbxrVXMI31fxqNguHuEfH3+76V4ksJub5ttWz/P7MK\\nBD48OreKsm1jODyi4saoTxAEMwWg5hlE+VeTIa4+k47bbrPRcD5wAQZj1CGCZyYz\\nKL0b7EBmERAaX8rsgB0sW54zyFgzyrgDsxPHKkSZcwtyrt+IDs7Fmfe4LVhH82yE\\ncP2U3Ox3AgMBAAECggEAH5FFeq19x31xhpt669E8JUNFHu7N+xzZMP++D29DFu0g\\nJ6NyCqDyfGlleXqpd/52yXkRWI47v/4kreZgLX0Akaxbs9gUDcXPwvHmpdti7lxP\\nKMMbTdWyuJ3Q2FIOdNlLsod4cEziDODkIf9nEDRs1MnQKm8+QsCRfMCs+fEX9Clf\\n04Ls+SkNeBtqnV8wp+87qwZG3ZXd9btdkqIiF6435P2l7+eYyDlpozQ2oNzn1Gsj\\nVlW6AM8f+UQaveflvvDwpIegzggbpA/fbVdOUDMlZmCeJ2BR83D1ZBc+GEohnWEI\\nyejPyWlNAfOYjEs3L3fPZ9L/8WO0uZAV3/sdyQQRGQKBgQDrI9Z2/T5GsPkmLvOk\\nYqvBevt7L/jV/Q0srqc9759pAJM85uK864TK8wy9ghwmP/oUYmEsUwbDOtGtY5M2\\n6bZC7KOWfjgwodNXep3jRUSzN7w6v6nqlfRwcMe6ASKDKn97z8lgYMOz6fsdbGwI\\nN7QmzOnT5Dihm3VpprgmINHgiwKBgQDWH7zTprS8TwL4++zFdLTUzRJGv8BAYQGN\\nMhIZTDvqm2mEHkJBijFs+zs4Ud1rrEtN1fAKk8W4Q0AdueRnWLr9i8dqjRLVsFRA\\nfaEUNd+oZrg2YsEi58Qj9UPd+p5jLQEyvLIyF7FkGBQDvFTQe6ARbqOi7iBrfZNF\\nYJTzSqAVRQKBgQDZFfAVUL4jE5YiO2yy0mnRqdHtLB/mp8Z2/xPmKYkZru9AZcTl\\nN+cUW4nil/GXGq+uDBm8izaOHYqhMnIiW2jqpoBtG0CTHYP5mnmT7kp7zzQXZQXh\\nTuoquOScBBox0JV74B0BvrRPMPCmfIfMmmjW/Y3PEz95bAXdMY+Hm/tH+wKBgAUn\\n2noWJ3/pPx8Toc3XU4tULL57W3uxWkI2FG17gm3RtCa0O9AsLah8HB7tCbHQAjgr\\nkI4QpNAc/mw4z6EVZ9s3BGQyZWOzzTOIUtqTuYDqiED2+8OFZRmgjDPKFwo4STEZ\\njgjavTC1y7WTUU97yozg5xvDNBCig2tGOg/pmhUpAoGBAMnN3W18098XlvD3Bi9V\\nDdU0VDWzMtQl47Ah72TGyZD64aXb2U+NrO54p83bc2c1KATHGh5U3zUriXGidv+C\\nUe6WZwmDQdchHKxIdkyg0MSBqayUlm7cvlbZnyEg83qyv6mjEJCgTFRRVqvdRbxw\\nWsjdvJZSC+XuN0nFwGA2wcy2\\n-----END PRIVATE KEY-----\\n\",\r\n  \"client_email\": \"sdsdevelopmentserviceaccou-304@mbtilesdriveproject.iam.gserviceaccount.com\",\r\n  \"client_id\": \"114776080122144848210\",\r\n  \"auth_uri\": \"https://accounts.google.com/o/oauth2/auth\",\r\n  \"token_uri\": \"https://oauth2.googleapis.com/token\",\r\n  \"auth_provider_x509_cert_url\": \"https://www.googleapis.com/oauth2/v1/certs\",\r\n  \"client_x509_cert_url\": \"https://www.googleapis.com/robot/v1/metadata/x509/sdsdevelopmentserviceaccou-304%40mbtilesdriveproject.iam.gserviceaccount.com\",\r\n  \"universe_domain\": \"googleapis.com\"\r\n}").CreateScoped(new[]
            {
                DriveService.ScopeConstants.DriveFile
            });
            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "POIViewerMap"
            });

            var files = await service.Files.List().ExecuteAsync();
            var fileId = files.Files.Where(n => n.Name == filename).ToList().FirstOrDefault();
            if (fileId != null)
            {
                var request = service.Files.Get(fileId.Id);
                var ms = new MemoryStream();

                // Add a handler which will be notified on progress changes.
                // It will notify on each chunk download and when the
                // download is completed or failed.
                request.MediaDownloader.ProgressChanged +=
                    progress =>
                    {
                        switch (progress.Status)
                        {
                            case DownloadStatus.Downloading:
                                {
                                    //Console.WriteLine(progress.BytesDownloaded);
                                    //WeakReferenceMessenger.Default.Send(new DownloadProgressMessage($"{progress.BytesDownloaded.ToString("#,#", CultureInfo.InvariantCulture)} bytes"));
                                    break;
                                }
                            case DownloadStatus.Completed:
                                {
                                    //WeakReferenceMessenger.Default.Send(new DownloadProgressMessage("Download complete."));
                                    //Console.WriteLine("Download complete.");
                                    break;
                                }
                            case DownloadStatus.Failed:
                                {
                                    //WeakReferenceMessenger.Default.Send(new DownloadProgressMessage("Download failed!."));
                                    break;
                                }
                        }
                    };
                await request.DownloadAsync(ms);
                return ms;
            }
            return null;
        }
        catch (Exception e)
        {
            // TODO(developer) - handle error appropriately
            if (e is AggregateException)
            {
                //Console.WriteLine("Credential Not found");
            }
            else
            {
                throw;
            }
        }
        return null;
    }
    public static async Task<MbTileFilesFetch> DriveListFilesAsync()
    {
        //string credentialsPath = @"C:\Steved\DriveAPI-Credentials\mbtilesdriveproject-10d8b21ac163.json";
        //string folderId = "1EynZUee7SM4oqZzlnIwySyov4q7Q5N-l";
        try
        {
            /* Load pre-authorized user credentials from the environment.
             TODO(developer) - See https://developers.google.com/identity for 
             guides on implementing OAuth2 for your application. */
            //GoogleCredential credential = GoogleCredential
            //    .GetApplicationDefault()
            //    .CreateScoped(DriveService.Scope.Drive);
            GoogleCredential credential;
            credential = GoogleCredential.FromJson("{\r\n  \"type\": \"service_account\",\r\n  \"project_id\": \"mbtilesdriveproject\",\r\n  \"private_key_id\": \"10d8b21ac16370329e11ad9e2c936571a3c2d516\",\r\n  \"private_key\": \"-----BEGIN PRIVATE KEY-----\\nMIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQDErRwPKUMe6c4c\\nTdFm7LMuM5qFHwDNFUDnIEWuTw8eKJ7K6VWktkMOky/X5WYDEJeqhnUAsg5pt4qA\\nnzbHcgUUGbu3LboA33NA8m82vAvSU1FiZo6vl0R3p0EnOoyXkHPv0e3vufeGYxNr\\nC/mpsFidq5L6BegNZ82COxVbxrVXMI31fxqNguHuEfH3+76V4ksJub5ttWz/P7MK\\nBD48OreKsm1jODyi4saoTxAEMwWg5hlE+VeTIa4+k47bbrPRcD5wAQZj1CGCZyYz\\nKL0b7EBmERAaX8rsgB0sW54zyFgzyrgDsxPHKkSZcwtyrt+IDs7Fmfe4LVhH82yE\\ncP2U3Ox3AgMBAAECggEAH5FFeq19x31xhpt669E8JUNFHu7N+xzZMP++D29DFu0g\\nJ6NyCqDyfGlleXqpd/52yXkRWI47v/4kreZgLX0Akaxbs9gUDcXPwvHmpdti7lxP\\nKMMbTdWyuJ3Q2FIOdNlLsod4cEziDODkIf9nEDRs1MnQKm8+QsCRfMCs+fEX9Clf\\n04Ls+SkNeBtqnV8wp+87qwZG3ZXd9btdkqIiF6435P2l7+eYyDlpozQ2oNzn1Gsj\\nVlW6AM8f+UQaveflvvDwpIegzggbpA/fbVdOUDMlZmCeJ2BR83D1ZBc+GEohnWEI\\nyejPyWlNAfOYjEs3L3fPZ9L/8WO0uZAV3/sdyQQRGQKBgQDrI9Z2/T5GsPkmLvOk\\nYqvBevt7L/jV/Q0srqc9759pAJM85uK864TK8wy9ghwmP/oUYmEsUwbDOtGtY5M2\\n6bZC7KOWfjgwodNXep3jRUSzN7w6v6nqlfRwcMe6ASKDKn97z8lgYMOz6fsdbGwI\\nN7QmzOnT5Dihm3VpprgmINHgiwKBgQDWH7zTprS8TwL4++zFdLTUzRJGv8BAYQGN\\nMhIZTDvqm2mEHkJBijFs+zs4Ud1rrEtN1fAKk8W4Q0AdueRnWLr9i8dqjRLVsFRA\\nfaEUNd+oZrg2YsEi58Qj9UPd+p5jLQEyvLIyF7FkGBQDvFTQe6ARbqOi7iBrfZNF\\nYJTzSqAVRQKBgQDZFfAVUL4jE5YiO2yy0mnRqdHtLB/mp8Z2/xPmKYkZru9AZcTl\\nN+cUW4nil/GXGq+uDBm8izaOHYqhMnIiW2jqpoBtG0CTHYP5mnmT7kp7zzQXZQXh\\nTuoquOScBBox0JV74B0BvrRPMPCmfIfMmmjW/Y3PEz95bAXdMY+Hm/tH+wKBgAUn\\n2noWJ3/pPx8Toc3XU4tULL57W3uxWkI2FG17gm3RtCa0O9AsLah8HB7tCbHQAjgr\\nkI4QpNAc/mw4z6EVZ9s3BGQyZWOzzTOIUtqTuYDqiED2+8OFZRmgjDPKFwo4STEZ\\njgjavTC1y7WTUU97yozg5xvDNBCig2tGOg/pmhUpAoGBAMnN3W18098XlvD3Bi9V\\nDdU0VDWzMtQl47Ah72TGyZD64aXb2U+NrO54p83bc2c1KATHGh5U3zUriXGidv+C\\nUe6WZwmDQdchHKxIdkyg0MSBqayUlm7cvlbZnyEg83qyv6mjEJCgTFRRVqvdRbxw\\nWsjdvJZSC+XuN0nFwGA2wcy2\\n-----END PRIVATE KEY-----\\n\",\r\n  \"client_email\": \"sdsdevelopmentserviceaccou-304@mbtilesdriveproject.iam.gserviceaccount.com\",\r\n  \"client_id\": \"114776080122144848210\",\r\n  \"auth_uri\": \"https://accounts.google.com/o/oauth2/auth\",\r\n  \"token_uri\": \"https://oauth2.googleapis.com/token\",\r\n  \"auth_provider_x509_cert_url\": \"https://www.googleapis.com/oauth2/v1/certs\",\r\n  \"client_x509_cert_url\": \"https://www.googleapis.com/robot/v1/metadata/x509/sdsdevelopmentserviceaccou-304%40mbtilesdriveproject.iam.gserviceaccount.com\",\r\n  \"universe_domain\": \"googleapis.com\"\r\n}").CreateScoped(new[]
            {
                DriveService.ScopeConstants.DriveFile
            });
            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Test_MapsuiOfflineApp"
            });

            var files = await service.Files.List().ExecuteAsync();
            var list = new List<string>();
            foreach(var file in files.Files)
            {
                if (file.Name != null)// && file.Name.Substring(2,1).Equals("-"))
                    list.Add(file.Name);
            }
            return new MbTileFilesFetch()
            {
                ErrorMsg = string.Empty,
                Error = false,
                MbTileFiles = [.. list.Select(n => new MbTileFile()
                {
                    Name = n,
                    LastUpdated = DateTime.Now
                })]
            };
        }
        catch (Exception e)
        {
            // TODO(developer) - handle error appropriately
            if (e is AggregateException)
            {
                //Console.WriteLine("Credential Not found");
                return new MbTileFilesFetch()
                {
                    ErrorMsg = $"{e.Message}",
                    Error = true
                };
            }
            else
            {
                //throw;
                return new MbTileFilesFetch()
                {
                    ErrorMsg = $"{e.Message}",
                    Error = true
                };
            }
        }
    }
}
