using Microsoft.Extensions.Configuration;
using System.Text.Json;


var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string? apiKey = config["WeatherApi:ApiKey"];

if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Ошибка: API ключ не найден");
    return;
}

Console.Write("Введите название города: ");
string? city = Console.ReadLine();

if (string.IsNullOrEmpty(city))
{
    Console.WriteLine("Ошибка: Город не указан");
    return;
}


using var httpClient = new HttpClient();

try
{
    string geoUrl = $"https://api.openweathermap.org/geo/1.0/direct?q={city}&limit=1&appid={apiKey}";

    HttpResponseMessage geoResponse = await httpClient.GetAsync(geoUrl);

    if (!geoResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"Не удалось найти город \"{city}\". Код ответа: {geoResponse.StatusCode}");
        return;
    }

    string geoJson = await geoResponse.Content.ReadAsStringAsync();
    using JsonDocument geoDoc = JsonDocument.Parse(geoJson);
    JsonElement geoRoot = geoDoc.RootElement;

    if (geoRoot.GetArrayLength() == 0)
    {
        Console.WriteLine($"Город \"{city}\" не найден");
        return;
    }

    JsonElement firstResult = geoRoot[0];
    double lat = firstResult.GetProperty("lat").GetDouble();
    double lon = firstResult.GetProperty("lon").GetDouble();

    string weatherUrl = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&units=metric&lang=ru&appid={apiKey}";
    HttpResponseMessage weatherResponse = await httpClient.GetAsync(weatherUrl);

    if (!weatherResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"Не удалось получить погоду. Код ответа: {weatherResponse.StatusCode}");
        return;
    }

    string weatherJson = await weatherResponse.Content.ReadAsStringAsync();
    using JsonDocument weatherDoc = JsonDocument.Parse(weatherJson);
    JsonElement weatherRoot = weatherDoc.RootElement;

    double temp = weatherRoot.GetProperty("main").GetProperty("temp").GetDouble();
    string description = weatherRoot.GetProperty("weather")[0].GetProperty("description").GetString() ?? "";

    Console.WriteLine($"\nПогода в городе {city}: ");
    Console.WriteLine($"Температура: {temp}°C");
    Console.WriteLine($"Описание: {description}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
}