﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Windows.Storage;
using mega;
using MegaApp.Classes;
using MegaApp.Enums;
using MegaApp.Interfaces;
using MegaApp.MegaApi;
using MegaApp.Pages;
using MegaApp.Resources;
using MegaApp.Services;
using Telerik.Windows.Controls;

namespace MegaApp.Models
{
    /// <summary>
    /// ViewModel of the main MEGA datatype (MNode)
    /// </summary>
    public abstract class NodeViewModel : BaseAppInfoAwareViewModel, IMegaNode
    {
        // Offset DateTime value to calculate the correct creation and modification time
        private static readonly DateTime OriginalDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        protected NodeViewModel(MegaSDK megaSdk, AppInformation appInformation, MNode megaNode,
            ObservableCollection<IMegaNode> parentCollection = null, ObservableCollection<IMegaNode> childCollection = null)
            : base(megaSdk, appInformation)
        {
            Update(megaNode);
            SetDefaultValues();
            
            this.ParentCollection = parentCollection;
            this.ChildCollection = childCollection;
        }

        #region Private Methods
        
        private void SetDefaultValues()
        {
            this.IsMultiSelected = false;
            this.DisplayMode = NodeDisplayMode.Normal;

            if (this.Type == MNodeType.TYPE_FOLDER) return;

            if (FileService.FileExists(ThumbnailPath))
            {
                this.IsDefaultImage = false;
                this.ThumbnailImageUri = new Uri(ThumbnailPath);
        }
            else
            {
                this.IsDefaultImage = true;
                this.DefaultImagePathData = ImageService.GetDefaultFileTypePathData(this.Name);
            }
        }

        private void GetThumbnail()
        {
            if (Convert.ToBoolean(MegaSdk.isLoggedIn()))
                this.MegaSdk.getThumbnail(OriginalMNode, ThumbnailPath, new GetThumbnailRequestListener(this));
        }

        /// <summary>
        /// Convert the MEGA time to a C# DateTime object in local time
        /// </summary>
        /// <param name="time">MEGA time</param>
        /// <returns>DateTime object in local time</returns>
        private static DateTime ConvertDateToString(ulong time)
        {
            return OriginalDateTime.AddSeconds(time).ToLocalTime();
        }

        #endregion

        #region IMegaNode Interface

        public async Task<NodeActionResult> Rename()
        {
            // User must be online to perform this operation
            if (!IsUserOnline()) return NodeActionResult.NotOnline;

            // Add the current name to the rename dialog textbox
            var textboxStyle = new Style(typeof(RadTextBox));
            textboxStyle.Setters.Add(new Setter(TextBox.TextProperty, this.Name));

            // Create the rename dialog and show it to the user
            var inputPromptClosedEventArgs = await RadInputPrompt.ShowAsync(new [] { UiResources.Rename.ToLower(), UiResources.Cancel.ToLower() },
                UiResources.RenameItem, vibrate: false, inputStyle: textboxStyle);

            // If the user did not press OK, do nothing
            if (inputPromptClosedEventArgs.Result != DialogResult.OK) return NodeActionResult.Cancelled;

            // Rename the node
            this.MegaSdk.renameNode(this.OriginalMNode, inputPromptClosedEventArgs.Text, new RenameNodeRequestListener(this));
            
            return NodeActionResult.IsBusy;
        }

        public NodeActionResult Move(IMegaNode newParentNode)
        {
            // User must be online to perform this operation
            if (!IsUserOnline()) return NodeActionResult.NotOnline;

            if (this.MegaSdk.checkMove(this.OriginalMNode, newParentNode.OriginalMNode).getErrorCode() == MErrorType.API_OK)
            {
                this.MegaSdk.moveNode(this.OriginalMNode, newParentNode.OriginalMNode,
                    new MoveNodeRequestListener());
                return NodeActionResult.IsBusy;
        }

            return NodeActionResult.Failed;
        }

