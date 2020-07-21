using Jot;
using System.Net.Http;

namespace FoliCon
{
    public static class Services
    {
        public static Tracker Tracker = new Tracker();
        public static HttpClient HttpC = new HttpClient();
    }
}