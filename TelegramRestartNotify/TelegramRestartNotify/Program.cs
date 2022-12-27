using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using File = System.IO.File;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace BotMain {
    class Program {
        static List<long> ChatIDsToSendNotify= new List<long>(10);
        static TelegramBotClient? botClient=null;
        const string BotTokenFileName = "BotToken.txt";
        const string ChatIDsFileName = "ChatIDs.txt";
        const string SubscribeFileName = "SubscribeToken.txt";
        static string BotTokenString = "";
        static string SubscribeTokenString = "";
        static void Main(string[] args)
        {
            if(!File.Exists(BotTokenFileName)) 
            {
                Console.WriteLine($@"{BotTokenFileName} не найден. Там должна лежать строка токена из BotFather.");
                return;
            }
            if (!File.Exists(ChatIDsFileName))
            {
                Console.WriteLine($@"{ChatIDsFileName} не найден. Там должны лежать по строкам UTF8 числа long с chatID, кого нужно уведомить. Файл будет создан.");
                File.WriteAllBytes(SubscribeFileName, Array.Empty<byte>());
                //return;
            }
            if (!File.Exists(SubscribeFileName))
            {
                Console.WriteLine($@"{SubscribeFileName} не найден. Там должны строка UTF8 с токеном для того, чтобы люди могли подписываться на уведомление. Если пусто, то бот будет выключаться сразу после уведомления всех, кто уже записан. Файл будет создан.");
                //return;
                File.WriteAllBytes(SubscribeFileName, Array.Empty<byte>());
            }
            BotTokenString= File.ReadAllLines(BotTokenFileName, System.Text.Encoding.UTF8)[0];

            string[] SubscribeStringLines=File.ReadAllLines(SubscribeFileName, System.Text.Encoding.UTF8);
            SubscribeTokenString = SubscribeStringLines.Length > 0 ? SubscribeStringLines[0] :string.Empty;

            string[] ChatIDsStrings = File.ReadAllLines(ChatIDsFileName, System.Text.Encoding.UTF8);
            //ChatIDsToSendNotify=new long[ChatIDsStrings.Length];
            Console.WriteLine("Найдены "+ ChatIDsStrings.Length+" пользователей для уведомления.");
            for(int i = 0; i < ChatIDsStrings.Length; i++) 
            {
                ChatIDsToSendNotify.Add(long.Parse(ChatIDsStrings[i]));
            }
            

            botClient = new TelegramBotClient(BotTokenString);
            Console.WriteLine("Loaded BotToken="+BotTokenString);
            Console.WriteLine("Loaded SubscribeToken=" + SubscribeTokenString);
            

            SendRebootNotificationMessage();
            Console.ReadKey();
        }

        private static async void SendRebootNotificationMessage() 
        {
            if (botClient == null)
            {
                return;
            }
            botClient.StartReceiving(Update, Error);
            for (int i = 0; i < ChatIDsToSendNotify.Count; i++)
            {
                long chatIDToSend = ChatIDsToSendNotify[i];
                Console.WriteLine("Отправляю уведомление к "+chatIDToSend);

                await botClient.SendTextMessageAsync(chatIDToSend, "Server rebooted "+DateTime.UtcNow);
                
            }
            //Никто не может подписаться, поэтому сразу выключаем бота, который должен запускаться по старту сервера сообщая о произошедшем перезапуске.
            if (string.IsNullOrEmpty(SubscribeTokenString)|| SubscribeTokenString.Length==0)
            {
                Console.WriteLine("Token для подписки через чат пуст, поэтому завершаем работу.");
                Environment.Exit(0);
            }
        }
        async static Task Update(ITelegramBotClient botClient,Update update,CancellationToken cancellationToken) 
        {
            //TODO: сделать управление сервером через телегу
            //Игнорируем пустые сообщения
            if (update.Message == null) 
            {
                Console.WriteLine("Пришла пустое сообщение дропаем");
                return;
            }
            //Игнорируем картинки
            if (update.Message.Photo!=null)
            {
                Console.WriteLine("Пришла картинка дропаем");
                return;
            }
            if(update.Message.Text== SubscribeTokenString) 
            {
                long chatId = update.Message.Chat.Id;
                using (StreamWriter writer = File.AppendText(ChatIDsFileName)) {
                    writer.WriteLine(chatId.ToString());
                }
                ChatIDsToSendNotify.Add(chatId);
                Console.WriteLine("Добавлен подписчик на уведомления: "+chatId);
            }
            Console.WriteLine("ChatMember="+update.ChatMember+ " MyChatMember=" +  update.MyChatMember+ " Message=" + update.Message);
            //return;
        }
        async static Task Error(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken) 
        {
            Console.WriteLine(exception);
        }

    }
}