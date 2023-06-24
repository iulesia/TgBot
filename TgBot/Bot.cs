using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Polling;
using Npgsql;
using Telegram.Bot.Args;
using System.Net.Http;
using System.Net.Http.Json;
using System.Xml.Linq;
using System;
using System.Diagnostics.Metrics;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;
using System.Collections.Generic;

using Message = Telegram.Bot.Types.Message;
using System.Text.RegularExpressions;
using UpdateType = Telegram.Bot.Types.Enums.UpdateType;
using Microsoft.AspNetCore;
using Client2;
using Client1;

using Model;

using Newtonsoft.Json;
using BotCons;
using System;
using System.Text.Json;
using db;


namespace Bot
{
    public class TelegramBot
    {

        static TelegramBotClient botClient = new TelegramBotClient("5874370753:AAEZqQZe5SxiYJqhhpCbpcvHxd5r3kkXXvU");

        private List<Results> results;
        CancellationToken cancellationToken = new CancellationToken();
        ReceiverOptions receiverOptions = new ReceiverOptions { AllowedUpdates = { } };

        public async Task Start()
        {
            botClient.StartReceiving(HandlerUpdateAsync, HandlerError, receiverOptions, cancellationToken);
            var botMe = await botClient.GetMeAsync();
            Console.WriteLine($"Бот {botMe.Username} почав працювати");

        }

        public Task HandlerError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"В API телеграм-бота сталася помилка:\n {apiRequestException.ErrorCode}" +
                $"\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            return Task.CompletedTask;
        }

