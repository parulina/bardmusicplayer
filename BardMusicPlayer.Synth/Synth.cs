﻿using System;
using System.Collections;
using System.Globalization;
using System.Threading.Tasks;
using BardMusicPlayer.Common;
using BardMusicPlayer.Notate.Song;
using BardMusicPlayer.Synth.AlphaTab.Audio.Synth;
using BardMusicPlayer.Synth.AlphaTab.CSharp.Platform.CSharp;
using BardMusicPlayer.Synth.Properties;
using NAudio.CoreAudioApi;

namespace BardMusicPlayer.Synth
{
    public class Synthesizer
    {
        private BmpSong _song;
        private IAlphaSynth _player;

        private static readonly Lazy<Synthesizer> LazyInstance = new(() => new Synthesizer());
        public static Synthesizer Instance => LazyInstance.Value;

        internal Synthesizer()
        {
            // TODO: seperate vsts into a global store and load them here so they aren't reloaded every time setup is called.
        }

        ~Synthesizer()
        {
            ShutDown();
            // TODO: unload global vsts
        }

        /// <summary>
        /// Gets a collection of available MMDevice objects
        /// </summary>
        public MMDeviceCollection AudioDevices => new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="device"></param>
        /// <param name="bufferCount"></param>
        /// <param name="latency"></param>
        public void Setup(MMDevice device, byte bufferCount = 3, byte latency = 100)
        {
            ShutDown();
            _player = new ManagedThreadAlphaSynthWorkerApi(new NAudioSynthOutput(device, bufferCount, latency), AlphaTab.Util.LogLevel.None, BeginInvoke);
            foreach (var resource in Resources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true))
                _player.LoadSoundFont((byte[])((DictionaryEntry)resource).Value, true);
            _player.PositionChanged += NotifyTimePosition;
            _player.MasterVolume = 1.0f;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsReady => _player.IsReady;

        /// <summary>
        /// 
        /// </summary>
        public bool IsReadyForPlayback => _player.IsReadyForPlayback;

        private readonly TaskQueue _taskQueue = new();
        internal void BeginInvoke(Action action) => _taskQueue.Enqueue(() => Task.Run(action));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bufferCount"></param>
        /// <param name="latency"></param>
        public void Setup(byte bufferCount = 2, byte latency = 100) => Setup(
            new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia), bufferCount, latency);

        /// <summary>
        /// 
        /// </summary>
        public void ShutDown()
        {
            if (_player == null) return;
            _player.Stop();
            _player.PositionChanged -= NotifyTimePosition;
            _player.Destroy();
        }
        
        /// <summary>
        /// Loads a BmpSong into the synthesizer
        /// </summary>
        /// <param name="song"></param> 
        /// <returns>This Synthesizer</returns>
        public async Task<Synthesizer> Load(BmpSong song)
        {
            if (_player == null) throw new BmpException("Synthesizer not initialized.");
            if (_player.State == PlayerState.Playing) _player.Stop();

            _player.LoadMidiFile(await song.GetSynthMidi());
            _song = song;
            return this;
        }

        /// <summary>
        /// Starts the playback if possible
        /// </summary>
        /// <returns>This Synthesizer</returns>
        public Synthesizer Play()
        {
            if (_player == null) throw new BmpException("Synthesizer not initialized.");
            if (_song == null || !_player.IsReadyForPlayback)
                throw new BmpException(
                    "No BmpSong file loaded.");
            _player.Play();
            return this;
        }

        /// <summary>
        /// Pauses the playback if was running
        /// </summary>
        /// <returns>This Synthesizer</returns>
        public Synthesizer Pause()
        {
            if (_player == null) throw new BmpException("Synthesizer not initialized.");
            if (_song == null || !_player.IsReadyForPlayback)
                throw new BmpException(
                    "No BmpSong file loaded.");
            _player.Pause();
            return this;
        }
        
        /// <summary>
        /// Stops the playback
        /// </summary>
        /// <returns>This Synthesizer</returns>
        public Synthesizer Stop()
        {
            if (_player == null) throw new BmpException("Synthesizer not initialized.");
            if (_song == null || !_player.IsReadyForPlayback)
                throw new BmpException(
                    "No BmpSong file loaded.");
            _player.Stop();
            return this;
        }

        /// <summary>
        /// Sets the current position of this song in milliseconds
        /// </summary>
        /// <returns>This Synthesizer</returns>
        public Synthesizer SetPosition(int time)
        {
            if (_player == null) throw new BmpException("Synthesizer not initialized.");
            if (_song == null || !_player.IsReadyForPlayback)
                throw new BmpException(
                    "No BmpSong file loaded.");
            if (time < 0) time = 0;
            if (time > _player.PlaybackRange.EndTick) return Stop();
            _player.TickPosition = time;
            return this;
        }

        /// <summary>
        /// Event fired when the position of a synthesized song changes.
        /// </summary>
        /// <param name="currentTime">The current time of this song in milliseconds</param>
        /// <param name="endTime">The total length of this song in milliseconds</param>
        public delegate void SynthTimePositionHandler(double currentTime, double endTime);

        internal event SynthTimePositionHandler SynthTimePosition;

        internal void NotifyTimePosition(PositionChangedEventArgs obj) => SynthTimePosition?.Invoke(obj.CurrentTime, obj.EndTime);
    }
}
