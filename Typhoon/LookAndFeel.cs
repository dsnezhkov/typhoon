using System;

namespace Typhoon
{
    static class LookAndFeel
    {
        internal static void SetInitConsole(String title, ConsoleColor fg, ConsoleColor bg)
        {

            SetColorConsole(fg, bg);
            Console.Title =  title;
            Console.Clear();
        }

        internal static void SetColorConsole(ConsoleColor fg, ConsoleColor? bg = null)
        {
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg.GetValueOrDefault(Console.BackgroundColor);
        }

        internal static void ResetColorConsole(bool dark)
        {
            if (dark)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.BackgroundColor = ConsoleColor.Black;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.White;

            }
        }

    }
}
