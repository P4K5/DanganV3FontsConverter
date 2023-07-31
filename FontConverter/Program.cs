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
            Console.Write("Type the path to the font file: ");
            String fontFileDirectory = Console.ReadLine();
            //String fontFileDirectory = @"C:\Users\patry\Desktop\test\plcon\game_font01_6_US.spc.decompressed\v3_font01_6.ttf";
            String fontFileName = Path.GetFileName(fontFileDirectory);
            String directoryPath = Path.GetDirectoryName(fontFileDirectory) + @"/";

            String stxFileName = fontFileName.Split('.')[0] + ".stx";
            String srdvFileName = fontFileName.Split('.')[0] + ".srdv";

            String charsetFileName = directoryPath + "charset.txt";
            String HTPath = File.ReadAllText(directoryPath + "HTpath.txt");

            String tempDirectory = Path.GetTempPath() + @"DanganV3FontsConverter\";

            Directory.CreateDirectory(tempDirectory);

            File.Copy(directoryPath + stxFileName, tempDirectory + stxFileName, true);
            File.Copy(directoryPath + srdvFileName, tempDirectory + srdvFileName, true);


            var unpackProcess = Process.Start(HTPath, "font extract -f " + tempDirectory + stxFileName);
            unpackProcess.WaitForExit();
            var decompressedFontDirectory = tempDirectory + stxFileName + ".decompressed_font";

            bool smallFont = false;
            var fontFiles = Directory.GetFiles(decompressedFontDirectory);
            foreach (var fontFile in fontFiles)
            {
                if (Path.GetFileName(fontFile).Contains(".json"))
                {
                    string jsonData = File.ReadAllText(fontFile);
                    var fontData = JsonSerializer.Deserialize<GlyphInfoJson>(jsonData);
                    if (fontData.Glyph == 'A')
                    {
                        using (var AImg = System.Drawing.Image.FromFile(fontFile.Replace(".json", ".bmp")))
                        {
                            smallFont = AImg.Height < 50;
                            break;
                        }
                    }
                }
            }

            float fontSize = smallFont ? 88f : 192f;

            string text = File.ReadAllText(directoryPath + "charset.txt");
            int nameLenght = text.Length.ToString().Length;
            int chr_id = 0;
            PixelFormat format = System.Drawing.Imaging.PixelFormat.Format32bppRgb;
            SizeF size;
            PrivateFontCollection fontCollection = new PrivateFontCollection();
            fontCollection.AddFontFile(fontFileDirectory);
            Font font = new Font(fontCollection.Families[0], fontSize, GraphicsUnit.Pixel);

            var di = new DirectoryInfo(decompressedFontDirectory);
            foreach (FileInfo file in di.GetFiles())
            {
                if (file.Name != "__font_info.json")
                {
                    file.Delete();
                }
            }

            using (var bitmap = (smallFont) ? new Bitmap(16, 47, format) : new Bitmap(33, 98, format))
            {
                string name = decompressedFontDirectory + @"\" + chr_id.ToString().PadLeft(nameLenght, '0');
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

                using (var bitmap192 = new Bitmap((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height), format))
                {
                    using (Graphics graphics = Graphics.FromImage(bitmap192))
                    {
                        graphics.DrawString(chr, font, Brushes.White, 0, 0);
                    }
                    using (var bitmap = new Bitmap(bitmap192, bitmap192.Width / 2, bitmap192.Height / 2))
                    {
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

                        string name = decompressedFontDirectory + @"\" + chr_id.ToString().PadLeft(nameLenght, '0');

                        GlyphInfoJson glyph = new GlyphInfoJson();
                        glyph.Glyph = chr[0];
                        if (smallFont)
                        {
                            glyph.Kerning = new()
                            {
                                {"Left", topX - 8},
                                {"Right", width - bottomX - 7},
                                {"Vertical", topY - 11 }
                            };
                        }
                        else
                        {
                            glyph.Kerning = new()
                            {
                                {"Left", topX - 17},
                                {"Right", width - bottomX - 17},
                                {"Vertical", topY - 18 }
                            };
                        }

                        string json = JsonSerializer.Serialize(glyph);
                        File.WriteAllText(name + ".json", json);

                        Bitmap croppedImage = new Bitmap(bitmap);
                        Rectangle cropRect = new Rectangle(topX, topY, bottomX - topX + 1, bottomY - topY + 1);
                        croppedImage = croppedImage.Clone(cropRect, format);
                        croppedImage.Save(name + ".bmp", ImageFormat.Bmp);
                        chr_id++;
                    }
                }
            }

            var packProcess = Process.Start(HTPath, "font pack -f" + decompressedFontDirectory);
            packProcess.WaitForExit();

            File.Copy(tempDirectory + stxFileName, directoryPath + stxFileName, true);
            File.Copy(tempDirectory + srdvFileName, directoryPath + srdvFileName, true);

            Directory.Delete(tempDirectory, true);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}