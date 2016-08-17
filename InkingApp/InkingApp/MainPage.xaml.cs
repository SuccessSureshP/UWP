using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace InkingApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            inkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Touch;
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;


        }
        int penSize = 1;
        void IncreaseSize(object sender, Windows.UI.Xaml.RoutedEventArgs e)

        {

            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
            penSize += 3;
            drawingAttributes.Size = new Size(penSize, penSize);

            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);

        }

        void DecreaseSize(object sender, Windows.UI.Xaml.RoutedEventArgs e)

        {

            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
            penSize -= 3;
            if (penSize < 0)
                penSize = 1;
            drawingAttributes.Size = new Size(penSize, penSize);

            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);

        }
        void SetRedPen(object sender, Windows.UI.Xaml.RoutedEventArgs e)

        {

            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();

            drawingAttributes.Color = Windows.UI.Colors.Red;
            drawingAttributes.Size = new Size(penSize, penSize);

            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);

        }

        void SetCalligraphyPen(object sender, RoutedEventArgs e)
        {

            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();

            drawingAttributes.Color = Windows.UI.Colors.Black;

            drawingAttributes.PenTip = PenTipShape.Rectangle;

            drawingAttributes.Size = new Size(penSize, penSize);



            double radians = 45.0 * Math.PI / 180;

            System.Numerics.Matrix3x2 matrix = System.Numerics.Matrix3x2.CreateRotation((float)radians);

            drawingAttributes.PenTipTransform = matrix;



            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);

        }

        void SetHighlighterPen(object sender,RoutedEventArgs e)
        {
            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();

            drawingAttributes.Color = Windows.UI.Colors.Yellow;
            drawingAttributes.DrawAsHighlighter = true;
            drawingAttributes.Size = new Size(penSize, penSize);
            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
        }
        void Clear(object sender, RoutedEventArgs e)
        {
            inkCanvas.InkPresenter.StrokeContainer.Clear();
        }

        void Eraser(object sender,RoutedEventArgs e)
        {
            if (EraserButton.Content.ToString().Equals("Eraser"))
            {
                EraserButton.Content = "Eraser ON";
                EraserButton.Background = new SolidColorBrush(Colors.Wheat);
                inkCanvas.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Erasing;
            }
            else
            {
                EraserButton.Content = "Eraser";
                EraserButton.Background = new SolidColorBrush(Colors.LightGray);
                inkCanvas.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
            }

        }
        async void Save(object sender, Windows.UI.Xaml.RoutedEventArgs e)

        {

            if (inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Count > 0)
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.FileTypeChoices.Add("GIF with embedded ISF", new System.Collections.Generic.List<string> { ".gif" });
                StorageFile file = await savePicker.PickSaveFileAsync();
                if (null != file)
                {
                    using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(stream);
                    }
                }
            }
        }
                
        async void Load(object sender, Windows.UI.Xaml.RoutedEventArgs e)

        {

            var loadPicker = new Windows.Storage.Pickers.FileOpenPicker();

            loadPicker.FileTypeFilter.Add(".gif");

            StorageFile file = await loadPicker.PickSingleFileAsync();

            if (null != file)

            {

                using (var stream = await file.OpenSequentialReadAsync())

                {

                    try

                    {

                        await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(stream);

                    }

                    catch (Exception ex)

                    { }

                }

            }

        }



        #region Extra Stuff

        async void OnRenderToSecondImage(object sender, RoutedEventArgs e)
        {
            // The original bitmap from the screen, missing the ink.
            var renderBitmap = new RenderTargetBitmap();
            await renderBitmap.RenderAsync(this.parentGrid);

            var bitmapSizeAt96Dpi = new Size(
              renderBitmap.PixelWidth,
              renderBitmap.PixelHeight);

            var renderBitmapPixels = await renderBitmap.GetPixelsAsync();

            var win2DDevice = CanvasDevice.GetSharedDevice();

            var displayInfo = DisplayInformation.GetForCurrentView();

            using (var win2DTarget = new CanvasRenderTarget(
              win2DDevice,
              (float)this.parentGrid.ActualWidth,
              (float)this.parentGrid.ActualHeight,
              96.0f))
            {
                using (var win2dSession = win2DTarget.CreateDrawingSession())
                {
                    using (var win2dRenderedBitmap =
                      CanvasBitmap.CreateFromBytes(
                        win2DDevice,
                        renderBitmapPixels,
                        (int)bitmapSizeAt96Dpi.Width,
                        (int)bitmapSizeAt96Dpi.Height,
                        Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                        96.0f))
                    {
                        win2dSession.DrawImage(win2dRenderedBitmap,
                          new Rect(0, 0, win2DTarget.SizeInPixels.Width, win2DTarget.SizeInPixels.Height),
                          new Rect(0, 0, bitmapSizeAt96Dpi.Width, bitmapSizeAt96Dpi.Height));
                    }
                    win2dSession.Units = CanvasUnits.Pixels;
                    win2dSession.DrawInk(this.inkCanvas.InkPresenter.StrokeContainer.GetStrokes());
                }
                // Get the output into a software bitmap.
                //var outputBitmap = new SoftwareBitmap(
                // BitmapPixelFormat.Bgra8,
                // (int)win2DTarget.SizeInPixels.Width,
                // (int)win2DTarget.SizeInPixels.Height,
                // BitmapAlphaMode.Premultiplied);

                //outputBitmap.CopyFromBuffer(
                //          win2DTarget.GetPixelBytes().AsBuffer());

                //// Now feed that to the XAML image.
                //var source = new SoftwareBitmapSource();
                //await source.SetBitmapAsync(outputBitmap);
                //this.SecondImage.Source = source;

                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.FileTypeChoices.Add("GIF with embedded ISF", new System.Collections.Generic.List<string> { ".png" });
                StorageFile targetFile = await savePicker.PickSaveFileAsync();

                if (targetFile != null)
                {
                    using (var stream = await targetFile.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        var logicalDpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                        encoder.SetPixelData(
                            BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Ignore,
                             win2DTarget.SizeInPixels.Width,
                            win2DTarget.SizeInPixels.Height,
                            //(uint)renderBitmap.PixelWidth,
                            //(uint)renderBitmap.PixelHeight,
                            logicalDpi,
                            logicalDpi,
                            win2DTarget.GetPixelBytes());

                        await encoder.FlushAsync();
                    }
                }
            }
        }
        InMemoryRandomAccessStream cachsedFile = null;

        private async void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {

            DataRequest request = args.Request;
            IStorageFile file = null;
            var deferral = request.GetDeferral();
            try
            {

                //var LocalDataFolder = ApplicationData.Current.LocalFolder;                
                //file = await LocalDataFolder.GetFileAsync("InkingFile.gif");

                var sourceFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                var assetsFolder = await sourceFolder.GetFolderAsync("Assets");
                file = await assetsFolder.GetFileAsync("InkingFile.gif");
                

                List<IStorageItem> st_items = new List<IStorageItem>();
                st_items.Add(file);

                request.Data.SetStorageItems(st_items);
                request.Data.Properties.Title = "Check out my Inking with Inking Alphabet App!";
                request.Data.Properties.Description = "I used Inking Alphabets App to draw this Inking. You can open with same App and edit Ink Strokes.";
            }
            catch (Exception ex)
            {
                MessageDialog msg = new MessageDialog("Something went wrong! Sorry. Please try again");
                await msg.ShowAsync();                
            }
            finally
            {
                deferral.Complete();
            }
            



            //DataRequest request = args.Request;

            //var deferral = request.GetDeferral();

            //try
            //{
            //    var ms = await getFileStream();
            //    var randomAccessStreamReference = RandomAccessStreamReference.CreateFromStream(ms);
            //    request.Data.SetBitmap(randomAccessStreamReference);                
            //    //request.Data.SetText("sample");
            //    request.Data.Properties.Title = "I used InkAlphabets App to draw this.";
            //}
            //catch (Exception exp)
            //{

            //}
            //finally
            //{
            //    deferral.Complete();
            //}
        }


        private void ShareAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            //DataTransferManager.ShowShareUI();
        }

        async System.Threading.Tasks.Task<InMemoryRandomAccessStream> getFileStream()
        {
            // The original bitmap from the screen, missing the ink.
            var renderBitmap = new RenderTargetBitmap();
            await renderBitmap.RenderAsync(this.parentGrid);

            var bitmapSizeAt96Dpi = new Size(
              renderBitmap.PixelWidth,
              renderBitmap.PixelHeight);

            var renderBitmapPixels = await renderBitmap.GetPixelsAsync();

            var win2DDevice = CanvasDevice.GetSharedDevice();

            var displayInfo = DisplayInformation.GetForCurrentView();

            using (var win2DTarget = new CanvasRenderTarget(
              win2DDevice,
              (float)this.parentGrid.ActualWidth,
              (float)this.parentGrid.ActualHeight,
              96.0f))
            {
                using (var win2dSession = win2DTarget.CreateDrawingSession())
                {
                    using (var win2dRenderedBitmap =
                      CanvasBitmap.CreateFromBytes(
                        win2DDevice,
                        renderBitmapPixels,
                        (int)bitmapSizeAt96Dpi.Width,
                        (int)bitmapSizeAt96Dpi.Height,
                        Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                        96.0f))
                    {
                        win2dSession.DrawImage(win2dRenderedBitmap,
                          new Rect(0, 0, win2DTarget.SizeInPixels.Width, win2DTarget.SizeInPixels.Height),
                          new Rect(0, 0, bitmapSizeAt96Dpi.Width, bitmapSizeAt96Dpi.Height));
                    }
                    win2dSession.Units = CanvasUnits.Pixels;
                    win2dSession.DrawInk(this.inkCanvas.InkPresenter.StrokeContainer.GetStrokes());
                }           

                InMemoryRandomAccessStream ms = new InMemoryRandomAccessStream();
                
                    var logicalDpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, ms);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Ignore,
                        win2DTarget.SizeInPixels.Width,
                        win2DTarget.SizeInPixels.Height,
                        logicalDpi,
                        logicalDpi,
                        win2DTarget.GetPixelBytes());

                    await encoder.FlushAsync();
                ms.Seek(0);

                    return ms;
                
            }
        }

        private async void ShareInkingButton_Click(object sender, RoutedEventArgs e)
        {

            //DataTransferManager.ShowShareUI();
            //return;
            try
            {

                if (inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Count <= 0)
                {
                    var msgDialog = new MessageDialog("You haven't started inking yet. Start Inking and then share!");
                    msgDialog.Commands.Add(new UICommand("Ok"));
                    await msgDialog.ShowAsync();
                    return;
                }

                //var LocalDataFolder = ApplicationData.Current.LocalFolder;

                var sourceFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                var assetsFolder = await sourceFolder.GetFolderAsync("Assets");


                StorageFile file;
                //try
                //{
                //    if (_localSettings.Keys.Contains("ShareInkingFilename"))
                //    {
                //        file = await LocalDataFolder.GetFileAsync(_localSettings["ShareInkingFilename"].ToString());
                //        await file.DeleteAsync();
                //    }
                //}
                //catch (FileNotFoundException exp)
                //{

                //}
                file = null;
                file = await assetsFolder.CreateFileAsync("InkingFile.gif", CreationCollisionOption.ReplaceExisting);
                //_localSettings["ShareInkingFilename"] = file.Name;

                var filestream = await file.OpenAsync(FileAccessMode.ReadWrite);

                await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(filestream);
                //ShareType = "ShareInkStrokes";
                DataTransferManager.ShowShareUI();
            }
            catch (Exception ex)
            {
                MessageDialog msg = new MessageDialog("Something went wrong! Sorry. Please try again");
                await msg.ShowAsync();                
            }

        }
        #endregion
    }
}
