using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace BatManager {
    public enum ResizeDirection {
        None = 0,
        Left = 1,
        TopLeft = 2,
        Top = 4,
        TopRight = 8,
        Right = 16,
        BottomRight = 32,
        Bottom = 64,
        BottomLeft = 128,
    }

    public static class MoveResizeBehavior {
        #region Move Window

        public static bool GetMoveWindow(DependencyObject obj) {
            return (bool)obj.GetValue(MoveWindowProperty);
        }

        public static void SetMoveWindow(DependencyObject obj, bool value) {
            obj.SetValue(MoveWindowProperty, value);
        }

        public static readonly DependencyProperty MoveWindowProperty = DependencyProperty.RegisterAttached("MoveWindow",
            typeof(bool), typeof(MoveResizeBehavior), new UIPropertyMetadata(false, OnMoveWindowChanged));

        private static void OnMoveWindowChanged(DependencyObject dpo, DependencyPropertyChangedEventArgs args) {
            UIElement uielem = dpo as UIElement;
            if (uielem != null) {
                if ((bool)args.NewValue) {
                    uielem.MouseDown += OnMouseDown;
                } else {
                    uielem.MouseDown -= OnMouseDown;
                }
            }
        }

        private static void OnMouseDown(object sender, MouseButtonEventArgs e) {
            if (e.ClickCount == 2) {
                var window = GetTopLevelControl(sender as UIElement) as Window;
                if (window.WindowState == WindowState.Normal)
                    window.WindowState = WindowState.Maximized;
                else if (window.WindowState == WindowState.Maximized)
                    window.WindowState = WindowState.Normal;
            }
            MoveWindow(GetTopLevelControl(sender as UIElement) as Window);
        }

        #endregion

        #region Resize Window

        public static ResizeDirection GetResizeWindow(DependencyObject obj) {
            return (ResizeDirection)obj.GetValue(ResizeWindowProperty);
        }

        public static void SetResizeWindow(DependencyObject obj, ResizeDirection value) {
            obj.SetValue(ResizeWindowProperty, value);
        }

        public static readonly DependencyProperty ResizeWindowProperty =
            DependencyProperty.RegisterAttached("ResizeWindow", typeof(ResizeDirection), typeof(MoveResizeBehavior),
                new UIPropertyMetadata(ResizeDirection.None, OnResizeWindowChanged));

        private static void OnResizeWindowChanged(DependencyObject dpo, DependencyPropertyChangedEventArgs args) {
            FrameworkElement felem = dpo as FrameworkElement;
            if (felem != null) {
                ResizeDirection dir = (ResizeDirection)args.NewValue;

                if (dir != ResizeDirection.None) {
                    felem.MouseDown += OnMouseDownResize;
                    felem.MouseEnter += OnMouseEnterResize;
                    felem.Cursor = GetCursor(dir);
                } else {
                    felem.MouseDown -= OnMouseDownResize;
                    felem.MouseEnter -= OnMouseEnterResize;
                    felem.Cursor = null;
                }
            }
        }

        private static Cursor GetCursor(ResizeDirection dir) {
            switch (dir) {
                case ResizeDirection.Top:
                case ResizeDirection.Bottom:
                    return Cursors.SizeNS;

                case ResizeDirection.Left:
                case ResizeDirection.Right:
                    return Cursors.SizeWE;

                case ResizeDirection.BottomLeft:
                case ResizeDirection.TopRight:
                    return Cursors.SizeNESW;

                case ResizeDirection.BottomRight:
                case ResizeDirection.TopLeft:
                    return Cursors.SizeNWSE;
            }

            return null;
        }

        private static void OnMouseEnterResize(object sender, MouseEventArgs e) {
            FrameworkElement uielem = sender as FrameworkElement;
            ResizeDirection dir = (ResizeDirection)uielem.GetValue(ResizeWindowProperty);
            if (!(GetTopLevelControl(uielem) is Window window) || window.WindowState != WindowState.Normal) {
                uielem.Cursor = Cursors.Arrow;
                return;
            }
            uielem.Cursor = GetCursor(dir);
        }
        
        private static void OnMouseDownResize(object sender, MouseButtonEventArgs e) {
            UIElement uielem = sender as UIElement;
            ResizeDirection dir = (ResizeDirection)uielem.GetValue(ResizeWindowProperty);
            if (!(GetTopLevelControl(uielem) is Window window) || window.WindowState != WindowState.Normal) return; 
            ResizeWindow(window, dir);
        }

        #endregion

        private static DependencyObject GetTopLevelControl(DependencyObject control) {
            DependencyObject tmp = control;
            DependencyObject parent = control;
            while ((tmp = VisualTreeHelper.GetParent(tmp)) != null) {
                parent = tmp;
            }

            return parent;
        }

        #region Win32 API Interop

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private static void MoveWindow(Window window) {
            ReleaseCapture();
            SendMessage(new WindowInteropHelper(window).Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        private static void ResizeWindow(Window window, ResizeDirection direction) {
            ReleaseCapture();
            HitTestValues dir = GetHitTestValue(direction);
            if (dir != HitTestValues.HTTRANSPARENT)
                SendMessage(new WindowInteropHelper(window).Handle, WM_NCLBUTTONDOWN, (int)dir, 0);
        }


        private static HitTestValues GetHitTestValue(ResizeDirection direction) {
            switch (direction) {
                case ResizeDirection.Left:
                    return HitTestValues.HTLEFT;
                case ResizeDirection.TopLeft:
                    return HitTestValues.HTTOPLEFT;
                case ResizeDirection.Top:
                    return HitTestValues.HTTOP;
                case ResizeDirection.TopRight:
                    return HitTestValues.HTTOPRIGHT;
                case ResizeDirection.Right:
                    return HitTestValues.HTRIGHT;
                case ResizeDirection.BottomRight:
                    return HitTestValues.HTBOTTOMRIGHT;
                case ResizeDirection.Bottom:
                    return HitTestValues.HTBOTTOM;
                case ResizeDirection.BottomLeft:
                    return HitTestValues.HTBOTTOMLEFT;
            }

            return HitTestValues.HTTRANSPARENT;
        }


        private enum HitTestValues {
            HTERROR = -2,
            HTTRANSPARENT = -1,
            HTNOWHERE = 0,
            HTCLIENT = 1,
            HTCAPTION = 2,
            HTSYSMENU = 3,
            HTGROWBOX = 4,
            HTMENU = 5,
            HTHSCROLL = 6,
            HTVSCROLL = 7,
            HTMINBUTTON = 8,
            HTMAXBUTTON = 9,
            HTLEFT = 10,
            HTRIGHT = 11,
            HTTOP = 12,
            HTTOPLEFT = 13,
            HTTOPRIGHT = 14,
            HTBOTTOM = 15,
            HTBOTTOMLEFT = 16,
            HTBOTTOMRIGHT = 17,
            HTBORDER = 18,
            HTOBJECT = 19,
            HTCLOSE = 20,
            HTHELP = 21
        }

        #endregion
    }
}