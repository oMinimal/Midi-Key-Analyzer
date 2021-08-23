using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;

namespace Midi_Key_Analyzer
{
    public class Track
    {
        public int TrackID { get; set; }
        public string Content { get; set; }

        public Track(int conID, string conContent)
        {
            TrackID = conID;
            Content = conContent;
        }
    }

    public partial class MainWindow : Window
    {
        private readonly string playMsg = "✨ Playing ✨";
        private readonly string pauseMsg = "Paused";
        private readonly string stopMsg = "Stopped";
        private bool zoomedOut = true;
        private OutputDevice CurrentOutputDevice { get; set; } = OutputDevice.GetById(0);
        private Playback CurrentPlayback { get; set; }
        private MidiFile CurrentMidiFile { get; set; }
        private TempoMap CurrentTempoMap { get; set; }
        private List<TrackChunk> AllTrackChunks { get; set; } = new List<TrackChunk>();
        private List<TrackChunk> CurrentTrackChunks { get; set; } = new List<TrackChunk>();
        private List<ScatterPoint> CurrentPattern { get; set; } = new List<ScatterPoint>();
        private List<Note> CurrentNotes { get; set; } = new List<Note>();
        private List<bool> CurrentCheckboxes { get; set; } = new List<bool>();
        private List<Track> TracksList { get; set; } = new List<Track>();

        public MainWindow()
        {
            Application.Current.MainWindow.Closing += new CancelEventHandler(MainWindow_Closing);

            InitializeComponent();
        }

        //DockPanel:
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (CurrentPlayback != null)
            {
                CurrentPlayback.Dispose();
                CurrentOutputDevice.Dispose();
            }
        }
        private void BtnImportMidi_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Midi files (*.mid)|*.mid",
                InitialDirectory = @"c:\temp\"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    //Initializes the MidiFile currentMidiFile:
                    CurrentMidiFile = MidiFile.Read(openFileDialog.FileName);

                    //Stops the last playback
                    if (CurrentPlayback != null)
                    {
                        CurrentPlayback.Stop();
                        CurrentOutputDevice.TurnAllNotesOff();
                        txtBInfo.Text = stopMsg;
                        CurrentPlayback.Dispose();
                    }

                    //Initializes the Playback CurrentPlayback:
                    CurrentPlayback = CurrentMidiFile.GetPlayback(CurrentOutputDevice);

                    //Initializes the TempoMap tempoMap:
                    CurrentTempoMap = CurrentMidiFile.GetTempoMap();

                    //Clears the current Pattern:
                    CurrentPattern.Clear();
                    
                    pPattern.InvalidatePlot(true);

                    //Filters out non-MTrk chunks:
                    AllTrackChunks.Clear();
                    CurrentTrackChunks.Clear();
                    foreach (TrackChunk chunk in CurrentMidiFile.GetTrackChunks())
                    {
                        if (chunk.GetNotes().Count() > 0)
                        {
                            AllTrackChunks.Add(chunk);
                            CurrentTrackChunks.Add(chunk);
                        }
                    }

                    //Manages the Track selection and visualization:
                    TracksList.Clear();
                    CurrentCheckboxes.Clear();
                    for (int i = 0; i < AllTrackChunks.Count(); i++)
                    {
                        TracksList.Add(new Track(i, $"Track {i + 1} (Channel: {AllTrackChunks[i].GetChannels().First() + 1})"));
                        CurrentCheckboxes.Add(true);
                    }
                    CollectionViewSource.GetDefaultView(TracksList).Refresh();
                    lBITracks.ItemsSource = TracksList;

                    CurrentNotes = AllTrackChunks[0].GetNotes().ToList();
                    RenderNotes(zoomedOut);

                    //Changes the file name on display:
                    txtBMidiFileName.Text = openFileDialog.SafeFileName;

