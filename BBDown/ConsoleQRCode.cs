using QRCoder;
using System;

namespace BBDown
{
    public class ConsoleQRCode : AbstractQRCode
	{
		public ConsoleQRCode() { }

		public ConsoleQRCode(QRCodeData data) : base(data) { }

        public void GetGraphic() => GetGraphic(ConsoleColor.Black, ConsoleColor.White);

        public void GetGraphic(ConsoleColor darkColor, ConsoleColor lightColor)
        {
            var previousBackColor = Console.BackgroundColor;
            var previousForeColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            for (int y = 0; y < QrCodeData.ModuleMatrix.Count; y++)
            {
                for (int x = 0; x < QrCodeData.ModuleMatrix[y].Count; x++)
                {
                    Console.ForegroundColor = QrCodeData.ModuleMatrix[y][x] ? darkColor : lightColor;
                    Console.Write("██");
                }
                Console.BackgroundColor = darkColor;
                Console.WriteLine("");
            }
            Console.BackgroundColor = previousBackColor;
            Console.ForegroundColor = previousForeColor;
        }
    }
}
