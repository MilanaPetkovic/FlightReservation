using FlightReservationConsole.Models;
using PuppeteerSharp;
using System;
using System.Configuration;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace FlightReservationConsole
{
    class Program
    {
        static readonly string mainUrl = "https://www.kiwi.com";
        static async Task Main(string[] args)
        {
            string searching = "y";
            while (searching == "y")
            {
                try
                {
                    FlightReservation flightReservation = new();

                    //Insert flight from-to locations
                    Console.Write("Flight from(ex belgrade-serbia): ");
                    flightReservation.FlightFrom = Console.ReadLine();
                    Console.Write("Flight to(ex. barcelona-spain): ");
                    flightReservation.FlightTo = Console.ReadLine();

                    //Insert between dates 
                    Console.WriteLine("Between dates:");
                    Console.Write("From(yyyy-MM-dd): ");
                    flightReservation.DateFrom = DateTime.ParseExact(Console.ReadLine(), "yyyy-MM-dd", CultureInfo.InvariantCulture);

                    Console.Write("To(yyyy-MM-dd):");
                    flightReservation.DateTo = DateTime.ParseExact(Console.ReadLine(), "yyyy-MM-dd", CultureInfo.InvariantCulture);

                    //Insert how much time you want to stay
                    Console.Write("You don't want to stay less than(nights): ");
                    flightReservation.LessThanDays = int.Parse(Console.ReadLine());
                    Console.Write("You don't want to stay more than(nights): ");
                    flightReservation.MoreThanDays = int.Parse(Console.ReadLine());

                    var fromFlyBack = flightReservation.DateFrom.AddDays(-flightReservation.LessThanDays);
                    var toArrive = flightReservation.DateTo.AddDays(-flightReservation.MoreThanDays);


                    var url = $"{mainUrl}/en/search/results/{flightReservation.FlightFrom}/{flightReservation.FlightTo}" +
                        $"/{flightReservation.DateFrom:yyyy-MM-dd}_{toArrive:yyyy-MM-dd}/{fromFlyBack:yyyyMMdd}_{flightReservation.DateTo:yyyy-MM-dd}?sortBy=price";

                    //Instance of chromium(puppeteer)
                    var puppeteer = new Downloader(ConfigurationManager.AppSettings["Path"], false);

                    using Browser browser = PuppeteerSharp.Puppeteer.LaunchAsync(puppeteer.SetBrowserOptions()).Result;
                    using Page page = browser.PagesAsync().Result[0];

                    //Go to our url
                    await page.GoToAsync(url, WaitUntilNavigation.DOMContentLoaded);
                    //Click accept all cookies
                    var button = await page.QuerySelectorAsync("button");
                    await button.ClickAsync();

                    Thread.Sleep(10000);
                    string html = await page.GetContentAsync();
                    int flightNumber = 0;
                    try
                    {
                        for (int i = 1; ; i++)
                        {
                            try
                            {
                                flightNumber++;

                                int nights = int.Parse(html.Split("ResultCardItinerarystyled__SectorLayoverTextBackground-sc-iwhyue-9 cJMqrQ\">")[i]
                                        .Split("nights")[0].Trim());

                                //If this filght okay for us, break the loop and book it
                                if (nights >= flightReservation.LessThanDays && nights <= flightReservation.MoreThanDays)
                                    break;
                            }
                            catch (Exception)
                            {
                                await page.EvaluateExpressionAsync("window.scrollBy(1, window.innerHeight)");
                                Thread.Sleep(5000);
                                var newHtml = await page.GetContentAsync();
                                i--;

                                if (newHtml == html)
                                {
                                    await page.ClickAsync("button[class='ButtonPrimitive__StyledButtonPrimitive-sc-1lbd19y-0 cVsCSD']");
                                    Thread.Sleep(10000);
                                    await page.EvaluateExpressionAsync("window.scrollBy(1, window.innerHeight)");
                                    Thread.Sleep(5000);
                                    html = await page.GetContentAsync();
                                }
                                else
                                {
                                    html = newHtml;
                                }
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        throw new Exception("There is no cheap flights for that dates");
                    }

                    string bookingUrl = "";
                    bool scrollMore = true;
                    while (scrollMore)
                    {
                        try
                        {
                             bookingUrl = html.Split("<a class=\"ButtonPrimitive__StyledButtonPrimitive-sc-1lbd19y-0 kBsuLf\"")[flightNumber]
                                .Split("rel=\"nofollow\"")[0]
                                .Replace("\"", "")
                                .Replace("href=", "")
                                .Replace(";", "&")
                                .Trim();
                            scrollMore = false;
                        }
                        catch (Exception)
                        {
                            await page.EvaluateExpressionAsync("window.scrollBy(1, window.innerHeight)");
                            Thread.Sleep(5000);
                            html = await page.GetContentAsync();
                        }
                    }

                    //Final booking url
                    bookingUrl = $"{mainUrl}{bookingUrl}";

                    //Final cheapest price
                    var price = html.Split("<span class=\" length-10\">")[flightNumber]
                        .Split("</span>")[0];

                    Console.WriteLine($"Cheapest flight {price}");
                    Console.WriteLine($"Can be booked on url: {bookingUrl}");
                    Console.Write("Is this answer satisfying y(stop the app)/n(search more): ");
                    searching = Console.ReadLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
