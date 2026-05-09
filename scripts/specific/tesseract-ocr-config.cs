return 0;

namespace Scripts
{
    internal static class TesseractOcrConfig
    {
        public const string Dir = @"C:\Users\user1\AppData\Local\Temp\wolfs-frames-extracted";
        public const string Pattern = "scene-*.png";
        public const string OutDir = @"C:\Users\user1\AppData\Local\Temp\wolfs-ocr";
        public const string TesseractExe = @"C:\Program Files\Tesseract-OCR\tesseract.exe";
    }
}
