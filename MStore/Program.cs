using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MStore
{
    class Program
    {
        static Boolean shouldStop = false;

        public static void KeepAlive()
        {
            while (!shouldStop)
            {
                Thread.Sleep(1000);
            }
        }

        static void Main(string[] args)
        {


            Thread thread = new Thread(new ThreadStart(KeepAlive));
            thread.Start();

            //Thread.Sleep(10000);

            //shouldStop = true;

            Environment.Exit(5);

        }
    }
}
