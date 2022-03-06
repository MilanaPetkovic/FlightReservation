# FlightReservation

### Find the cheapest flight for you

> note: This script uses PuppeteerSharp, which will download Chromium instance on the path you set in "App.congfig" file (default is C:\\chromium)

- [How to set chromium download path](#chromiumdownload)
- [Input parameters](#inputparameters)
- [Output](#output)

## ChromiumDownload

 ![image](https://github.com/MilanaPetkovic/FlightReservation/blob/main/ReadmePictures/chromiumdownloadpath.png)

## InputParameters

Imagine want to fly from Belgrade to Barcelona between 2022-03-12 and 2022-04-12 and don't want to stay less than 7 nights and more than 10 nights in Barcelona
> note between dates are in yyyy-MM-dd format

 ![image](https://github.com/MilanaPetkovic/FlightReservation/blob/main/ReadmePictures/inputParameters.png)

Once enter is pressed chroimium instance will be fired up, and script will start searching for cheapest flights based on input parameters

 ![image](https://github.com/MilanaPetkovic/FlightReservation/blob/main/ReadmePictures/chromiumInstance.png)

## Output

 ![image](https://github.com/MilanaPetkovic/FlightReservation/blob/main/ReadmePictures/output.png)

Cheapest price and url where tickets can be booked are shown in console output (no wories, price will be in your local currency)

If it is not satisfactory solution just type "n" and press enter, script will continue searching for another one

<details>
	<summary>Tikcet booking url example<summary>

 
 ![image](https://github.com/MilanaPetkovic/FlightReservation/blob/main/ReadmePictures/bookingUrlExample.png)

</details>





