using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using Model;
using BotCons;
using Telegram.Bot.Types;
namespace db

{
    public class Database
    {
        NpgsqlConnection con = new NpgsqlConnection(Constants.Connect);
        
        public async Task AddWatchedMovieAsync(Films movie)
        {
            var sql = "INSERT into public.\"Films\"(\"Movie_name\",\"id\")"
                        + "values (@Movie_name, @id)";
            NpgsqlCommand comm = new NpgsqlCommand(sql, con);
            comm.Parameters.AddWithValue("Movie_name", movie.Movie_name);
            comm.Parameters.AddWithValue("id", movie.id);
            await con.OpenAsync();
            await comm.ExecuteNonQueryAsync();
            await con.CloseAsync();
        }
        public async Task DeleteWatchedMovieAsync(string[] Movie_name)
        {
            var sql = "DELETE from public.\"Films\" where \"Movie_name\" = @Movie_name";
            NpgsqlCommand comm = new NpgsqlCommand(sql, con);
            comm.Parameters.AddWithValue("Movie_name", Movie_name);
           
            await con.OpenAsync();
            await comm.ExecuteNonQueryAsync();
            await con.CloseAsync();
        }
      
        public List<string> GetMoviesByUserId(long id)
        {
            List<string> movies = new List<string>();

            using (NpgsqlConnection con = new NpgsqlConnection(Constants.Connect)) ;
            {
                con.Open();

                var sql = "SELECT \"Movie_name\" FROM public.\"Films\" WHERE \"id\" = @id";
                NpgsqlCommand comm = new NpgsqlCommand(sql, con);
                {
                    comm.Parameters.AddWithValue("@id", id);

                    using (NpgsqlDataReader reader = comm.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string[] movieNames = (string[])reader.GetValue(0);
                            movies.AddRange(movieNames);
                        }
                    }
                }
            }

            return movies;
        }
       
        
    }
}

