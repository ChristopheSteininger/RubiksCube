using System;
using System.Windows.Forms;

namespace RubiksCube
{
#if WINDOWS || XBOX
    static class Program
    {
        static void Main(string[] args)
        {
            using (RubiksCube game = new RubiksCube())
            {
                game.Run();
            }
        }
    }
#endif
}