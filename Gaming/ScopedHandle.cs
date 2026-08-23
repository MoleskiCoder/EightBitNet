namespace Gaming
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    public class ScopedHandle(Action<IntPtr> deleter) : SafeHandle(IntPtr.Zero, true)
    {
        private readonly Action<IntPtr> _deleter = deleter ?? throw new ArgumentNullException(nameof(deleter));

        public override bool IsInvalid => this.handle == IntPtr.Zero;
        public bool IsValid => !this.IsInvalid;

        public IntPtr Handle
        {
            get => this.handle;
            set => this.handle = value;
        }

        protected override bool ReleaseHandle()
        {
            if (this.IsValid)
                this._deleter(this.handle);
            return true;
        }

        public static nint FromScopedHandle(ScopedHandle h)
        {
            // But it can still be invalid (i.e. h.IsInvalid == true)
            Debug.Assert(h is not null);
            return h.DangerousGetHandle();
        }

        public static implicit operator IntPtr(ScopedHandle h) => FromScopedHandle(h);
    }
}
