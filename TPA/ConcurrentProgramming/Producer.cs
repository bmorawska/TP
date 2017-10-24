﻿
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Threading.Tasks;

namespace TPA.AsynchronousBehavior.ConcurrentProgramming
{
  public class Producer<T> : IDisposable
  {

    public Producer(IProductFactory<T> factory)
    {

      m_CriticalSection = new CriticalSection<T>(factory);
      m_Timer = new Timer(x => m_CriticalSection.GenerateChar(), null, 0, 200);
    }

    #region Task-based Asynchronous Pattern (TAP)
    public async Task<T> TPAReadKeyFromKeyboardBufferAsync()
    {
      return await Task<T>.Run(() => m_CriticalSection.ReadKeyFromKeyboardBuffer());
    }
    #endregion

    #region Event-based Asynchronous Pattern (EAP)
    public event EventHandler<InstanceCreationCompletedEventArgs<T>> ReadKeyFromKeyboardBufferCompleted;
    public void EAPReadKeyFromKeyboardBufferAsync()
    {
      ThreadPool.QueueUserWorkItem(x =>
          {
            T _char = m_CriticalSection.ReadKeyFromKeyboardBuffer();
            ReadKeyFromKeyboardBufferCompleted?.Invoke(this, new InstanceCreationCompletedEventArgs<T>(_char));
          }
      );
    }
    #endregion

    #region Asynchronous Programming Model  Pattern (APM)
    public IAsyncResult BeginReadKeyFromKeyboardBuffer(AsyncCallback callback, object parameter)
    {
      m_Caller = m_CriticalSection.ReadKeyFromKeyboardBuffer;
      return m_Caller.BeginInvoke(callback, parameter);
    }
    public T EndReadKeyFromKeyboardBuffer(IAsyncResult result)
    {
      return m_Caller.EndInvoke(result);
    }
    #endregion

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls
    protected virtual void Dispose(bool disposing)
    {
      if (disposedValue)
        return;
      if (disposing)
      {
        m_Timer.Dispose();
        m_CriticalSection.Dispose();
      }
      disposedValue = true;
    }
    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
    }
    #endregion

    #region private
    private delegate T AsyncMethodCaller();
    private Timer m_Timer;
    private AsyncMethodCaller m_Caller;
    private CriticalSection<T> m_CriticalSection;

    [Synchronization(true)]
    class CriticalSection<T> : IDisposable
    {
      public CriticalSection(IProductFactory<T> factory)
      {
        if (factory == null)
          throw new ArgumentNullException(nameof(factory));
        m_Factory = factory;
      }
      internal void GenerateChar()
      {
        m_charBuffer.Enqueue(m_Factory.Create());
        if (m_charBuffer.Count == 1)
          m_AutoResetEvent.Set();
      }
      internal T ReadKeyFromKeyboardBuffer()
      {
        if (m_charBuffer.Count == 0)
          m_AutoResetEvent.WaitOne(-1, true);
        else if (m_charBuffer.Count == 1)
          m_AutoResetEvent.Reset();
        Debug.Assert(m_charBuffer.Any<T>());
        return m_charBuffer.Dequeue();
      }

      public void Dispose()
      {
        m_AutoResetEvent.Dispose();
      }
      private IProductFactory<T> m_Factory;
      private readonly Queue<T> m_charBuffer = new Queue<T>();
      private readonly AutoResetEvent m_AutoResetEvent = new AutoResetEvent(false);
    }

    #endregion

  }
}