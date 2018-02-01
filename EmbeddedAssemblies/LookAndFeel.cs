using System;

namespace Cradle
{
    static class LookAndFeel
    {
        internal static void SetInitConsole(String title, ConsoleColor fg, ConsoleColor bg)
        {

            SetColorConsole(fg, bg);
            Console.Title =  title;
            Console.Clear();
        }

        internal static void SetColorConsole(ConsoleColor fg, ConsoleColor bg)
        {
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }

        internal static void ResetColorConsole()
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.BackgroundColor = ConsoleColor.Black;
        }

    }
}
