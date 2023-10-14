using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Forms;
using System.Windows.Controls;

namespace Vif
{
    public partial class MainWindow : Window
    {
        private List<string> imageFilenames;
        private string currentFilename;

        private DispatcherTimer imageSwitchTimer;
        private Storyboard imageFadeInStoryboard;
        private Storyboard imageFadeOutStoryboard;

        private DoubleAnimation windowFadeOut;
        private DoubleAnimation windowFadeIn;

        private Random random;

        public MainWindow()
        {
            InitializeComponent();

            // initialize globals

            imageFilenames = new List<string>();
            imageSwitchTimer = new DispatcherTimer();
            random = new Random();

            // add event handlers

            StateChanged += MainWindow_StateChanged;

            MouseEnter += MainWindow_MouseEnter;
            MouseLeave += MainWindow_MouseLeave;
            MouseDown += MainWindow_MouseDown;

            Closed += (sender, e) =>
            {
                MainWindow_Close();
            };

            // set window Fade in and Fade out animations

            windowFadeIn = new DoubleAnimation(0.5, TimeSpan.FromSeconds(0.3));
            windowFadeOut = new DoubleAnimation(0.05, TimeSpan.FromSeconds(0.3));

            // set image Fade in and Fade out animations

            DoubleAnimation imageFadeInAnimation = new DoubleAnimation(1.0, TimeSpan.FromSeconds(1));

            imageFadeInStoryboard = new Storyboard();
            imageFadeInStoryboard.Children.Add(imageFadeInAnimation);

            Storyboard.SetTarget(imageFadeInAnimation, CurrentImage);
            Storyboard.SetTargetProperty(imageFadeInAnimation, new PropertyPath("Opacity"));

            DoubleAnimation imageFadeOutAnimation = new DoubleAnimation(0.0, TimeSpan.FromSeconds(1));

            imageFadeOutStoryboard = new Storyboard();
            imageFadeOutStoryboard.Children.Add(imageFadeOutAnimation);

            Storyboard.SetTarget(imageFadeOutAnimation, CurrentImage);
            Storyboard.SetTargetProperty(imageFadeOutAnimation, new PropertyPath("Opacity"));

            imageFadeOutStoryboard.Completed += (sender, e) =>
            {
                if (currentFilename != null)
                {
                    CurrentImage.Source = new BitmapImage(new Uri(currentFilename));

                    imageFadeInStoryboard.Begin();
                }

                else
                {
                    imageFilenames.Clear();
                    SwitchToHomeScreen();
                }
            };

            // add a timer to switch images

            imageSwitchTimer.Interval = TimeSpan.FromSeconds(IntervalSlider.Value);
            imageSwitchTimer.Tick += ImageSwitchTimer_Tick;
            imageSwitchTimer.Start();
        }

        private void MainWindow_Close()
        {
            // cleanup

            imageSwitchTimer.Stop();
            imageFilenames.Clear();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Maximized:
                    MaximizeButtonText.Text = "\uE1D8";
                    break;

                case WindowState.Normal:
                    MaximizeButtonText.Text = "\uE1D9";
                    break;
            }
        }

        private void MainWindow_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            BackgroundBrush.BeginAnimation(
                SolidColorBrush.OpacityProperty,
                windowFadeIn
            );

