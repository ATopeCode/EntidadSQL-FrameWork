using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace GestionSql
{
    public class ConvertImage
    {
        public static Image Byte2Image(Byte[] aImg)
        {
            if (aImg == null)
            {
                return null;
            }

            Image img = null;
            MemoryStream ms = new MemoryStream(aImg);
            img = Image.FromStream(ms);
            return img;
        }

        public static byte[] Image2Byte(Image img)
        {
            if (img == null)
            {
                return null;
            }

            Byte[] ret = null;

            MemoryStream ms = new MemoryStream();
            {     
                img.Save(ms,System.Drawing.Imaging.ImageFormat.Jpeg);
                //img.Save(ms, img.RawFormat);
                ret = ms.ToArray();
                ms.Close();
            }
            return ret;
        }
    }
}
