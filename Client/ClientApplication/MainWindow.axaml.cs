using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Mapster.ClientApplication;

public partial class MainWindow : Window
{
    private static readonly HttpClient _httpClient = new HttpClient();

    private int _clickCounter;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Model.Data = "Render";
		RenderCityModel.Data = "Render Andorra City";
	}

    // Model used for the button text
    private DataModel Model { get; } = new DataModel();
    private DataModel RenderCityModel { get; } = new DataModel();
    // Model used for the list of items
    private ObservableCollection<MapTile> Items { get; } = new ObservableCollection<MapTile>();

    private void OnButtonPressed(object? sender, RoutedEventArgs eventArgs)
    {
        Console.WriteLine($"Button clicked {++_clickCounter} times");

        try
        {
            var response = _httpClient.GetAsync("http://localhost:8080/render?minLon=1.388397216796875&minLat=42.402164470921285&maxLon=1.8024444580078125&maxLat=42.67688269641377&size=2000").Result;
            if (response.IsSuccessStatusCode)
            {
                Items.Add(new MapTile(response.Content.ReadAsByteArrayAsync().Result, 2000));
            }

        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
        }
    }

    private void OnRenderCityPressed(object? sender, RoutedEventArgs eventArgs) {
		try
		{
			var response = _httpClient.GetAsync("http://localhost:8080/render?minLon=1.52&minLat=42.5022817&maxLon=1.5285380&maxLat=42.5103326&size=2000").Result;
			if (response.IsSuccessStatusCode)
			{
				Items.Add(new MapTile(response.Content.ReadAsByteArrayAsync().Result, 2000));
			}

		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex.Message);
		}
    }

    private class ServiceResponse
    {
        public int tileCount { get; set; }
        public byte[][]? imageData { get; set; }
    }
}
