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


        public MainWindowViewModel()
        {
            Properties.Settings.Default.RepositoryPath = @"C:\Source";
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





        private RelayCommand _refreshCommand;
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
        private void ExecuteValidateBranchCommand(object param)
        {
            foreach (var repository in Repositories)
            {
                if(repository.Repo.Branches.Where(x=>x.FriendlyName.Contains(param.ToString())).ToList().Count >1)
                {
                    
                }
            }
        }
        private void ExecuteBranchChangedCommand(object param)
        {

        }

    }

}