        public async Task HandlerUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                await HandlerMessageAsync(botClient, update);
            }

        }


        private Dictionary<long, string> currentStage = new Dictionary<long, string>();

        public async Task HandlerMessageAsync(ITelegramBotClient botClient, Update update)
        {
            var message = update.Message;
            if (!currentStage.ContainsKey(message.Chat.Id))
            {
                currentStage.Add(message.Chat.Id, "меню");
            }

            switch (currentStage[message.Chat.Id]) 
            {
                case "/info":

                    await GetMovieInfo(message.Text);
                    ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(
                        new[]
                        {
                        new KeyboardButton[] { "інформація про фільм", "фільми, що зараз у прокаті" },
                        new KeyboardButton[] { "Фільми в моєму списку", "Видалити фільм зі списку", "Додати фільм у список"},
                        }
                    )
                    {
                        ResizeKeyboard = true
                    };
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть пункт меню:", replyMarkup: replyKeyboardMarkup);
                    break;

                case "/AddFilm":

                    Films films = new Films();
                    films.id = message.Chat.Id;
                    var name = message.Text;
                    films.Movie_name = name.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    await AddWatchedMovie(films.id, films.Movie_name);

                    break;
                case "/deleteFilm":
                    Database db = new Database();
                    Films film = new Films();
                    var movie_name = message.Text;
                    film.Movie_name = movie_name.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    await db.DeleteWatchedMovieAsync(film.Movie_name);
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Фільм успішно видалено!");
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"повернутися в меню /keyboard, якщо бажаєте видалити зі списку фільми, то просто продовжуйте вводити назву)");



                    break;

                default:
                    break;
            }

            switch (message.Text) 
            {
                case "/AddFilm":

                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Ви можете додавати фільми послідовно, не викликаючи цю команду знову)");
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть назву фільма\n ");
                    currentStage[message.Chat.Id] = "/AddFilm";

                    break;

                case "/myFilms":

                    currentStage[message.Chat.Id] = "/myFilms";
                    break;
                case "/deleteFilm":
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Ви можете видаляти фільми послідовно, не викликаючи цю команду знову)");
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть назву фільма,який ви хочете видалити зі списку\n ");
                    currentStage[message.Chat.Id] = "/deleteFilm";
                    break;
                case "/start":
                    currentStage[message.Chat.Id] = "/start";
                    break;
                case "/keyboard":
                    currentStage[message.Chat.Id] = "/keyboard";
                    break;
                case "/info":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть назву фільма\n ");
                    currentStage[message.Chat.Id] = "/info";
                    break;
                case "/movieNowPlaying":
                    currentStage[message.Chat.Id] = "/movieNowPlaying";
                    break;
            }

            switch (message.Text) 
            {
                case "/start":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Ласкаво просимо до бота. Виберіть " +
                        "команду, щоб продовжити /keyboard");
                    break;
                case "/myFilms":
                    long id = message.Chat.Id;
                    await GetWatchedMovie(id);

                    
                    break;
                case "/movieNowPlaying":
                    await HandleCallBackQuery(botClient);
                    break;
                case "/keyboard":
                    ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(
                        new[]
                        {
                        new KeyboardButton[] { "інформація про фільм", "фільми, що зараз у прокаті" },
                        new KeyboardButton[] { "Фільми в моєму списку", "Видалити фільм зі списку", "Додати фільм у список"},
                        }
                    )
                    {
                        ResizeKeyboard = true
                    };
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть пункт меню:", replyMarkup: replyKeyboardMarkup);
                    break;
                case "інформація про фільм":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Для того щоб дізнатися інформацію про фільм оберіть цю команду /info");
                    break;
                case "Фільми в моєму списку":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть цю команду /myFilms");
                    break;
                case "Видалити фільм зі списку":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть цю команду /deleteFilm");
                    break;
                case "Додати фільм у список":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть цю команду /AddFilm");
                    break;
                case "фільми, що зараз у прокаті":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Для того, щоб знайти фільми, які зараз у прокаті виберіть цю команду /movieNowPlaying");
                    break;
            }

            async Task GetMovieInfo(string movie_name)
            {
                //string movie_name = "movie_name";
                MovieInfoClient movieinfoClient = new MovieInfoClient();
                var result1 = await movieinfoClient.GetMovieInfoAsync(movie_name);
                await botClient.SendTextMessageAsync(message.Chat.Id, "Зачекайте, доки я знайду для Вас усі фільми");
                Thread.Sleep(2000);
                if (result1.Results.Length != 0)
                {
                    for (int i = 0; i < result1.Results.Length; i++)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"назва фільму --> {result1.Results[i].Title}\n\n" +
                            $"опис фільму --> {result1.Results[i].Overview}\n\n");
                    }
                    Thread.Sleep(1000);
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Ось усі фільми, які мені вдалося знайти");
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "вибачте, я не зміг нічого знайти");

                    Thread.Sleep(1000);
                    await botClient.SendTextMessageAsync(message.Chat.Id, "перевірте чи Ви правильно ввели дані", replyToMessageId: message.MessageId);
                }



            }

            async Task HandleCallBackQuery(ITelegramBotClient botClient)
            {


                MovieClient movieClient = new MovieClient();
                var result = await movieClient.GetMovieNowPlayingAsync();
                await botClient.SendTextMessageAsync(message.Chat.Id, "Зачекайте, доки я знайду для Вас усі фільми");
                Thread.Sleep(2000);
                for (int i = 0; i < result.Results.Length; i++)
                {
                    try
                    {

                        Message photo = await botClient.SendPhotoAsync(
                        chatId: message.Chat.Id,
                        photo: InputFile.FromUri($"http://image.tmdb.org/t/p/original{result.Results[i].Poster_path}"),
                        caption: $"{i}\n Назва фільму:\t{result.Results[i].Title}\n\n Опис фільму:\t{result.Results[i].Overview}\n\n Дата виходу:\t{result.Results[i].Release_date}\n\n Середня оцінка:\t{result.Results[i].Vote_average}\n\n",
                        parseMode: ParseMode.Html);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            Message photo = await botClient.SendPhotoAsync(
                           chatId: message.Chat.Id,
                           photo: InputFile.FromUri($"http://image.tmdb.org/t/p/w780{result.Results[i].Poster_path}"),
                           caption: $"{i}\n Назва фільму:\t{result.Results[i].Title}\n\n Опис фільму:\t{result.Results[i].Overview}\n\n Дата виходу:\t{result.Results[i].Release_date}\n\n Середня оцінка:\t{result.Results[i].Vote_average}\n\n",
                           parseMode: ParseMode.Html);
                        }
                        catch (Exception)
                        {
                            try
                            {
                                Message photo = await botClient.SendPhotoAsync(
                                chatId: message.Chat.Id,
                                photo: InputFile.FromUri($"http://image.tmdb.org/t/p/w500{result.Results[i].Poster_path}"),
                                caption: $"{i}\n Назва фільму:\t{result.Results[i].Title}\n\n Опис фільму:\t{result.Results[i].Overview}\n\n☆ Дата виходу:\t{result.Results[i].Release_date}\n\n Середня оцінка:\t{result.Results[i].Vote_average}\n\n",
                                parseMode: ParseMode.Html);
                            }
                            catch (Exception)
                            {
                                try
                                {
                                    Message photo = await botClient.SendPhotoAsync(
                                    chatId: message.Chat.Id,
                                    photo: InputFile.FromUri($"http://image.tmdb.org/t/p/w342{result.Results[i].Poster_path}"),
                                    caption: $"{i}\n Назва фільму:\t{result.Results[i].Title}\n\n Опис фільму:\t{result.Results[i].Overview}\n\n Дата виходу:\t{result.Results[i].Release_date}\n\n Середня оцінка:\t{result.Results[i].Vote_average}\n\n",
                                    parseMode: ParseMode.Html);
                                }
                                catch (Exception)
                                {
                                    try
                                    {
                                        Message photo = await botClient.SendPhotoAsync(
                                        chatId: message.Chat.Id,
                                        photo: InputFile.FromUri($"http://image.tmdb.org/t/p/w185{result.Results[i].Poster_path}"),
                                        caption: $"{i}\n Назва фільму:\t{result.Results[i].Title}\n\n Опис фільму:\t{result.Results[i].Overview}\n\n Дата виходу:\t{result.Results[i].Release_date}\n\n Середня оцінка:\t{result.Results[i].Vote_average}\n\n",
                                        parseMode: ParseMode.Html);
                                    }
                                    catch (Exception)
                                    {
                                        try
                                        {
                                            Message photo = await botClient.SendPhotoAsync(
                                            chatId: message.Chat.Id,
                                            photo: InputFile.FromUri($"http://image.tmdb.org/t/p/w154{result.Results[i].Poster_path}"),
                                            caption: $"{i}\n Назва фільму:\t{result.Results[i].Title}\n\n Опис фільму:\t{result.Results[i].Overview}\n\n Дата виходу:\t{result.Results[i].Release_date}\n\n Середня оцінка:\t{result.Results[i].Vote_average}\n\n",
                                            parseMode: ParseMode.Html);
                                        }
                                        catch (Exception)
                                        {
                                            try
                                            {
                                                Message photo = await botClient.SendPhotoAsync(
                                                chatId: message.Chat.Id,
                                                photo: InputFile.FromUri($"http://image.tmdb.org/t/p/w92{result.Results[i].Poster_path}"),
                                                caption: $"{i}\n Назва фільму:\t{result.Results[i].Title}\n\n Опис фільму:\t{result.Results[i].Overview}\n\n Дата виходу:\t{result.Results[i].Release_date}\n\n Середня оцінка:\t{result.Results[i].Vote_average}\n\n",
                                                parseMode: ParseMode.Html);
                                            }
                                            catch (Exception)
                                            {
                                                Message photo = await botClient.SendPhotoAsync(
                                                chatId: message.Chat.Id,
                                                photo: InputFile.FromUri("https://www.movienewz.com/img/films/poster-holder.jpg"),
                                                caption: $"{i}\n Назва фільму:\t{result.Results[i].Title}\n\n Опис фільму:\t{result.Results[i].Overview}\n\n Дата виходу:\t{result.Results[i].Release_date}\n\n Середня оцінка:\t{result.Results[i].Vote_average}\n\n",
                                                parseMode: ParseMode.Html);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }






                }







            }

            async Task DeleteWatchedMovie(ITelegramBotClient botClient, Message message, string[] Movie_name)
            {
                Database db = new Database();

                try
                {
                    await db.DeleteWatchedMovieAsync(Movie_name);
                    await botClient.SendTextMessageAsync(message.Chat.Id, $" успішно видалено");
                }
                catch (Exception ex)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Error: {ex.Message}");

                }



            }


          
            async Task GetWatchedMovie(long id)
            {
               
                Database db = new Database();
                
                List<string> movies = db.GetMoviesByUserId(id);

                if (movies.Count > 0)
                {
                    string message = "Список ваших фільмів:\n";
                    foreach (string movie in movies)
                    {
                        message += "- " + movie + "\n";
                    }

                    await botClient.SendTextMessageAsync(id, message);
                }
                else
                {
                    await botClient.SendTextMessageAsync(id, "Ви ще не додали жодного фільму до списку.");
                }
            }
            
            async Task AddWatchedMovie(long id, string[] Movie_name)
            {

                Database db = new Database();
                Films films = new Films { id = id, Movie_name = Movie_name };
                await db.AddWatchedMovieAsync(films);
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Фільм успішно додано!");
                await botClient.SendTextMessageAsync(message.Chat.Id, $"повернутися в меню /keyboard, якщо бажаєте поповнити свій список ще, то просто продовжуйте вводити назву)");



            }
        }
    }
}