                    //Analyzes the midi:
                    AnalyzeMidi();
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void MiTutorial_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"This program analyzes the key of a midi. {Environment.NewLine}Import a midi file by clicking 'File' in the dockpanel and selecting 'Import Midi'. {Environment.NewLine}Disable obstructing tracks like percussion to get a more accurate result. {Environment.NewLine}You can see the pattern of a track by clicking on it. {Environment.NewLine}Furthermore, you can play the midi with only the currently enabled tracks. {Environment.NewLine}Click 'Refresh' to update the midi player and the key analyzer.", "How do I use this?", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void Mi1_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Try playing it with Windows Media Player. If you can't hear it there then you won't hear it here. {Environment.NewLine}{Environment.NewLine}...and if it does work in WMA and not here then sorry about that.", "Why can't I hear the midi play?", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void Mi2_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("I'm not sure myself. It looks like the analyzation process fails for certain midi files.", "Why are some midis not supported?", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void Mi3_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Because certain events like instrumental changes or panning aren't read properly when playing from a different starting position.", "Why does the midi sound weird after I skip to a new position while it's playing?", MessageBoxButton.OK, MessageBoxImage.Information);
        }   
        private void Mi4_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(":(", "Nothing is working!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
        private void MiAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Midi Key Analyzer v1.1.0 { Environment.NewLine}{ Environment.NewLine}Author: Marf41 { Environment.NewLine}Twitter: @MarF_41 { Environment.NewLine}YouTube: youtube.com/c/MarF41HQ { Environment.NewLine}GitHub: https://github.com/Marf2019/Midi-Key-Analyzer { Environment.NewLine}{ Environment.NewLine}I hope you enjoy the program!", "About Midi Key Analyzer");
        }

        //Buttons:
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            MidiTimeSpan CurrentTime = new MidiTimeSpan(0);

            //Stops the last playback
            if (CurrentPlayback != null)
            {
                CurrentTime = CurrentPlayback.GetCurrentTime<MidiTimeSpan>();
                CurrentPlayback.Stop();
                CurrentOutputDevice.TurnAllNotesOff();
                txtBInfo.Text = "Refreshed";
                CurrentPlayback.Dispose();
            }

            //Updated the current playback
            CurrentTrackChunks.Clear();
            for (int i = 0; i < AllTrackChunks.Count(); i++)
            {
                if (CurrentCheckboxes[i] == true)
                {
                    CurrentTrackChunks.Add(AllTrackChunks[i]);
                }
            }
            CurrentPlayback = PlaybackUtilities.GetPlayback(CurrentTrackChunks, CurrentTempoMap, CurrentOutputDevice);
            CurrentPlayback.MoveToTime(CurrentTime);

            AnalyzeMidi();
        }
        private void RibbonCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var box = sender as RibbonCheckBox;
            CurrentCheckboxes[(int)box.Tag] = Convert.ToBoolean(box.IsChecked);
        }
        private void LBITracks_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ItemsControl.ContainerFromElement(lBITracks, e.OriginalSource as DependencyObject) is ListBoxItem item)
            {
                CurrentNotes = AllTrackChunks[lBITracks.ItemContainerGenerator.IndexFromContainer(item)].GetNotes().ToList();

                RenderNotes(zoomedOut);
            }
        }
        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPlayback != null && !CurrentPlayback.IsRunning && CurrentTrackChunks.Count() != 0)
            {
                PlayMidi();
                txtBInfo.Text = playMsg;
                Duration();
            }
        }
        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPlayback != null && CurrentPlayback.IsRunning && CurrentTrackChunks.Count() != 0)
            {
                CurrentPlayback.Stop();
                CurrentOutputDevice.TurnAllNotesOff();

                txtBInfo.Text = pauseMsg;
            }
            else if (CurrentPlayback != null && !CurrentPlayback.IsRunning && txtBInfo.Text != stopMsg)
            {
                PlayMidi();
                txtBInfo.Text = playMsg;
                Duration();
            }
        }
        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPlayback != null)
            {
                CurrentPlayback.Stop();
                CurrentOutputDevice.TurnAllNotesOff();
                CurrentPlayback.MoveToStart();
                txtBInfo.Text = stopMsg;
            }
        }
        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentNotes.Count != 0)
            {
                zoomedOut = false;
                RenderNotes(zoomedOut);
            }
        }
        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentNotes.Count != 0)
            {
                zoomedOut = true;
                RenderNotes(zoomedOut);
            }
        }
        private void SldVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CurrentOutputDevice != null)
            {
                CurrentOutputDevice.Volume = new Volume((ushort)(65535 * sldVolume.Value));
            }
        }
        private void SldTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CurrentOutputDevice != null && CurrentPlayback != null && CurrentPlayback.GetDuration<MidiTimeSpan>().TimeSpan != 0)
            {
                CurrentPlayback.MoveToTime(new MidiTimeSpan((long)(sldTime.Value / 100.0 * CurrentPlayback.GetDuration<MidiTimeSpan>().TimeSpan)));
            }
        }

        //Internal Methods:
        private void AnalyzeMidi()
        {
            if (CurrentTrackChunks.Count() == 0)
            {
                txtBKeyOne.Text = "No tracks selected.";
                txtBKeyTwo.Text = "";
                txtBKeyThree.Text = "";
                return;
            }

            //Filters the notes of the selected tracks:
            double[] ranking = new double[12];
            for (int i = 0; i < CurrentTrackChunks.Count(); i++)
            {
                IEnumerable<Note> notes = CurrentTrackChunks[i].GetNotes();
                foreach (Note note in notes)
                {
                    int n = note.NoteNumber % 12;
                    ranking[n]++;
                }
            }

            //Normalizes the midi array/vector:
            double length = 0;
            foreach (double value in ranking)
            {
                length += Math.Pow(value, 2);
            }
            length = Math.Sqrt(length);
            for (int i = 0; i < 12; i++)
            {
                ranking[i] /= length;
            }

            //Creates the key scales arrays/vectors:
            double[][] keyScales = new double[24][];
            string[] keyTable = new string[24] { "C/Am", "C#/A#m", "D/Bm", "Eb/Cm", "E/C#m", "F/Dm", "Gb/Ebm", "G/Em", "G#/Fm", "A/F#m", "Bb/Gm", "B/G#m", "Cm/Eb", "C#m/E", "Dm/F", "Ebm/Gb", "Em/G", "Fm/Ab", "F#m/A", "Gm/Bb", "Abm/Cb", "Am/C", "Bbm/Db", "Bm/D" };
            //Major Scale                       c, c#,d, d#,e, f, f#,g, g#,a, a#,b,rank
            double[] c = new double[13] { 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 0 };
            keyScales[0] = c;
            double[] cSharp = new double[13] { 1, 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 0, 0 };
            keyScales[1] = cSharp;
            double[] d = new double[13] { 0, 1, 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 0 };
            keyScales[2] = d;
            double[] dSharp = new double[13] { 1, 0, 1, 1, 0, 1, 0, 1, 1, 0, 1, 0, 0 };
            keyScales[3] = dSharp;
            double[] e = new double[13] { 0, 1, 0, 1, 1, 0, 1, 0, 1, 1, 0, 1, 0 };
            keyScales[4] = e;
            double[] f = new double[13] { 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 1, 0, 0 };
            keyScales[5] = f;
            double[] fSharp = new double[13] { 0, 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 1, 0 };
            keyScales[6] = fSharp;
            double[] g = new double[13] { 1, 0, 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 0 };
            keyScales[7] = g;
            double[] gSharp = new double[13] { 1, 1, 0, 1, 0, 1, 0, 1, 1, 0, 1, 0, 0 };
            keyScales[8] = gSharp;
            double[] a = new double[13] { 0, 1, 1, 0, 1, 0, 1, 0, 1, 1, 0, 1, 0 };
            keyScales[9] = a;
            double[] aSharp = new double[13] { 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 1, 0, 0 };
            keyScales[10] = aSharp;
            double[] b = new double[13] { 0, 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 1, 0 };
            keyScales[11] = b;
            //Minor Scale                       c, c#,d, d#,e, f, f#,g, g#,a, a#,b,rank
            double[] cM = new double[13] { 1, 0, 1, 1, 0, 1, 0, 1, 1, 0, 1, 0, 0 };
            keyScales[12] = cM;
            double[] cSharpM = new double[13] { 0, 1, 0, 1, 1, 0, 1, 0, 1, 1, 0, 1, 0 };
            keyScales[13] = cSharpM;
            double[] dM = new double[13] { 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 1, 0, 0 };
            keyScales[14] = dM;
            double[] dSharpM = new double[13] { 0, 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 1, 0 };
            keyScales[15] = dSharpM;
            double[] eM = new double[13] { 1, 0, 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 0 };
            keyScales[16] = eM;
            double[] fM = new double[13] { 1, 1, 0, 1, 0, 1, 0, 1, 1, 0, 1, 0, 0 };
            keyScales[17] = fM;
            double[] fSharpM = new double[13] { 0, 1, 1, 0, 1, 0, 1, 0, 1, 1, 0, 1, 0 };
            keyScales[18] = fSharpM;
            double[] gM = new double[13] { 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 1, 0, 0 };
            keyScales[19] = gM;
            double[] gSharpM = new double[13] { 0, 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 1, 0 };
            keyScales[20] = gSharpM;
            double[] aM = new double[13] { 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 0 };
            keyScales[21] = aM;
            double[] aSharpM = new double[13] { 1, 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 0, 0 };
            keyScales[22] = aSharpM;
            double[] bM = new double[13] { 0, 1, 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 0 };
            keyScales[23] = bM;

            //Calculates a score for each scale based on its dotproduct with the midi array/vector:
            static double DotProduct(double[] a, double[] b)
            {
                double dot = 0;
                for (int i = 0; i < 12; i++)
                {
                    dot += a[i] * (b[i] / Math.Sqrt(7));
                }
                return dot;
            }
            double[] results = new double[24];
            for (int i = 0; i < 24; i++)
            {
                keyScales[i][12] = results[i] = DotProduct(ranking, keyScales[i]);
            }

            //Calculates the result:
            Array.Sort(results);
            for (int i = 0; i < 24; i++)
            {
                if (keyScales[i][12] == results[23])
                {
                    txtBKeyOne.Text = "1. The key is " + keyTable[i] + " with an accuracy of " + Math.Round(DotProduct(ranking, keyScales[i]) * 100, 1) + "%.";
                    break;
                }
            }
            for (int i = 0; i < 24; i++)
            {
                if (keyScales[i][12] == results[21])
                {
                    txtBKeyTwo.Text = "2. The key is " + keyTable[i] + " with an accuracy of " + Math.Round(DotProduct(ranking, keyScales[i]) * 100, 1) + "%.";
                    break;
                }
            }
            for (int i = 0; i < 24; i++)
            {
                if (keyScales[i][12] == results[19])
                {
                    txtBKeyThree.Text = "3. The key is " + keyTable[i] + " with an accuracy of " + Math.Round(DotProduct(ranking, keyScales[i]) * 100, 1) + "%.";
                    break;
                }
            }
        }
        async private void PlayMidi()
        {
            await Task.Run(CurrentPlayback.Play);
        }
        async private void Duration()
        {
            while (txtBInfo.Text == playMsg && CurrentPlayback.GetDuration<MidiTimeSpan>().TimeSpan != 0)
            {
                sldTime.Value = 100 * CurrentPlayback.GetCurrentTime<MidiTimeSpan>().TimeSpan / CurrentPlayback.GetDuration<MidiTimeSpan>().TimeSpan;
                await Task.Delay(TimeSpan.FromMilliseconds(10));
            }
        }
        private void RenderNotes(bool zOut)
        {
            double noteSize = 1;

            CurrentPattern.Clear();

            if (zOut)
            {
                CurrentPattern.Add(new ScatterPoint(0, 0, 0, 0));
                noteSize = 0.5;
            }

            foreach (Note note in CurrentNotes)
            {
                CurrentPattern.Add(new ScatterPoint(note.Time, note.NoteNumber, noteSize, 500));
            
            }

            if (zOut)
            {
                CurrentPattern.Add(new ScatterPoint(CurrentMidiFile.GetDuration<MidiTimeSpan>(), 120, 0, 0));
            }
            else
            {

            }

            ssPattern.ItemsSource = CurrentPattern;
            pPattern.InvalidatePlot(true);
        }
    }
}
