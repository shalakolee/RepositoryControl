using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using System.Reflection;
using System.IO;

namespace GitBranchSwitcher.Models
{
    public class RepositoryInformation : ModelBase, IDisposable
    {
        private bool _disposed;
        private bool _selected;
        private bool _canChangeBranches;
        private Branch _selectedBranch;
        private string _branchName;
        private string _path;
        private Repository _repo;
        public Repository Repo
        {
            get
            {
                return _repo;
            }
            set
            {
                if (_repo == value) return;
                _repo = value;
                OnPropertyChanged();
            }
        }

        public List<Branch> LocalBranches
        {
            get
            {
                return Repo.Branches.Where(x => x.IsRemote == false).ToList();
            }
        }
        public Branch SelectedBranch
        {
            get
            {
                if (_selectedBranch == null)
                    _selectedBranch = Repo.Branches.First(x => x.IsCurrentRepositoryHead == true);
                return _selectedBranch;
            }
            set
            {
                if (_selectedBranch == value) return;
                try
                {
                    _selectedBranch = value;
                    checkoutBranch(_selectedBranch.FriendlyName);
                    OnPropertyChanged();

                }
                catch (Exception)
                {

                }
            }
        }
        public Branch CurrentBranch
        {
            get
            {
                return Repo.Branches.First(x=>x.IsCurrentRepositoryHead == true);
            }
        }


        public bool Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                if (value == _selected) return;
                _selected = value;
                OnPropertyChanged();
            }
        }
        public bool CanChangeBranches
        {
            get
            {
                var temp = _canChangeBranches;

                _canChangeBranches = true;
                if (HasUncommittedChanges || HasUnpushedCommits)
                    _canChangeBranches = false;
                if (temp != _canChangeBranches)
                    OnPropertyChanged();
                return _canChangeBranches;
            }
        }
        FileSystemWatcher watcher;

        private void watch()
        {
            watcher = new FileSystemWatcher();
            watcher.Path = _path;
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                   | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Filter = "*.*";
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;
            
        }
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            this.Refresh();
        }

        public static RepositoryInformation GetRepositoryInformationForPath(string path)
        {
            if (Repository.IsValid(path))
            {
                return new RepositoryInformation(path);
            }
            return null;
        }


        public string DisplayName
        {
            get
            {
                System.IO.DirectoryInfo dinfo = new System.IO.DirectoryInfo(_path);
                return dinfo.Name;
            }
        }

        public string CommitHash
        {
            get
            {
                return _repo.Head.Tip.Sha;
            }
        }

        public string BranchName
        {
            get
            {
                var temp = _branchName;
                _branchName = Repo.Head.FriendlyName;
                if (temp != _branchName)
                    OnPropertyChanged();
                return _branchName;
            }
        }

        public bool HasUnpushedCommits
        {
            get
            {
                return Repo.Head.TrackingDetails.AheadBy > 0;
            }
        }
        public int UnPushedCommits
        {
            get
            {
                return Repo.Head.TrackingDetails.AheadBy.Value;
            }
        }

        public bool HasUncommittedChanges
        {
            get
            {
                return _repo.RetrieveStatus().Any(s => s.State != FileStatus.Ignored);
            }
        }
        public int UnCommitedChanges
        {
            get
            {
                return Repo.RetrieveStatus().Where(x => x.State != FileStatus.Ignored).Count();
            }
        }

        public IEnumerable<Commit> Log
        {
            get
            {
                return _repo.Head.Commits;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _repo.Dispose();
                watcher.Dispose();
            }
        }

        private RepositoryInformation(string path)
        {
            Repo = new Repository(path);
            this._path = path;
            this.watch();

        }

        public void Refresh()
        {
            Repo = new Repository(_path);

            PropertyInfo[] properties = typeof(RepositoryInformation).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                OnPropertyChanged(property.Name);
            }
        }

        public bool checkoutBranch(string branchName)
        {
            try
            {
                LibGit2Sharp.Commands.Checkout(Repo, branchName);
                var credentials = Repo.Config.BuildSignature(DateTimeOffset.Now);

                return true;
            }
            catch (Exception ex)
            {
                //if we could not check out the branch
                return false;
            }
        }
    }
}
