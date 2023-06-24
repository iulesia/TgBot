
using Newtonsoft.Json;
using BotCons;
using Model;




namespace Client1
{

    public class MovieInfoClient
    {
        private HttpClient _httpClient;
        private static string _address;
        public MovieInfoClient()
        {
            _address = Constants.address;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_address);
        }

        public async Task<Models> GetMovieInfoAsync(string name)
        {
            var response = await _httpClient.GetAsync($"movie/movie_info?name={name}");
            response.EnsureSuccessStatusCode();
            var content = response.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<Models>(content);
            return result;
        }
    }
}

