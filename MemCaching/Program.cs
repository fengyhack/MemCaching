using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MemCaching
{
    static class Program
    {
        static MemCache cache;

        static void Main(string[] args)
        {
            cache = new MemCache(Update);            
            cache.Add("B", 0, 2);
            cache.AddImmediate("A", false);
            cache.AddOnce("C", "INITIAL_VALUE");

            var keys = new string[] { "A", "B", "C" };
            foreach (var key in keys)
            {
                var value = cache.Get(key);
                Console.WriteLine("INIT {0}={1}", key, value);
            }

            Task.Run(() => { Loop(cache); });

            Console.ReadLine();
        }

        public static void Loop(MemCache cache)
        {
            var i = 0;
            while (++i < 100)
            {
                var keys = new string[] { "A", "B", "C" };
                foreach (var key in keys)
                {
                    var value = cache.Get(key);
                    Console.WriteLine("GET {0}={1}", key, value);
                }

                Thread.Sleep(1000);
            }
        }

        static bool b = false;
        static int n = 0;

        public static void Update(string key)
        {
            if (key == "A")
            {
                b = !b;
                cache.Set(key, b);
            }
            else if (key == "B")
            {
                cache.Set(key, ++n);
            }
            else if (key == "C")
            {
                //
            }
            else
            {
                Console.WriteLine("{0}---", key);
            }
        }
    }
}
