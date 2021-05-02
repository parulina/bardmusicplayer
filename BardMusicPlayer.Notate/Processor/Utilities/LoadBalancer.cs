﻿/*
 * Copyright(c) 2021 MoogleTroupe, 2018-2020 parulina
 * Licensed under the GPL v3 license. See https://github.com/BardMusicPlayer/BardMusicPlayer/blob/develop/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using BardMusicPlayer.Common;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Interaction;

namespace BardMusicPlayer.Notate.Processor.Utilities
{
    internal sealed class LoadBalancer : IDisposable
    {
        private readonly Stack<BardVoice> _freeVoices;
        private readonly Stack<BardVoice> _activeVoices;
        private readonly BardVoice[,,] _registry;
        
        internal LoadBalancer(int voiceCount)
        {
            var voicePool = new BardVoice[voiceCount];
            for (var x = 0; x < voicePool.Length; x++) voicePool[x] = new BardVoice(x);
            _freeVoices = new Stack<BardVoice>(voicePool.Reverse());
            _activeVoices = new Stack<BardVoice>();
            _registry = new BardVoice[16, 128, 128];
        }

        internal (int, Note) NotifyNoteOn(long time, int channel, int note, int velocity)
        {
            BardVoice voice;
            if (_freeVoices.Count > 0)
            {
                voice = _freeVoices.Pop();
                voice.Start(time, channel, note, velocity);
                _registry[channel, note, velocity] = voice;
                _activeVoices.Push(voice);
                return (-1, null);
            }

            voice = _activeVoices.Pop();
            var (stoppedBard, stoppedNote) = voice.Stop(time);
            _registry[voice.Channel, voice.Note, voice.Velocity] = null;

            voice.Start(time, channel, note, velocity);
            _registry[channel, note, velocity] = voice;
            _activeVoices.Push(voice);

            return (stoppedBard, stoppedNote);
        }

        internal (int, Note) NotifyNoteOff(long time, int channel, int note, int velocity)
        {
            if (_registry[channel, note, velocity] == null) return (-1, null);
            var voice = _registry[channel, note, velocity];
            _registry[channel, note, velocity] = null;
            var (stoppedBard, stoppedNote) = voice.Stop(time);
            _activeVoices.Remove(voice);
            _freeVoices.Push(voice);
            return (stoppedBard, stoppedNote);
        }

        public void Dispose()
        {
            _freeVoices.Clear();
            _activeVoices.Clear();
        }

        internal class BardVoice
        {
            internal int BardNumber { get; }
            internal int Channel { get; private set; }
            internal int Note { get; private set; }
            internal int Velocity { get; private set; }
            internal long Time { get; private set; }
            internal BardVoice(int bardNumber)
            {
                BardNumber = bardNumber;
            }
            internal void Start(long startTime, int startChannel, int startNote, int startVelocity)
            {
                Channel = startChannel;
                Note = startNote;
                Velocity = startVelocity;
                Time = startTime;
            }
            internal (int,Note) Stop(long stopTime) => (BardNumber, new Note((SevenBitNumber) Note, stopTime - Time <= 0 ? 1 : stopTime - Time, Time));
        }
    }
}