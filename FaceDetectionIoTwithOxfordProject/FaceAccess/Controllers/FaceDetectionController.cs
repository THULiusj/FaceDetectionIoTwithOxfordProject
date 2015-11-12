using FaceAccess.Context;
using FaceAccess.Models;
using Microsoft.Azure.NotificationHubs;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace FaceAccess.Controllers
{
    public class FaceDetectionController : ApiController
    {
        private MemberInfoContext db = new MemberInfoContext();

        [HttpPost]
        public async Task<string[]> FaceUpload(string DeviceId)
        {
            Stream req = null;
            req = await Request.Content.ReadAsStreamAsync();
            byte[] bytes = null;
            MemoryStream ms = new MemoryStream();
            int count = 0;
            do
            {
                byte[] buf = new byte[1024];
                count = req.Read(buf, 0, 1024);
                ms.Write(buf, 0, count);
            } while (req.CanRead && count > 0);
            bytes = ms.ToArray();
            Stream stream = new MemoryStream(bytes);
            FaceServiceClient faceclient = new FaceServiceClient(ConfigurationManager.AppSettings["OxfordSubscriptionKeyPrimary"]);
            Face[] faceresult = null;
            try
            {
                faceresult = await faceclient.DetectAsync(stream, false, false, false, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            if (faceresult.Length == 0)
            {
                return new string[]{"Invalid"};
            }
            Guid[] FaceIdSet = new Guid[faceresult.Length];
            for (int i = 0; i < faceresult.Length; i ++)
            {
                FaceIdSet[i] = faceresult[i].FaceId;
            }
            IdentifyResult[] identityresultnew = await faceclient.IdentifyAsync(ConfigurationManager.AppSettings["MemberGroupId"], FaceIdSet, 1);
            string IdentifyResultName = null;
            string[] IdentifyResultJson = new String[identityresultnew.Length];
            int StrangerNum = 0;
            for (int j = 0; j < identityresultnew.Length; j++)
            {
                if (identityresultnew[j].Candidates.Length == 0)
                {
                    IdentifyResultJson[j] = "Stranger";
                    StrangerNum ++;
                }
                else
                {
                    string candidateid = identityresultnew[j].Candidates[0].PersonId.ToString();
                    Person candidate = await faceclient.GetPersonAsync(ConfigurationManager.AppSettings["MemberGroupId"], new Guid(candidateid));
                    IdentifyResultName += candidate.Name + "_";
                    IdentifyResultJson[j] = candidate.Name;
                }
            }
            DateTime temp = DateTime.Now;
            string ImageNameDate = temp.Year.ToString() + "Y" + temp.Month.ToString() + "M" + temp.Day.ToString() + "D" + temp.Hour.ToString() + "h" + temp.Minute.ToString() + "m" + temp.Second.ToString() + "s";
            string ImagePath = await StorageUpload("visitorcapture", ImageNameDate + "_" + IdentifyResultName + StrangerNum.ToString() + "Strangers", bytes);
            
            return IdentifyResultJson;

        }
        public async Task<string> StorageUpload(string ContainerName, string ImageName, byte[] bytes)
        {
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudBlobClient blobStorage = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobStorage.GetContainerReference(ContainerName);
            container.CreateIfNotExists();
            var permissions = container.GetPermissions();
            permissions.PublicAccess = BlobContainerPublicAccessType.Container;
            container.SetPermissions(permissions);
            string date = DateTime.Now.ToString();
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(ImageName + ".jpg");
            blockBlob.Properties.ContentType = "jpg";
            await blockBlob.UploadFromByteArrayAsync(bytes, 0, bytes.Length);

            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5);
            sasConstraints.SharedAccessExpiryTime = DateTime.UtcNow.AddHours(120);
            sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Delete;
            string sasBlobToken = blockBlob.GetSharedAccessSignature(sasConstraints);
            return blockBlob.Uri + sasBlobToken;
        }        
    }
}
