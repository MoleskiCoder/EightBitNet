namespace Gaming
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    public class ScopedHandle(Action<IntPtr> deleter) : SafeHandle(IntPtr.Zero, true)
    {
        private readonly Action<IntPtr> _deleter = deleter ?? throw new ArgumentNullException(nameof(deleter));

        public override bool IsInvalid => this.handle == IntPtr.Zero;

        public IntPtr Handle
        {
            get => this.handle;
            set => this.handle = value;
        }

        protected override bool ReleaseHandle()
        {
            if (!this.IsInvalid)
                this._deleter(this.handle);
            return true;
        }

        public static nint FromScopedHandle(ScopedHandle h)
        {
            Debug.Assert(h is not null);
            return h.DangerousGetHandle();
        }

        public static implicit operator IntPtr(ScopedHandle h) => FromScopedHandle(h);
    }
}
