using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Text.Json;

namespace FontConverter
{
    struct GlyphInfoJson
    {
        public char Glyph { get; set; }
        public Dictionary<string, int> Kerning { get; set; }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("FONTCONVERTER stx_file font_file");
                return;
            }

            var unpackProcess = Process.Start("HTFont.exe", "--unpack " + args[0]);
            unpackProcess.WaitForExit();
            var directory = args[0] + ".decompressed_font";
            string text = File.ReadAllText("characters.txt");
            int nameLenght = text.Length.ToString().Length;
            int chr_id = 0;
            PixelFormat format = System.Drawing.Imaging.PixelFormat.Format32bppRgb;
            SizeF size;
            PrivateFontCollection fontCollection = new PrivateFontCollection();
            fontCollection.AddFontFile(args[1]);
            Font font = new Font(fontCollection.Families[0], 96f, GraphicsUnit.Pixel);

            var di = new DirectoryInfo(directory);
            foreach (FileInfo file in di.GetFiles())
            {
                if(file.Name != "__font_info.json")
                {
                    file.Delete();
                }
            }

            using (var bitmap = new Bitmap(33, 98, format))
            {
                string name = directory + @"\" + chr_id.ToString().PadLeft(nameLenght, '0');
                bitmap.Save(name + ".bmp", ImageFormat.Bmp);

                GlyphInfoJson glyph = new GlyphInfoJson();
                glyph.Glyph = ' ';
                glyph.Kerning = new()
                {
                    {"Left", 0},
                    {"Right", 0},
                    {"Vertical", 0}
                };

                string json = JsonSerializer.Serialize(glyph);
                File.WriteAllText(name + ".json", json);

                chr_id++;
            }

            foreach (var item in text)
            {
                string chr = item.ToString();

                using (var bitmap = new Bitmap(1, 1, format))
                {
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        size = graphics.MeasureString(chr, font);
                    }
                }

                using (var bitmap = new Bitmap((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height), format))
                {
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.DrawString(chr, font, Brushes.White, 0, 0);
                    }

                    int topX = 0, bottomX = 0, topY = 0, bottomY = 0;
                    int width = bitmap.Width - 1, height = bitmap.Height - 1;

                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            var color = bitmap.GetPixel(x, y);
                            if (color.R != 0)
                            {
                                topX = x;
                                break;
                            }
                        }
                        if (topX != 0)
                        {
                            break;
                        }
                    }

                    for (int x = width; x > topX; x--)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            var color = bitmap.GetPixel(x, y);
                            if (color.R != 0)
                            {
                                bottomX = x;
                                break;
                            }
                        }
                        if (bottomX != 0)
                        {
                            break;
                        }
                    }

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var color = bitmap.GetPixel(x, y);
                            if (color.R != 0)
                            {
                                topY = y;
                                break;
                            }
                        }
                        if (topY != 0)
                        {
                            break;
                        }
                    }

                    for (int y = height; y > topY; y--)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var color = bitmap.GetPixel(x, y);
                            if (color.R != 0)
                            {
                                bottomY = y;
                                break;
                            }
                        }
                        if (bottomY != 0)
                        {
                            break;
                        }
                    }

                    string name = directory + @"\" + chr_id.ToString().PadLeft(nameLenght, '0');

                    GlyphInfoJson glyph = new GlyphInfoJson();
                    glyph.Glyph = chr[0];
                    glyph.Kerning = new()
                    {
                        {"Left", topX - 17},
                        {"Right", width - bottomX - 17},
                        {"Vertical", topY - 18 }
                    };

                    string json = JsonSerializer.Serialize(glyph);
                    File.WriteAllText(name + ".json", json);

                    Bitmap croppedImage = new Bitmap(bitmap);
                    Rectangle cropRect = new Rectangle(topX, topY, bottomX - topX + 1, bottomY - topY + 1);
                    croppedImage = croppedImage.Clone(cropRect, format);
                    croppedImage.Save(name + ".bmp", ImageFormat.Bmp);
                    chr_id++;
                }
            }

            var packProcess = Process.Start("HTFont.exe", "--pack " + directory);
            unpackProcess.WaitForExit();
        }
    }
}