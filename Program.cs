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
        private static FlightReservation flightReservation = new();
        private static string html = "";
        private static int flightNumber = 1;
        //private static Page page;
        static async Task Main(string[] args)
        {
            string searching = "n";

            try
            {
                FillFlightReservationModel(flightReservation);

                var fromFlyBack = flightReservation.DateFrom.AddDays(-flightReservation.LessThanDays);
                var toArrive = flightReservation.DateTo.AddDays(-flightReservation.MoreThanDays);

                var url = $"{mainUrl}/en/search/results/{flightReservation.FlightFrom}/{flightReservation.FlightTo}" +
                    $"/{flightReservation.DateFrom:yyyy-MM-dd}_{toArrive:yyyy-MM-dd}/{fromFlyBack:yyyyMMdd}_{flightReservation.DateTo:yyyy-MM-dd}?sortBy=price";

                //Instance of chromium(puppeteer)
                var puppeteer = new Downloader(ConfigurationManager.AppSettings["Path"], false);

                using Browser browser = PuppeteerSharp.Puppeteer.LaunchAsync(puppeteer.SetBrowserOptions()).Result;
                using Page page = browser.PagesAsync().Result[0];

                await GetPageContent(url, page);

                while (searching == "n")
                {
                     await FindCheapestFlight(page);

                    (string price, string bookingUrl) = await FindCheapestPriceAndBookingUrl(flightNumber, page);

                    searching = WriteResponse(price, bookingUrl);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        /// <summary>
        /// Write response
        /// </summary>
        /// <param name="price">cheapest price</param>
        /// <param name="bookingUrl">url where flight can be booked</param>
        /// <returns></returns>
        private static string WriteResponse(string price, string bookingUrl)
        {
            string searching;
            Console.WriteLine($"Cheapest flight {price}");
            Console.WriteLine($"Can be booked on url: {bookingUrl}");
            Console.Write("Is this answer satisfying y(stop the app)/n(search more): ");
            searching = Console.ReadLine();
            return searching;
        }

        /// <summary>
        /// Fill flight reservation model with appropriate parameters
        /// </summary>
        /// <param name="flightReservation">model</param>
        private static void FillFlightReservationModel(FlightReservation flightReservation)
        {
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
        }

        /// <summary>
        /// Get page content from the url
        /// </summary>
        /// <param name="url">url</param>
        /// <returns></returns>
        private static async Task GetPageContent(string url, Page page)
        {
            //Go to our url
            await page.GoToAsync(url, WaitUntilNavigation.DOMContentLoaded);
            //Click accept all cookies button
            var button = await page.QuerySelectorAsync("button");
            await button.ClickAsync();
            //Wait for chromium to his job
            Thread.Sleep(10000);
            //Get html page conent
            html = await page.GetContentAsync();
        }

        /// <summary>
        /// Find cheapest flight
        /// </summary>
        /// <returns></returns>
        private static async Task FindCheapestFlight(Page page)
        {
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
                        //If we can't see more flights, scroll the page
                        await page.EvaluateExpressionAsync("window.scrollBy(1, window.innerHeight)");
                        //Wait for chromium to his job
                        Thread.Sleep(5000);
                        //Get scrolled page content
                        var newHtml = await page.GetContentAsync();
                        i--;

                        //If there is "Load more" button, click it, scroll the page and get scrolled page content
                        if (newHtml == html)
                        {
                            //ButtonPrimitive__StyledButtonPrimitive-sc-1lbd19y-0 cVsCSD - "Load more" button class
                            await page.ClickAsync("button[class='ButtonPrimitive__StyledButtonPrimitive-sc-1lbd19y-0 cVsCSD']");
                            //Wait for chromium to his job
                            Thread.Sleep(10000);
                            //Scroll once
                            await page.EvaluateExpressionAsync("window.scrollBy(1, window.innerHeight)");
                            //Wait for chromium to his job
                            Thread.Sleep(5000);
                            //Get scrolled page content
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
        }

        /// <summary>
        /// Find cheapest flight price and flight booking url
        /// </summary>
        /// <param name="flightNumber">cheapest flight number</param>
        /// <returns></returns>
        private static async Task<(string price, string url)> FindCheapestPriceAndBookingUrl(int flightNumber, Page page)
        {
            string url = "";
            bool scrollMore = true;
            //Scroll the page until we get our url
            while (scrollMore)
            {
                try
                {
                    url = html.Split("<a class=\"ButtonPrimitive__StyledButtonPrimitive-sc-1lbd19y-0 kBsuLf\"")[flightNumber]
                       .Split("rel=\"nofollow\"")[0]
                       .Replace("\"", "")
                       .Replace("href=", "")
                       .Replace(";", "&")
                       .Trim();
                    scrollMore = false;
                }
                catch (Exception)
                {
                    //Scroll once 
                    await page.EvaluateExpressionAsync("window.scrollBy(1, window.innerHeight)");
                    //Wait for chromium to his job
                    Thread.Sleep(5000);
                    //Get scrolled page content
                    html = await page.GetContentAsync();
                }
            }

            //Final booking url
            url = $"{mainUrl}{url}";

            //Final cheapest price
            var price = html.Split("<span class=\" length-10\">")[flightNumber]
                .Split("</span>")[0];

            return (price, url);
        }
    }
}
