using Newtonsoft.Json;
using Model;

using BotCons;

namespace Client2
{

    public class MovieClient
    {
        private HttpClient _httpClient;
        private static string _address;
        public MovieClient()
        {
            _address = Constants.address;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_address);
        }

        public async Task<Models> GetMovieNowPlayingAsync()
        {
            var response = await _httpClient.GetAsync($"movie/cinema_movie");
            response.EnsureSuccessStatusCode();
            var content = response.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<Models>(content);
            return result;

        }
    }
}