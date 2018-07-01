﻿using System;
using System.Windows;
using System.Windows.Input;

namespace BatManager.Util {
    public class DelegateCommand : ICommand {
        private readonly Predicate<object> _canExecute;
        private readonly Action<object> _execute;

        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action<object> execute)
            : this(execute, null) { }

        public DelegateCommand(Action<object> execute,
            Predicate<object> canExecute) {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) {
            if (_canExecute == null) {
                return true;
            }

            return _canExecute(parameter);
        }

        public void Execute(object parameter) {
            if (CanExecute(parameter))
                _execute(parameter);
        }

        public void RaiseCanExecuteChanged() {
            Application.Current?.Dispatcher?.Invoke(() => { CanExecuteChanged?.Invoke(this, EventArgs.Empty); });
        }
    }
}