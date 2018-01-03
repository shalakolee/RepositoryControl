using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using LibGit2Sharp;
using System.Collections.ObjectModel;
using System.Windows.Data;
using GitBranchSwitcher.Commands;
using System.Windows;

namespace GitBranchSwitcher.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string _targetBranch;
        private bool _allSelected;
        private static ObservableCollection<Models.RepositoryInformation> _repositories;

        public bool AllSelected
        {
            get
            {
                return _allSelected;
            }
            set
            {
                _allSelected = value;
                foreach (var repository in Repositories)
                    repository.Selected = value;
                OnPropertyChanged();
            }
        }
        public string TargetBranch
        {
            get
            {
                return _targetBranch;
            }
            set
            {
                if (_targetBranch == value) return;
                _targetBranch = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Base folder where all the repositories are stored
        /// </summary>
        public static string RepositoryPath
        {
            get
            {
                return Properties.Settings.Default.RepositoryPath;
            }
        }
        public ObservableCollection<Models.RepositoryInformation> Repositories
        {
            get
            {
                return _repositories;
            }
            set
            {
                if (value == _repositories) return;
                _repositories = value;
                OnPropertyChanged();
            }
        }

        public SettingsWindow SettingsWindow = new SettingsWindow();
        public SettingsWindowViewModel SettingsWindowViewModel = new SettingsWindowViewModel();





        public MainWindowViewModel()
        {
            SettingsWindow.DataContext = SettingsWindowViewModel;
            MainWindowStartup();
        }

        public async void MainWindowStartup()
        {
            if (string.IsNullOrEmpty(Properties.Settings.Default.RepositoryPath))
                ExecuteOpenSettingsCommand(null);

            while (SettingsWindowViewModel.IsVisible)
            {
                await Task.Run(()=>System.Threading.Thread.Sleep(20));
            }

            Repositories = GetAvailableRepositories();
        }
        public static ObservableCollection<Models.RepositoryInformation> GetAvailableRepositories()
        {
            ObservableCollection<Models.RepositoryInformation> repositories = new ObservableCollection<Models.RepositoryInformation>();
            foreach (string dir in Directory.GetDirectories(RepositoryPath))
            {
                var repository = Models.RepositoryInformation.GetRepositoryInformationForPath(dir);
                if (null != repository)
                {
                    repositories.Add(repository);
                }
            };
            return repositories;
        }
        public void validateRepositoryPath(string originalPath)
        {
            if (originalPath != RepositoryPath)
                OnPropertyChanged("RepositoryPath");


        }





        private RelayCommand _exitCommand;
        private RelayCommand _refreshCommand;
        private RelayCommand _openSettingsCommand;
        private RelayCommand _validateBranchCommand;
        private RelayCommand _branchChangedCommand;



        public RelayCommand RefreshCommand
        {
            get
            {
                if (_refreshCommand == null)
                    _refreshCommand = new RelayCommand(e => ExecuteRefreshCommand(e));
                return _refreshCommand;
            }
        }
        public RelayCommand ExitCommand
        {
            get
            {
                if (_exitCommand == null)
                    _exitCommand = new RelayCommand(e => ExecuteExitCommand(e));
                return _exitCommand;
            }
        }
        public RelayCommand OpenSettingsCommand
        {
            get
            {
                if (_openSettingsCommand == null)
                    _openSettingsCommand = new RelayCommand(e => ExecuteOpenSettingsCommand(e), e=>canExecuteOpenSettingsCommand(e));
                return _openSettingsCommand;
            }
        }
        public RelayCommand ValidateBranchCommand
        {
            get
            {
                if (_validateBranchCommand == null)
                    _validateBranchCommand = new RelayCommand(e => ExecuteValidateBranchCommand(e));
                return _validateBranchCommand;
            }
        }
        public RelayCommand BranchChangedCommand
        {
            get
            {
                if (_branchChangedCommand == null)
                    _branchChangedCommand = new RelayCommand(e => ExecuteBranchChangedCommand(e));
                return _branchChangedCommand;
            }
        }

        private void ExecuteRefreshCommand(object param)
        {
            foreach (var repository in Repositories)
            {
                //repository.checkoutBranch("dev");
            }
        }
        private void ExecuteExitCommand(object param)
        {
            Application.Current.Shutdown();
        }
        private void ExecuteOpenSettingsCommand(object param)
        {
            SettingsWindowViewModel.IsVisible = true;
        }
        private void ExecuteValidateBranchCommand(object param)
                        {
            if(param == null)
                foreach (var repository in Repositories)
                {
                    repository.HasBranch = false;
                }

            foreach (var repository in Repositories.Where(x => x.Selected == true))
            {
                if(repository.Repo.Branches.Where(x=>x.FriendlyName.Contains(param.ToString())).ToList().Count >1)
                    repository.HasBranch = true;
                repository.HasBranch = false;
                repository.Refresh();
            }
            

        }
        private void ExecuteBranchChangedCommand(object param)
        {

        }

        private bool canExecuteOpenSettingsCommand(object param)
        {
            if (SettingsWindowViewModel.IsVisible) return false;
            return true;
        }
    }

}