            Controls.Visibility = Visibility.Visible;
        }

        private void MainWindow_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (CurrentImage.Visibility == Visibility.Visible)
            {
                BackgroundBrush.BeginAnimation(
                    SolidColorBrush.OpacityProperty,
                    windowFadeOut
                );

                Controls.Visibility = Visibility.Collapsed;
            }
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void SwitchToHomeScreen()
        {
            HomeText.Visibility = Visibility.Visible;
            CurrentImage.Visibility = Visibility.Collapsed;

            imageSwitchTimer.Stop();
        }

        private void SwitchToImageScreen()
        {
            HomeText.Visibility = Visibility.Collapsed;
            CurrentImage.Visibility = Visibility.Visible;

            BackgroundBrush.BeginAnimation(
                SolidColorBrush.OpacityProperty,
                windowFadeOut
            );

            Controls.Visibility = Visibility.Collapsed;

            imageSwitchTimer.Start();
        }

        private string PickDirectory()
        {
            string selectedDirectory = null;

            using (
                var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Select a directory with images"
                }
            )
            {
                System.Windows.Forms.DialogResult result = folderBrowserDialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    selectedDirectory = folderBrowserDialog.SelectedPath;
                }
            }

            return selectedDirectory;
        }

        private bool IsSupportedImageFormat(string filename)
        {
            string extension = System.IO.Path.GetExtension(filename).ToLower();

            return (
                extension.EndsWith(".tiff") ||
                extension.EndsWith(".jpeg") ||
                extension.EndsWith(".jpg") ||
                extension.EndsWith(".png") ||
                extension.EndsWith(".bmp")
            );
        }

        private void LoadImages(string directory)
        {
            imageFilenames.Clear();

            string[] files = System.IO.Directory.GetFiles(directory);
            foreach (string file in files)
            {
                if (IsSupportedImageFormat(file))
                {
                    imageFilenames.Add(file);
                }
            }

            if (imageFilenames.Count > 0)
            {
                System.Windows.MessageBox.Show(
                    directory + " contains " + imageFilenames.Count + " supported images.",
                    "Images found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                ImageSwitchTimer_Tick(null, null);
                SwitchToImageScreen();
            }

            else
            {
                System.Windows.MessageBox.Show(
                    directory + " contains no supported images.",
                    "Images not found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                SwitchToHomeScreen();
            }
        }

        private void ImageSwitchTimer_Tick(object sender, EventArgs e)
        {
            if (imageFilenames.Count > 0)
            {
                int randomIndex = random.Next(0, imageFilenames.Count);
                currentFilename = imageFilenames[randomIndex];

                imageFadeOutStoryboard.Begin();
            }

            else currentFilename = null;
        }

        private void DirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedDirectory = PickDirectory();
            if (selectedDirectory != null) LoadImages(selectedDirectory);
        }

        private void OnTopButton_Click(object sender, RoutedEventArgs e)
        {
            if (Topmost)
            {
                Topmost = false;
                OnTopButtonText.Text = "\uE141";
            }

            else
            {
                Topmost = true;
                OnTopButtonText.Text = "\uE196";
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeButtonText.Text = "\uE1D9";
            }

            else
            {
                WindowState = WindowState.Maximized;
                MaximizeButtonText.Text = "\uE1D8";
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ImageOriginalModeButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentImage.Stretch = Stretch.None;
        }

        private void ImageStretchModeButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentImage.Stretch = Stretch.Fill;
        }

        private void ImageFitModeButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentImage.Stretch = Stretch.Uniform;
        }

        private void ImageCoverModeButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentImage.Stretch = Stretch.UniformToFill;
        }

        private void ImageHorizontalLeftAlignmentButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentImage.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
        }

        private void ImageHorizontalCenterAlignmentButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentImage.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        }

        private void ImageHorizontalRightAlignmentButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentImage.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
        }

        private void ImageVerticalTopAlignmentButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentImage.VerticalAlignment = System.Windows.VerticalAlignment.Top;
        }

        private void ImageVerticalCenterAlignmentButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentImage.VerticalAlignment = System.Windows.VerticalAlignment.Center;
        }

        private void ImageVerticalBottomAlignmentButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentImage.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
        }

        private void IntervalIncreaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (IntervalSlider.Value >= 600) return;
            IntervalSlider.Value++;
        }

        private void IntervalDecreaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (IntervalSlider.Value <= 2) return;
            IntervalSlider.Value--;
        }

        private void IntervalSliderValueChanged(object sender, RoutedEventArgs e)
        {
            if (imageSwitchTimer != null)
            {
                imageSwitchTimer.Interval = TimeSpan.FromSeconds(IntervalSlider.Value);
            }
        }
    }
}
