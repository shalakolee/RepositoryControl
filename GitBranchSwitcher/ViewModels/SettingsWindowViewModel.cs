using GitBranchSwitcher.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitBranchSwitcher.ViewModels
{
    public class SettingsWindowViewModel : ViewModelBase
    {
        private bool _isVisible;
        public bool IsVisible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                if (_isVisible == value) return;
                _isVisible = value;
                OnPropertyChanged();
            }
        }


        private RelayCommand _exitCommand;
        private RelayCommand _saveSettingsCommand;

        public RelayCommand ExitCommand
        {
            get
            {
                if (_exitCommand == null)
                    _exitCommand = new RelayCommand(e => ExecuteExitCommand());
                return _exitCommand;
            }
        }
        public RelayCommand SaveSettingsCommand
        {
            get
            {
                if (_saveSettingsCommand == null)
                    _saveSettingsCommand = new RelayCommand(e => ExecuteSaveSettingsCommand());
                return _saveSettingsCommand;
            }
        }

        public void ExecuteExitCommand()
        {
            IsVisible = false;
        }
        public void ExecuteSaveSettingsCommand()
        {
            Properties.Settings.Default.Save();
            ExecuteExitCommand();
        }


    }
}
