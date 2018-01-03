using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GitBranchSwitcher.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
    {

        private bool _viewModelIsLinked = false;
        private List<object> _linkedObjects = new List<object>();
        private List<string> _propertiesToIgnore = new List<string>();
        /// <summary>
        /// System properties that should be ignored always
        /// </summary>
        private List<string> _basePropertiesToIgnore = new List<string>() { "PropertiesToIgnore" };


        protected bool ViewModelIsLinked
        {
            get
            {
                return _viewModelIsLinked;
            }
            set
            {
                if (_viewModelIsLinked == value) return;
                _viewModelIsLinked = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// If linking properties from this viewmodel to another view model, this is a list of the properties that 
        /// should not be copied
        /// </summary>
        protected List<string> PropertiesToIgnore
        {
            get
            {
                return _propertiesToIgnore;
            }
            set
            {
                if (_propertiesToIgnore == value) return;
                _propertiesToIgnore = value;
                OnPropertyChanged();
            }
        }


        #region Constructor

        protected ViewModelBase()
        {
        }

        #endregion // Constructor

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Invoked when this object is being removed from the application
        /// and will be subject to garbage collection.
        /// </summary>
        public void Dispose()
        {
            this.OnDispose();
        }

        /// <summary>
        /// Child classes can override this method to perform 
        /// clean-up logic, such as removing event handlers.
        /// </summary>
        protected virtual void OnDispose()
        {
        }
        #endregion // IDisposable Members

        #region View Model Linking

        /// <summary>
        /// Link the properties of one class instance to another class instance of the same type
        /// </summary>
        /// <param name="target">Target class to send our property values to</param>
        public void LinkTo(object target)
        {
            try
            {
                //make sure that the class types are the same
                if (this.GetType() != target.GetType()) throw new Exception("Sender type does not match target type.");
                //attach property changed event to the target class
                this.PropertyChanged += (sender, e) => ViewModel_PropertyChanged(sender, e, target);
                //add the target to the list of linked classes
                this._linkedObjects.Add(target);

                if (this._linkedObjects.Count > 0)
                    this.ViewModelIsLinked = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Remove the linked target class
        /// </summary>
        /// <param name="target">Target class instance to remove the link from</param>
        public void UnLinkFrom(object target)
        {
            try
            {
                //remove the property changed event 
                this.PropertyChanged -= (sender, e) => ViewModel_PropertyChanged(sender, e, target);

                this._linkedObjects.Remove(target);

                if (this._linkedObjects.Count == 0)
                    this.ViewModelIsLinked = false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Copies the matching values from one class to another
        /// </summary>
        /// <param name="sender">The Source Control</param>
        /// <param name="Target">The Target Control</param>
        public void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e, object Target)
        {
            if (sender.GetType() != Target.GetType()) throw new Exception("Sender type does not match target type.");

            //see if we need to ignore this property
            if (!_basePropertiesToIgnore.Contains(e.PropertyName) && !PropertiesToIgnore.Contains(e.PropertyName))
            {
                var newValue = (sender).GetType().GetProperty(e.PropertyName).GetValue((sender), null);
                Target.GetType().GetProperty(e.PropertyName).SetValue(Target, newValue);
            }
        }

        #endregion


        private static readonly Type OwnerType = typeof(ViewModelBase);
        public static readonly DependencyProperty DisableCloseButtonProperty =
            DependencyProperty.RegisterAttached(
                "DisableCloseButton",
                typeof(bool),
                OwnerType,
                new FrameworkPropertyMetadata(false, new PropertyChangedCallback(DisableCloseButtonChangedCallback)));
        private static readonly DependencyPropertyKey IsCloseButtonDisabledKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "IsCloseButtonDisabled",
            typeof(bool),
            OwnerType,
            new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty IsCloseButtonDisabledProperty = IsCloseButtonDisabledKey.DependencyProperty;
        [AttachedPropertyBrowsableForType(typeof(Window))]
        public static bool GetDisableCloseButton(Window obj)
        {
            return (bool)obj.GetValue(DisableCloseButtonProperty);
        }
        [AttachedPropertyBrowsableForType(typeof(Window))]
        public static void SetDisableCloseButton(Window obj, bool value)
        {
            obj.SetValue(DisableCloseButtonProperty, value);
        }
        [AttachedPropertyBrowsableForType(typeof(Window))]
        public static bool GetIsCloseButtonDisabled(Window obj)
        {
            return (bool)obj.GetValue(IsCloseButtonDisabledProperty);
        }
        private static void SetIsCloseButtonDisabled(Window obj, bool value)
        {
            obj.SetValue(IsCloseButtonDisabledKey, value);
        }

        private static void DisableCloseButtonChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as Window;
            if (window == null) return;

            var disableCloseButton = (bool)e.NewValue;
            if (disableCloseButton && !GetIsCloseButtonDisabled(window))
            {
                if (!window.IsLoaded)
                {
                    window.Loaded += DisableCloseWhenLoadedDelegate;
                }
                else
                {
                    DisableCloseButton(window);
                }
                SetIsCloseButtonDisabled(window, true);
            }
            else if (!disableCloseButton && GetIsCloseButtonDisabled(window))
            {
                if (!window.IsLoaded)
                {
                    window.Loaded -= EnableCloseWhenLoadedDelegate;
                }
                else
                {
                    EnableCloseButton(window);
                }
                SetIsCloseButtonDisabled(window, false);
            }


        }
        private static readonly RoutedEventHandler DisableCloseWhenLoadedDelegate = (sender, args) => {
            if (sender is Window == false) return;
            var w = (Window)sender;
            DisableCloseButton(w);
            w.Loaded -= DisableCloseWhenLoadedDelegate;
        };
        private static readonly RoutedEventHandler EnableCloseWhenLoadedDelegate = (sender, args) => {
            if (sender is Window == false) return;
            var w = (Window)sender;
            EnableCloseButton(w);
            w.Loaded -= EnableCloseWhenLoadedDelegate;
        };
        private static void DisableCloseButton(Window w)
        {
        }
        private static void EnableCloseButton(Window w)
        {
        }


    }
}
