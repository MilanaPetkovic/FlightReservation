using FlightReservationConsole.Models;
using PuppeteerSharp;
using RestSharp;
using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace FlightReservationConsole
{
    class Program
    {
        static readonly string mainUrl = "https://www.kiwi.com";
        static async Task Main(string[] args)
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
                DateTime helper;
                Console.Write("From(yyyy-MM-dd): ");
                flightReservation.DateFrom = DateTime.ParseExact(Console.ReadLine(), "yyyy-MM-dd", CultureInfo.InvariantCulture);

                Console.Write("To(yyyy-MM-dd):");
                flightReservation.DateTo = DateTime.ParseExact(Console.ReadLine(), "yyyy-MM-dd", CultureInfo.InvariantCulture);

                //Insert how much time you want to stay
                Console.Write("You don't want to stay less than(days): ");
                flightReservation.LessThanDays = int.Parse(Console.ReadLine());
                Console.Write("You don't want to stay more than(days): ");
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
                //Scrol one time
                await page.EvaluateExpressionAsync("window.scrollBy(0, window.innerHeight)");

                string html = await page.GetContentAsync();
                int flightNumber = 0;
                try
                {
                    bool isSecondTry = false;
                    for (int i = 1; ; i = i + 8)
                    {
                        flightNumber++;
                        DateTime arrive = DateTime.Now;
                        DateTime flightBack = DateTime.Now;
                        try
                        {
                             arrive = DateTime.Parse
                                (html.Split("<time")[i]
                                    .Split("datetime=")[1]
                                    .Split(">")[0]
                                    .Replace("\"", "")
                                );

                             flightBack = DateTime.Parse
                                (html.Split("<time")[i + 4]
                                    .Split("datetime=")[1]
                                    .Split(">")[0]
                                    .Replace("\"", "")
                                );
                        }
                        catch(Exception ex)
                        {
                            string expectedMessage = "was not recognized as a valid DateTime";
                            //Sometimes page have 1 or 2 more than ussual <time> selectors per item, there is the logic to handle that
                            if (ex.Message.Contains(expectedMessage))
                            {
                                i = i - 8 + 1;
                                isSecondTry = true;
                            }                                
                            else if(ex.Message.Contains(expectedMessage) && isSecondTry)
                            {
                                i = i - 8 + 2;
                                isSecondTry = false;
                            }

                        }

                        var timeSpan = flightBack - arrive;

                        //If this filght okay for us, break the loop and book it
                        if (timeSpan.Days >= flightReservation.LessThanDays && timeSpan.Days <= flightReservation.MoreThanDays)
                            break;

                        //Scroll one time
                        await page.EvaluateExpressionAsync("window.scrollBy(0, window.innerHeight)");
                        //Get scrolled page content
                        var newHtml = await page.GetContentAsync();

                        if (html != newHtml)
                            html = newHtml;                         
                        else
                            await page.ClickAsync("button[class='ButtonPrimitive__StyledButtonPrimitive-sc-1lbd19y-0 cVsCSD']");                        
                    }                                               
                }
                
                catch (Exception ex)
                {
                    throw new Exception("There is no cheap flights for that dates");
                }


                var bookingUrl = html.Split("<a class=\"ButtonPrimitive__StyledButtonPrimitive-sc-1lbd19y-0 kBsuLf\"")[flightNumber]
                    .Split("rel=\"nofollow\"")[0]
                    .Replace("\"", "")
                    .Replace("href=", "")
                    .Replace(";", "&")
                    .Trim();

                //Final booking url
                bookingUrl = $"{mainUrl}{bookingUrl}";

                //Final cheapest price
                var price = html.Split("<span class=\" length-10\">")[flightNumber]
                    .Split("</span>")[0];

                Console.WriteLine($"Cheapest flight {price}");
                Console.WriteLine("Can be booked on url: {bookingUrl}");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
