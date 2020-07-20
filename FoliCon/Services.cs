using Jot;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace FoliCon
{
    public static class Services
    {
        public static Tracker Tracker = new Tracker();
        public static HttpClient HttpC=new HttpClient();
    }
}
