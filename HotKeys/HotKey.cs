using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Resolve.HotKeys
{
    public class HotKey : IMessageFilter, IDisposable
    {
        private bool _disposed;

        public event EventHandler Pressed;

        public Keys Key { get; }

        public ModifierKey Modifiers { get; }

        public int? Id { get; private set; }

        public IntPtr Handle { get; }

        public HotKey(Keys key) : this(key, ModifierKey.None, IntPtr.Zero)
        {
        }

        public HotKey(Keys key, ModifierKey modifiers) : this(key, modifiers, IntPtr.Zero)
        {
        }

        public HotKey(Keys key, ModifierKey modifiers, IntPtr handle) : base()
        {
            Key       = key;
            Modifiers = modifiers;
            Handle    = handle;
            _disposed = true;
        }

        public void Register()
        {
            if (Id.HasValue)
            {
                return;
            }

            NativeMethods.SetLastError(NativeMethods.ERROR_SUCCESS);
            Id = NativeMethods.GlobalAddAtom(GetHashCode().ToString());

            var error = Marshal.GetLastWin32Error();


            if (error != NativeMethods.ERROR_SUCCESS)
            {
                Id = null;
                throw new Win32Exception(error);
            }

            var vk = unchecked((uint) (Key & ~Keys.Modifiers));
            NativeMethods.SetLastError(NativeMethods.ERROR_SUCCESS);
            var result = NativeMethods.RegisterHotKey(Handle, Id.Value, (uint) Modifiers, vk);

            error = Marshal.GetLastWin32Error();

            if (error != 0)
            {
                Id = null;
                throw new Win32Exception(error);
            }

            if (result)
            {
                Application.AddMessageFilter(this);
            }
            else
            {
                Id = null;
            }
        }

        public void Unregister()
        {
            if (Id == null)
            {
                return;
            }

            NativeMethods.SetLastError(NativeMethods.ERROR_SUCCESS);
            var result = NativeMethods.UnregisterHotKey(Handle, Id.Value);
            var error  = Marshal.GetLastWin32Error();
            if (error != NativeMethods.ERROR_SUCCESS)
            {
                throw new Win32Exception(error);
            }

            NativeMethods.SetLastError(NativeMethods.ERROR_SUCCESS);
            NativeMethods.GlobalDeleteAtom(Id.Value);
            error = Marshal.GetLastWin32Error();
            if (error != NativeMethods.ERROR_SUCCESS)
            {
                throw new Win32Exception(error);
            }

            Id = null;
            Application.RemoveMessageFilter(this);
        }

        public bool PreFilterMessage(ref Message m)
        {
            switch (m.Msg)
            {
                case NativeMethods.WM_HOTKEY:
                    if (m.HWnd == Handle && m.WParam == (IntPtr) Id && Pressed != null)
                    {
                        Pressed(this, EventArgs.Empty);
                        return true;
                    }

                    break;
            }

            return false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary> 
        /// Unregister the hotkey. 
        /// </summary> 
        protected virtual void Dispose(bool disposing)
        {
            // Protect from being called multiple times. 
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Removes a message filter from the message pump of the application. 


                Unregister();
            }

            _disposed = true;
        }

        protected bool Equals(HotKey other)
        {
            return Key == other.Key && Modifiers == other.Modifiers;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HotKey) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) Key * 397) ^ (int) Modifiers;
            }
        }
    }
}