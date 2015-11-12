using FaceAccess.Context;
using FaceAccess.Models;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace FaceAccess.Controllers
{
    public class MemberMngController : ApiController
    {
        FaceServiceClient faceclient = new FaceServiceClient(ConfigurationManager.AppSettings["OxfordSubscriptionKeyPrimary"]);
        private MemberInfoContext db = new MemberInfoContext();
        [HttpPost]
        public async Task<string> MemberCreate(string LastName, string FirstName, string Permission)
        {
            CreatePersonResult membertemp = await faceclient.CreatePersonAsync(ConfigurationManager.AppSettings["MemberGroupId"], null, LastName + " " + FirstName, Permission);
            MemberInfo entity = new MemberInfo();
            entity.LastName = LastName;
            entity.FirstName = FirstName;
            entity.MemberId = membertemp.PersonId;
            entity.Permission = Permission;
            db.MemberInfos.Add(entity);
            db.SaveChanges();
            return membertemp.PersonId.ToString();
        }
        [HttpPut]
        public async Task<string> MemberPictureUpload(string MemberId)
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

            Face[] faceresult = await faceclient.DetectAsync(stream, false, false, false, false);
            await faceclient.AddPersonFaceAsync(ConfigurationManager.AppSettings["MemberGroupId"], new Guid(MemberId), faceresult[0].FaceId);
            string bloburi = await StorageUpload("memberpic", MemberId, bytes);
            return bloburi;
        }

        [HttpDelete]
        public async Task MemberDelete(string MemberId)
        {
            try
            {
                await faceclient.DeletePersonAsync(ConfigurationManager.AppSettings["MemberGroupId"], new Guid(MemberId));
            }
            catch(Exception ex)
            { }
            MemberInfo memberresult = db.MemberInfos.Find(new Guid(MemberId));
            db.MemberInfos.Remove(memberresult);
            db.SaveChanges();
            return;
        }

        [HttpGet]
        public string MemberList()
        {
            MemberInfo[] memberlist = db.MemberInfos.ToArray<MemberInfo>();
            string json = new JavaScriptSerializer().Serialize(memberlist);
            return json;
        }
        [HttpPatch]
        public async Task MemberTrain()
        {
            await faceclient.TrainPersonGroupAsync(ConfigurationManager.AppSettings["MemberGroupId"]);
            return;
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
            CloudBlockBlob blockBlob = null;
            for (int i = 0; ; i++)
            {
                blockBlob = container.GetBlockBlobReference(ImageName + "_" + i.ToString() + ".jpg");
                if (blockBlob.Exists())
                    continue;
                else
                    break;
            }
            blockBlob.Properties.ContentType = "jpg";
            await blockBlob.UploadFromByteArrayAsync(bytes, 0, bytes.Length);
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5);
            sasConstraints.SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24);
            sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Delete;
            string sasBlobToken = blockBlob.GetSharedAccessSignature(sasConstraints);
            return blockBlob.Uri + sasBlobToken;
        }
    }
}
