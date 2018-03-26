using Gallery.BAL.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gallery.BAL.Services
{
    public class LocalFileService : IFileService
    {
        public void DeleteAllFile(long userId)
        {
            throw new NotImplementedException();
        }

        public void DeleteFile(string filePath, long userId)
        {
            throw new NotImplementedException();
        }

        public string UploadFile(Stream stream, string fileName, long userId)
        {
            var  folder = userId.ToString() + @"/";

            var appPath = System.AppDomain.CurrentDomain.BaseDirectory + @"images\\" + userId;

           // DirectoryInfo di = Directory.CreateDirectory(appPath);

            var directory = new DirectoryInfo(appPath);

            if (directory.Exists == false)
            {
                directory.Create();
            }
            else
            {

            }
            


           // upload.SaveAs(Server.MapPath("~/Files/" + fileName));
           //var file = Path.Combine(stream, fileName, userId);

            // upload.SaveAs(file);



            return  "";
            //var b = cloudContainer.GetBlockBlobReference(folder + fileName);
            //if (b.Exists())
            //{
            //    return "";
            //}
            //else
            //{
            //    b = cloudContainer.GetBlockBlobReference(folder + fileName);
            //}
        }
    }
}
