using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace Sanford.Multimedia.Midi
{
    public class Sequencer : IComponent//定义Sequencer类
    {
        private Sequence sequence = null;

        private List<IEnumerator<int>> enumerators = new List<IEnumerator<int>>();

        private MessageDispatcher dispatcher = new MessageDispatcher();

        private ChannelChaser chaser = new ChannelChaser();

        private ChannelStopper stopper = new ChannelStopper();

        private MidiInternalClock clock = new MidiInternalClock();

        private int tracksPlayingCount;

        private readonly object lockObject = new object();

        private bool playing = false;

        private bool disposed = false;

        private ISite site = null;

        #region Events

        public event EventHandler PlayingCompleted;//播放完成的委托

        public event EventHandler<ChannelMessageEventArgs> ChannelMessagePlayed
        {
            add
            {
                dispatcher.ChannelMessageDispatched += value;
            }
            remove
            {
                dispatcher.ChannelMessageDispatched -= value;
            }
        }

        public event EventHandler<SysExMessageEventArgs> SysExMessagePlayed
        {
            add
            {
                dispatcher.SysExMessageDispatched += value;
            }
            remove
            {
                dispatcher.SysExMessageDispatched -= value;
            }
        }

        public event EventHandler<MetaMessageEventArgs> MetaMessagePlayed
        {
            add
            {
                dispatcher.MetaMessageDispatched += value;
            }
            remove
            {
                dispatcher.MetaMessageDispatched -= value;
            }
        }

        public event EventHandler<ChasedEventArgs> Chased
        {
            add
            {
                chaser.Chased += value;
            }
            remove
            {
                chaser.Chased -= value;
            }
        }

        public event EventHandler<StoppedEventArgs> Stopped
        {
            add
            {
                stopper.Stopped += value;
            }
            remove
            {
                stopper.Stopped -= value;
            }
        }

        #endregion

        public Sequencer()
        {
            dispatcher.MetaMessageDispatched += delegate (object sender, MetaMessageEventArgs e)//+=注册运算符,当发送MIDI信息时
            {
                if (e.Message.MetaType == MetaType.EndOfTrack)//e的metatype为停止的指令（0x2F）时
                {
                    tracksPlayingCount--;

                    if (tracksPlayingCount == 0)//playingcount为0时停止播放
                    {
                        Stop();

                        OnPlayingCompleted(EventArgs.Empty);
                    }
                }
                else
                {
                    clock.Process(e.Message);//开始时钟
                }
            };

            dispatcher.ChannelMessageDispatched += delegate (object sender, ChannelMessageEventArgs e)
            {
                stopper.Process(e.Message);
            };

            clock.Tick += delegate (object sender, EventArgs e)
            {
                lock (lockObject)
                {
                    if (!playing)//未播放则返回
                    {
                        return;
                    }

                    foreach (IEnumerator<int> enumerator in enumerators)
                    {
                        enumerator.MoveNext();//依次向后
                    }
                }
            };

            PlayingCompleted += delegate (object sender, EventArgs e)
            {
                //Thread thread = new Thread(replay);
            };
        }

        //public void replay()
        //{

        //}

        ~Sequencer()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (lockObject)
                {
                    Stop();

                    clock.Dispose();

                    disposed = true;

                    GC.SuppressFinalize(this);
                }
            }
        }

        public void Start()//开始按钮
        {
            #region Require

            if (disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion           

            lock (lockObject)
            {
                Stop();//停止

                Position = 0;//定位0

                Continue();//继续
            }
        }

        public void Continue()
        {
            #region Require

            if (disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            #region Guard

            if (Sequence == null)
            {
                return;
            }

            #endregion

            lock (lockObject)
            {
                Stop();//停止播放

                enumerators.Clear();//计数器清空

                foreach (Track t in Sequence)//再次加入计数器
                {
                    enumerators.Add(t.TickIterator(Position, chaser, dispatcher).GetEnumerator());
                }

                tracksPlayingCount = Sequence.Count;//给tracksplayingcount赋值为当前时间

                playing = true;
                clock.Ppqn = sequence.Division;
                clock.Continue();//继续播放
            }
        }

        public void Stop()//停止按钮
        {
            #region Require

            if (disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            lock (lockObject)
            {
                #region Guard

                if (!playing)
                {
                    return;
                }

                #endregion

                playing = false;
                clock.Stop();
                stopper.AllSoundOff();
            }
        }

        protected virtual void OnPlayingCompleted(EventArgs e)//播放完成时，委托
        {
            EventHandler handler = PlayingCompleted;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnDisposed(EventArgs e)
        {
            EventHandler handler = Disposed;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        public int Position
        {
            get
            {
                #region Require

                if (disposed)
                {
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                #endregion

                return clock.Ticks;
            }
            set
            {
                #region Require

                if (disposed)
                {
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                else if (value < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                #endregion

                bool wasPlaying;

                lock (lockObject)
                {
                    wasPlaying = playing;

                    Stop();

                    clock.SetTicks(value);
                }

                lock (lockObject)
                {
                    if (wasPlaying)
                    {
                        Continue();
                    }
                }
            }
        }

        public Sequence Sequence//序列
        {
            get
            {
                return sequence;
            }
            set
            {
                #region Require

                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                else if (value.SequenceType == SequenceType.Smpte)
                {
                    throw new NotSupportedException();
                }

                #endregion

                lock (lockObject)
                {
                    Stop();
                    sequence = value;
                }
            }
        }

        #region IComponent Members

        public event EventHandler Disposed;

        public ISite Site
        {
            get
            {
                return site;
            }
            set
            {
                site = value;
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            #region Guard

            if (disposed)
            {
                return;
            }

            #endregion

            Dispose(true);
        }

        #endregion
    }
}
