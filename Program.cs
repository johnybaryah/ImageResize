using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageResize
{
    class Program
    {
        async static Task Main(string[] args)
        {
            var resizer = new ImageResizer(1500, 100L);

            var allFiles = System.IO.Directory.GetFiles(@"C:\original_images\amandeep\");        

            foreach (var file in allFiles)
            {
                var outputStream = resizer.Resize(file, ImageFormat.Tiff);
                using (outputStream)
                using (var fileStream = File.Create($@"C:\imageresize\{Path.GetFileName(file)}"))
                {
                    outputStream.Seek(0, SeekOrigin.Begin);
                    await outputStream.CopyToAsync(fileStream);
                }    
            }

            allFiles.ToList().ForEach(file => File.Delete(file));
        }
    }

    public class ImageResizer
    {
        // assumes defaults
        public ImageResizer(): this(1000, 100L) {}

        public ImageResizer(int size, long quality)
        {
            this.size = size;
            this.quality = quality;    
        }

        private int size;
        private long quality;

        public Stream Resize(string inputPath, ImageFormat format)
        {
            if (format == null)
                throw new ArgumentNullException(nameof(format), "Must specify a format of System.Drawing.Imaging.ImageFormat"); 

            if (string.IsNullOrEmpty(inputPath))
                throw new ArgumentNullException(nameof(inputPath), "input path is null. What do you expect me to resize?");
            using(var img = System.Drawing.Image.FromFile(inputPath))
            using (var image = new Bitmap(img))
            {
                int width, height = 0;
                
                if (image.Width > image.Height)
                {
                    width = size;
                    height = Convert.ToInt32(image.Height * size / (double)image.Width);
                }
                else
                {
                    width = Convert.ToInt32(image.Width * size / (double)image.Height);
                    height = size;
                }

                return DoResizeAndSave(image, width, height, format);
            }
        }

        private Stream DoResizeAndSave(Bitmap image, int width, int height, ImageFormat format)
        {
            var resized = new Bitmap(width, height);

            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.CompositingQuality = CompositingQuality.AssumeLinear;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(image, 0, 0, width, height);
                
                var qualityParamId = Encoder.Quality;
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(qualityParamId, quality);

                var codec = ImageCodecInfo.GetImageDecoders()?.FirstOrDefault<ImageCodecInfo>(c => c.FormatID == format.Guid);

                if (codec == null)
                    throw new InvalidOperationException($"Failed to load {format.ToString()} codec");

                var output = new MemoryStream();
                resized.Save(output, codec, encoderParameters);
                return output;
            }
        }
    }
}
