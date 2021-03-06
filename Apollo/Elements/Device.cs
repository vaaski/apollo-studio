using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

using Apollo.Core;
using Apollo.Helpers;
using Apollo.Rendering;
using Apollo.Selection;
using Apollo.Structures;
using Apollo.Viewers;

namespace Apollo.Elements {
    public abstract class Device: SignalReceiver, ISelect, IMutable {
        public readonly string DeviceIdentifier;
        public readonly string Name;

        public ISelectViewer IInfo {
            get => Viewer;
        }

        public ISelectParent IParent {
            get => Parent;
        }

        public int? IParentIndex {
            get => ParentIndex;
        }
        
        public ISelect IClone() => (ISelect)Clone();

        public DeviceViewer Viewer { get; set; }
        
        public Chain Parent;
        public int? ParentIndex;

        public bool Collapsed = false;
        
        bool _enabled = true;
        public bool Enabled {
            get => _enabled;
            set {
                if (_enabled != value) {
                    _enabled = value;

                    Viewer?.SetEnabled();
                }
            }
        }

        public abstract Device Clone();
        
        protected Device(string identifier, string name = null) {
            DeviceIdentifier = identifier;
            Name = name?? this.GetType().ToString().Split(".").Last();
        }

        bool ListeningToProjectLoaded = false;

        protected virtual void Initialized() {}

        public void Initialize() {
            if (!Disposed) {
                if (Program.Project == null) {
                    Program.ProjectLoaded += Initialize;
                    ListeningToProjectLoaded = true;
                    return;
                }

                if (Track.Get(this) != null)
                    Initialized();
            }

            if (ListeningToProjectLoaded) {
                Program.ProjectLoaded -= Initialize;
                ListeningToProjectLoaded = false;
            }
        }

        public void InvokeExit(List<Signal> n) {
            if (!(n is StopSignal) && !n.Any()) return;

            Viewer?.Indicator.Trigger(n);
            MIDIExit?.Invoke(n);
        }

        public abstract void MIDIProcess(List<Signal> n);

        public override void MIDIEnter(List<Signal> n) {
            if (Disposed) return;

            if (n is StopSignal) Stop();
            else if (Enabled) {
                MIDIProcess(n);
                return;
            }
            
            InvokeExit(n);
        }

        protected void Stop() {
            jobs.Clear();
            Stopped();
        }

        protected virtual void Stopped() {}

        ConcurrentHashSet<Action> jobs = new ConcurrentHashSet<Action>();

        protected void Schedule(Action job, double time) {
            void Job() {
                if (!jobs.Contains(Job)) return;
                jobs.Remove(Job);

                job.Invoke();
            };

            jobs.Add(Job);
            Heaven.Schedule(Job, time);
        }

        public bool Disposed { get; private set; } = false;

        public virtual void Dispose() {
            if (Disposed) return;

            MIDIExit = null;
            Viewer = null;
            Parent = null;
            ParentIndex = null;
            
            Disposed = true;
        }

        public static Device Create(Type device, Chain parent) {
            object obj = FormatterServices.GetUninitializedObject(device);
            device.GetField("Parent").SetValue(obj, parent);

            ConstructorInfo ctor = device.GetConstructors()[0];
            ctor.Invoke(
                obj,
                BindingFlags.OptionalParamBinding,
                null, Enumerable.Repeat(Type.Missing, ctor.GetParameters().Count()).ToArray(),
                CultureInfo.CurrentCulture
            );
            
            return (Device)obj;
        }
    }
}