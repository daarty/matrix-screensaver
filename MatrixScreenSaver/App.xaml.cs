// Copyright (c) 2015 Wm. Barrett Simms wbsimms.com
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using WaveSim;
using Application = System.Windows.Application;

namespace MatrixScreenSaver
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private HwndSource winWPFContent;

        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            // Without arguments Windows wants the settings, e.g. from "Configure" in the context menu of the .scr.
            string mode = e.Args.Length == 0 ? "/c" : e.Args[0].ToLowerInvariant();

            if (mode.StartsWith("/s"))
            {
                foreach (Screen s in Screen.AllScreens)
                {
                    //if (s != Screen.PrimaryScreen)

                    MainWindow window = new MainWindow();

                    // Screen.Bounds is in pixels, WPF positions windows in device independent units.
                    DpiScale dpi = VisualTreeHelper.GetDpi(window);
                    window.Left = s.Bounds.Left / dpi.DpiScaleX;
                    window.Top = s.Bounds.Top / dpi.DpiScaleY;
                    window.Width = s.Bounds.Width / dpi.DpiScaleX;
                    window.Height = s.Bounds.Height / dpi.DpiScaleY;

                    // Maximizing fits the window exactly to its monitor, even if that monitor has a different scaling.
                    window.WindowState = WindowState.Maximized;
                    window.Show();
                }
            }
            else if (mode.StartsWith("/p"))
            {
                MainWindow window = new MainWindow();
                Int32 previewHandle = Convert.ToInt32(e.Args[1]);
                IntPtr pPreviewHnd = new IntPtr(previewHandle);
                RECT lpRect = new RECT();
                bool bGetRect = Win32API.GetClientRect(pPreviewHnd, ref lpRect);

                HwndSourceParameters sourceParams = new HwndSourceParameters("sourceParams");

                sourceParams.PositionX = 0;
                sourceParams.PositionY = 0;
                sourceParams.Height = lpRect.Bottom - lpRect.Top;
                sourceParams.Width = lpRect.Right - lpRect.Left;
                sourceParams.ParentWindow = pPreviewHnd;
                sourceParams.WindowStyle = (int)(WindowStyles.WS_VISIBLE | WindowStyles.WS_CHILD | WindowStyles.WS_CLIPCHILDREN);

                winWPFContent = new HwndSource(sourceParams);
                winWPFContent.Disposed += (o, args) => window.Close();
                winWPFContent.RootVisual = window.MainGrid;
            }
            else if (mode.StartsWith("/c"))
            {
                SettingsWindow settings = new SettingsWindow();

                // The Screen Saver Settings dialog passes its handle as "/c:1234" and expects a modal child.
                if (mode.StartsWith("/c:") && long.TryParse(mode.Substring(3), out long parentHandle))
                {
                    new WindowInteropHelper(settings).Owner = new IntPtr(parentHandle);
                    settings.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }

                settings.ShowDialog();
            }
        }
    }
}