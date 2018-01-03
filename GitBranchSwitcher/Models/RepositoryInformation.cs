using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using System.Reflection;
using System.IO;
using GitBranchSwitcher.Commands;

namespace GitBranchSwitcher.Models
{
    public class RepositoryInformation : ModelBase, IDisposable
    {
        private bool _disposed;
        private bool _selected;
        private bool _canChangeBranches;
        private bool _hasBranch = false;

        private Branch _selectedLocalBranch;
        private Branch _selectedRemoteBranch;
        private Branch _lastBranch;


        public bool HasBranch
        {
            get
            {
                return _hasBranch;
            }
            set
            {
                if (_hasBranch == value) return;
                _hasBranch = value;
                OnPropertyChanged();
            }
        }
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
        public List<Branch> RemoteBranches
        {
            get
            {
                return Repo.Branches.Where(x => x.IsRemote == true).ToList();
            }
        }

        public Branch SelectedLocalBranch
        {
            get
            {
                if (_selectedLocalBranch == null)
                    SelectedLocalBranch = Repo.Branches.First(x => x.IsCurrentRepositoryHead == true);
                return _selectedLocalBranch;
            }
            set
            {
                if (_selectedLocalBranch == value) return;
                _lastBranch = _selectedLocalBranch;
                _selectedLocalBranch = value;
                OnPropertyChanged();
            }
        }
        public Branch SelectedRemoteBranch
        {
            get
            {
                return _selectedRemoteBranch;
            }
            set
            {
                if (_selectedRemoteBranch == value) return;
                _selectedRemoteBranch = value;
                OnPropertyChanged();
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
            watcher.IncludeSubdirectories = true;
            
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

        public async Task<bool> checkoutBranch(string branchName)
        {
            return await Task.Run<bool>(() =>
            {
                try
                {
                    PullOptions options = new PullOptions();
                    options.FetchOptions = new FetchOptions();
                    options.FetchOptions.CredentialsProvider = new LibGit2Sharp.Handlers.CredentialsHandler(
                        (url, usernameFromUrl, types) =>
                            new UsernamePasswordCredentials()
                            {
                                Username = Properties.Settings.Default.Username,
                                Password = Properties.Settings.Default.Password
                            });
                    var merger = Repo.Config.BuildSignature(DateTimeOffset.Now);

                    //check to see if the branch is remote
                    string localbranchName = branchName;
                    if (branchName.Contains("origin/"))
                    {
                        localbranchName = branchName.Replace("origin/", "");
                        Branch trackedBranch = Repo.Branches[branchName];
                        //check if we have the same local branch
                        if (Repo.Branches[localbranchName] != null)
                        {
                            //we already have the branch, lets checkout and pull
                            LibGit2Sharp.Commands.Checkout(Repo, localbranchName);
                            try
                            {
                                LibGit2Sharp.Commands.Pull(Repo, merger, options);
                            }
                            catch (Exception)
                            {

                            }

                        }
                        else
                        {
                            Branch branch = Repo.CreateBranch(localbranchName, trackedBranch.Tip);
                            Repo.Branches.Update(branch, b => b.TrackedBranch = trackedBranch.CanonicalName);
                            LibGit2Sharp.Commands.Checkout(Repo, localbranchName);
                        }

                    }
                    else
                    {
                        LibGit2Sharp.Commands.Checkout(Repo, branchName);
                        try
                        {
                            LibGit2Sharp.Commands.Pull(Repo, merger, options);

                        }
                        catch (Exception)
                        {

                        }

                    }

                    
                    return true;
                }
                catch (Exception ex)
                {
                    //if we could not check out the branch
                    throw ex;
                }
            });

        }

        private RelayCommand _selectedBranchChangedCommand;
        public RelayCommand SelectedBranchChangedCommand
        {
            get
            {
                if (_selectedBranchChangedCommand == null)
                    _selectedBranchChangedCommand = new RelayCommand(e => ExecuteSelectedBranchChangedCommand(e));
                return _selectedBranchChangedCommand;
            }
        }
        private void ExecuteSelectedBranchChangedCommand(object param)
        {
            if (param == null) return;
            Branch myBranch = (Branch)param;
            if(_lastBranch == null)
            {
                if (myBranch.FriendlyName.Replace("origin/", "") == _selectedLocalBranch.FriendlyName.Replace("origin/", "")) return;
            }
            else
            {
                if (myBranch.FriendlyName.Replace("origin/", "") == _lastBranch.FriendlyName.Replace("origin/", "")) return;
            }
            bool branchupdated = Task.Run(() => checkoutBranch(myBranch.FriendlyName)).Result;

            SelectedRemoteBranch = null;
            _lastBranch = null;
            SelectedLocalBranch = Repo.Branches.First(x => x.IsCurrentRepositoryHead == true);

          


        }

    }
}
