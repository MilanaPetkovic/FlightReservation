using System;

namespace FlightReservationConsole.Models
{
    public class FlightReservation
    {
        public string FlightFrom { get; set; }

        public string FlightTo { get; set; }
        private DateTime dateFrom;

        public DateTime DateFrom
        {
            get { return dateFrom; }
            set
            {
                if (value < DateTime.Now.Date)
                    throw new Exception("Date can not be less than today");
                else
                    dateFrom = value;
            }
        }

        private DateTime dateTo;

        public DateTime DateTo
        {
            get { return dateTo; }
            set
            {
                if (value < DateTime.Now.Date)
                    throw new Exception("Date can not be less than today");
                else
                    dateTo = value;
            }
        }

        private int lessThanDays;

        public int LessThanDays
        {
            get { return lessThanDays; }
            set
            {
                if (value < 0)
                    throw new Exception("You must stay at least 1 day");
                else
                    lessThanDays = value;
            }
        }

        private int moreThanDays;

        public int MoreThanDays
        {
            get { return moreThanDays; }
            set
            {
                if (value < 0)
                    throw new Exception("You must stay at least 1 day");
                else
                    moreThanDays = value;
            }
        }
    }
}
