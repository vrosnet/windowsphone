﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Storage;
using mega;
using MegaApp.Extensions;
using MegaApp.MegaApi;
using MegaApp.Resources;
using MegaApp.Services;
using Microsoft.Xna.Framework.Media;
using Telerik.Windows.Controls;

namespace MegaApp.Models
{
    /// <summary>
    /// ViewModel of the main MEGA datatype (MNode)
    /// </summary>
    public class NodeViewModel : BaseViewModel
    {
        private readonly MegaSDK _megaSdk;
        // Original MNode object from the MEGA SDK
        private readonly MNode _baseNode;
        // Offset DateTime value to calculate the correct creation and modification time
        private static readonly DateTime OriginalDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        public NodeViewModel(MegaSDK megaSdk, MNode baseNode)
        {
            this._megaSdk = megaSdk;
            this._baseNode = baseNode;
            this.Name = baseNode.getName();
            this.Size = baseNode.getSize();
            this.CreationTime = ConvertDateToString(baseNode.getCreationTime()).ToString("dd MMM yyyy");
            this.SizeAndSuffix = Size.ToStringAndSuffix();
            this.Type = baseNode.getType();

            if(this.Type == MNodeType.TYPE_FOLDER)
                SetFolderInfo();
        }
        #region Methods
        
        private void SetFolderInfo()
        {
            int childFolders = this._megaSdk.getNumChildFolders(this._baseNode);
            int childFiles = this._megaSdk.getNumChildFiles(this._baseNode);
            this.FolderInfo = String.Format("{0} {1} | {2} {3}",
                childFolders, childFolders == 1 ? UiResources.SingleFolder : UiResources.MultipleFolders,
                childFiles, childFiles == 1 ? UiResources.SingleFile : UiResources.MultipleFiles);
        }

        public void SetThumbnailImage()
        {
            if (this.Type == MNodeType.TYPE_FOLDER) return;

            if (this.ThumbnailImage != null) return;

            this.ThumbnailImage = ImageService.GetDefaultFileImage(this.Name);

            if (this.IsImage && this.GetBaseNode().hasThumbnail())
            {
                GetThumbnail();
            }
        }

        private void GetThumbnail()
        {
            string filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, AppResources.ThumbnailsDirectory, this._baseNode.getBase64Handle());

            if (FileService.FileExists(filePath))
            {
                LoadThumbnailImage(filePath);
            }
            else
            {
                this._megaSdk.getThumbnail(this._baseNode, filePath, new GetThumbnailRequestListener(this));
            }
        }

        public void SetPreviewImage()
        {
            if (this.PreviewImage != null) return;
            if (this.IsBusy) return;
            if (!this.IsImage) return;
            if (this._baseNode.hasPreview())
            {
                GetPreview();
            }
        }

        private void GetPreview()
        {
            string filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, AppResources.PreviewsDirectory, this._baseNode.getBase64Handle());

            if (FileService.FileExists(filePath))
            {
                LoadPreviewImage(filePath);
            }
            else
            {
                this._megaSdk.getPreview(this._baseNode, filePath, new GetPreviewRequestListener(this));
            }
        }

        public void SetImage()
        {
            if (this.Image != null) return;
            if (this.IsBusy) return;
            if (!this.IsImage) return;
            GetImage();
        }

        private void GetImage()
        {
            string filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, AppResources.DownloadsDirectory, this._baseNode.getBase64Handle());

            if (FileService.FileExists(filePath))
            {
                LoadImage(filePath);
            }
            else
            {
                this._megaSdk.startDownload(this._baseNode, filePath, new DownloadTransferListener(this));
            }
        }

        public void LoadThumbnailImage(string path)
        {
            this.ThumbnailImage = null;
            this.ThumbnailImage = new BitmapImage();
            this.ThumbnailImage.ImageFailed += ThumbnailImageOnImageFailed;
            this.ThumbnailImage.UriSource = new Uri(path);
        }

        public void LoadPreviewImage(string path)
        {
            this.PreviewImage = null;
            this.PreviewImage = new BitmapImage();
            this.PreviewImage.ImageFailed += PreviewImageOnImageFailed;
            this.PreviewImage.UriSource = new Uri(path);
        }

        public void LoadImage(string path)
        {
            var bitmapImage = new BitmapImage(new Uri(path));
            this.Image = bitmapImage;
        }

        public void SaveImageToCameraRoll()
        {
            if (this.Image == null) return;

            if (MessageBox.Show(AppMessages.SaveImageQuestion, AppMessages.SaveImageQuestion_Title,
                    MessageBoxButton.OKCancel) == MessageBoxResult.Cancel) return;

            if (ImageService.SaveToCameraRoll(this.Name, this.Image))
                MessageBox.Show(AppMessages.ImageSaved, AppMessages.ImageSaved_Title, MessageBoxButton.OK);
            else
                MessageBox.Show(AppMessages.ImageSaveError, AppMessages.ImageSaveError_Title, MessageBoxButton.OK);
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

        #region Events

        private void PreviewImageOnImageFailed(object sender, ExceptionRoutedEventArgs exceptionRoutedEventArgs)
        {
            var bitmapImage = new BitmapImage(new Uri("/Assets/Images/preview_error.png"));
            this.PreviewImage = bitmapImage;
        }

        private void ThumbnailImageOnImageFailed(object sender, ExceptionRoutedEventArgs exceptionRoutedEventArgs)
        {
            this.ThumbnailImage = ImageService.GetDefaultFileImage(this.Name);
        }

        #endregion

        #region Properties

        public string Name { get; private set;}

        public ulong Size { get; private set; }

        public MNodeType Type { get; private set ; }

        public string CreationTime { get; private set; }

        public string SizeAndSuffix { get; private set; }

        public string FolderInfo { get; private set; }

        private BitmapImage _thumbnailImage;
        public BitmapImage ThumbnailImage
        {
            get { return _thumbnailImage; }
            set
            {
                _thumbnailImage = value;
                OnPropertyChanged("ThumbnailImage");
            }
        }

        private BitmapImage _previewImage;
        public BitmapImage PreviewImage
        {
            get { return _previewImage; }
            set
            {
                _previewImage = value;
                OnPropertyChanged("PreviewImage");
            }
        }

        private BitmapImage _image;
        public BitmapImage Image
        {
            get { return _image; }
            set
            {
                _image = value;
                OnPropertyChanged("Image");
            }
        }

        private ulong _totalBytes;
        public ulong TotalBytes
        {
            get { return _totalBytes; }
            set
            {
                _totalBytes = value;
                OnPropertyChanged("TotalBytes");
            }
        }

        private ulong _transferedBytes;
        public ulong TransferedBytes
        {
            get { return _transferedBytes; }
            set
            {
                _transferedBytes = value;
                OnPropertyChanged("TransferedBytes");
            }
        }


        public bool IsImage
        {
            get { return ImageService.IsImage(this.Name); }
        }

        public MNode GetBaseNode()
        {
            return this._baseNode;
        }

        #endregion

        
    }
}