        public NodeActionResult Remove(bool isMultiRemove, AutoResetEvent waitEventRequest = null)
        {
            // User must be online to perform this operation
            if (!IsUserOnline()) return NodeActionResult.NotOnline;

            // Looking for the absolute parent of the node to remove
            MNode parentNode;
            MNode absoluteParentNode = this.OriginalMNode;

            while ((parentNode = this.MegaSdk.getParentNode(absoluteParentNode)) != null)
                absoluteParentNode = parentNode;

            // If the node is on the rubbish bin, delete it forever
            if (absoluteParentNode.getType() == MNodeType.TYPE_RUBBISH)
            {
                if (!isMultiRemove)
                    if (MessageBox.Show(String.Format(AppMessages.RemoveItemQuestion, this.Name),
                        AppMessages.RemoveItemQuestion_Title, MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                            return NodeActionResult.Cancelled;

                this.MegaSdk.remove(this.OriginalMNode, new RemoveNodeRequestListener(this, isMultiRemove, absoluteParentNode.getType(),
                    waitEventRequest));
                
                return NodeActionResult.IsBusy;
        }

            // if the node in in the Cloud Drive, move it to rubbish bin
            if (!isMultiRemove)
                if (MessageBox.Show(String.Format(AppMessages.MoveToRubbishBinQuestion, this.Name),
                    AppMessages.MoveToRubbishBinQuestion_Title, MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    return NodeActionResult.Cancelled;

            this.MegaSdk.moveNode(this.OriginalMNode, this.MegaSdk.getRubbishNode(),
                new RemoveNodeRequestListener(this, isMultiRemove, absoluteParentNode.getType(), waitEventRequest));

            return NodeActionResult.IsBusy;
        }

        public NodeActionResult Delete()
        {
            // User must be online to perform this operation
            if (!IsUserOnline()) return NodeActionResult.NotOnline;

            if (MessageBox.Show(String.Format(AppMessages.DeleteNodeQuestion, this.Name),
                    AppMessages.DeleteNodeQuestion_Title, MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                return NodeActionResult.Cancelled;

            this.MegaSdk.remove(this.OriginalMNode, new RemoveNodeRequestListener(this, false, this.Type, null));

            return NodeActionResult.IsBusy;
        }

        public NodeActionResult GetLink()
        {
            // User must be online to perform this operation
            if (!IsUserOnline()) return NodeActionResult.NotOnline;

            this.MegaSdk.exportNode(this.OriginalMNode, new ExportNodeRequestListener());

            return NodeActionResult.IsBusy;
        }

#if WINDOWS_PHONE_80
        public virtual void Download(TransferQueu transferQueu, string downloadPath = null)
        {
            if (!IsUserOnline()) return;
            NavigateService.NavigateTo(typeof(DownloadPage), NavigationParameter.Normal, this);
        }
#elif WINDOWS_PHONE_81
        public async void Download(TransferQueu transferQueu, string downloadPath = null)
        {
            // User must be online to perform this operation
            if (!IsUserOnline()) return;
            
            if (AppInformation.PickerOrAsyncDialogIsOpen) return;

            if (downloadPath == null)
            {
                AppInformation.PickerOrAsyncDialogIsOpen = true;
                if (!await FolderService.SelectDownloadFolder(this)) return;
            }
                

            this.Transfer.DownloadFolderPath = downloadPath;
            transferQueu.Add(this.Transfer);
            this.Transfer.StartTransfer();

            // TODO Remove this global declaration in method
            App.CloudDrive.NoFolderUpAction = true;
            NavigateService.NavigateTo(typeof(TransferPage), NavigationParameter.Downloads);
        }
#endif

        public void Update(MNode megaNode)
        {
            OriginalMNode = megaNode;
            this.Handle = megaNode.getHandle();
            this.Name = megaNode.getName();
            this.Size = megaNode.getSize();
            this.CreationTime = ConvertDateToString(megaNode.getCreationTime()).ToString("dd MMM yyyy");
            this.ModificationTime = ConvertDateToString(megaNode.getModificationTime()).ToString("dd MMM yyyy");
            this.Type = megaNode.getType();
        }

        public virtual void Open()
        {
            throw new NotImplementedException();
        }

        public void SetThumbnailImage()
        {
            if (this.Type == MNodeType.TYPE_FOLDER) return;

            if (this.ThumbnailImageUri != null && !IsDefaultImage) return;
            
            if (this.IsImage || this.OriginalMNode.hasThumbnail())
            {
                GetThumbnail();
            }
        }

        #region Interface Properties

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetField(ref _name, value); }
        }

        public string CreationTime { get; private set; }

        public string ModificationTime { get; private set; }

        public string ThumbnailPath
            {
            get { return Path.Combine(ApplicationData.Current.LocalFolder.Path, 
                                      AppResources.ThumbnailsDirectory, 
                                      this.OriginalMNode.getBase64Handle());
            }
        }

        private string _information;
        public string Information
        {
            get { return _information; }
            set { SetField(ref _information, value); }
        }

        public ulong Handle { get; set; }

        public ulong Size { get; private set; }

        public ObservableCollection<IMegaNode> ParentCollection { get; set; }

        public ObservableCollection<IMegaNode> ChildCollection { get; set; }

        public MNodeType Type { get; private set; }

        private NodeDisplayMode _displayMode;
        public NodeDisplayMode DisplayMode
        {
            get { return _displayMode; }
            set { SetField(ref _displayMode, value); }
        }

        private bool _isMultiSelected;
        public bool IsMultiSelected
        {
            get { return _isMultiSelected; }
            set { SetField(ref _isMultiSelected, value); }
        }

        public bool IsImage
        {
            get { return ImageService.IsImage(this.Name); }
        }

        private bool _IsDefaultImage;
        public bool IsDefaultImage
        {
            get { return _IsDefaultImage; }
            set { SetField(ref _IsDefaultImage, value); }
        }

        private Uri _thumbnailImageUri;
        public Uri ThumbnailImageUri
        {
            get { return _thumbnailImageUri; }
            set { SetField(ref _thumbnailImageUri, value); }
        }

        private string _defaultImagePathData;
        public string DefaultImagePathData
        {
            get { return _defaultImagePathData; }
            set { SetField(ref _defaultImagePathData, value); }
        }

        public TransferObjectModel Transfer { get; set; }

        public MNode OriginalMNode { get; private set; }

        #endregion

        #endregion
    }
}