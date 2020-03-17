using SimpleInjector;
using System;
using System.Windows.Forms;

namespace Client
{
    static class Program
    {
        private static Container _container;

        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Bootstrap();
            Application.Run(_container.GetInstance<MainForm>());
        }

        private static void Bootstrap()
        {
            // Create the container as usual.
            _container = new Container();

            // Register your types, for instance:
            _container.Register<MainForm>(Lifestyle.Singleton);
            _container.Register<BackgroundWorker>(Lifestyle.Singleton);

            // Optionally verify the container.
            _container.Verify();
        }
    }
}
