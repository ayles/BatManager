using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using BatManager.Core;

namespace BatManager.UI {
    public class ExtendedTextBox : RichTextBox {
        public ProcessContainer.ProcessLogHelper Logger {
            get => (ProcessContainer.ProcessLogHelper)GetValue(LoggerProperty);
            set => SetValue(LoggerProperty, value);
        }

        public static readonly DependencyProperty LoggerProperty =
            DependencyProperty.Register("Logger", typeof(ProcessContainer.ProcessLogHelper),
                typeof(ExtendedTextBox), new UIPropertyMetadata(OnLoggerChanged));


        private ProcessContainer.ProcessLogHelper _logHelper;
        private Paragraph _paragraph;
        private Queue<LogString> _newLogs = new Queue<LogString>();
        private Thread _flusher;
        private AutoResetEvent _newLogStringsEvent = new AutoResetEvent(false);
        private volatile bool _refresh;
        
        public ExtendedTextBox() {
            Document.Blocks.Clear();
            _paragraph = new Paragraph();
            Document.Blocks.Add(_paragraph);
            StartFlush();
        }

        private void OnNewLogString(LogString logString) {
            if (logString == null) _refresh = true;
            _newLogStringsEvent.Set();
        }
        
        private void Flush() {
            var maxRows = _logHelper.LogStringsMaxLength;
            
            if (_refresh) {
                _logHelper.CopyLogStrings(_newLogs, false);
                _paragraph.Inlines.Clear();
            } else {
                _logHelper.CopyLogStrings(_newLogs);
            }
            
            if (_newLogs.Count >= maxRows) {
                _paragraph.Inlines.Clear();
                foreach (var logString in _newLogs) {
                    _paragraph.Inlines.Add(GetInlineFromLogString(logString)); 
                }
            } else {
                for (var i = _paragraph.Inlines.Count + _newLogs.Count - maxRows; i > 0; --i) {
                    _paragraph.Inlines.Remove(_paragraph.Inlines.FirstInline);
                }

                foreach (var t in _newLogs) {
                    _paragraph.Inlines.Add(GetInlineFromLogString(t));
                }
            }

            _refresh = false;
            _newLogs.Clear();
            
            if (Parent is ScrollViewer scrollViewer) {
                if (scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 0.1) {
                    scrollViewer.ScrollToEnd();
                }
            }
        }

        private void StartFlush() {
            if (_flusher != null) return;
            _flusher = new Thread(() => {
                while (true) {
                    _newLogStringsEvent.WaitOne();
                    Dispatcher.Invoke(new Action(Flush));
                    _newLogStringsEvent.Reset();
                    Thread.Sleep(50);
                }
            });
            _flusher.IsBackground = true;
            _flusher.Start();
        }

        private static Inline GetInlineFromLogString(LogString logString) {
            Brush brush;
            switch (logString.Type) {
                case LogString.LogType.Debug:
                    brush = Brushes.DeepSkyBlue;
                    break;
                case LogString.LogType.Info:
                    brush = Brushes.LightGray;
                    break;
                case LogString.LogType.Error:
                    brush = Brushes.Red;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new Run {
                Text = logString.Data,
                Foreground = brush
            };
        }

        private static void OnLoggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var extendedTextBox = d as ExtendedTextBox;
            var logHelper = e.NewValue as ProcessContainer.ProcessLogHelper;
            if (extendedTextBox == null || logHelper == null) return;

            extendedTextBox._logHelper = logHelper;
            extendedTextBox._refresh = true;
            extendedTextBox._newLogStringsEvent.Set();
            
            if (e.OldValue is ProcessContainer.ProcessLogHelper logHelperOld) {
                logHelperOld.NewLogStringEvent -= extendedTextBox.OnNewLogString;
            }

            logHelper.NewLogStringEvent += extendedTextBox.OnNewLogString;
        }
    }
}