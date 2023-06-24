using System;

namespace Bot
{
    class Program
    {
        static void Main()
        {
            TelegramBot bot = new TelegramBot();
            bot.Start().Wait();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}

